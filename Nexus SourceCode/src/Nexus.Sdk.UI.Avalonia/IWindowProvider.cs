namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Provides window descriptors to the host.
/// </summary>
public interface IWindowProvider
{
    /// <summary>
    /// Creates a window descriptor for the plugin.
    /// </summary>
    /// <param name="serviceProvider">Host service provider.</param>
    /// <returns>A descriptor that instructs the host how to instantiate the window.</returns>
    WindowDescriptor Create(IServiceProvider serviceProvider);
}
