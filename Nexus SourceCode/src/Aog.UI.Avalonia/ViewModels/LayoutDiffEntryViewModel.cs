using System;

namespace Aog.UI.Avalonia.ViewModels;

public enum LayoutDiffChangeKind
{
    Added,
    Modified,
    Removed
}

public enum LayoutDiffImpact
{
    Low,
    Medium,
    High
}

public sealed class LayoutDiffEntryViewModel
{
    public LayoutDiffEntryViewModel(
        string elementName,
        LayoutDiffChangeKind changeKind,
        LayoutDiffImpact impact,
        string description,
        string? rollbackNote = null)
    {
        ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ChangeKind = changeKind;
        Impact = impact;
        RollbackNote = rollbackNote;
    }

    public string ElementName { get; }

    public LayoutDiffChangeKind ChangeKind { get; }

    public LayoutDiffImpact Impact { get; }

    public string Description { get; }

    public string? RollbackNote { get; }

    public bool HasRollbackNote => !string.IsNullOrWhiteSpace(RollbackNote);

    public string ChangeKindDisplay => ChangeKind switch
    {
        LayoutDiffChangeKind.Added => "Added",
        LayoutDiffChangeKind.Removed => "Removed",
        _ => "Modified"
    };

    public string ImpactDisplay => Impact switch
    {
        LayoutDiffImpact.Low => "Low impact",
        LayoutDiffImpact.Medium => "Medium impact",
        _ => "High impact"
    };

    public bool RequiresReview => Impact != LayoutDiffImpact.Low;
}
