namespace Nexus.Sdk.Core;

/// <summary>
/// Sends commands to registered handlers.
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Sends a command to the host and awaits completion.
    /// </summary>
    /// <typeparam name="TCommand">Command payload type.</typeparam>
    /// <param name="command">Command instance.</param>
    /// <param name="cancellationToken">Token used to cancel the command.</param>
    /// <returns>A task that completes once the command is processed.</returns>
    ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : notnull;
}
