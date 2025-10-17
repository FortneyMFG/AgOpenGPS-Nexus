using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Linux.Tests;

internal sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    private TOptions _currentValue;
    private event Action<TOptions, string?>? _listeners;

    public TestOptionsMonitor(TOptions currentValue)
    {
        _currentValue = currentValue ?? throw new ArgumentNullException(nameof(currentValue));
    }

    public TOptions CurrentValue => _currentValue;

    public TOptions Get(string? name) => _currentValue;

    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        if (listener is null)
        {
            throw new ArgumentNullException(nameof(listener));
        }

        _listeners += listener;
        return new Subscription(this, listener);
    }

    public void Update(TOptions value, string? name = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _currentValue = value;
        var handlers = Volatile.Read(ref _listeners);
        if (handlers is null)
        {
            return;
        }

        var invocationList = handlers.GetInvocationList().Cast<Action<TOptions, string?>>();
        foreach (var handler in invocationList)
        {
            handler(value, name ?? Options.DefaultName);
        }
    }

    private void Unsubscribe(Action<TOptions, string?> listener)
    {
        _listeners -= listener;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly TestOptionsMonitor<TOptions> _monitor;
        private Action<TOptions, string?>? _listener;

        public Subscription(TestOptionsMonitor<TOptions> monitor, Action<TOptions, string?> listener)
        {
            _monitor = monitor;
            _listener = listener;
        }

        public void Dispose()
        {
            var listener = Interlocked.Exchange(ref _listener, null);
            if (listener is not null)
            {
                _monitor.Unsubscribe(listener);
            }
        }
    }
}
