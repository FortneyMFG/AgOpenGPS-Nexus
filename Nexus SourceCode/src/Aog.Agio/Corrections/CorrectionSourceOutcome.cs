namespace Aog.Agio.Corrections;

/// <summary>
/// Represents how a correction source run terminated.
/// </summary>
public enum CorrectionSourceOutcome
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
