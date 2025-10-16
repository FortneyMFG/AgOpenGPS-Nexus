namespace Nexus.Cli.Host.Core.Endpoints;

public interface ICoreEndpointResolver
{
    Task<CoreEndpointResolution> ResolveAsync(string? endpointOverride, CancellationToken cancellationToken);
}
