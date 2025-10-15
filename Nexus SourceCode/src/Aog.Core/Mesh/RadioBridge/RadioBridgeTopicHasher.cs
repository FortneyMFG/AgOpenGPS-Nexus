using System;
using System.Text;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Provides a deterministic hash used to map mesh topics onto compact radio identifiers.
/// The implementation uses the 32-bit FNV-1a hash which provides stable output while
/// remaining lightweight for embedded firmware.
/// </summary>
public static class RadioBridgeTopicHasher
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    /// <summary>
    /// Computes the 32-bit FNV-1a hash for the specified mesh topic.
    /// </summary>
    /// <param name="topic">Mesh topic string.</param>
    /// <returns>Deterministic 32-bit hash.</returns>
    public static uint ComputeHash(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var hash = FnvOffsetBasis;
        var bytes = Encoding.UTF8.GetBytes(topic.Trim());
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return hash;
    }
}
