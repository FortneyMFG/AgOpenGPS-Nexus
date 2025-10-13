using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aog.UI.Avalonia.Settings;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.Telemetry;

/// <summary>
/// Captures crash information and manages telemetry opt-in state.
/// </summary>
public sealed class CrashTelemetryService : ICrashTelemetryService, IDisposable
{
    private const string CrashDirectoryName = "crash-reports";
    private const string PendingDirectoryName = "pending";
    private const string UploadedDirectoryName = "uploaded";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<CrashTelemetryService> _logger;
    private readonly IUiPreferencesService _preferencesService;
    private readonly string _pendingDirectory;
    private readonly string _uploadedDirectory;
    private readonly object _gate = new();

    private bool _isTelemetryOptedIn;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrashTelemetryService"/> class.
    /// </summary>
    /// <param name="preferencesService">Service providing persisted UI preferences.</param>
    /// <param name="logger">Logger used for diagnostics.</param>
    public CrashTelemetryService(IUiPreferencesService preferencesService, ILogger<CrashTelemetryService> logger)
    {
        ArgumentNullException.ThrowIfNull(preferencesService);
        ArgumentNullException.ThrowIfNull(logger);

        _preferencesService = preferencesService;
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Path.Combine(appData, "AgOpenGPS", "Nexus", CrashDirectoryName);
        _pendingDirectory = Path.Combine(root, PendingDirectoryName);
        _uploadedDirectory = Path.Combine(root, UploadedDirectoryName);

        Directory.CreateDirectory(_pendingDirectory);
        Directory.CreateDirectory(_uploadedDirectory);

        _isTelemetryOptedIn = _preferencesService.GetPreferences().TelemetryOptIn;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <inheritdoc />
    public CrashTelemetryState GetState()
    {
        lock (_gate)
        {
            return new CrashTelemetryState
            {
                IsTelemetryOptedIn = _isTelemetryOptedIn,
                PendingReports = EnumeratePendingReports().ToArray(),
            };
        }
    }

    /// <inheritdoc />
    public void SetTelemetryOptIn(bool isOptedIn)
    {
        lock (_gate)
        {
            if (_isTelemetryOptedIn == isOptedIn)
            {
                return;
            }

            _isTelemetryOptedIn = isOptedIn;
            _preferencesService.UpdateTelemetryOptIn(isOptedIn);
            _logger.LogInformation("Telemetry opt-in set to {OptedIn}.", isOptedIn);
        }
    }

    /// <inheritdoc />
    public void ClearPendingReports()
    {
        lock (_gate)
        {
            foreach (var file in Directory.EnumerateFiles(_pendingDirectory, "*.json"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Failed to delete crash report {File}.", file);
                }
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CrashReportSummary> UploadPendingReports()
    {
        lock (_gate)
        {
            if (!_isTelemetryOptedIn)
            {
                _logger.LogInformation("Telemetry opt-in disabled; skipping crash report upload.");
                return Array.Empty<CrashReportSummary>();
            }

            var uploaded = new List<CrashReportSummary>();

            foreach (var file in Directory.EnumerateFiles(_pendingDirectory, "*.json"))
            {
                try
                {
                    var summary = ReadSummary(file);
                    var destination = Path.Combine(_uploadedDirectory, Path.GetFileName(file));
                    File.Move(file, destination, overwrite: true);
                    uploaded.Add(summary);
                    _logger.LogInformation("Crash report {File} marked as uploaded.", destination);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                {
                    _logger.LogWarning(ex, "Failed to mark crash report {File} as uploaded.", file);
                }
            }

            return uploaded;
        }
    }

    /// <summary>Disposes the service and detaches event handlers.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            return;
        }

        lock (_gate)
        {
            PersistCrashReport(exception, isTerminating: e.IsTerminating);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        lock (_gate)
        {
            PersistCrashReport(e.Exception.Flatten(), isTerminating: false);
        }

        e.SetObserved();
    }

    private void PersistCrashReport(Exception exception, bool isTerminating)
    {
        try
        {
            var document = new CrashReportDocument
            {
                Timestamp = DateTimeOffset.UtcNow,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = Sanitize(exception.Message),
                StackTrace = Sanitize(exception.StackTrace),
                FrameworkDescription = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                IsTerminating = isTerminating,
            };

            var fileName = $"crash-{document.Timestamp:yyyyMMddHHmmssfffZ}-{Guid.NewGuid():N}.json";
            var path = Path.Combine(_pendingDirectory, fileName);

            using var stream = File.Create(path);
            JsonSerializer.Serialize(stream, document, SerializerOptions);

            _logger.LogWarning("Crash report captured at {Path} for exception {Type}.", path, document.ExceptionType);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex, "Failed to persist crash report: {Message}.", ex.Message);
        }
    }

    private IEnumerable<CrashReportSummary> EnumeratePendingReports()
    {
        foreach (var file in Directory.EnumerateFiles(_pendingDirectory, "*.json"))
        {
            CrashReportSummary? summary = null;
            try
            {
                summary = ReadSummary(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "Failed to read crash report {File}.", file);
            }

            if (summary is not null)
            {
                yield return summary;
            }
        }
    }

    private static CrashReportSummary ReadSummary(string file)
    {
        using var stream = File.OpenRead(file);
        var document = JsonSerializer.Deserialize<CrashReportDocument>(stream, SerializerOptions)
            ?? throw new JsonException($"Crash report at {file} was empty.");

        return new CrashReportSummary
        {
            FileName = Path.GetFileName(file),
            Timestamp = document.Timestamp,
            ExceptionType = document.ExceptionType,
            Message = document.Message,
        };
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ');
        return normalized.Length <= 512 ? normalized : normalized.Substring(0, 512);
    }

    private sealed class CrashReportDocument
    {
        public DateTimeOffset Timestamp { get; set; }

        public string ExceptionType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        public string FrameworkDescription { get; set; } = string.Empty;

        public string OperatingSystem { get; set; } = string.Empty;

        public bool IsTerminating { get; set; }
    }
}
