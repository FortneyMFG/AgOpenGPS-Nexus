using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Aog.UI.Avalonia.Layout;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Represents the editable overlay for an anchored workspace panel.
/// </summary>
public sealed class PanelOverlayViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, string> PanelTitles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["panel.map"] = "Main Display Panel",
        };

    private readonly PanelSpec _spec;
    private readonly BlockLayoutViewModel _owner;
    private Rect _bounds;
    private bool _isLocked;

    public PanelOverlayViewModel(PanelSpec spec, BlockLayoutViewModel owner)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _bounds = spec.ToPixelRect(owner.Grid);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the unique identifier for the panel specification.</summary>
    public string Id => _spec.Id;

    /// <summary>Gets the rendered bounds of the overlay in device pixels.</summary>
    public Rect Bounds
    {
        get => _bounds;
        private set
        {
            if (_bounds == value)
            {
                return;
            }

            _bounds = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets a value indicating whether the overlay is currently locked for interaction.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        private set
        {
            if (_isLocked == value)
            {
                return;
            }

            _isLocked = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the human-friendly display name for the panel.</summary>
    public string DisplayName => ResolveDisplayName();

    internal PanelSpec Spec => _spec;

    internal BlockLayoutViewModel Owner => _owner;

    /// <summary>Recomputes the overlay bounds from the underlying specification.</summary>
    public void RefreshBounds()
    {
        Bounds = _owner?.Grid is { } layout ? _spec.ToPixelRect(layout) : default;
        OnPropertyChanged(nameof(DisplayName));
    }

    /// <summary>Sets the lock state reflecting whether layout edits are allowed.</summary>
    /// <param name="isLocked">True when the layout is locked.</param>
    public void SetLockState(bool isLocked)
    {
        IsLocked = isLocked;
    }

    /// <summary>Requests that the owning layout apply the supplied bounds.</summary>
    /// <param name="bounds">The desired bounds.</param>
    public void UpdateBounds(Rect bounds)
    {
        _owner.UpdatePanelBounds(this, bounds);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string ResolveDisplayName()
    {
        if (PanelTitles.TryGetValue(_spec.Id, out var title))
        {
            return title;
        }

        var id = _spec.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return "Workspace Panel";
        }

        var token = id.Contains('.') ? id[(id.LastIndexOf('.') + 1)..] : id;
        token = token.Replace('_', ' ').Replace('-', ' ');
        if (string.IsNullOrWhiteSpace(token))
        {
            return "Workspace Panel";
        }

        return char.ToUpperInvariant(token[0]) + token[1..];
    }
}
