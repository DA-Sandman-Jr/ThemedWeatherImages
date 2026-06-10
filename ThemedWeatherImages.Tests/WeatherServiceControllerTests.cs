using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ThemedWeatherImages.Controllers;
using ThemedWeatherImages.Services;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherServiceControllerTests
{
    [Fact]
    public async Task GetCurrentWeather_ReturnsGatewayTimeoutWhenWeatherApiTimesOut()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler());
        var httpClientFactory = new FakeHttpClientFactory(httpClient);
        WeatherService weatherService = CreateWeatherService(httpClientFactory);
        var controller = new WeatherServiceController(weatherService, CreateOptions(), NullLogger<WeatherServiceController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        IActionResult result = await controller.GetCurrentWeather(null, null);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, objectResult.StatusCode);

        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Weather service timeout", problem.Title);
        Assert.Equal("The weather service did not respond in time. Please try again shortly.", problem.Detail);
    }

    [Fact]
    public async Task GetCurrentWeather_ReturnsClientSafeProblemWhenWeatherApiReturnsError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":{\"message\":\"API key invalid\"}}")
        };
        var httpClient = new HttpClient(new ResponseHttpMessageHandler(response));
        var httpClientFactory = new FakeHttpClientFactory(httpClient);
        WeatherService weatherService = CreateWeatherService(httpClientFactory);
        var controller = new WeatherServiceController(weatherService, CreateOptions(), NullLogger<WeatherServiceController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        IActionResult result = await controller.GetCurrentWeather(10, 20);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Unable to retrieve weather data", problem.Title);
        Assert.Equal("The weather service could not complete the request. Please try again shortly.", problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("upstreamRequest"));
        Assert.False(problem.Extensions.ContainsKey("upstreamResponseBody"));
        Assert.DoesNotContain("API key invalid", problem.Detail);
    }

    [Fact]
    public async Task GetCurrentWeather_IncludesSanitizedDiagnosticsWhenDebugFlagIsEnabled()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":{\"message\":\"API key invalid\"}}")
        };
        var httpClient = new HttpClient(new ResponseHttpMessageHandler(response));
        var httpClientFactory = new FakeHttpClientFactory(httpClient);
        WeatherService weatherService = CreateWeatherService(httpClientFactory);
        IOptions<ThemedWeatherImagesOptions> options = CreateOptions(exposeErrorDiagnostics: true);
        var controller = new WeatherServiceController(weatherService, options, NullLogger<WeatherServiceController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        IActionResult result = await controller.GetCurrentWeather(10, 20);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<int>(problem.Extensions["upstreamStatus"]));
        Assert.Equal("proxy", Assert.IsType<string>(problem.Extensions["proxyMode"]));
        string upstreamRequest = Assert.IsType<string>(problem.Extensions["upstreamRequest"]);
        Assert.Contains("key=****", upstreamRequest);
        Assert.DoesNotContain("test-key", upstreamRequest);
        Assert.Equal("{\"error\":{\"message\":\"API key invalid\"}}", Assert.IsType<string>(problem.Extensions["upstreamResponseBody"]));
    }

    [Fact]
    public async Task GetCurrentWeather_ReturnsClientSafeProblemWhenUnexpectedErrorOccurs()
    {
        var httpClient = new HttpClient(
            new ThemedWeatherImages.Tests.Helpers.ThrowingHttpMessageHandler(
                new InvalidOperationException("internal configuration detail")));
        var httpClientFactory = new FakeHttpClientFactory(httpClient);
        WeatherService weatherService = CreateWeatherService(httpClientFactory);
        var controller = new WeatherServiceController(weatherService, CreateOptions(), NullLogger<WeatherServiceController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        IActionResult result = await controller.GetCurrentWeather(10, 20);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("The weather service is temporarily unavailable. Please try again shortly.", problem.Detail);
        Assert.DoesNotContain("internal configuration detail", problem.Detail);
    }

    private static IOptions<ThemedWeatherImagesOptions> CreateOptions(bool exposeErrorDiagnostics = false)
    {
        var options = new ThemedWeatherImagesOptions();
        options.Theme.DisplayName = "Test Weather Images";
        options.Theme.SubjectName = "test subject";
        options.Theme.SubjectSlug = "test-subject";
        options.Theme.ImageFileNamePrefix = "test-subject";
        options.WeatherApi.ApiKey = "test-key";
        options.WeatherApi.DefaultIp = "127.0.0.1";
        options.WeatherApi.ExposeErrorDiagnostics = exposeErrorDiagnostics;
        options.Images.BlobBaseUrl = "https://example.test/images";

        return Options.Create(options);
    }

    private static WeatherService CreateWeatherService(IHttpClientFactory httpClientFactory)
    {
        var requestService = new WeatherApiRequestService(httpClientFactory, NullLogger<WeatherApiRequestService>.Instance);

        return new WeatherService(requestService, NullLogger<WeatherService>.Instance);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Timed out", new SocketException((int)SocketError.TimedOut));
        }
    }
}
