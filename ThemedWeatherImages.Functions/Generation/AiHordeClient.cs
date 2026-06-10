using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ThemedWeatherImages.Functions.Generation;

public interface IAiHordeClient
{
    Task<AiHordeSubmissionResult> SubmitGenerationAsync(AiHordeGenerationRequest request, CancellationToken cancellationToken);
}

public sealed class AiHordeClient : IAiHordeClient
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiHordeClient> _logger;
    private readonly ThemedWeatherImageGenerationOptions _generationOptions;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _webhookUrl;

    public AiHordeClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<ThemedWeatherImagesOptions> options,
        ILogger<AiHordeClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _generationOptions = options.Value.Generation;
        _apiUrl = configuration["AI_HORDE_API_URL"] ?? throw new InvalidOperationException("AI_HORDE_API_URL is required.");
        _apiKey = configuration["AI_HORDE_API_KEY"] ?? throw new InvalidOperationException("AI_HORDE_API_KEY is required.");
        _webhookUrl = configuration["WEBHOOK_URL"] ?? throw new InvalidOperationException("WEBHOOK_URL is required.");
    }

    public async Task<AiHordeSubmissionResult> SubmitGenerationAsync(
        AiHordeGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new AiHordeGenerationPayload
        {
            Prompt = request.Prompt,
            Webhook = _webhookUrl,
            Subject = request.Subject,
            Params = new AiHordeGenerationParameters
            {
                Model = _generationOptions.Model,
                CfgScale = _generationOptions.CfgScale,
                SamplerName = _generationOptions.SamplerName,
                Width = _generationOptions.Width,
                Height = _generationOptions.Height
            },
            CensorNsfw = true
        };

        string rawPayload = JsonSerializer.Serialize(payload, CamelCaseOptions);
        _logger.LogInformation(
            "Submitting AI Horde generation for '{Category}' on {DateTag} using model {Model}, sampler {SamplerName}, size {Width}x{Height}.",
            request.Category,
            request.DateTag,
            payload.Params.Model,
            payload.Params.SamplerName,
            payload.Params.Width,
            payload.Params.Height);

        using var content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        HttpClient httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);

        using HttpResponseMessage response = await httpClient.PostAsync(_apiUrl, content, cancellationToken);
        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation(
            "Submitted generation for '{Category}' on {DateTag}: {StatusCode}",
            request.Category,
            request.DateTag,
            response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Generation submission failed: {Response}", result);
            return AiHordeSubmissionResult.Failed($"Generation submission failed: {response.StatusCode}");
        }

        using var json = JsonDocument.Parse(result);
        if (!json.RootElement.TryGetProperty("id", out JsonElement idElement))
        {
            _logger.LogWarning("Response did not include 'id' field.");
            return AiHordeSubmissionResult.Failed("Response did not include 'id' field.");
        }

        string? hordeId = idElement.GetString();
        return string.IsNullOrWhiteSpace(hordeId)
            ? AiHordeSubmissionResult.Failed("Response 'id' field was empty.")
            : AiHordeSubmissionResult.Submitted(hordeId);
    }

    private sealed class AiHordeGenerationPayload
    {
        public required string Prompt { get; init; }

        public required string Webhook { get; init; }

        public required string Subject { get; init; }

        [JsonPropertyName("params")]
        public required AiHordeGenerationParameters Params { get; init; }

        [JsonPropertyName("censor_nsfw")]
        public required bool CensorNsfw { get; init; }
    }

    private sealed class AiHordeGenerationParameters
    {
        public required string Model { get; init; }

        [JsonPropertyName("cfg_scale")]
        public required double CfgScale { get; init; }

        [JsonPropertyName("sampler_name")]
        public required string SamplerName { get; init; }

        public required int Width { get; init; }

        public required int Height { get; init; }
    }
}

public sealed record AiHordeGenerationRequest(string Prompt, string Subject, string Category, string DateTag);

public sealed record AiHordeSubmissionResult(bool IsSuccess, string? HordeId, string? FailureMessage)
{
    public static AiHordeSubmissionResult Submitted(string hordeId) => new(true, hordeId, null);

    public static AiHordeSubmissionResult Failed(string message) => new(false, null, message);
}
