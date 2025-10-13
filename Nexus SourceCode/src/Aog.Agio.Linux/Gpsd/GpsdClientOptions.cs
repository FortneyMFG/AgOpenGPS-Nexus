namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Configuration for the gpsd client integration.
/// </summary>
public sealed class GpsdClientOptions
{
    private string _socketPath = "/var/run/gpsd.sock";
    private TimeSpan _reconnectDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the unix domain socket path exposed by gpsd.
    /// </summary>
    public string SocketPath
    {
        get => _socketPath;
        set => _socketPath = string.IsNullOrWhiteSpace(value) ? _socketPath : value;
    }

    /// <summary>
    /// Gets or sets the delay applied before retrying when gpsd is unavailable.
    /// </summary>
    public TimeSpan ReconnectDelay
    {
        get => _reconnectDelay;
        set => _reconnectDelay = value <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : value;
    }
}
