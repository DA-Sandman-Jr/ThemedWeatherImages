using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThemedWeatherImages.Functions.Generation;
using ThemedWeatherImages.Tests.Helpers;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class AiHordeClientTests
{
    [Fact]
    public async Task SubmitGenerationAsync_UsesConfiguredGenerationParameters()
    {
        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler);
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_HORDE_API_URL"] = "https://aihorde.example/generate",
                ["AI_HORDE_API_KEY"] = "test-api-key",
                ["WEBHOOK_URL"] = "https://beforeforeverafter.example/aihorde-webhook"
            })
            .Build();

        var options = new ThemedWeatherImagesOptions();
        options.Generation.Model = "custom-model";
        options.Generation.CfgScale = 7.5;
        options.Generation.SamplerName = "k_dpmpp_2m";
        options.Generation.Width = 768;
        options.Generation.Height = 640;

        var logger = new CapturingLogger<AiHordeClient>();
        var client = new AiHordeClient(
            new FakeHttpClientFactory(httpClient),
            configuration,
            Options.Create(options),
            logger);

        AiHordeSubmissionResult result = await client.SubmitGenerationAsync(
            new AiHordeGenerationRequest("storm prompt", "squirrel", "Rain", "20260531"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("horde-123", result.HordeId);
        Assert.Equal(new Uri("https://aihorde.example/generate"), handler.RequestUri);
        Assert.Equal("test-api-key", Assert.Single(handler.ApiKeys));

        using var document = JsonDocument.Parse(handler.Body!);
        JsonElement root = document.RootElement;

        Assert.Equal("storm prompt", root.GetProperty("prompt").GetString());
        Assert.Equal("https://beforeforeverafter.example/aihorde-webhook", root.GetProperty("webhook").GetString());
        Assert.Equal("squirrel", root.GetProperty("subject").GetString());
        Assert.True(root.GetProperty("censor_nsfw").GetBoolean());
        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains("https://beforeforeverafter.example/aihorde-webhook", StringComparison.OrdinalIgnoreCase));

        JsonElement parameters = root.GetProperty("params");
        Assert.Equal("custom-model", parameters.GetProperty("model").GetString());
        Assert.Equal(7.5, parameters.GetProperty("cfg_scale").GetDouble());
        Assert.Equal("k_dpmpp_2m", parameters.GetProperty("sampler_name").GetString());
        Assert.Equal(768, parameters.GetProperty("width").GetInt32());
        Assert.Equal(640, parameters.GetProperty("height").GetInt32());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public IReadOnlyList<string> ApiKeys { get; private set; } = [];

        public string? Body { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            ApiKeys = request.Headers.TryGetValues("apikey", out IEnumerable<string>? values)
                ? values.ToArray()
                : [];
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"horde-123\"}")
            };
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
