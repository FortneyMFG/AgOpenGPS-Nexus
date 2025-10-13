namespace Aog.Agio.Linux;

/// <summary>
/// Represents an error raised when gpsd cannot be reached.
/// </summary>
public sealed class GpsdUnavailableException : Exception
{
    public GpsdUnavailableException(string message)
        : base(message)
    {
    }

    public GpsdUnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
