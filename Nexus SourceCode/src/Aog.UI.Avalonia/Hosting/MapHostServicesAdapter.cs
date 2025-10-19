using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aog.Abstractions.Mapping;
using Microsoft.Extensions.Configuration;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Default implementation of <see cref="IMapHostServices"/> exposed to mapping plugins.
/// </summary>
public sealed class MapHostServicesAdapter : IMapHostServices
{
    private readonly Func<CancellationToken, IAsyncEnumerable<PoseSample>> _poseStreamFactory;
    private readonly IObservable<FieldContext> _fieldContext;
    private readonly IStorage _storage;
    private readonly IConfig _config;

    public MapHostServicesAdapter(
        Func<CancellationToken, IAsyncEnumerable<PoseSample>> poseStreamFactory,
        IObservable<FieldContext> fieldContext,
        IStorage storage,
        IConfig config)
    {
        _poseStreamFactory = poseStreamFactory ?? throw new ArgumentNullException(nameof(poseStreamFactory));
        _fieldContext = fieldContext ?? throw new ArgumentNullException(nameof(fieldContext));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public static MapHostServicesAdapter CreateDefault(IConfiguration configuration, string storageRoot, PoseSample? seedPose = null, FieldContext? seedField = null)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            throw new ArgumentException("Storage root is required.", nameof(storageRoot));
        }

        var initialPose = seedPose ?? new PoseSample(0, 0, 0, 0);
        var initialField = seedField ?? new FieldContext(0, 0, 0, null);

        return new MapHostServicesAdapter(
            ct => SinglePoseStream(initialPose, ct),
            new StaticObservable<FieldContext>(initialField),
            new FileSystemStorage(storageRoot),
            new ConfigurationAdapter(configuration));
    }

    public IAsyncEnumerable<PoseSample> PoseStream(CancellationToken cancellationToken)
        => _poseStreamFactory(cancellationToken);

    public IObservable<FieldContext> FieldContext => _fieldContext;

    public IStorage Storage => _storage;

    public IConfig Config => _config;

    private static async IAsyncEnumerable<PoseSample> SinglePoseStream(
        PoseSample pose,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            yield return pose;
        }

        await Task.CompletedTask;
    }

    private sealed class StaticObservable<T> : IObservable<T>
    {
        private readonly T _value;

        public StaticObservable(T value)
        {
            _value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer is null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            observer.OnNext(_value);
            observer.OnCompleted();
            return Disposable.Instance;
        }

        private sealed class Disposable : IDisposable
        {
            public static readonly Disposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class FileSystemStorage : IStorage
    {
        public FileSystemStorage(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath ?? throw new ArgumentNullException(nameof(rootPath)));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public long? QuotaBytes => null;

        public ValueTask DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var path = Resolve(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var path = Resolve(relativePath);
            if (!File.Exists(path))
            {
                return ValueTask.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            return ValueTask.FromResult<Stream?>(stream);
        }

        public ValueTask<Stream> OpenWriteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var path = Resolve(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            return ValueTask.FromResult(stream);
        }

        private string Resolve(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Path must be provided.", nameof(relativePath));
            }

            var combined = Path.GetFullPath(Path.Combine(RootPath, relativePath));
            if (!combined.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Attempted to access storage outside of the plugin root.");
            }

            return combined;
        }
    }

    private sealed class ConfigurationAdapter : IConfig
    {
        private readonly IConfiguration _configuration;

        public ConfigurationAdapter(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public ValueTask<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var section = _configuration.GetSection(path);
            var value = section.Exists() ? section.Get<T>() : default;
            return ValueTask.FromResult(value);
        }

        public ValueTask SetAsync<T>(string path, T? value, CancellationToken cancellationToken = default)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (_configuration is IConfigurationRoot root)
            {
                root[path] = value?.ToString();
            }

            return ValueTask.CompletedTask;
        }
    }
}
