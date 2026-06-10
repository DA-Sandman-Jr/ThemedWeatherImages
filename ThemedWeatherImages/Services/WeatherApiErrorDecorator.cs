using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ThemedWeatherImages.Services;

public static class WeatherApiErrorDecorator
{
    public const string ProxyModeDataKey = "ThemedWeatherImages.ProxyMode";
    public const string UpstreamResponseBodyDataKey = "ThemedWeatherImages.UpstreamResponseBody";
    public const string RequestUrlDataKey = "ThemedWeatherImages.RequestUrl";

    private const int MaxDiagnosticBodyLength = 2048;
    private const string TrimMarker = "...";

    public static async Task ThrowDetailedHttpRequestExceptionAsync(HttpResponseMessage response, string proxyMode, string sanitizedUrl)
    {
        HttpStatusCode statusCode = response.StatusCode;
        string? responseBody = null;

        try
        {
            responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore failures when reading the error body.
        }
        finally
        {
            response.Dispose();
        }

        var messageBuilder = new StringBuilder(
            $"Weather API request failed with status {(int)statusCode} ({statusCode}).");

        string? detailedMessage = ExtractWeatherApiErrorMessage(responseBody);
        if (!string.IsNullOrWhiteSpace(detailedMessage))
        {
            messageBuilder.Append(' ').Append(detailedMessage);
        }

        var exception = new HttpRequestException(messageBuilder.ToString(), null, statusCode);
        Decorate(exception, proxyMode, sanitizedUrl, responseBody);
        throw exception;
    }

    public static void Decorate(HttpRequestException exception, string proxyMode, string sanitizedUrl, string? responseBody = null)
    {
        AttachProxyMode(exception, proxyMode);
        AttachRequestUrl(exception, sanitizedUrl);

        string? trimmedBody = TrimForDiagnostics(responseBody);
        if (!string.IsNullOrEmpty(trimmedBody))
        {
            exception.Data[UpstreamResponseBodyDataKey] = trimmedBody;
        }
    }

    public static void MarkProxyMode(HttpResponseMessage response, string proxyMode)
    {
        response.Headers.Remove(WeatherService.ProxyModeResponseHeaderName);
        response.Headers.TryAddWithoutValidation(WeatherService.ProxyModeResponseHeaderName, proxyMode);
    }

    private static string? ExtractWeatherApiErrorMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var token = JObject.Parse(responseBody);
            string? message = token["error"]?["message"]?.ToString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }
        catch
        {
            // Ignore JSON parsing errors and fall back to a trimmed body string.
        }

        string trimmedBody = responseBody.Trim();
        if (trimmedBody.Length > 500)
        {
            trimmedBody = Trim(trimmedBody, 500);
        }

        return $"Response body: {trimmedBody}";
    }

    private static void AttachProxyMode(HttpRequestException exception, string proxyMode)
    {
        if (!string.IsNullOrWhiteSpace(proxyMode))
        {
            exception.Data[ProxyModeDataKey] = proxyMode;
        }
    }

    private static void AttachRequestUrl(HttpRequestException exception, string sanitizedUrl)
    {
        if (!string.IsNullOrWhiteSpace(sanitizedUrl))
        {
            exception.Data[RequestUrlDataKey] = sanitizedUrl;
        }
    }

    private static string? TrimForDiagnostics(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        string trimmed = responseBody.Trim();
        if (trimmed.Length > MaxDiagnosticBodyLength)
        {
            trimmed = Trim(trimmed, MaxDiagnosticBodyLength);
        }

        return trimmed;
    }

    private static string Trim(string value, int maxLength)
    {
        return value[..(maxLength - TrimMarker.Length)] + TrimMarker;
    }
}
