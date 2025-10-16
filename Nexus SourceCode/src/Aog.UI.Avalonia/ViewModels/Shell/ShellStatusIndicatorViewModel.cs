namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single status indicator displayed in the shell status strip.
/// </summary>
public sealed class ShellStatusIndicatorViewModel : ObservableObject
{
    private string _value;
    private StatusIndicatorLevel _level;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellStatusIndicatorViewModel"/> class.
    /// </summary>
    public ShellStatusIndicatorViewModel(string label, string value, StatusIndicatorLevel level, string? description = null)
    {
        Label = label;
        _value = value;
        _level = level;
        Description = description;
    }

    /// <summary>Gets the label identifying the indicator.</summary>
    public string Label { get; }

    /// <summary>Gets or sets the current value for the indicator.</summary>
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>Gets or sets the severity level for the indicator.</summary>
    public StatusIndicatorLevel Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    /// <summary>Gets an optional description shown in the tooltip.</summary>
    public string? Description { get; }
}

/// <summary>
/// Severity levels reported by <see cref="ShellStatusIndicatorViewModel"/>.
/// </summary>
public enum StatusIndicatorLevel
{
    Normal,
    Warning,
    Critical,
}
