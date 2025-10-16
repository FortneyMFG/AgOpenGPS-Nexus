using Nexus.Cli.Host.Core.Endpoints;

namespace Nexus.Cli.Host.Core.Status;

public interface ICoreStatusProbe
{
    Task<CoreStatusResult> CheckAsync(CoreEndpoint endpoint, CancellationToken cancellationToken);
}
