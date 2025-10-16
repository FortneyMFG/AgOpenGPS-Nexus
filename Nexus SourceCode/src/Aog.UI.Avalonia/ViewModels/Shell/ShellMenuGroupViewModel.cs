using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a top-level menu grouping such as File, Field, or Tools.
/// </summary>
public sealed class ShellMenuGroupViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellMenuGroupViewModel"/> class.
    /// </summary>
    /// <param name="header">Menu header text.</param>
    /// <param name="items">Child menu items.</param>
    public ShellMenuGroupViewModel(string header, IEnumerable<ShellMenuItemViewModel> items)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Items = new ReadOnlyObservableCollection<ShellMenuItemViewModel>(
            new ObservableCollection<ShellMenuItemViewModel>(items?.ToList() ?? throw new ArgumentNullException(nameof(items))));
    }

    /// <summary>Gets the menu header text.</summary>
    public string Header { get; }

    /// <summary>Gets the menu items contained within the group.</summary>
    public ReadOnlyObservableCollection<ShellMenuItemViewModel> Items { get; }
}
