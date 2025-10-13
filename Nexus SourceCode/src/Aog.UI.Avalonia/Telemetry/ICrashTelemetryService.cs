using System.Collections.Generic;

namespace Aog.UI.Avalonia.Telemetry;

/// <summary>
/// Provides access to crash reports and telemetry opt-in state.
/// </summary>
public interface ICrashTelemetryService
{
    /// <summary>Gets the current telemetry state snapshot.</summary>
    CrashTelemetryState GetState();

    /// <summary>Updates the telemetry opt-in flag.</summary>
    /// <param name="isOptedIn">Whether telemetry uploads are enabled.</param>
    void SetTelemetryOptIn(bool isOptedIn);

    /// <summary>Deletes all pending crash reports.</summary>
    void ClearPendingReports();

    /// <summary>Marks pending crash reports as uploaded.
    /// The default implementation stores them in an "uploaded" directory
    /// to provide an audit trail without transmitting data.
    /// </summary>
    /// <returns>A list of reports that were marked as uploaded.</returns>
    IReadOnlyList<CrashReportSummary> UploadPendingReports();
}
