using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class EndpointConnectivityTests
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    [WeatherApiKeyFact]
    public async Task WeatherApiEndpoint_IsReachableWithConfiguredKey()
    {
        string apiKey = GetWeatherApiKey();
        string requestUri = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q=New%20York";

        using HttpClient client = CreateHttpClient();
        using HttpResponseMessage response = await client.GetAsync(requestUri);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"WeatherAPI responded with {(int)response.StatusCode}: {body}");
        Assert.Contains("\"location\"", body);
    }

    [ManualTriggerUrlFact]
    public async Task ManualTriggerEndpoint_AcceptsRequestsWhenConfigured()
    {
        string? manualTriggerUrl = Environment.GetEnvironmentVariable("WEATHER_IMAGES__GENERATION__MANUALTRIGGERURL");
        string? functionKey = Environment.GetEnvironmentVariable("WEATHER_IMAGES__GENERATION__FUNCTIONKEY");

        if (string.IsNullOrWhiteSpace(manualTriggerUrl))
        {
            throw new InvalidOperationException("Manual trigger URL is not configured.");
        }

        string subjectSlug = Environment.GetEnvironmentVariable("WEATHER_IMAGES__THEME__SUBJECTSLUG") ?? "squirrel";
        string resolvedUrl = manualTriggerUrl
            .Replace("{subject}", subjectSlug, StringComparison.OrdinalIgnoreCase)
            .Replace("{animal}", subjectSlug, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(functionKey))
        {
            resolvedUrl = AppendQuery(resolvedUrl, "code", functionKey);
        }

        var payload = new
        {
            hour = 0,
            force = false,
            subject = subjectSlug,
            dates = new[] { DateTime.UtcNow.ToString("yyyy-MM-dd") }
        };

        using HttpClient client = CreateHttpClient();
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(resolvedUrl, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"Manual trigger returned {(int)response.StatusCode}: {responseBody}");
    }

    private static string GetWeatherApiKey()
    {
        string? apiKey = Environment.GetEnvironmentVariable("WEATHERAPI__APIKEY")
                    ?? Environment.GetEnvironmentVariable("WEATHER_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Weather API key is not configured.");
        }

        return apiKey;
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = RequestTimeout
        };
    }

    private static string AppendQuery(string baseUrl, string key, string value)
    {
        string separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}

internal sealed class WeatherApiKeyFactAttribute : FactAttribute
{
    public WeatherApiKeyFactAttribute()
    {
        string? apiKey = Environment.GetEnvironmentVariable("WEATHERAPI__APIKEY")
                    ?? Environment.GetEnvironmentVariable("WEATHER_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Skip = "Weather API key is not configured.";
        }
    }
}

internal sealed class ManualTriggerUrlFactAttribute : FactAttribute
{
    public ManualTriggerUrlFactAttribute()
    {
        string? manualTriggerUrl = Environment.GetEnvironmentVariable("WEATHER_IMAGES__GENERATION__MANUALTRIGGERURL");
        if (string.IsNullOrWhiteSpace(manualTriggerUrl))
        {
            Skip = "Manual trigger URL is not configured.";
        }
    }
}
