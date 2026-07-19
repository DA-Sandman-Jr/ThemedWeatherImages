using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ThemedWeatherImages.Tests.Helpers;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _content;

    public FakeHttpMessageHandler(string content)
    {
        _content = content;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_content)
        };
        return Task.FromResult(response);
    }
}

internal sealed class ErrorHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;

    public ErrorHttpMessageHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        return Task.FromResult(response);
    }
}

internal sealed class ResponseHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public ResponseHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        RequestCount++;
        return Task.FromResult(_response);
    }
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo.Capture(_exception).Throw();
        throw new UnreachableException();
    }
}
