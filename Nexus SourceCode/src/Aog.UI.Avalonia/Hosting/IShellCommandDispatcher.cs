using System.Threading;
using System.Threading.Tasks;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Dispatches shell UI commands to registered plugin or core handlers based on injection point metadata.
/// </summary>
public interface IShellCommandDispatcher
{
    /// <summary>
    /// Dispatches a command identified by the provided injection point and command identifier.
    /// </summary>
    /// <param name="injectionPoint">Logical injection point described in <c>ui-to-plugin.yaml</c>.</param>
    /// <param name="commandId">Identifier of the command to execute.</param>
    /// <param name="cancellationToken">Cancellation token propagated to handlers.</param>
    /// <returns><c>true</c> when a handler processed the command.</returns>
    ValueTask<bool> DispatchAsync(string injectionPoint, string commandId, CancellationToken cancellationToken = default);
}
