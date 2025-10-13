namespace Aog.Agio.Gnss;

/// <summary>
/// Represents how a position source run terminated.
/// </summary>
public enum PositionSourceOutcome
{
    /// <summary>
    /// The source completed its work normally.
    /// </summary>
    Completed,

    /// <summary>
    /// The source was cancelled by the caller.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The source faulted and should be retried or replaced.
    /// </summary>
    Faulted,
}
