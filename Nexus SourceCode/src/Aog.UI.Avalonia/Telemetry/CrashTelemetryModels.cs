using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.Telemetry;

/// <summary>
/// Represents a snapshot of crash telemetry data.
/// </summary>
public sealed class CrashTelemetryState
{
    /// <summary>Gets or sets whether the user has opted into telemetry uploads.</summary>
    public bool IsTelemetryOptedIn { get; set; }

    /// <summary>Gets or sets the pending crash reports awaiting upload.</summary>
    public IReadOnlyList<CrashReportSummary> PendingReports { get; set; } = Array.Empty<CrashReportSummary>();
}

/// <summary>
/// Describes a crash report stored on disk.
/// </summary>
public sealed class CrashReportSummary
{
    /// <summary>Gets or sets the file name backing the crash report.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the crash.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the exception type captured in the report.</summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets the sanitized exception message.</summary>
    public string Message { get; set; } = string.Empty;
}
