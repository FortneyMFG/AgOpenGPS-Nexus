# AOG-Link Transport Rollout Guide

ADR-006 moves MCU communications to the AOG-Link protocol across Ethernet, RS-485, and CAN. This guide covers how to stage, validate, and support each transport as part of the rollout. For the normative protocol requirements see [Section 53 — AOG-Link Compatibility](../SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md) and [ADR-006](../SRS/sections/4X_Interprocess_Communications/42-ADR-006%20-%20MCU%20communications%20over%20AOG-Link%20(nanopb).md).

## Transport Matrix

| Transport | Typical Hardware | Notes |
| --- | --- | --- |
| Ethernet/UDP | Bridge service or SBC with NIC | Default transport for lab and dealer rigs; supports TLS tunnels. |
| RS-485/Serial | Legacy controller harnesses | Requires USB adapters; Bridge handles framing + retries. |
| CAN/CAN-FD | Tractor/implement bus | Integrates with SocketCAN on Linux and PEAK/ValueCAN on Windows. |

## Rollout Steps

1. **Bridge configuration.** Update the Bridge manifest to enable the desired transport. Each transport exposes a dedicated section for baud rates, channel names, and retry budgets.
2. **Codec alignment.** Regenerate the nanopb definitions and verify generated structs match the gRPC contract used by the Bridge translators.
3. **Hardware smoke.** Run the transport-specific smoke test suite with loopback harnesses or hardware-in-the-loop rigs.
4. **Telemetry audit.** Capture AOG-Link frames and confirm they translate cleanly to gRPC and legacy PGN payloads using the Bridge analyzer.
5. **Rollout communication.** Publish release notes with supported hardware lists, configuration examples, and troubleshooting tips.

## Support Checklist

- [ ] Dealer enablement docs updated with wiring diagrams and provisioning steps.
- [ ] Bridge diagnostics dashboard shows per-transport latency, retry, and error counters.
- [ ] CI replay harness exercises each transport configuration at least once per week.
- [ ] Firmware team acknowledges transport changes and publishes matching firmware revisions.

Following this guide ensures the AOG-Link transport rollout stays coordinated with ADR-006 expectations while keeping operators informed.
