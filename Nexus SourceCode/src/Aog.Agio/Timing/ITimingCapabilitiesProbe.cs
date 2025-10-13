using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Timing;

/// <summary>
/// Probes the host platform for timing-related capabilities.
/// </summary>
public interface ITimingCapabilitiesProbe
{
    /// <summary>
    /// Probes the host for timing capabilities.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the probe operation.</param>
    /// <returns>A <see cref="TimingCaps"/> message describing the detected capabilities.</returns>
    Task<TimingCaps> ProbeAsync(CancellationToken cancellationToken = default);
}
