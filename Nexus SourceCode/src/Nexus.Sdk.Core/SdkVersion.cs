namespace Nexus.Sdk.Core;

/// <summary>
/// Provides the version of the Nexus SDK consumed by plugins.
/// </summary>
public static class SdkVersion
{
    /// <summary>
    /// The current Nexus SDK version exposed to plugins.
    /// </summary>
    public static readonly Version Current = new(1, 0, 0);
}
