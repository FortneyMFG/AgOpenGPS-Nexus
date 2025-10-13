using System.IO;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Creates connections to the gpsd daemon.
/// </summary>
public interface IGpsdConnectionFactory
{
    /// <summary>
    /// Attempts to create a stream connected to gpsd.
    /// Returns <c>null</c> when gpsd is not available.
    /// </summary>
    Task<Stream?> ConnectAsync(CancellationToken cancellationToken);
}
