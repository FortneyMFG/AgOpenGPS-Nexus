using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Nexus.Sdk.Core;

namespace Aog.UI.Avalonia.Plugins;

internal sealed class PluginEnvironmentInfo : IPluginEnvironment
{
    public PluginEnvironmentInfo(PluginDescriptor descriptor, Version sdkVersion)
    {
        PluginId = descriptor.Id;
        PluginVersion = descriptor.Version;
        SdkVersion = sdkVersion;
    }

    public string PluginId { get; }

    public Version PluginVersion { get; }

    public Version SdkVersion { get; }
}

internal sealed class PluginPathsInfo : IPluginPaths
{
    public PluginPathsInfo(string baseDirectory)
    {
        BaseDirectory = baseDirectory;
        LogDirectory = Ensure(Path.Combine(baseDirectory, "logs"));
        CacheDirectory = Ensure(Path.Combine(baseDirectory, "cache"));
        TempDirectory = Ensure(Path.Combine(baseDirectory, "tmp"));
    }

    public string BaseDirectory { get; }

    public string LogDirectory { get; }

    public string CacheDirectory { get; }

    public string TempDirectory { get; }

    public string Resolve(string relativePath)
    {
        relativePath ??= string.Empty;
        return Path.Combine(BaseDirectory, relativePath);
    }

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}

internal sealed class PluginHostServices : IHostServices
{
    private readonly PluginDescriptor _descriptor;
    private readonly IDictionary<Type, object> _features;

    public PluginHostServices(
        PluginDescriptor descriptor,
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        IPluginPaths paths,
        IPluginEnvironment environment,
        IDictionary<Type, object>? features = null)
    {
        _descriptor = descriptor;
        Services = services ?? throw new ArgumentNullException(nameof(services));
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _features = features ?? new Dictionary<Type, object>();
    }

    public string PluginId => _descriptor.Id;

    public IServiceProvider Services { get; }

    public ILoggerFactory LoggerFactory { get; }

    public IPluginEnvironment Environment { get; }

    public IPluginPaths Paths { get; }

    public IEventBus EventBus { get; } = NullEventBus.Instance;

    public ICommandBus CommandBus { get; } = NullCommandBus.Instance;

    public ISettingsStore Settings { get; } = new NullSettingsStore();

    public ITelemetry Telemetry { get; } = NullTelemetry.Instance;

    public TFeature? GetFeature<TFeature>()
        where TFeature : class
    {
        return _features.TryGetValue(typeof(TFeature), out var value) ? (TFeature)value : null;
    }
}

internal sealed class NullEventBus : IEventBus
{
    public static NullEventBus Instance { get; } = new();

    public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull
        => ValueTask.CompletedTask;

    public IAsyncEnumerable<TEvent> SubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        return EmptyAsyncEnumerable<TEvent>.Instance;
    }

    private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        public static readonly EmptyAsyncEnumerable<T> Instance = new();

        public T Current => default!;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class NullCommandBus : ICommandBus
{
    public static NullCommandBus Instance { get; } = new();

    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : notnull
        => ValueTask.CompletedTask;
}

internal sealed class NullTelemetry : ITelemetry
{
    public static NullTelemetry Instance { get; } = new();

    public void TrackMetric(string name, double value, IReadOnlyDictionary<string, string>? properties = null)
    {
    }

    public void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null)
    {
    }

    public void TrackException(Exception exception, IReadOnlyDictionary<string, string>? properties = null)
    {
    }
}

internal sealed class NullSettingsStore : ISettingsStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_values.TryGetValue(key, out var value) && value is T typed)
        {
            return ValueTask.FromResult<T?>(typed);
        }

        return ValueTask.FromResult<T?>(default);
    }

    public ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
