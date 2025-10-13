using System;

namespace Aog.Agio.Gnss;

/// <summary>
/// Encapsulates the terminal outcome of a position source run.
/// </summary>
public sealed record PositionSourceResult
{
    private PositionSourceResult(PositionSourceOutcome outcome, Exception? error)
    {
        Outcome = outcome;
        Error = error;
    }

    /// <summary>
    /// Gets how the source run terminated.
    /// </summary>
    public PositionSourceOutcome Outcome { get; }

    /// <summary>
    /// Gets the exception associated with a faulted outcome.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Creates a successful completion result.
    /// </summary>
    public static PositionSourceResult Completed() => new(PositionSourceOutcome.Completed, null);

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static PositionSourceResult Cancelled() => new(PositionSourceOutcome.Cancelled, null);

    /// <summary>
    /// Creates a faulted result.
    /// </summary>
    /// <param name="error">Exception that caused the fault.</param>
    public static PositionSourceResult Faulted(Exception error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return new PositionSourceResult(PositionSourceOutcome.Faulted, error);
    }
}
