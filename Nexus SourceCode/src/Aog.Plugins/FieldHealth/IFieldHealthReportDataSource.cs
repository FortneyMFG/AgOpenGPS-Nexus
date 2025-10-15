using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Contract for retrieving field health report data for a given scope.
/// </summary>
public interface IFieldHealthReportDataSource
{
    /// <summary>
    /// Retrieves a snapshot of field health data for the requested report scope.
    /// </summary>
    ValueTask<FieldHealthReportSnapshot?> GetSnapshotAsync(ReportScope scope, CancellationToken cancellationToken);
}
