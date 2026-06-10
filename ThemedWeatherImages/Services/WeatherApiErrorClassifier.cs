using System.Net;
using System.Net.Sockets;

namespace ThemedWeatherImages.Services;

public static class WeatherApiErrorClassifier
{
    public static bool ShouldFallback(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.ProxyAuthenticationRequired
               or HttpStatusCode.BadGateway
               or HttpStatusCode.GatewayTimeout;
    }

    public static bool ShouldFallback(HttpRequestException exception)
    {
        if (exception.StatusCode is HttpStatusCode statusCode)
        {
            return ShouldFallback(statusCode);
        }

        return exception.InnerException is SocketException;
    }
}
