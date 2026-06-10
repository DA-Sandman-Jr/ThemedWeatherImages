#nullable enable

using System.Net.Http;
using System.Threading.Tasks;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherServiceParsingTests
{
    [Fact]
    public async Task GetCurrentWeatherAsync_ParsesWeatherResponse()
    {
        string json = "{\"location\":{\"name\":\"Seattle\",\"country\":\"United States of America\"},\"current\":{\"temp_f\":60.5,\"temp_c\":15.8,\"condition\":{\"text\":\"Sunny\",\"code\":1000}}}";
        var handler = new FakeHttpMessageHandler(json);
        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        (WeatherResponse? result, bool usedProxy) = await service.GetCurrentWeatherAsync("key", "1.2.3.4", 47, -122);

        Assert.True(usedProxy);
        Assert.Equal("Seattle", result.LocationName);
        Assert.Equal("60.5°F", result.DisplayTemperature);
        Assert.Equal("Sunny", result.Condition);
        Assert.Equal("1000", result.ConditionCode);
        Assert.Equal("Clear/Sunny", result.ConditionCategory);
        Assert.Contains("q=47", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UsesIpWhenNoCoordinates()
    {
        string json = "{\"location\":{\"name\":\"Paris\",\"country\":\"France\"},\"current\":{\"temp_f\":55.0,\"temp_c\":12.8,\"condition\":{\"text\":\"Cloudy\",\"code\":1006}}}";
        var handler = new FakeHttpMessageHandler(json);
        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        await service.GetCurrentWeatherAsync("key", "2.3.4.5", null, null);

        Assert.Contains("q=2.3.4.5", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UsesCelsiusOutsideUnitedStates()
    {
        string json = "{\"location\":{\"name\":\"Vancouver\",\"country\":\"Canada\"},\"current\":{\"temp_f\":52.0,\"temp_c\":11.1,\"condition\":{\"text\":\"Overcast\",\"code\":1009}}}";
        var handler = new FakeHttpMessageHandler(json);
        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        (WeatherResponse? weather, bool usedProxy) = await service.GetCurrentWeatherAsync("key", "3.3.3.3", null, null);

        Assert.True(usedProxy);
        Assert.Equal("Vancouver", weather.LocationName);
        Assert.Equal("Canada", weather.LocationCountry);
        Assert.Equal("11.1°C", weather.DisplayTemperature);
        Assert.Equal("52", weather.TemperatureFahrenheit);
        Assert.Equal("11.1", weather.TemperatureCelsius);
        Assert.Equal("Overcast", weather.Condition);
        Assert.Equal("1009", weather.ConditionCode);
        Assert.Equal("Cloudy", weather.ConditionCategory);
        Assert.Contains("q=3.3.3.3", handler.LastRequest!.RequestUri!.Query);
    }
}
