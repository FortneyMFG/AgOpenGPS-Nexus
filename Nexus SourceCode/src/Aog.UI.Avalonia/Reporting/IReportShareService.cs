using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;

namespace Aog.UI.Avalonia.Reporting;

/// <summary>
/// Abstraction responsible for distributing generated report packages.
/// </summary>
public interface IReportShareService
{
    /// <summary>
    /// Shares the supplied report generation result with an external destination.
    /// </summary>
    /// <param name="result">Report result to share.</param>
    /// <param name="cancellationToken">Token used to cancel the share operation.</param>
    Task ShareAsync(ReportGenerationResult result, CancellationToken cancellationToken = default);
}
