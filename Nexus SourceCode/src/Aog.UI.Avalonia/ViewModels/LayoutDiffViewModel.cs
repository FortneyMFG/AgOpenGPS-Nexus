using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presents differences between a baseline layout and a candidate update, enabling operators to
/// review changes and queue rollbacks as described in ADR-032.
/// </summary>
public sealed class LayoutDiffViewModel : ObservableObject
{
    private readonly ObservableCollection<LayoutDiffEntryViewModel> _changes;
    private readonly ReadOnlyObservableCollection<LayoutDiffEntryViewModel> _readonlyChanges;
    private readonly DelegateCommand _applyCandidateCommand;
    private readonly DelegateCommand _rollbackCommand;

    private string? _statusMessage;
    private bool _isRollbackPending;

    public LayoutDiffViewModel(
        string layoutDisplayName,
        LayoutVersionViewModel baseline,
        LayoutVersionViewModel candidate,
        IEnumerable<LayoutDiffEntryViewModel> changes,
        string? linkedPresetName = null)
    {
        LayoutDisplayName = layoutDisplayName ?? throw new ArgumentNullException(nameof(layoutDisplayName));
        Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        LinkedPresetName = linkedPresetName;

        ArgumentNullException.ThrowIfNull(changes);
        _changes = new ObservableCollection<LayoutDiffEntryViewModel>(changes.ToList());
        _readonlyChanges = new ReadOnlyObservableCollection<LayoutDiffEntryViewModel>(_changes);

        _applyCandidateCommand = new DelegateCommand(_ => ApplyCandidate(), _ => !IsRollbackPending);
        _rollbackCommand = new DelegateCommand(_ => QueueRollback(), _ => !IsRollbackPending && !IsIdenticalVersion);

        StatusMessage = "Review layout changes before applying to live sessions.";
    }

    /// <summary>Gets the user-facing layout name.</summary>
    public string LayoutDisplayName { get; }

    /// <summary>Gets the preset tied to this layout, if any.</summary>
    public string? LinkedPresetName { get; }

    /// <summary>Gets a value indicating whether a preset is linked.</summary>
    public bool HasLinkedPreset => !string.IsNullOrWhiteSpace(LinkedPresetName);

    /// <summary>Gets the baseline layout metadata.</summary>
    public LayoutVersionViewModel Baseline { get; }

    /// <summary>Gets the candidate layout metadata being compared.</summary>
    public LayoutVersionViewModel Candidate { get; }

    /// <summary>Gets the diff entries describing widget changes.</summary>
    public ReadOnlyObservableCollection<LayoutDiffEntryViewModel> Changes => _readonlyChanges;

    /// <summary>Gets a value indicating whether any changes are tracked.</summary>
    public bool HasChanges => _readonlyChanges.Count > 0;

    /// <summary>Gets a value indicating whether any high-impact changes were detected.</summary>
    public bool HasBreakingChanges => _readonlyChanges.Any(change => change.Impact == LayoutDiffImpact.High);

    /// <summary>Gets the number of high-impact changes.</summary>
    public int BreakingChangeCount => _readonlyChanges.Count(change => change.Impact == LayoutDiffImpact.High);

    /// <summary>Gets a short summary comparing versions.</summary>
    public string ComparisonSummary => $"{Candidate.VersionLabel} vs {Baseline.VersionLabel}";

    /// <summary>Gets a status message summarising the latest operator action.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets a value indicating whether a rollback has been queued.</summary>
    public bool IsRollbackPending
    {
        get => _isRollbackPending;
        private set
        {
            if (SetProperty(ref _isRollbackPending, value))
            {
                _applyCandidateCommand.RaiseCanExecuteChanged();
                _rollbackCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the command that applies the candidate layout.</summary>
    public DelegateCommand ApplyCandidateCommand => _applyCandidateCommand;

    /// <summary>Gets the command that queues a rollback to the baseline.</summary>
    public DelegateCommand RollbackCommand => _rollbackCommand;

    /// <summary>Applies the candidate layout, clearing any pending rollbacks.</summary>
    public void ApplyCandidate()
    {
        IsRollbackPending = false;
        StatusMessage = $"Applied {Candidate.VersionLabel} ({Candidate.VersionHashShort}).";
    }

    /// <summary>Queues a rollback to the baseline layout.</summary>
    public void QueueRollback()
    {
        if (IsRollbackPending)
        {
            return;
        }

        IsRollbackPending = true;
        StatusMessage = $"Rollback to {Baseline.VersionLabel} queued for deployment.";
    }

    /// <summary>Marks the rollback as complete.</summary>
    public void CompleteRollback(string? message = null)
    {
        if (!IsRollbackPending)
        {
            return;
        }

        IsRollbackPending = false;
        StatusMessage = message ?? $"Rollback to {Baseline.VersionLabel} complete.";
    }

    private bool IsIdenticalVersion => string.Equals(Baseline.VersionHash, Candidate.VersionHash, StringComparison.Ordinal);

    /// <summary>Creates sample data aligning with ADR-032 for design-time usage.</summary>
    public static LayoutDiffViewModel CreateSample()
    {
        var baseline = new LayoutVersionViewModel(
            versionLabel: "Season baseline",
            versionHash: "a1f5c9d2",
            author: "M. Ortega",
            updatedAtUtc: DateTime.SpecifyKind(new DateTime(2024, 1, 3, 18, 45, 0), DateTimeKind.Utc),
            isLiveLink: true,
            summary: "Organization-wide planter layout baseline with diagnostics.");

        var candidate = new LayoutVersionViewModel(
            versionLabel: "Planter – 16 Row",
            versionHash: "d42be7c9",
            author: "K. Patel",
            updatedAtUtc: DateTime.SpecifyKind(new DateTime(2024, 1, 6, 9, 30, 0), DateTimeKind.Utc),
            isLiveLink: true,
            summary: "Operator tweaks after headland calibration and PID retune.");

        var changes = new[]
        {
            new LayoutDiffEntryViewModel(
                elementName: "Autosteer dashboard",
                changeKind: LayoutDiffChangeKind.Modified,
                impact: LayoutDiffImpact.High,
                description: "PID gauges moved to screen 2 and saturation raised to 90°.",
                rollbackNote: "Rollback restores gauge placement and PID thresholds."),
            new LayoutDiffEntryViewModel(
                elementName: "Section control heatmap",
                changeKind: LayoutDiffChangeKind.Modified,
                impact: LayoutDiffImpact.Medium,
                description: "Color ramp adjusted to highlight overlap by +15%."),
            new LayoutDiffEntryViewModel(
                elementName: "Task progress widget",
                changeKind: LayoutDiffChangeKind.Removed,
                impact: LayoutDiffImpact.Low,
                description: "Widget removed from right column to surface controller diagnostics.")
        };

        var viewModel = new LayoutDiffViewModel(
            layoutDisplayName: "Planter productivity",
            baseline,
            candidate,
            changes,
            linkedPresetName: "Planter – 16 Row");

        viewModel.StatusMessage = "High-impact changes require review before publishing.";
        return viewModel;
    }
}
