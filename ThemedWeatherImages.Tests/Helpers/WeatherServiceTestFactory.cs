using System.Net.Http;
using ThemedWeatherImages.Services;

namespace ThemedWeatherImages.Tests.Helpers;

internal static class WeatherServiceTestFactory
{
    public static WeatherService CreateService(IHttpClientFactory factory)
    {
        var requestService = new WeatherApiRequestService(factory, new FakeLogger<WeatherApiRequestService>());

        return new WeatherService(requestService, new FakeLogger<WeatherService>());
    }
}
