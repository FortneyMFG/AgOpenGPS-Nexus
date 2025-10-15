using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Aog.Core.Reporting;
using Aog.UI.Avalonia.Reporting;
using DelegateCommand = Aog.UI.Avalonia.ViewModels.DelegateCommand;

namespace Aog.UI.Avalonia.ViewModels.Reporting;

/// <summary>
/// Presents report generation results for preview and sharing flows.
/// </summary>
public sealed class ReportPreviewViewModel : INotifyPropertyChanged
{
    private readonly IReportShareService _shareService;
    private readonly DelegateCommand _shareCommand;

    private ReportGenerationResult? _currentResult;
    private string _templateName = string.Empty;
    private string _templateVersion = string.Empty;
    private string _scopeDisplay = string.Empty;
    private DateTimeOffset? _generatedAt;
    private IReadOnlyList<ReportPreviewSectionViewModel> _sections = Array.Empty<ReportPreviewSectionViewModel>();
    private IReadOnlyList<ReportPreviewOutputViewModel> _outputs = Array.Empty<ReportPreviewOutputViewModel>();
    private string _auditSummary = "Generate a report to preview exports.";
    private string? _statusMessage;
    private bool _hasError;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportPreviewViewModel"/> class.
    /// </summary>
    /// <param name="shareService">Service used to distribute generated reports.</param>
    public ReportPreviewViewModel(IReportShareService shareService)
    {
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _shareCommand = new DelegateCommand(_ => ShareReport(), _ => _currentResult is { Artifacts.Count: > 0 });
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the template name associated with the current report.</summary>
    public string TemplateName
    {
        get => _templateName;
        private set => SetProperty(ref _templateName, value);
    }

    /// <summary>Gets the template version associated with the current report.</summary>
    public string TemplateVersion
    {
        get => _templateVersion;
        private set => SetProperty(ref _templateVersion, value);
    }

    /// <summary>Gets a display string describing the report scope.</summary>
    public string ScopeDisplay
    {
        get => _scopeDisplay;
        private set => SetProperty(ref _scopeDisplay, value);
    }

    /// <summary>Gets the UTC timestamp when the report was generated.</summary>
    public DateTimeOffset? GeneratedAt
    {
        get => _generatedAt;
        private set => SetProperty(ref _generatedAt, value);
    }

    /// <summary>Gets the sections included in the report.</summary>
    public IReadOnlyList<ReportPreviewSectionViewModel> Sections
    {
        get => _sections;
        private set => SetProperty(ref _sections, value);
    }

    /// <summary>Gets the available outputs for the report.</summary>
    public IReadOnlyList<ReportPreviewOutputViewModel> Outputs
    {
        get => _outputs;
        private set => SetProperty(ref _outputs, value);
    }

    /// <summary>Gets the audit summary generated for diffing purposes.</summary>
    public string AuditSummary
    {
        get => _auditSummary;
        private set => SetProperty(ref _auditSummary, value);
    }

    /// <summary>Gets a status message describing the latest operation.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    /// <summary>Gets a value indicating whether the latest status represents an error.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>Gets a value indicating whether a status message is available.</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>Gets a value indicating whether a report has been loaded.</summary>
    public bool HasReport => _currentResult is not null;

    /// <summary>Gets the total number of artifacts attached to the loaded report.</summary>
    public int TotalArtifacts => _currentResult?.Artifacts.Count ?? 0;

    /// <summary>Gets the command that shares the active report.</summary>
    public DelegateCommand ShareCommand => _shareCommand;

    /// <summary>
    /// Loads the supplied report generation result into the view-model.
    /// </summary>
    public void Load(ReportGenerationResult result)
    {
        _currentResult = result ?? throw new ArgumentNullException(nameof(result));

        TemplateName = result.Template.Name;
        TemplateVersion = result.Template.Version;
        ScopeDisplay = $"{result.Scope.Kind} — {result.Scope.Identifier}";
        GeneratedAt = result.GeneratedAt.ToUniversalTime();
        Sections = result.Sections
            .OrderBy(section => section.SectionId, StringComparer.OrdinalIgnoreCase)
            .Select(section => new ReportPreviewSectionViewModel(section))
            .ToArray();
        Outputs = BuildOutputs(result);
        AuditSummary = ReportExportAuditor.GenerateSummary(result);
        HasError = false;
        StatusMessage = "Report ready to share.";

        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(TotalArtifacts));
        _shareCommand.RaiseCanExecuteChanged();
    }

    private void ShareReport()
    {
        if (_currentResult is null)
        {
            HasError = true;
            StatusMessage = "Generate a report before sharing.";
            return;
        }

        try
        {
            _shareService.ShareAsync(_currentResult, CancellationToken.None).GetAwaiter().GetResult();
            HasError = false;
            StatusMessage = "Report shared successfully.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to share report: {ex.Message}";
        }
    }

    private IReadOnlyList<ReportPreviewOutputViewModel> BuildOutputs(ReportGenerationResult result)
    {
        var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artifact in result.Artifacts)
        {
            if (!string.IsNullOrWhiteSpace(artifact.Format))
            {
                formats.Add(artifact.Format);
            }
        }

        var requested = ParseRequestedFormats(result.Metadata);
        var declaredCounts = ParseDeclaredCounts(result.Metadata);
        var declaredNames = ParseDeclaredNames(result.Metadata);

        foreach (var format in requested)
        {
            formats.Add(format);
        }

        foreach (var format in declaredCounts.Keys)
        {
            formats.Add(format);
        }

        foreach (var format in declaredNames.Keys)
        {
            formats.Add(format);
        }

        var actualCounts = result.Artifacts
            .GroupBy(artifact => artifact.Format, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var actualNames = result.Artifacts
            .GroupBy(artifact => artifact.Format, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(artifact => artifact.Name ?? string.Empty).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var outputs = new List<ReportPreviewOutputViewModel>();
        foreach (var format in formats.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            actualCounts.TryGetValue(format, out var producedCount);
            actualNames.TryGetValue(format, out var names);
            declaredCounts.TryGetValue(format, out var declaredCount);

            if (names is null && declaredNames.TryGetValue(format, out var declaredList))
            {
                names = declaredList;
            }

            outputs.Add(new ReportPreviewOutputViewModel(
                format,
                requested.Contains(format),
                producedCount,
                names ?? Array.Empty<string>(),
                declaredCount));
        }

        return outputs;
    }

    private static HashSet<string> ParseRequestedFormats(IReadOnlyDictionary<string, string> metadata)
    {
        var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!pair.Key.StartsWith("requestedOutput.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var format = pair.Key["requestedOutput.".Length..];
            if (!string.IsNullOrWhiteSpace(format) &&
                string.Equals(pair.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                formats.Add(format);
            }
        }

        return formats;
    }

    private static Dictionary<string, int> ParseDeclaredCounts(IReadOnlyDictionary<string, string> metadata)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!pair.Key.StartsWith("outputs.produced.", StringComparison.OrdinalIgnoreCase) ||
                !pair.Key.EndsWith(".count", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var format = pair.Key["outputs.produced.".Length..];
            var dotIndex = format.LastIndexOf('.');
            if (dotIndex <= 0)
            {
                continue;
            }

            format = format[..dotIndex];
            if (string.IsNullOrWhiteSpace(format))
            {
                continue;
            }

            if (int.TryParse(pair.Value, out var count))
            {
                counts[format] = count;
            }
        }

        return counts;
    }

    private static Dictionary<string, string[]> ParseDeclaredNames(IReadOnlyDictionary<string, string> metadata)
    {
        var names = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!pair.Key.StartsWith("outputs.produced.", StringComparison.OrdinalIgnoreCase) ||
                !pair.Key.EndsWith(".names", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var format = pair.Key["outputs.produced.".Length..];
            var dotIndex = format.LastIndexOf('.');
            if (dotIndex <= 0)
            {
                continue;
            }

            format = format[..dotIndex];
            if (string.IsNullOrWhiteSpace(format))
            {
                continue;
            }

            var tokens = pair.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            names[format] = tokens;
        }

        return names;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
