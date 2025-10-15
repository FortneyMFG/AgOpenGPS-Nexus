using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a logical grouping of crop quick-select options.
/// </summary>
public sealed class CropQuickSelectGroupViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CropQuickSelectGroupViewModel"/> class.
    /// </summary>
    /// <param name="title">Title describing the group.</param>
    /// <param name="description">Supporting description providing additional context.</param>
    /// <param name="options">Options contained in the group.</param>
    public CropQuickSelectGroupViewModel(
        string title,
        string description,
        IEnumerable<CropQuickSelectOptionViewModel> options)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Group title is required.", nameof(title));
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();

        ArgumentNullException.ThrowIfNull(options);

        var materialized = new List<CropQuickSelectOptionViewModel>();
        foreach (var option in options)
        {
            if (option is null)
            {
                throw new ArgumentException("Group options cannot contain null entries.", nameof(options));
            }

            materialized.Add(option);
        }

        Options = new ReadOnlyCollection<CropQuickSelectOptionViewModel>(materialized);
    }

    /// <summary>Gets the group title.</summary>
    public string Title { get; }

    /// <summary>Gets the supporting description for the group.</summary>
    public string Description { get; }

    /// <summary>Gets the options belonging to the group.</summary>
    public IReadOnlyList<CropQuickSelectOptionViewModel> Options { get; }
}
