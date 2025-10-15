using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.Plugins.Crop;

/// <summary>
/// Contract for services that provide crop analytics snapshots during report generation.
/// </summary>
public interface ICropAnalyticsProvider
{
    /// <summary>
    /// Retrieves the crop analytics snapshot for the supplied report context.
    /// </summary>
    /// <param name="context">Report generation context requesting analytics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<CropAnalyticsSnapshot?> GetSnapshotAsync(ReportGenerationContext context, CancellationToken cancellationToken);
}
