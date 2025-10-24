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

    public FloatingPanelViewModel(FloatingPanelSpec spec, BlockLayoutViewModel owner)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _bounds = CreateBounds(spec);
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

    internal FloatingPanelSpec Spec => _spec;

    internal void UpdateBounds(Rect bounds)
    {
        Bounds = bounds;
        _spec.X = bounds.X;
        _spec.Y = bounds.Y;
        _spec.Width = bounds.Width;
        _spec.Height = bounds.Height;
    }

    internal void SetCollision(bool isColliding)
    {
        IsColliding = isColliding;
    }

    internal void SetLockState(bool isLocked)
    {
        IsLocked = isLocked;
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
