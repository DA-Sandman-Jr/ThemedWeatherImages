using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThemedWeatherImages;

namespace ThemedWeatherImages.Controllers;

[ApiController]
[Route("api/weather-service")]
public class WeatherServiceController : ControllerBase
{
    private readonly IOptions<ThemedWeatherImagesOptions> _options;
    private readonly WeatherService _weatherService;
    private readonly ILogger<WeatherServiceController> _logger;

    public WeatherServiceController(
        WeatherService weatherService,
        IOptions<ThemedWeatherImagesOptions> options,
        ILogger<WeatherServiceController> logger)
    {
        _weatherService = weatherService;
        _options = options;
        _logger = logger;
    }

    // Accept optional latitude and longitude query parameters
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentWeather([FromQuery] double? lat, [FromQuery] double? lng)
    {
        if (lat.HasValue && (lat.Value < -90 || lat.Value > 90))
        {
            return BadRequest("Latitude must be between -90 and 90.");
        }

        if (lng.HasValue && (lng.Value < -180 || lng.Value > 180))
        {
            return BadRequest("Longitude must be between -180 and 180.");
        }

        try
        {
            string weatherApiKey = _options.Value.WeatherApi.ApiKey!;

            // Use IP if lat or lng are not provided
            string clientIpAddress = (lat.HasValue && lng.HasValue)
                ? string.Empty // we won't use IP if coordinates are provided
                : GetClientIpAddress();

            (WeatherResponse? weatherResponse, bool usedProxy) = await _weatherService.GetCurrentWeatherAsync(weatherApiKey, clientIpAddress, lat, lng);

            Response.Headers[WeatherService.ProxyModeResponseHeaderName] = usedProxy ? "proxy" : "direct";

            return Ok(weatherResponse);
        }
        catch (HttpRequestException ex)
        {
            int statusCode = (int)DetermineStatusCode(ex);
            LogWeatherApiFailure(ex, statusCode);

            string detail = DetermineProblemDetail(statusCode);
            string title = DetermineProblemTitle(statusCode);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            if (ex.Data[WeatherService.ProxyModeDataKey] is string proxyMode)
            {
                Response.Headers[WeatherService.ProxyModeResponseHeaderName] = proxyMode;
            }

            if (_options.Value.WeatherApi.ExposeErrorDiagnostics)
            {
                AddWeatherApiDiagnostics(problem, ex);
            }

            return StatusCode(statusCode, problem);
        }
        catch (Exception ex)
        {
            _logger.UnexpectedWeatherError(ex);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unable to retrieve weather data",
                Detail = "The weather service is temporarily unavailable. Please try again shortly."
            };

            return StatusCode(StatusCodes.Status500InternalServerError, problem);
        }
    }

    private static HttpStatusCode DetermineStatusCode(HttpRequestException exception)
    {
        if (exception.StatusCode is HttpStatusCode statusCode)
        {
            return statusCode;
        }

        if (exception.InnerException is TaskCanceledException or TimeoutException)
        {
            return HttpStatusCode.GatewayTimeout;
        }

        if (exception.InnerException is SocketException socketException)
        {
            return socketException.SocketErrorCode switch
            {
                SocketError.TimedOut => HttpStatusCode.GatewayTimeout,
                SocketError.HostUnreachable or SocketError.NetworkUnreachable => HttpStatusCode.BadGateway,
                SocketError.ConnectionRefused => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.ServiceUnavailable
            };
        }

        return HttpStatusCode.ServiceUnavailable;
    }

    private void LogWeatherApiFailure(HttpRequestException exception, int statusCode)
    {
        string proxyMode = exception.Data[WeatherService.ProxyModeDataKey] as string ?? "unknown";
        string upstreamRequest = exception.Data[WeatherService.RequestUrlDataKey] as string ?? "unknown";
        string? upstreamBody = exception.Data[WeatherService.UpstreamResponseBodyDataKey] as string;

        if (string.IsNullOrWhiteSpace(upstreamBody))
        {
            _logger.WeatherApiFailure(exception, statusCode, proxyMode, upstreamRequest);
            return;
        }

        _logger.WeatherApiFailureWithBody(exception, statusCode, proxyMode, upstreamRequest, upstreamBody);
    }

    private static void AddWeatherApiDiagnostics(ProblemDetails problem, HttpRequestException exception)
    {
        if (exception.StatusCode is HttpStatusCode upstreamStatus)
        {
            problem.Extensions["upstreamStatus"] = (int)upstreamStatus;
        }

        if (exception.Data[WeatherService.ProxyModeDataKey] is string proxyMode)
        {
            problem.Extensions["proxyMode"] = proxyMode;
        }

        if (exception.Data[WeatherService.RequestUrlDataKey] is string sanitizedUrl)
        {
            problem.Extensions["upstreamRequest"] = sanitizedUrl;
        }

        if (exception.Data[WeatherService.UpstreamResponseBodyDataKey] is string upstreamBody)
        {
            problem.Extensions["upstreamResponseBody"] = upstreamBody;
        }
    }

    private static string DetermineProblemDetail(int statusCode)
    {
        if (statusCode == (int)HttpStatusCode.GatewayTimeout)
        {
            return "The weather service did not respond in time. Please try again shortly.";
        }

        return statusCode is (int)HttpStatusCode.BadGateway or (int)HttpStatusCode.ServiceUnavailable
            ? "The weather service is temporarily unavailable. Please try again shortly."
            : "The weather service could not complete the request. Please try again shortly.";
    }

    private static string DetermineProblemTitle(int statusCode)
    {
        if (statusCode == (int)HttpStatusCode.GatewayTimeout)
        {
            return "Weather service timeout";
        }

        return statusCode == (int)HttpStatusCode.ServiceUnavailable
            ? "Weather service unavailable"
            : "Unable to retrieve weather data";
    }

    private string GetClientIpAddress()
    {
        // If X-Forwarded-For is not present, fall back to RemoteIpAddress
        if (!IPAddress.TryParse(HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').First().Trim(), out IPAddress? ip))
        {
            // If RemoteIpAddress is not available, treat it as loopback so we can use the configured default IP.
            ip = HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;
        }

        // Default useful value for localhost
        if (IPAddress.IsLoopback(ip))
        {
            return _options.Value.WeatherApi.DefaultIp!;
        }

        // If the IP address is IPv6 and starts with ::ffff:, it's an IPv4-mapped address
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        return ip.ToString();
    }
}
