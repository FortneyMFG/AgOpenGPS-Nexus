using Nexus.Cli.Host.Core.Endpoints;

namespace Nexus.Cli.Host.Core.Status;

public sealed record CoreStatusResult(
    CoreEndpoint Endpoint,
    CoreStatusState State,
    TimeSpan? Latency,
    string? Message,
    DateTimeOffset CheckedAt);
