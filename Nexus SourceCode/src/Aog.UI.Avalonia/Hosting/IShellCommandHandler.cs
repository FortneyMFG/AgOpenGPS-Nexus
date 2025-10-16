using System.Threading;
using System.Threading.Tasks;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Handles shell command invocations for a particular injection point.
/// </summary>
public interface IShellCommandHandler
{
    /// <summary>
    /// Determines whether the handler can process commands emitted from the specified injection point.
    /// </summary>
    /// <param name="injectionPoint">Injection point identifier.</param>
    /// <returns><c>true</c> when the handler can process commands.</returns>
    bool CanHandle(string injectionPoint);

    /// <summary>
    /// Attempts to process the provided command.
    /// </summary>
    /// <param name="injectionPoint">Injection point that triggered the command.</param>
    /// <param name="commandId">Identifier of the command.</param>
    /// <param name="cancellationToken">Cancellation token propagated to the handler.</param>
    /// <returns><c>true</c> when the command was handled.</returns>
    ValueTask<bool> HandleAsync(string injectionPoint, string commandId, CancellationToken cancellationToken);
}
