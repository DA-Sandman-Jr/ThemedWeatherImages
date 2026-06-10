using System;
using System.Net.Http;

namespace ThemedWeatherImages.Tests.Helpers;

internal class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public FakeHttpClientFactory(HttpClient client)
    {
        _client = client;
    }

    public HttpClient CreateClient(string name) => _client;
}

internal class SwitchingHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _proxyClient;
    private readonly HttpClient _directClient;

    public SwitchingHttpClientFactory(HttpClient proxyClient, HttpClient directClient)
    {
        _proxyClient = proxyClient;
        _directClient = directClient;
    }

    public HttpClient CreateClient(string name)
    {
        if (string.Equals(name, "ProxyClient", StringComparison.Ordinal))
        {
            return _proxyClient;
        }

        return _directClient;
    }
}
