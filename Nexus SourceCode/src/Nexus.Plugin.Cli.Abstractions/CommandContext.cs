using System.CommandLine;

namespace Nexus.Plugin.Cli.Abstractions;

/// <summary>
/// Provides services for configuring commands contributed by CLI modules.
/// </summary>
public sealed class CommandContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="rootCommand">The root command used by the Nexus CLI host.</param>
    /// <param name="services">The service provider available to the module.</param>
    /// <param name="outputOption">The shared output mode option exposed by the host, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rootCommand"/> or <paramref name="services"/> is <c>null</c>.</exception>
    public CommandContext(RootCommand rootCommand, IServiceProvider services, Option? outputOption = null)
    {
        RootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        OutputOption = outputOption;
    }

    /// <summary>
    /// Gets the root command of the Nexus CLI host.
    /// </summary>
    public RootCommand RootCommand { get; }

    /// <summary>
    /// Gets the service provider available to the module.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the shared output mode option exposed by the host, when available.
    /// </summary>
    public Option? OutputOption { get; }
}
