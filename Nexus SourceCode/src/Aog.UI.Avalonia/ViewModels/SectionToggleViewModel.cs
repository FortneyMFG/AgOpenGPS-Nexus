using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single manual section toggle presented in the UI.
/// </summary>
public sealed class SectionToggleViewModel : ObservableObject
{
    private readonly Action<int, bool> _onToggled;
    private bool _isEnabled;
    private bool _isVisible = true;
    private bool _suppressNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionToggleViewModel"/> class.
    /// </summary>
    /// <param name="index">Zero-based section index.</param>
    /// <param name="onToggled">Callback invoked when the toggle changes due to user interaction.</param>
    public SectionToggleViewModel(int index, Action<int, bool> onToggled)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Label = $"S{index + 1}";
        _onToggled = onToggled ?? throw new ArgumentNullException(nameof(onToggled));
    }

    /// <summary>
    /// Gets the zero-based section index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the human-friendly label displayed in the UI.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the section is currently active.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value) || _suppressNotifications)
            {
                return;
            }

            _onToggled(Index, value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the toggle should be visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Updates the toggle state without triggering the change callback.
    /// </summary>
    /// <param name="value">New toggle value.</param>
    public void SetIsEnabledFromParent(bool value)
    {
        try
        {
            _suppressNotifications = true;
            IsEnabled = value;
        }
        finally
        {
            _suppressNotifications = false;
        }
    }
}
