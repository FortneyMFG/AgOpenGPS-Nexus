namespace Nexus.Sdk.AgIo;

/// <summary>
/// Describes an AgIO sidecar process contributed by a plugin.
/// </summary>
/// <param name="PluginId">Identifier of the plugin that owns the sidecar.</param>
/// <param name="ExecutablePath">Relative path to the executable inside the plugin package.</param>
/// <param name="Arguments">Command line arguments provided by the host.</param>
/// <param name="Environment">Environment variables to apply when spawning the sidecar.</param>
public sealed record AgIoSidecarDescriptor(
    string PluginId,
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment);
