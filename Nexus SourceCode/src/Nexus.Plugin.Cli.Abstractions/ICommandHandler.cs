namespace Nexus.Plugin.Cli.Abstractions;

/// <summary>
/// Represents a module that can contribute commands to the Nexus CLI host.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Configures the command tree using the provided context.
    /// </summary>
    /// <param name="context">The module context supplied by the host.</param>
    void Configure(CommandContext context);
}
