using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Tests;

internal sealed class TestLogger<T> : ILogger<T>
{
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }

    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var message = formatter(state, exception);
        Entries.Add((logLevel, message));
    }
}
