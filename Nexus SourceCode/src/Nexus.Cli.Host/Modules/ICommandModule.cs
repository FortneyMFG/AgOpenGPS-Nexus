using Nexus.Cli.Host.Output;

namespace Nexus.Cli.Host.Modules;

public interface ICommandModule
{
    void Configure(CommandModuleContext context);
}
