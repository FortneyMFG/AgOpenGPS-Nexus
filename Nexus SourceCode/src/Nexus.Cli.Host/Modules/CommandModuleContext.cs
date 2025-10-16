using System.CommandLine;
using Nexus.Cli.Host.Output;

namespace Nexus.Cli.Host.Modules;

public sealed class CommandModuleContext
{
    public CommandModuleContext(RootCommand rootCommand, IServiceProvider services, Option<OutputFormat> outputOption)
    {
        RootCommand = rootCommand ?? throw new ArgumentNullException(nameof(rootCommand));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        OutputOption = outputOption ?? throw new ArgumentNullException(nameof(outputOption));
    }

    public RootCommand RootCommand { get; }

    public IServiceProvider Services { get; }

    public Option<OutputFormat> OutputOption { get; }
}
