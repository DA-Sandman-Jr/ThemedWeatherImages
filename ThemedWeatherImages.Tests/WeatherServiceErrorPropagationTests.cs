#nullable enable

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherServiceErrorPropagationTests
{
    [Fact]
    public async Task GetCurrentWeatherAsync_ThrowsHttpRequestExceptionWhenResponseFails()
    {
        var handler = new ErrorHttpMessageHandler(HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetCurrentWeatherAsync("key", "2.3.4.5", null, null));
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_AttachesDiagnosticsWhenDirectRequestFails()
    {
        var proxyException = new HttpRequestException("proxy failure", new SocketException());
        var proxyHandler = new ThrowingHttpMessageHandler(proxyException);

        string lengthyBody = new string('x', 2100);
        var directResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(lengthyBody, Encoding.UTF8)
        };
        var directHandler = new ResponseHttpMessageHandler(directResponse);

        var factory = new SwitchingHttpClientFactory(
            proxyClient: new HttpClient(proxyHandler),
            directClient: new HttpClient(directHandler));
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.GetCurrentWeatherAsync("secretKey", "9.9.9.9", null, null));

        Assert.Equal("direct", exception.Data[key: WeatherService.ProxyModeDataKey]);
        string requestUrl = Assert.IsType<string>(exception.Data[WeatherService.RequestUrlDataKey]);
        Assert.Contains("key=****", requestUrl);
        Assert.DoesNotContain("secretKey", requestUrl);

        string responseBody = Assert.IsType<string>(exception.Data[WeatherService.UpstreamResponseBodyDataKey]);
        Assert.True(responseBody.Length <= 2048);
        Assert.EndsWith("...", responseBody);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ReturnsDetailedErrorMessageFromWeatherApi()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":{\"message\":\"API key invalid\"}}")
        };
        var handler = new ResponseHttpMessageHandler(response);
        var factory = new SwitchingHttpClientFactory(new HttpClient(handler), new HttpClient(handler));
        WeatherService service = WeatherServiceTestFactory.CreateService(factory);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.GetCurrentWeatherAsync("secret-key", "5.5.5.5", 10, 20));

        Assert.Equal("proxy", exception.Data[WeatherService.ProxyModeDataKey]);
        string sanitizedUrl = Assert.IsType<string>(exception.Data[WeatherService.RequestUrlDataKey]);
        Assert.DoesNotContain("secret-key", sanitizedUrl);
        Assert.Contains("key=****", sanitizedUrl);
        Assert.Contains("API key invalid", exception.Message);

        string upstreamBody = Assert.IsType<string>(exception.Data[WeatherService.UpstreamResponseBodyDataKey]);
        Assert.Equal("{\"error\":{\"message\":\"API key invalid\"}}", upstreamBody);
    }
}
