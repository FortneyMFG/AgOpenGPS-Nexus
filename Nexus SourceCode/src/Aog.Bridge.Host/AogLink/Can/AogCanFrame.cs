namespace Aog.Bridge.Host.AogLink.Can;

/// <summary>
/// Represents a CAN or CAN-FD frame used by the AOG-Link transport.
/// </summary>
public readonly record struct AogCanFrame(uint Identifier, byte[] Data, bool IsExtendedId = true, bool IsFd = true);
