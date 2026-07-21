using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThemedWeatherImages.Functions.Domain;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions;

public sealed class AihordeWebhookReceiver
{
    private const long MaxImageBytes = 10 * 1024 * 1024; // 10 MiB safety limit.
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IOptions<ThemedWeatherImagesOptions> _options;
    private readonly ImageHostAllowList _allowList;
    private readonly HordeRequestMappingStore _mappingStore;
    private readonly BlobServiceClient _blobServiceClient;

    public AihordeWebhookReceiver(
        IOptions<ThemedWeatherImagesOptions> options,
        ImageHostAllowList allowList,
        HordeRequestMappingStore mappingStore,
        BlobServiceClient blobServiceClient)
    {
        _options = options;
        _allowList = allowList;
        _mappingStore = mappingStore;
        _blobServiceClient = blobServiceClient;
    }

    [Function("AihordeWebhookReceiver")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "aihorde-webhook")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        CancellationToken cancellationToken = executionContext.CancellationToken;
        ILogger logger = executionContext.GetLogger("AihordeWebhookReceiver");
        logger.WebhookReceived();

        string requestBody;
        using (var reader = new StreamReader(req.Body, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync(cancellationToken);
        }

        AihordeWebhookPayload? webhookPayload = JsonSerializer.Deserialize<AihordeWebhookPayload>(requestBody, PayloadSerializerOptions);

        if (webhookPayload == null
            || string.IsNullOrWhiteSpace(webhookPayload.Id)
            || string.IsNullOrWhiteSpace(webhookPayload.Img)
            || string.IsNullOrWhiteSpace(webhookPayload.Request))
        {
            HttpResponseData badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid webhook payload", cancellationToken);
            return badResponse;
        }

        if (!_allowList.TryGetTrustedUri(webhookPayload.Img, out Uri? imageUri, out string? rejectionReason))
        {
            logger.RejectedWebhookPayload(rejectionReason, webhookPayload.Img);
            HttpResponseData invalidUriResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await invalidUriResponse.WriteStringAsync("Invalid image URI supplied.", cancellationToken);
            return invalidUriResponse;
        }

        logger.LookingUpHordeRequestId(webhookPayload.Request);
        string? resolvedFilename = await _mappingStore.GetFilenameByHordeIdAsync(webhookPayload.Request, cancellationToken);

        if (string.IsNullOrWhiteSpace(resolvedFilename))
        {
            logger.NoFilenameMappingFound(webhookPayload.Request);
            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Missing filename mapping for Horde ID.", cancellationToken);
            return errorResponse;
        }

        (byte[]? imageBytes, string? downloadError) = await TryDownloadImageAsync(imageUri, cancellationToken);
        if (imageBytes is null)
        {
            logger.UnableToRetrieveImage(imageUri, downloadError);
            HttpResponseData downloadFailed = req.CreateResponse(HttpStatusCode.BadGateway);
            await downloadFailed.WriteStringAsync("Failed to retrieve image from upstream host.", cancellationToken);
            return downloadFailed;
        }

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_options.Value.Images.BlobContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        BlobClient blobClient = containerClient.GetBlobClient(resolvedFilename);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "image/webp",
                CacheControl = "public, max-age=86400, immutable"
            }
        };

        await using var uploadStream = new MemoryStream(imageBytes, writable: false);
        await blobClient.UploadAsync(uploadStream, uploadOptions, cancellationToken);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("Webhook received and image saved.", cancellationToken);
        return response;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CheckCertificateRevocationList = true
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("ThemedWeatherImages-AI-Horde-Webhook/1.0");
        return client;
    }

    private static async Task<(byte[]? Content, string? FailureReason)> TryDownloadImageAsync(Uri imageUri, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, $"Remote host returned status {(int)response.StatusCode}.");
            }

            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > MaxImageBytes)
            {
                return (null, "Image exceeds the maximum allowed size.");
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            long totalBytes = await CopyToStreamWithLimitAsync(responseStream, buffer, MaxImageBytes, cancellationToken);
            if (totalBytes > MaxImageBytes)
            {
                return (null, "Image exceeds the maximum allowed size.");
            }

            return (buffer.ToArray(), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (null, "Operation cancelled.");
        }
        catch (HttpRequestException ex)
        {
            return (null, $"HTTP error while fetching image: {ex.Message}");
        }
    }

    private static async Task<long> CopyToStreamWithLimitAsync(Stream source, Stream destination, long maxBytes, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            int read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (totalBytes > maxBytes)
            {
                return totalBytes;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return totalBytes;
    }
}

public class AihordeWebhookPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("img")]
    public string Img { get; set; } = string.Empty;

    [JsonPropertyName("request")]
    public string Request { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
