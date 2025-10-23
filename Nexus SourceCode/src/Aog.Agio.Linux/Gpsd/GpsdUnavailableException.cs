namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Represents an error raised when gpsd cannot be reached.
/// </summary>
public sealed class GpsdUnavailableException : GpsdSocketUnavailableException
{
    public GpsdUnavailableException(string message)
        : base(message)
    {
    }

    public GpsdUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
