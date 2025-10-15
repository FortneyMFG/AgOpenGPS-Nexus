# AOG-Link Bridge Host

The bridge host is a standalone process that mediates between the gRPC contracts
published by `Aog.Abstractions` and the nanopb-based AOG-Link transports running
on MCU hardware. NX-117 establishes the executable shell so future tasks can
layer in the translators, transports, and PGN compatibility shims.

## Configuration

Configuration is supplied through `appsettings.json` or environment variables
prefixed with `NEXUS_`. The primary settings live under the `BridgeHost` section:

- `NodeId` / `FirmwareVersion` identify the bridge when broadcasting discovery
  and heartbeat frames.
- `Grpc` defines the binding address, port, and whether insecure HTTP/2 is
  permitted during development.
- `Udp` configures the multicast and command sockets used for AOG-Link traffic,
  including retry budgets and watchdog cadences.

Each section is validated on startup so misconfigurations surface early in the
boot sequence. The current gateway implementation logs configuration details and
acts as a placeholder until the NX-118 translator work lands.
