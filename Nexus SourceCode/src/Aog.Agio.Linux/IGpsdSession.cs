namespace Aog.Agio.Linux;

/// <summary>
/// Represents an active gpsd session.
/// </summary>
public interface IGpsdSession : IAsyncDisposable
{
    /// <summary>
    /// Sends a command to gpsd.
    /// </summary>
    Task SendAsync(string command, CancellationToken cancellationToken);

    /// <summary>
    /// Streams newline-delimited JSON messages from gpsd.
    /// </summary>
    IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken);
}
