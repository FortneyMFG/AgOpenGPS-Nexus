using System.Text.Json.Serialization;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Stores user interface preferences persisted between sessions.
/// </summary>
public sealed class UiPreferences
{
    /// <summary>Gets or sets the selected user interface theme.</summary>
    public UiTheme Theme { get; set; } = UiTheme.Light;

    /// <summary>Gets or sets whether crash and telemetry uploads are enabled.</summary>
    public bool TelemetryOptIn { get; set; }

    /// <summary>Gets or sets the saved window placement.</summary>
    public WindowPlacement Window { get; set; } = new();

    /// <summary>Gets or sets the preferred run mode.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AvaloniaRunMode RunMode { get; set; } = AvaloniaRunMode.CompanionRemote;

    /// <summary>Gets or sets the persisted shell layout preferences.</summary>
    public ShellLayoutPreferences ShellLayout { get; set; } = new();

    /// <summary>Creates a deep copy of the preferences.</summary>
    public UiPreferences Clone() => new()
    {
        Theme = Theme,
        TelemetryOptIn = TelemetryOptIn,
        Window = Window.Clone(),
        RunMode = RunMode,
        ShellLayout = ShellLayout.Clone(),
    };
}
