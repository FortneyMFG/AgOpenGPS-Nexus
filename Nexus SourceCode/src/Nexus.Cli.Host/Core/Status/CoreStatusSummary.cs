using System;
using System.Linq;
using Nexus.Cli.Host.Core.Endpoints;

namespace Nexus.Cli.Host.Core.Status;

public sealed class CoreStatusSummary
{
    public CoreStatusSummary(CoreEndpointResolution resolution, IReadOnlyList<CoreStatusResult> attempts)
    {
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        Attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
    }

    public CoreEndpointResolution Resolution { get; }

    public IReadOnlyList<CoreStatusResult> Attempts { get; }

    public CoreStatusResult? SuccessfulAttempt => Attempts.LastOrDefault(attempt => attempt.State == CoreStatusState.Healthy);

    public CoreStatusResult FinalAttempt => Attempts.Count > 0 ? Attempts[^1] : new CoreStatusResult(
        Resolution.Candidates[0],
        CoreStatusState.Unknown,
        null,
        "No status attempts were recorded.",
        DateTimeOffset.UtcNow);

    public CoreStatusState FinalState => FinalAttempt.State;
}
