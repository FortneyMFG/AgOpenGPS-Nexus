using System;
using Aog.Abstractions.Runtime;
using Xunit;

namespace Aog.Abstractions.Tests;

public sealed class RuntimeBaselineTests
{
    [Fact]
    public void GetCurrentRuntimeInfo_ReturnsNonEmptyData()
    {
        var info = DotNetRuntimeBaseline.GetCurrentRuntimeInfo();

        Assert.NotNull(info);
        Assert.Equal(Environment.Version, info.Version);
        Assert.False(string.IsNullOrWhiteSpace(info.FrameworkDescription));
        Assert.False(string.IsNullOrWhiteSpace(info.OperatingSystem));
        Assert.False(string.IsNullOrWhiteSpace(info.Architecture));
    }

    [Fact]
    public void EnsureSupported_DoesNotThrow_OnNet8()
    {
        // The test runner executes on the same runtime as the code under test. If the
        // runner is misconfigured the guard will throw, which is exactly the behaviour
        // we need to verify for ADR-001 enforcement.
        DotNetRuntimeBaseline.EnsureSupported();
    }

    [Fact]
    public void IsSupported_ReturnsReason_ForUnsupportedFramework()
    {
        var info = new RuntimeBaselineInfo(new Version(8, 0), "Mono 8.0", "Linux", "X64");

        var supported = DotNetRuntimeBaseline.IsSupported(info, out var reason);

        Assert.False(supported);
        Assert.Equal("Runtime framework must be .NET", reason);
    }

    [Fact]
    public void EnsureSupported_WithWrongVersion_Throws()
    {
        var info = new RuntimeBaselineInfo(new Version(7, 0), ".NET 7.0.0", "Linux", "X64");

        var exception = Assert.Throws<PlatformNotSupportedException>(() => DotNetRuntimeBaseline.EnsureSupported(info));
        Assert.Contains("Runtime major version must be 8", exception.Message);
    }
}
