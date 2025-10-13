using System.Runtime.InteropServices;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel()
    {
        SimBar = new SimBarViewModel();
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
    /// Gets the view model powering the simulation control bar.
    /// </summary>
    public SimBarViewModel SimBar { get; }
}
