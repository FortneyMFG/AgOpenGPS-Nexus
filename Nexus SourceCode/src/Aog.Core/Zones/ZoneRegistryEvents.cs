namespace Aog.Core.Zones;

/// <summary>
/// Published when the zone registry changes.
/// </summary>
/// <param name="Delta">Delta describing the registry mutation.</param>
public sealed record ZoneRegistryDeltaEvent(ZoneStoreDelta Delta);
