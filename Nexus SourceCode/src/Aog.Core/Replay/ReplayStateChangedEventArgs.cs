using System;

namespace Aog.Core.Replay;

/// <summary>
/// Event arguments raised when the replay controller state changes.
/// </summary>
public sealed class ReplayStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="state">The new controller state.</param>
    public ReplayStateChangedEventArgs(ReplayState state)
    {
        State = state;
    }

    /// <summary>
    /// Gets the updated controller state.
    /// </summary>
    public ReplayState State { get; }
}
