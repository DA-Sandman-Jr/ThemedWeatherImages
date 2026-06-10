using System.Net;
using Microsoft.Extensions.Logging;

namespace ThemedWeatherImages.Services;

public record struct WeatherApiResult(HttpResponseMessage Response, bool UsedProxy);

public class WeatherApiRequestService
{
    internal const string ProxyModeResponseHeaderName = "X-Weather-Proxy-Mode";

    private const string ProxyModeProxyValue = "proxy";
    private const string ProxyModeDirectValue = "direct";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherApiRequestService> _logger;

    public WeatherApiRequestService(
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherApiRequestService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WeatherApiResult> GetWeatherApiResponseAsync(string url, string sanitizedUrl, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage? response = null;

        try
        {
            HttpClient proxyClient = _httpClientFactory.CreateClient("ProxyClient");
            _logger.LogDebug("Requesting Weather API via proxy for {Url}", sanitizedUrl);
            response = await proxyClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (WeatherApiErrorClassifier.ShouldFallback(response.StatusCode))
                {
                    HttpStatusCode statusCode = response.StatusCode;
                    response.Dispose();
                    response = null;
                    _logger.LogWarning("Weather API returned status {StatusCode} via proxy for {Url}; retrying without proxy.", (int)statusCode, sanitizedUrl);
                    return await GetWeatherApiResponseWithoutProxyAsync(url, sanitizedUrl, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogWarning("Weather API returned status {StatusCode} via proxy for {Url}.", (int)response.StatusCode, sanitizedUrl);
                await WeatherApiErrorDecorator.ThrowDetailedHttpRequestExceptionAsync(response, ProxyModeProxyValue, sanitizedUrl).ConfigureAwait(false);
            }

            WeatherApiErrorDecorator.MarkProxyMode(response, ProxyModeProxyValue);
            return new WeatherApiResult(response, UsedProxy: true);
        }
        catch (HttpRequestException ex) when (WeatherApiErrorClassifier.ShouldFallback(ex))
        {
            response?.Dispose();
            WeatherApiErrorDecorator.Decorate(ex, ProxyModeProxyValue, sanitizedUrl);
            _logger.LogWarning(ex, "Weather API request via proxy failed for {Url}; retrying without proxy.", sanitizedUrl);
            return await GetWeatherApiResponseWithoutProxyAsync(url, sanitizedUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            WeatherApiErrorDecorator.Decorate(ex, ProxyModeProxyValue, sanitizedUrl);
            throw;
        }
    }

    private async Task<WeatherApiResult> GetWeatherApiResponseWithoutProxyAsync(string url, string sanitizedUrl, CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage? response = null;

        try
        {
            _logger.LogDebug("Requesting Weather API without proxy for {Url}", sanitizedUrl);
            response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned status {StatusCode} without proxy for {Url}.", (int)response.StatusCode, sanitizedUrl);
                await WeatherApiErrorDecorator.ThrowDetailedHttpRequestExceptionAsync(response, ProxyModeDirectValue, sanitizedUrl).ConfigureAwait(false);
            }

            WeatherApiErrorDecorator.MarkProxyMode(response, ProxyModeDirectValue);
            return new WeatherApiResult(response, UsedProxy: false);
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            WeatherApiErrorDecorator.Decorate(ex, ProxyModeDirectValue, sanitizedUrl);
            throw;
        }
    }
}
