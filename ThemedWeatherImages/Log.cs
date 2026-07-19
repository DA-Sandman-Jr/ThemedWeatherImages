using Microsoft.Extensions.Logging;

namespace ThemedWeatherImages;

/// <summary>
/// Source-generated <see cref="ILogger"/> extension methods (CA1848). Centralizing
/// them here keeps the call sites free of inline format-string allocations.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Unexpected status code {StatusCode} when retrieving {BlobUri}")]
    public static partial void UnexpectedImageStatusCode(this ILogger logger, System.Net.HttpStatusCode statusCode, string blobUri);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error retrieving weather image from {BlobUri}")]
    public static partial void ErrorRetrievingWeatherImage(this ILogger logger, Exception exception, string blobUri);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Unexpected error while retrieving current weather.")]
    public static partial void UnexpectedWeatherError(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Weather API request failed with status code {StatusCode}. Proxy mode: {ProxyMode}. Upstream request: {UpstreamRequest}.")]
    public static partial void WeatherApiFailure(this ILogger logger, Exception exception, int statusCode, string proxyMode, string upstreamRequest);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Weather API request failed with status code {StatusCode}. Proxy mode: {ProxyMode}. Upstream request: {UpstreamRequest}. Upstream response body: {UpstreamResponseBody}")]
    public static partial void WeatherApiFailureWithBody(this ILogger logger, Exception exception, int statusCode, string proxyMode, string upstreamRequest, string upstreamResponseBody);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Requesting Weather API via proxy for {Url}")]
    public static partial void RequestingWeatherApiViaProxy(this ILogger logger, string url);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Weather API returned status {StatusCode} via proxy for {Url}; retrying without proxy.")]
    public static partial void WeatherApiProxyStatusRetrying(this ILogger logger, int statusCode, string url);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Weather API returned status {StatusCode} via proxy for {Url}.")]
    public static partial void WeatherApiProxyStatus(this ILogger logger, int statusCode, string url);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Weather API request via proxy failed for {Url}; retrying without proxy.")]
    public static partial void WeatherApiProxyRequestFailedRetrying(this ILogger logger, Exception exception, string url);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Requesting Weather API without proxy for {Url}")]
    public static partial void RequestingWeatherApiWithoutProxy(this ILogger logger, string url);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Weather API returned status {StatusCode} without proxy for {Url}.")]
    public static partial void WeatherApiDirectStatus(this ILogger logger, int statusCode, string url);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Fetching current weather from Weather API using url {Url}")]
    public static partial void FetchingCurrentWeather(this ILogger logger, string url);
}
