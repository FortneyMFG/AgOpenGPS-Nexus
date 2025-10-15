namespace Aog.Agio.RadioBridge;

/// <summary>
/// Configuration options for the ELRS radio bridge adapter.
/// </summary>
public sealed class RadioBridgeElrsAdapterOptions : RadioBridgeAdapterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeElrsAdapterOptions"/> class.
    /// </summary>
    public RadioBridgeElrsAdapterOptions()
        : base("bridge.elrs", "RadioBridge ELRS", "sim://loopback", "radio")
    {
    }
}
