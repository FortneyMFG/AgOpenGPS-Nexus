namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Provides platform specific defaults for run mode selection.
/// </summary>
public interface IRunModePlatform
{
    /// <summary>
    /// Gets the default run mode for the current platform.
    /// </summary>
    AvaloniaRunMode GetDefaultMode();
}
