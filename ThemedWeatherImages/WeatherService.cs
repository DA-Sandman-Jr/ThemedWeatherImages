using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Services;

namespace ThemedWeatherImages;

public class WeatherService
{
    private readonly ILogger<WeatherService> _logger;
    private readonly WeatherApiRequestService _requestService;

    public const string ProxyModeDataKey = WeatherApiErrorDecorator.ProxyModeDataKey;
    public const string UpstreamResponseBodyDataKey = WeatherApiErrorDecorator.UpstreamResponseBodyDataKey;
    public const string RequestUrlDataKey = WeatherApiErrorDecorator.RequestUrlDataKey;
    internal const string ProxyModeResponseHeaderName = WeatherApiRequestService.ProxyModeResponseHeaderName;

    public WeatherService(
        WeatherApiRequestService requestService,
        ILogger<WeatherService> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    public async Task<(WeatherResponse Weather, bool UsedProxy)> GetCurrentWeatherAsync(string apiKey, string clientIpAddress, double? lat, double? lng)
    {
        string queryParam = BuildQueryParameter(clientIpAddress, lat, lng);
        string url = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={queryParam}";
        string sanitizedUrl = WeatherApiUrlSanitizer.Sanitize(url);

        _logger.FetchingCurrentWeather(sanitizedUrl);
        WeatherApiResult apiResult = await _requestService.GetWeatherApiResponseAsync(url, sanitizedUrl).ConfigureAwait(false);
        using HttpResponseMessage response = apiResult.Response;
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        WeatherResponse weatherResponse = WeatherApiResponseMapper.Map(content);
        return (weatherResponse, apiResult.UsedProxy);
    }

    private static string BuildQueryParameter(string clientIpAddress, double? lat, double? lng)
    {
        return lat.HasValue && lng.HasValue
            ? FormattableString.Invariant($"{lat.Value},{lng.Value}")
            : clientIpAddress;
    }
}
