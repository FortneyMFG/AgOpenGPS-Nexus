using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model describing the shell status strip indicators.
/// </summary>
public sealed class StatusStripViewModel
{
    private readonly ReadOnlyObservableCollection<ShellStatusIndicatorViewModel> _indicators;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusStripViewModel"/> class.
    /// </summary>
    /// <param name="indicators">Indicators displayed in the status strip.</param>
    public StatusStripViewModel(IEnumerable<ShellStatusIndicatorViewModel> indicators)
    {
        ArgumentNullException.ThrowIfNull(indicators);
        _indicators = new ReadOnlyObservableCollection<ShellStatusIndicatorViewModel>(
            new ObservableCollection<ShellStatusIndicatorViewModel>(indicators.ToList()));
    }

    /// <summary>Gets the indicators displayed in the status strip.</summary>
    public ReadOnlyObservableCollection<ShellStatusIndicatorViewModel> Indicators => _indicators;
}
