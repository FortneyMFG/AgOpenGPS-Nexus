namespace Aog.Agio;

/// <summary>
/// Captures metadata about the backend selected for the current AGiO host instance.
/// </summary>
/// <param name="AssemblyName">The assembly name used for loading.</param>
/// <param name="TypeName">The fully qualified type name.</param>
/// <param name="BackendType">The runtime type instantiated.</param>
/// <param name="BackendName">The display name reported by the backend.</param>
public sealed record AgioBackendRegistration(
    string AssemblyName,
    string TypeName,
    Type BackendType,
    string BackendName);
