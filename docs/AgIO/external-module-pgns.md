# External Module Message & PGN Guide

This guide explains how external microcontrollers, ECUs, and telemetry modules
connect to AgIO over AOG-Link v1. It captures the framing, message catalogue,
and acknowledgements firmware teams must implement when integrating new
hardware. Use it alongside the canonical [AOG-Link compatibility SRS](../development/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md)
and the legacy [AgIO PGN baseline](../development/SRS/references/AgIO_PGN_Baseline.md).

## Frame structure

AOG-Link v1 transports a protobuf payload with a fixed 8-byte header. Serial and
CAN(FD) links append a CRC-16-CCITT trailer, while UDP may omit it because the
underlying transport already provides checksums.

```
+--------+-----+-------+---------+---------+------+------+
| PFX    | VER | FLAGS | MSG_ID  | LEN     | SVC  | MTH  |
| 0xA5   | 0x1 | 1 b   | 2 bytes | 2 bytes | 1 b  | 1 b  |
+--------+-----+-------+---------+---------+------+------+
```

- **PFX** — Sync byte shared across all transports.
- **VER** — Wire-format major version (`0x1` for v1.x.y). Reject frames when the
  major version differs from the node’s advertised support.
- **FLAGS** — Bitfield combining acknowledgement requirements, fragmentation
  markers, and relative priority. The upper nibble is reserved for CAN(FD)
  segmentation.
- **MSG_ID** — Monotonic session counter enabling dedupe and replay protection.
- **LEN** — Payload length in bytes (excluding header/CRC).
- **SVC/MTH** — Identify the logical service and method inside the protobuf
  catalogue.

### Fragmentation & acknowledgements

- Set `FLAGS.ACK_REQUIRED` on latency-critical commands (steer, sections,
  authority changes). The bridge responds with `MGMT.Ack`/`MGMT.Nack` using the
  same `MSG_ID`.
- Use `FLAGS.FRAGMENT_START`/`FRAGMENT_CONT` on payloads that exceed the
  transport MTU. The bridge reassembles fragments before pushing frames onto the
  gRPC bus.
- Retransmit commands that lack an `Ack` within the negotiated TTL from
  `MGMT.Hello`. Respect exponential backoff to avoid saturating the link.

## Service catalogue

The protobuf package `aoglink.v1` defines the services below. Firmware may
implement a subset as long as `MGMT.Hello` advertises the supported roles and
methods.

| Service ID | Name    | Example methods                  | Notes |
|------------|---------|----------------------------------|-------|
| `0x01`     | `NAV`   | `SetSteerTarget`, `SetABLine`    | Steer and navigation commands with ACK requirements. |
| `0x02`     | `SENSORS` | `GpsFix`, `ImuSample`, `WheelTicks` | Telemetry streams. Batch low-priority updates where bandwidth is limited. |
| `0x03`     | `CTRL`  | `SteerStatus`, `SectionStatus`   | Actuator feedback mirrored to UI dashboards. |
| `0x04`     | `MGMT`  | `Hello`, `Health`, `FwChunk`, `Ack`, `Nack` | Capability negotiation, health telemetry, and firmware transport. |
| `0x05`     | `AUX` (reserved) | `AuxCommand`, `AuxStatus` | Placeholder for ISOBUS/auxiliary controllers. Coordinate before use. |

### Roles & negotiation

1. Nodes start with `MGMT.Hello`, advertising `roles_mask`, firmware semver, and
   optional features (e.g., CAN segmentation, compressed firmware chunks).
2. AgIO replies with compatibility (`OK`, `DOWNLEVEL`, `UNSUPPORTED`) and the
   session TTL used for acknowledgement retries.
3. Role-specific expectations:
   - **`HOST`/`CTRL` nodes** MUST support `NAV` and `CTRL` services and honour
     `Ack` requirements on control commands.
   - **`SENSOR` nodes** SHOULD publish `SENSORS.GpsFix` or `ImuSample` at the
     cadence defined in machine profiles.
   - **`BRIDGE` nodes** translate between legacy PGNs and AOG-Link v1 frames and
     SHOULD surface `MGMT.Health` counters for diagnostics.

## Transport notes

| Transport | Encoding details | Guidance |
|-----------|------------------|----------|
| UDP | Native frames with optional CRC. | Preferred for field retrofits; retain legacy ports for v0 coexistence. |
| USB-CDC Serial | Wrap frames with CRC-16 and optional COBS framing. | Bump baud rate (≥921600 bps) for dense section or rate controllers. |
| CAN(FD) | Segment frames into ≤64-byte chunks using `FLAGS` continuation bits. | Reserve arbitration IDs per controller to avoid collisions. |
| MQTT / MQTT-SN | Publish binary frames under `aoglink/{svc}/{mth}` topics with retained metadata. | Fan-out telemetry only; do not run latency-critical loops solely over MQTT. |
| Shared memory (CM5) | Bypass frames for steer setpoints per the CM5 HAL contract while mirroring telemetry onto MQTT. | Keep the AgIO bridge enabled so external adapters remain compatible. |

## Testing checklist

- Validate `MGMT.Hello`/`Ack` handshakes on every transport you enable.
- Capture golden frames for each implemented method and add them to firmware CI.
- Monitor `MGMT.Health` counters (drops, RTT, voltage, temperature) to spot
  regressions before field deployment.
- When dual-stacking with legacy PGNs, confirm the bridge publishes identical
  telemetry on both protocols before disabling v0 nodes.
