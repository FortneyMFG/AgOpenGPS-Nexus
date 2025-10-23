using Nexus.Sdk.Core;

namespace Nexus.Sdk.UI;

/// <summary>
/// Extension helpers for obtaining UI host services from <see cref="IHostServices"/>.
/// </summary>
public static class HostServicesExtensions
{
    /// <summary>
    /// Retrieves the UI host services feature if the host exposes it.
    /// </summary>
    public static IUiHostServices? GetUiHostServices(this IHostServices services)
        => services.GetFeature<IUiHostServices>();
}
