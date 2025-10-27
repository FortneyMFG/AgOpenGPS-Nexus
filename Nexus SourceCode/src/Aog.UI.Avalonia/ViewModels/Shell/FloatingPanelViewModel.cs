using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.ViewModels;
using Avalonia;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Represents a floating panel hosted on the shell canvas.
/// </summary>
public sealed class FloatingPanelViewModel : INotifyPropertyChanged
{
    private readonly FloatingPanelSpec _spec;
    private readonly BlockLayoutViewModel _owner;
    private readonly DelegateCommand _openSettingsCommand;
    private Rect _bounds;
    private bool _isColliding;
    private bool _isLayoutLocked;
    private bool _isVisible;
    private bool _isVisibleWhenLocked;
    private bool _requestedVisibility;

    public FloatingPanelViewModel(FloatingPanelSpec spec, BlockLayoutViewModel owner)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _bounds = CreateBounds(spec);
        _requestedVisibility = spec.IsVisible;
        _isVisibleWhenLocked = spec.IsVisibleWhenLocked;
        _isVisible = spec.IsVisible;
        _openSettingsCommand = new DelegateCommand(
            _ => _owner.RequestFloatingPanelSettings(this),
            _ => !IsLocked);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the unique identifier for the panel.</summary>
    public string Id => _spec.Id;

    /// <summary>Gets the persisted title metadata.</summary>
    public string Title => string.IsNullOrWhiteSpace(_spec.Title) ? "Panel" : _spec.Title;

    /// <summary>Gets the optional content identifier hosted by the panel.</summary>
    public string? ContentId => _spec.ContentId;

    /// <summary>Gets or sets the bounds for the floating panel.</summary>
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

    /// <summary>Gets a value indicating whether the panel intersects other floating content.</summary>
    public bool IsColliding
    {
        get => _isColliding;
        private set
        {
            if (_isColliding == value)
            {
                return;
            }

            _isColliding = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets a value indicating whether the panel is locked for interaction.</summary>
    public bool IsLocked
    {
        get => _isLayoutLocked || _spec.IsLocked;
        private set
        {
            if (_isLayoutLocked == value)
            {
                return;
            }

            _isLayoutLocked = value;
            OnPropertyChanged();
            _openSettingsCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the command that launches the panel settings UI.</summary>
    public ICommand OpenSettingsCommand => _openSettingsCommand;

    /// <summary>Gets a value indicating whether the panel should be rendered.</summary>
    public bool IsVisible => _isVisible;

    /// <summary>Gets a value indicating whether the panel remains visible when the layout is locked.</summary>
    public bool IsVisibleWhenLocked
    {
        get => _isVisibleWhenLocked;
        private set
        {
            if (_isVisibleWhenLocked == value)
            {
                return;
            }

            _isVisibleWhenLocked = value;
            OnPropertyChanged();
            UpdateEffectiveVisibility();
        }
    }

    internal FloatingPanelSpec Spec => _spec;

    internal BlockLayoutViewModel Owner => _owner;

    internal void UpdateBounds(Rect bounds)
    {
        Bounds = bounds;
        _spec.X = bounds.X;
        _spec.Y = bounds.Y;
        _spec.Width = bounds.Width;
        _spec.Height = bounds.Height;
    }

    internal void ApplySettings(string title, string? contentId)
    {
        var normalizedTitle = title ?? string.Empty;
        if (!string.Equals(_spec.Title, normalizedTitle, StringComparison.Ordinal))
        {
            _spec.Title = normalizedTitle;
            OnPropertyChanged(nameof(Title));
        }

        var normalizedContent = string.IsNullOrWhiteSpace(contentId) ? null : contentId.Trim();
        if (!string.Equals(_spec.ContentId, normalizedContent, StringComparison.Ordinal))
        {
            _spec.ContentId = normalizedContent;
            OnPropertyChanged(nameof(ContentId));
        }
    }

    internal void SetCollision(bool isColliding)
    {
        IsColliding = isColliding;
    }

    internal void SetLockState(bool isLocked)
    {
        IsLocked = isLocked;
        UpdateEffectiveVisibility();
    }

    internal void UpdatePanelLock(bool isLocked)
    {
        if (_spec.IsLocked == isLocked)
        {
            return;
        }

        _spec.IsLocked = isLocked;
        OnPropertyChanged(nameof(IsLocked));
        _openSettingsCommand.RaiseCanExecuteChanged();
    }

    internal void SetVisibility(bool isVisible)
    {
        _requestedVisibility = isVisible;
        _spec.IsVisible = isVisible;
        UpdateEffectiveVisibility();
    }

    internal void UpdateVisibilityPolicy(bool isVisibleWhenLocked)
    {
        IsVisibleWhenLocked = isVisibleWhenLocked;
        _spec.IsVisibleWhenLocked = isVisibleWhenLocked;
    }

    private void UpdateEffectiveVisibility()
    {
        var effective = (!_isLayoutLocked || _isVisibleWhenLocked) && _requestedVisibility;
        if (_isVisible == effective)
        {
            return;
        }

        _isVisible = effective;
        OnPropertyChanged(nameof(IsVisible));
    }

    private static Rect CreateBounds(FloatingPanelSpec spec)
    {
        var width = spec.Width <= 0 ? 240d : spec.Width;
        var height = spec.Height <= 0 ? 240d : spec.Height;
        return new Rect(spec.X, spec.Y, width, height);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
