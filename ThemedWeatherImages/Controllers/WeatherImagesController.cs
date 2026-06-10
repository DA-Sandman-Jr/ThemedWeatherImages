using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ThemedWeatherImages.Controllers;

[ApiController]
[Route("api/weather-images")]
public class WeatherImagesController : ControllerBase
{
    private static readonly TimeSpan SuccessfulImageCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan NotFoundCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PublicImageCacheDuration = TimeSpan.FromHours(48);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherImagesController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<ThemedWeatherImagesOptions> _options;

    public WeatherImagesController(
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherImagesController> logger,
        IMemoryCache memoryCache,
        IOptions<ThemedWeatherImagesOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _memoryCache = memoryCache;
        _options = options;
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetWeatherImage(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("File name is required.");
        }

        string sanitizedFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, sanitizedFileName, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid file name.");
        }

        string blobUri = BuildBlobUri(_options.Value.Images, sanitizedFileName, includeSasToken: true);
        string blobLogUri = BuildBlobUri(_options.Value.Images, sanitizedFileName, includeSasToken: false);

        string cacheKey = GetImageCacheKey(sanitizedFileName);
        if (_memoryCache.TryGetValue(cacheKey, out CachedImage? cachedImage) && cachedImage is not null)
        {
            ApplyCachingHeaders(cachedImage.ETag, cachedImage.LastModified);
            return File(cachedImage.Content, cachedImage.ContentType);
        }

        if (_memoryCache.TryGetValue(GetNotFoundCacheKey(sanitizedFileName), out _))
        {
            return NotFound();
        }

        try
        {
            HttpClient client = _httpClientFactory.CreateClient("ProxyClient");
            using HttpResponseMessage response = await client.GetAsync(blobUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                CacheNotFound(sanitizedFileName);
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unexpected status code {StatusCode} when retrieving {BlobUri}", response.StatusCode, blobLogUri);
                return StatusCode((int)response.StatusCode);
            }

            string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            string? etag = response.Headers.ETag?.Tag;
            DateTimeOffset? lastModified = response.Content.Headers.LastModified;

            CacheImage(sanitizedFileName, imageBytes, contentType, etag, lastModified);

            ApplyCachingHeaders(etag, lastModified);

            return File(imageBytes, contentType);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error retrieving weather image from {BlobUri}", blobLogUri);
            return StatusCode(StatusCodes.Status502BadGateway, "Error retrieving image from storage.");
        }
    }

    private void CacheImage(string fileName, byte[] content, string contentType, string? etag, DateTimeOffset? lastModified)
    {
        string cacheKey = GetImageCacheKey(fileName);
        var cachedImage = new CachedImage(content, contentType, etag, lastModified);
        _memoryCache.Set(cacheKey, cachedImage, SuccessfulImageCacheDuration);

        // A successful fetch invalidates any negative cache entry.
        _memoryCache.Remove(GetNotFoundCacheKey(fileName));
    }

    private void CacheNotFound(string fileName)
    {
        _memoryCache.Set(GetNotFoundCacheKey(fileName), true, NotFoundCacheDuration);
        _memoryCache.Remove(GetImageCacheKey(fileName));
    }

    private static string GetImageCacheKey(string fileName) => $"weather-image::{fileName}";

    private static string GetNotFoundCacheKey(string fileName) => $"weather-image-miss::{fileName}";

    private static string BuildBlobUri(ThemedWeatherImagesImageOptions options, string fileName, bool includeSasToken)
    {
        var builder = new UriBuilder($"{options.BlobBaseUrl!.TrimEnd('/')}/{Uri.EscapeDataString(fileName)}");

        if (includeSasToken && !string.IsNullOrWhiteSpace(options.BlobSasToken))
        {
            string sasToken = options.BlobSasToken.Trim();
            if (sasToken.StartsWith("?", StringComparison.Ordinal))
            {
                sasToken = sasToken[1..];
            }

            if (!string.IsNullOrWhiteSpace(sasToken))
            {
                string existingQuery = builder.Query.TrimStart('?');
                builder.Query = string.IsNullOrWhiteSpace(existingQuery)
                    ? sasToken
                    : $"{existingQuery}&{sasToken}";
            }
        }

        return builder.Uri.AbsoluteUri;
    }

    private void ApplyCachingHeaders(string? etag, DateTimeOffset? lastModified)
    {
        var cacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = PublicImageCacheDuration
        };

        cacheControl.Extensions.Add(new NameValueHeaderValue(
            "stale-while-revalidate",
            ((int)PublicImageCacheDuration.TotalSeconds).ToString()));

        ResponseHeaders headers = Response.GetTypedHeaders();
        headers.CacheControl = cacheControl;

        if (!string.IsNullOrEmpty(etag))
        {
            Response.Headers[HeaderNames.ETag] = etag;
        }

        if (lastModified.HasValue)
        {
            headers.LastModified = lastModified;
        }
    }

    private sealed record CachedImage(byte[] Content, string ContentType, string? ETag, DateTimeOffset? LastModified);
}
