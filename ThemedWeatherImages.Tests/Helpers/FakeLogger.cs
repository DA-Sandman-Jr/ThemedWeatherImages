using System;
using Microsoft.Extensions.Logging;

namespace ThemedWeatherImages.Tests.Helpers;

internal class FakeLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();

        public void Dispose()
        {
        }
    }
}
