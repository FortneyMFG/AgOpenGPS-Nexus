namespace Aog.Agio.RadioBridge;

/// <summary>
/// Creates radio bridge link instances based on configured endpoints.
/// </summary>
public interface IRadioBridgeLinkFactory
{
    /// <summary>
    /// Creates a link for the ELRS adapter using the supplied options.
    /// </summary>
    IRadioBridgeLink Create(RadioBridgeElrsAdapterOptions options);
}
