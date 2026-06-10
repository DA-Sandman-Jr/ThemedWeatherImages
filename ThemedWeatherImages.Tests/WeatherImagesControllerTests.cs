using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ThemedWeatherImages.Controllers;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherImagesControllerTests
{
    [Fact]
    public async Task GetWeatherImage_AppendsConfiguredBlobSasTokenToUpstreamRequest()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/webp");

        var handler = new ResponseHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler);

        WeatherImagesController controller = CreateController(httpClient, "?sv=2026-01-01&sp=r&sig=test%2Fsignature");

        IActionResult result = await controller.GetWeatherImage("squirrel-clear-sunny-20260531.webp", CancellationToken.None);

        Assert.IsType<FileContentResult>(result);
        Assert.Equal(
            "https://example.test/images/squirrel-clear-sunny-20260531.webp?sv=2026-01-01&sp=r&sig=test%2Fsignature",
            handler.LastRequest?.RequestUri?.AbsoluteUri);
    }

    private static WeatherImagesController CreateController(HttpClient httpClient, string? blobSasToken)
    {
        ThemedWeatherImagesOptions options = new();
        options.Theme.DisplayName = "Test Weather Images";
        options.Theme.SubjectName = "test subject";
        options.Theme.SubjectSlug = "test-subject";
        options.Theme.ImageFileNamePrefix = "test-subject";
        options.WeatherApi.ApiKey = "test-key";
        options.WeatherApi.DefaultIp = "127.0.0.1";
        options.Images.BlobBaseUrl = "https://example.test/images";
        options.Images.BlobSasToken = blobSasToken;

        return new WeatherImagesController(
            new FakeHttpClientFactory(httpClient),
            NullLogger<WeatherImagesController>.Instance,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
