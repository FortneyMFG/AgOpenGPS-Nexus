namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Provides dashboard block descriptors to the host.
/// </summary>
public interface IBlockProvider
{
    /// <summary>
    /// Gets the blocks exposed by the plugin.
    /// </summary>
    /// <param name="serviceProvider">Host service provider.</param>
    IEnumerable<BlockDescriptor> GetBlocks(IServiceProvider serviceProvider);
}
