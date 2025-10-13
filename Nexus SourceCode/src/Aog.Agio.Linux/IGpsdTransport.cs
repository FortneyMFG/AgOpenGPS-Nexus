namespace Aog.Agio.Linux;

/// <summary>
/// Creates gpsd sessions.
/// </summary>
public interface IGpsdTransport
{
    /// <summary>
    /// Connects to gpsd and returns an active session.
    /// </summary>
    Task<IGpsdSession> ConnectAsync(CancellationToken cancellationToken);
}
