using System;
using System.Runtime.InteropServices;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel
{
    private readonly ConnectionSettingsViewModel _connectionSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="connectionSettings">The connection settings view-model to expose to the view.</param>
    public MainWindowViewModel(ConnectionSettingsViewModel connectionSettings)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);
        _connectionSettings = connectionSettings;
    }

    /// <summary>
    /// Gets the title displayed in the main window.
    /// </summary>
    public string Title => "AgOpenGPS Nexus";

    /// <summary>
    /// Gets a description of the platform the application is currently running on.
    /// </summary>
    public string PlatformDescription =>
        $"Running on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}) with {RuntimeInformation.FrameworkDescription}";

    /// <summary>
    /// Gets the connection settings view-model.
    /// </summary>
    public ConnectionSettingsViewModel Connection => _connectionSettings;
}
