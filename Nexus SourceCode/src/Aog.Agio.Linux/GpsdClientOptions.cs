namespace Aog.Agio.Linux;

/// <summary>
/// Configuration options for connecting to gpsd.
/// </summary>
public sealed class GpsdClientOptions
{
    private string _socketPath = "/var/run/gpsd.sock";

    /// <summary>
    /// Gets or sets the Unix domain socket path used to reach gpsd.
    /// </summary>
    public string SocketPath
    {
        get => _socketPath;
        set => _socketPath = string.IsNullOrWhiteSpace(value) ? _socketPath : value;
    }
}
