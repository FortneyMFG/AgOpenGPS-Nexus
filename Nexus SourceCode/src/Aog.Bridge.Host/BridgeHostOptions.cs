using System.ComponentModel.DataAnnotations;

namespace Aog.Bridge.Host;

public sealed class BridgeHostOptions
{
    [Required(AllowEmptyStrings = false)]
    public string NodeId { get; set; } = "bridge";

    [Required(AllowEmptyStrings = false)]
    public string FirmwareVersion { get; set; } = "0.1.0";

    [Required]
    [ValidateComplexType]
    public GrpcEndpointOptions Grpc { get; set; } = new();

    [Required]
    [ValidateComplexType]
    public UdpLinkOptions Udp { get; set; } = new();

    public sealed class GrpcEndpointOptions
    {
        [Required(AllowEmptyStrings = false)]
        public string BindAddress { get; set; } = "0.0.0.0";

        [Range(1, 65535)]
        public int Port { get; set; } = 5600;

        public bool AllowInsecureHttp2 { get; set; } = true;
    }

    public sealed class UdpLinkOptions
    {
        [Required(AllowEmptyStrings = false)]
        public string MulticastAddress { get; set; } = "239.10.6.1";

        [Range(1, 65535)]
        public int MulticastPort { get; set; } = 16666;

        [Range(1, 65535)]
        public int CommandPort { get; set; } = 16667;

        [Range(0, 10)]
        public int RetryCount { get; set; } = 3;

        [Range(50, 10000)]
        public int HeartbeatIntervalMs { get; set; } = 1000;

        [Range(10, 5000)]
        public int CommandAckTimeoutMs { get; set; } = 250;

        public string? NetworkInterfaceName { get; set; }
    }
}
