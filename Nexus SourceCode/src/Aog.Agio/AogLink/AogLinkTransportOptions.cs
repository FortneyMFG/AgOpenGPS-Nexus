using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.AogLink;

/// <summary>
/// Options controlling each AOG-Link transport.
/// </summary>
public sealed class AogLinkTransportOptions
{
    public EthernetOptions Ethernet { get; set; } = new();
    public SerialOptions Serial { get; set; } = new();
    public CanOptions Can { get; set; } = new();

    public sealed class EthernetOptions
    {
        public bool Enabled { get; set; } = true;

        [Required]
        [RegularExpression(@"^\d{1,3}(\.\d{1,3}){3}$", ErrorMessage = "Multicast group must be an IPv4 address.")]
        public string MulticastGroup { get; set; } = "239.16.0.10";

        [Range(1024, 65535)]
        public int Port { get; set; } = 1778;
    }

    public sealed class SerialOptions
    {
        public bool Enabled { get; set; }
            = false;

        [Range(1200, 115200)]
        public int BaudRate { get; set; } = 115200;

        [Required]
        public string DevicePath { get; set; } = "/dev/ttyUSB0";
    }

    public sealed class CanOptions
    {
        public bool Enabled { get; set; }
            = false;

        [Required]
        public string Channel { get; set; } = "can0";

        [Range(125_000, 1_000_000)]
        public int BitRate { get; set; } = 500_000;
    }
}
