namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Provides tool descriptors to the host.
/// </summary>
public interface IToolProvider
{
    /// <summary>
    /// Gets the tools exposed by the plugin.
    /// </summary>
    /// <param name="serviceProvider">Host service provider.</param>
    IEnumerable<ToolDescriptor> GetTools(IServiceProvider serviceProvider);
}
