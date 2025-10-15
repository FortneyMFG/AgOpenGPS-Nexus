using System;

namespace Aog.Agio.Corrections;

/// <summary>
/// Encapsulates the terminal outcome of a correction source run.
/// </summary>
public sealed record CorrectionSourceResult
{
    private CorrectionSourceResult(CorrectionSourceOutcome outcome, Exception? error)
    {
        Outcome = outcome;
        Error = error;
    }

    /// <summary>
    /// Gets how the source run terminated.
    /// </summary>
    public CorrectionSourceOutcome Outcome { get; }

    /// <summary>
    /// Gets the exception associated with a faulted outcome.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Creates a successful completion result.
    /// </summary>
    public static CorrectionSourceResult Completed() => new(CorrectionSourceOutcome.Completed, null);

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static CorrectionSourceResult Cancelled() => new(CorrectionSourceOutcome.Cancelled, null);

    /// <summary>
    /// Creates a faulted result.
    /// </summary>
    /// <param name="error">Exception that caused the fault.</param>
    public static CorrectionSourceResult Faulted(Exception error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new CorrectionSourceResult(CorrectionSourceOutcome.Faulted, error);
    }
}
