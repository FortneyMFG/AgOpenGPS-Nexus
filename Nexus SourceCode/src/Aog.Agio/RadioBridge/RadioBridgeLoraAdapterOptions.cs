using System;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Configuration options for the LoRa radio bridge adapter.
/// </summary>
public sealed class RadioBridgeLoraAdapterOptions : RadioBridgeAdapterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeLoraAdapterOptions"/> class.
    /// </summary>
    public RadioBridgeLoraAdapterOptions()
        : base("bridge.lora", "RadioBridge LoRa", "sim://lora-loopback", "radio-lora")
    {
        EnableForwardErrorCorrection = true;
        SendInterval = TimeSpan.FromMilliseconds(250);
    }
}
