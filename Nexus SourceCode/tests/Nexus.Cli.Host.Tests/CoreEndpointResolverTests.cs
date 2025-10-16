using System;
using System.IO;
using FluentAssertions;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Runtime;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public class CoreEndpointResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesOverrideWhenProvided()
    {
        var environment = new FakeEnvironment();
        var resolver = new CoreEndpointResolver(environment);

        var resolution = await resolver.ResolveAsync("unix:///tmp/nexus-core.sock", CancellationToken.None);

        resolution.Strategy.Should().Be("override");
        resolution.Candidates.Should().ContainSingle();
        var endpoint = resolution.Candidates[0];
        endpoint.Transport.Should().Be(CoreTransportKind.UnixDomainSocket);
        endpoint.Address.Should().Be("/tmp/nexus-core.sock");
    }

    [Fact]
    public async Task ResolveAsync_UsesEnvironmentVariable()
    {
        var environment = new FakeEnvironment();
        var resolver = new CoreEndpointResolver(environment);
        Environment.SetEnvironmentVariable("NEXUS_CORE_ENDPOINT", "pipe://custom-pipe");

        try
        {
            var resolution = await resolver.ResolveAsync(null, CancellationToken.None);

            resolution.Strategy.Should().Be("environment");
            resolution.Candidates.Should().ContainSingle();
            var endpoint = resolution.Candidates[0];
            endpoint.Transport.Should().Be(CoreTransportKind.NamedPipe);
            endpoint.Address.Should().Be("custom-pipe");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXUS_CORE_ENDPOINT", null);
        }
    }

    [Fact]
    public async Task ResolveAsync_UsesUnixDomainSocketOnLinux()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Skip check when running on Windows.
        }

        var environment = new FakeEnvironment { UserDirectoryValue = "/home/test/.nexus" };
        var resolver = new CoreEndpointResolver(environment);

        var resolution = await resolver.ResolveAsync(null, CancellationToken.None);

        resolution.Strategy.Should().Be("default");
        resolution.Candidates.Should().NotBeEmpty();
        resolution.Candidates[0].Transport.Should().Be(CoreTransportKind.UnixDomainSocket);
        resolution.Candidates[0].Address.Should().Be("/home/test/.nexus/run/core.sock");
    }

    private sealed class FakeEnvironment : INexusEnvironment
    {
        public string UserDirectoryValue { get; set; } = "/tmp/.nexus";

        public string UserDirectory => UserDirectoryValue;

        public string ConfigFilePath => Path.Combine(UserDirectoryValue, "config.yml");

        public string CredentialsFilePath => Path.Combine(UserDirectoryValue, "credentials");

        public string CacheDirectory => Path.Combine(UserDirectoryValue, "cache");

        public string PluginsDirectory => Path.Combine(UserDirectoryValue, "plugins");

        public string RepositoryConfigDirectory => Path.Combine(UserDirectoryValue, "repo");
    }
}
