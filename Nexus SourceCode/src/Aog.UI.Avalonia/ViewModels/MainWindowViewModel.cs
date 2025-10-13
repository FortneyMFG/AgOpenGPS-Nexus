using System.Runtime.InteropServices;
using Aog.UI.Avalonia.Models;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the bootstrap shell window.
/// </summary>
public class MainWindowViewModel
{
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
    /// Gets a sample vehicle pose used to seed the map view.
    /// </summary>
    public VehiclePose VehiclePose { get; } = new(10, 15, 45);
}
