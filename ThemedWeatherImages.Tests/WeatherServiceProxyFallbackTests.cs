#nullable enable

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherServiceProxyFallbackTests
{
    [Fact]
    public async Task GetCurrentWeatherAsync_FallsBackToDirectClientWhenProxyFails()
    {
        var proxyHandler = new ResponseHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));
        var directHandler = new ResponseHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"location\":{\"name\":\"Portland\",\"country\":\"United States of America\"},\"current\":{\"temp_f\":70.0,\"temp_c\":21.1,\"condition\":{\"text\":\"Sunny\",\"code\":1000}}}")
        });

        var factory = new SwitchingHttpClientFactory(
            proxyClient: new HttpClient(proxyHandler),
            directClient: new HttpClient(directHandler));
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        (WeatherResponse? result, bool usedProxy) = await service.GetCurrentWeatherAsync("key", "1.2.3.4", 45.5, -122.6);

        Assert.False(usedProxy);
        Assert.Equal("Portland", result.LocationName);
        Assert.Equal(1, proxyHandler.RequestCount);
        Assert.Equal(1, directHandler.RequestCount);
        Assert.Contains("q=45.5,-122.6", directHandler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_FallsBackWhenProxyRequiresAuthentication()
    {
        var proxyHandler = new ResponseHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ProxyAuthenticationRequired));
        var directHandler = new ResponseHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"location\":{\"name\":\"Dublin\",\"country\":\"Ireland\"},\"current\":{\"temp_f\":55.0,\"temp_c\":12.8,\"condition\":{\"text\":\"Light rain\",\"code\":1183}}}")
        });

        var factory = new SwitchingHttpClientFactory(
            proxyClient: new HttpClient(proxyHandler),
            directClient: new HttpClient(directHandler));
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        (WeatherResponse? weather, bool usedProxy) = await service.GetCurrentWeatherAsync("key", "4.4.4.4", 53.3, -6.26);

        Assert.False(usedProxy);
        Assert.Equal("Dublin", weather.LocationName);
        Assert.Equal("Ireland", weather.LocationCountry);
        Assert.Equal("12.8°C", weather.DisplayTemperature);
        Assert.Equal(1, proxyHandler.RequestCount);
        Assert.Equal(1, directHandler.RequestCount);
        Assert.Contains("q=53.3,-6.26", directHandler.LastRequest!.RequestUri!.Query);
    }
}
