using System;

namespace Aog.Plugins.Sections;

/// <summary>
/// Implements guard rails for CAN/UDP transport of variable-rate prescriptions per ADR-016.
/// </summary>
public sealed class VariableRateTransportGuard
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _heartbeatTimeout;
    private string? _expectedLayerId;
    private string? _expectedHash;
    private DateTimeOffset? _lastHeartbeat;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRateTransportGuard"/> class.
    /// </summary>
    /// <param name="heartbeatTimeout">Maximum allowed interval between heartbeats (defaults to 300 ms).</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public VariableRateTransportGuard(TimeSpan? heartbeatTimeout = null, TimeProvider? timeProvider = null)
    {
        _heartbeatTimeout = heartbeatTimeout ?? TimeSpan.FromMilliseconds(300);
        if (_heartbeatTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatTimeout), "Heartbeat timeout must be positive.");
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Arms the guard with the expected layer identifier and registry hash.
    /// </summary>
    public void Arm(string layerId, string registryHash)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(registryHash))
        {
            throw new ArgumentException("Registry hash is required.", nameof(registryHash));
        }

        _expectedLayerId = layerId.Trim();
        _expectedHash = registryHash.Trim();
        _lastHeartbeat = _timeProvider.GetUtcNow();
    }

    /// <summary>
    /// Clears the guard state and stops monitoring heartbeats.
    /// </summary>
    public void Disarm()
    {
        _expectedLayerId = null;
        _expectedHash = null;
        _lastHeartbeat = null;
    }

    /// <summary>
    /// Ensures the last recorded heartbeat remains within the allowed timeout window.
    /// </summary>
    /// <param name="now">Optional timestamp used instead of <see cref="TimeProvider"/>.</param>
    public void EnsureHeartbeatFresh(DateTimeOffset? now = null)
    {
        if (_expectedLayerId is null)
        {
            return;
        }

        var reference = now ?? _timeProvider.GetUtcNow();
        if (!_lastHeartbeat.HasValue || reference - _lastHeartbeat.Value > _heartbeatTimeout)
        {
            throw new InvalidOperationException("Variable-rate transport heartbeat expired; entering failsafe per ADR-016.");
        }
    }

    /// <summary>
    /// Records a heartbeat after verifying the layer identity and registry hash.
    /// </summary>
    /// <param name="layerId">Layer identifier advertised by firmware.</param>
    /// <param name="registryHash">Registry hash advertised by firmware.</param>
    /// <param name="timestampUtc">Optional timestamp overriding <see cref="TimeProvider"/>.</param>
    public void RecordHeartbeat(string layerId, string registryHash, DateTimeOffset? timestampUtc = null)
    {
        if (_expectedLayerId is null || _expectedHash is null)
        {
            throw new InvalidOperationException("Transport guard has not been armed.");
        }

        if (!string.Equals(_expectedLayerId, layerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Firmware layer handshake mismatch. Expected '{_expectedLayerId}', received '{layerId}'.");
        }

        if (!string.Equals(_expectedHash, registryHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Firmware registry hash mismatch. Guard rails require hash parity per ADR-016.");
        }

        _lastHeartbeat = timestampUtc ?? _timeProvider.GetUtcNow();
    }
}
