using System.IO;
using System.Runtime.InteropServices;
using Nexus.Cli.Host.Runtime;

namespace Nexus.Cli.Host.Core.Endpoints;

public sealed class CoreEndpointResolver : ICoreEndpointResolver
{
    private const string EnvironmentVariableName = "NEXUS_CORE_ENDPOINT";
    private const string OverrideStrategy = "override";
    private const string EnvironmentStrategy = "environment";
    private const string DefaultStrategy = "default";
    private const string DefaultPipeName = "nexus-core";
    private static readonly Uri DefaultTcpEndpoint = new("https://127.0.0.1:5157");

    private readonly INexusEnvironment _environment;

    public CoreEndpointResolver(INexusEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Task<CoreEndpointResolution> ResolveAsync(string? endpointOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(endpointOverride))
        {
            var parsedOverride = ParseEndpoint(endpointOverride.Trim(), OverrideStrategy);
            return Task.FromResult(new CoreEndpointResolution(new[] { parsedOverride }, OverrideStrategy));
        }

        var environmentValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            var parsedEnvironment = ParseEndpoint(environmentValue.Trim(), EnvironmentStrategy);
            return Task.FromResult(new CoreEndpointResolution(new[] { parsedEnvironment }, EnvironmentStrategy));
        }

        var candidates = new List<CoreEndpoint>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            candidates.Add(new CoreEndpoint(CoreTransportKind.NamedPipe, DefaultPipeName, "windows"));
        }
        else
        {
            var runDirectory = Path.Combine(_environment.UserDirectory, "run");
            var socketPath = Path.Combine(runDirectory, "core.sock");
            candidates.Add(new CoreEndpoint(CoreTransportKind.UnixDomainSocket, socketPath, "unix"));
        }

        candidates.Add(new CoreEndpoint(CoreTransportKind.Tcp, DefaultTcpEndpoint.ToString(), "tcp-fallback"));

        return Task.FromResult(new CoreEndpointResolution(candidates, DefaultStrategy));
    }

    private static CoreEndpoint ParseEndpoint(string value, string strategy)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "pipe":
                case "npipe":
                    return new CoreEndpoint(CoreTransportKind.NamedPipe, ExtractPipeName(uri), strategy);
                case "unix":
                    return new CoreEndpoint(
                        CoreTransportKind.UnixDomainSocket,
                        ExpandHome(Uri.UnescapeDataString(uri.AbsolutePath)),
                        strategy);
                case "http":
                case "https":
                case "tcp":
                    return new CoreEndpoint(CoreTransportKind.Tcp, uri.ToString(), strategy);
            }
        }

        if (LooksLikeTcp(value))
        {
            var normalized = NormalizeTcpEndpoint(value);
            return new CoreEndpoint(CoreTransportKind.Tcp, normalized, strategy);
        }

        if (LooksLikeUnixPath(value))
        {
            return new CoreEndpoint(CoreTransportKind.UnixDomainSocket, ExpandHome(value), strategy);
        }

        return new CoreEndpoint(CoreTransportKind.NamedPipe, value.Trim(), strategy);
    }

    private static bool LooksLikeTcp(string value)
    {
        var span = value.AsSpan();
        var colonIndex = span.LastIndexOf(':');
        if (colonIndex <= 0 || colonIndex == span.Length - 1)
        {
            return false;
        }

        var portSpan = span[(colonIndex + 1)..];
        return int.TryParse(portSpan, out var port) && port is > 0 and <= 65535;
    }

    private static string NormalizeTcpEndpoint(string value)
    {
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (value.StartsWith("[", StringComparison.Ordinal))
        {
            return $"https://{value}";
        }

        return $"https://{value}";
    }

    private static bool LooksLikeUnixPath(string value)
    {
        return value.Contains('/') || value.Contains(Path.DirectorySeparatorChar);
    }

    private static string ExpandHome(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (path.StartsWith("~", StringComparison.Ordinal))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrWhiteSpace(home))
            {
                var relative = path[1..].TrimStart('/', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
                return Path.Combine(home, relative);
            }
        }

        return path;
    }

    private static string ExtractPipeName(Uri uri)
    {
        var unescapedPath = Uri.UnescapeDataString(uri.AbsolutePath);

        if (!string.IsNullOrEmpty(unescapedPath) && unescapedPath != "/")
        {
            return unescapedPath.Trim('/');
        }

        if (!string.IsNullOrEmpty(uri.Host))
        {
            return uri.Host.Trim('/');
        }

        var original = uri.ToString();
        var lastSlash = original.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < original.Length - 1)
        {
            return original[(lastSlash + 1)..];
        }

        return "nexus-core";
    }
}
