using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.ViewModels;
using Avalonia;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Represents a floating block rendered directly on the shell canvas.
/// </summary>
public sealed class FloatingBlockViewModel : INotifyPropertyChanged
{
    private readonly BlockLayoutViewModel _owner;
    private readonly FloatingBlockSpec _spec;
    private readonly DelegateCommand _openSettingsCommand;
    private Rect _bounds;
    private bool _isColliding;
    private bool _isLocked;

    public FloatingBlockViewModel(
        BlockInstance instance,
        BlockDefinition definition,
        FloatingBlockSpec spec,
        BlockLayoutViewModel owner)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _bounds = CreateBounds(spec);

        _openSettingsCommand = new DelegateCommand(
            _ => _owner.RequestFloatingBlockSettings(this),
            _ => !_isLocked);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the underlying block instance metadata.</summary>
    public BlockInstance Instance { get; }

    /// <summary>Gets the definition describing the floating block.</summary>
    public BlockDefinition Definition { get; }

    /// <summary>Gets the resolved display label for the block.</summary>
    public string Label => string.IsNullOrWhiteSpace(Definition.Label) ? Definition.Id.Value : Definition.Label;

    /// <summary>Gets the optional icon asset key for the block.</summary>
    public string? IconKey => Definition.IconKey;

    /// <summary>Gets the last measured bounds for the floating block.</summary>
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

    /// <summary>Gets a value indicating whether the block is colliding with another element.</summary>
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

    /// <summary>Gets a value indicating whether layout interactions are locked.</summary>
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
            _openSettingsCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the command used to open the floating block settings dialog.</summary>
    public ICommand OpenSettingsCommand => _openSettingsCommand;

    internal FloatingBlockSpec Spec => _spec;

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

    private static Rect CreateBounds(FloatingBlockSpec spec)
    {
        var width = spec.Width <= 0 ? 160d : spec.Width;
        var height = spec.Height <= 0 ? 160d : spec.Height;
        return new Rect(spec.X, spec.Y, width, height);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
