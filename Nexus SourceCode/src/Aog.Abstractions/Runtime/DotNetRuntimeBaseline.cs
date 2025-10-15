using System;
using System.Runtime.InteropServices;

namespace Aog.Abstractions.Runtime;

/// <summary>
/// Provides guard rails that ensure Nexus components run on the supported .NET runtime.
/// </summary>
public static class DotNetRuntimeBaseline
{
    private const int SupportedMajorVersion = 8;
    private const string SupportedFrameworkName = ".NET";

    /// <summary>
    /// Gets information about the current runtime that can be reported in diagnostics.
    /// </summary>
    public static RuntimeBaselineInfo GetCurrentRuntimeInfo()
    {
        return new RuntimeBaselineInfo(
            Environment.Version,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString());
    }

    /// <summary>
    /// Throws when the active runtime does not match the supported baseline from ADR-001.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Raised when the runtime is not .NET 8.</exception>
    public static void EnsureSupported()
    {
        EnsureSupported(GetCurrentRuntimeInfo());
    }

    /// <summary>
    /// Throws when the provided runtime information does not meet the baseline requirements.
    /// </summary>
    /// <param name="info">Runtime characteristics to validate.</param>
    /// <exception cref="PlatformNotSupportedException">Raised when <paramref name="info"/> does not describe a supported runtime.</exception>
    public static void EnsureSupported(RuntimeBaselineInfo info)
    {
        if (!IsSupported(info, out var reason))
        {
            Throw(info, reason ?? "Runtime does not meet baseline requirements.");
        }
    }

    /// <summary>
    /// Determines whether the provided runtime characteristics comply with the supported baseline.
    /// </summary>
    /// <param name="info">Runtime characteristics to evaluate.</param>
    /// <param name="reason">Populated with a human readable rejection reason when unsupported.</param>
    /// <returns><see langword="true"/> when the runtime matches the baseline; otherwise <see langword="false"/>.</returns>
    public static bool IsSupported(RuntimeBaselineInfo info, out string? reason)
    {
        if (!info.FrameworkDescription.StartsWith(SupportedFrameworkName, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Runtime framework must be .NET";
            return false;
        }

        if (info.Version.Major != SupportedMajorVersion)
        {
            reason = $"Runtime major version must be {SupportedMajorVersion}";
            return false;
        }

        reason = null;
        return true;
    }

    private static void Throw(RuntimeBaselineInfo info, string reason)
    {
        var message =
            $"{reason}. Detected runtime: {info.FrameworkDescription} ({info.Version}) on {info.OperatingSystem} {info.Architecture}.";
        throw new PlatformNotSupportedException(message);
    }
}

/// <summary>
/// Captures information about the runtime Nexus is executing on.
/// </summary>
/// <param name="Version">The runtime version as reported by <see cref="Environment.Version"/>.</param>
/// <param name="FrameworkDescription">Description of the runtime framework.</param>
/// <param name="OperatingSystem">Host operating system description.</param>
/// <param name="Architecture">Process architecture.</param>
public sealed record RuntimeBaselineInfo(Version Version, string FrameworkDescription, string OperatingSystem, string Architecture);
