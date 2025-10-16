namespace Nexus.Cli.Host.Core.Endpoints;

public sealed class CoreEndpointResolution
{
    public CoreEndpointResolution(IReadOnlyList<CoreEndpoint> candidates, string strategy)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one endpoint candidate is required.", nameof(candidates));
        }

        Candidates = candidates;
        Strategy = strategy ?? string.Empty;
    }

    public IReadOnlyList<CoreEndpoint> Candidates { get; }

    public string Strategy { get; }
}
