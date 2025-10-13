namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Indicates that the gpsd socket could not be opened.
/// </summary>
public sealed class GpsdSocketUnavailableException : Exception
{
    public GpsdSocketUnavailableException(string message)
        : base(message)
    {
    }

    public GpsdSocketUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
