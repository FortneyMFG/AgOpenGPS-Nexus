# Communications & Transports (Status: collecting proposals)

## Problem statement
Define how field devices, guidance engines, and remote clients exchange data across serial, UDP, CAN, and higher-level transports with resiliency and observability.

## Requirements (from contributors)
- R-COMM-000 (MUST, current-AgIO): Preserve the serial port management that bridges GPS, IMU, steer, and machine modules through configurable baud/port settings.【F:SourceCode/AgIO/Source/Forms/FormCommSetGPS.cs†L20-L160】
- R-COMM-001 (MUST, current-AgIO): Maintain UDP discovery, scanning, and monitoring workflows used to find and supervise field modules.【F:SourceCode/AgIO/Source/Forms/FormUDP.cs†L13-L160】【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L100】
- R-COMM-002 (MUST, current-AgOpenGPS): Continue emitting and receiving CAN/UDP PGNs that drive auto-steer, machine control, and section data flows.【F:SourceCode/GPS/Forms/PGN.Designer.cs†L430-L491】
- R-COMM-003 (SHOULD, current-AgIO): Support NTRIP over TCP alongside UDP/serial routing for GNSS corrections.【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L22-L160】
- R-COMM-010 (MUST, proposed-variable-layer): Provide versioned PGNs, sequencing, and schema negotiation so layer definitions and feedback streams stay consistent across firmware and apps.【F:docs/SRS/options/O-COMM-5_VariableRatePGNs.md†L1-L41】
- R-COMM-011 (SHOULD, proposed-variable-layer): Enforce monotonic timestamps, bounds checks, and bad-sample counters on layer transports to simplify diagnostics and retries.【F:docs/SRS/options/O-COMM-5_VariableRatePGNs.md†L19-L41】【F:docs/SRS/options/O-TELE-4_LayerDiagnostics.md†L7-L22】
- R-COMM-004 (SHOULD, proposed-LinuxCore): Stand up a gRPC/WebSocket facade that coexists with legacy PGNs so new clients can attach without rewriting firmware.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L44】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】
- R-COMM-005 (MUST, proposed-PGNBridge): Preserve byte-for-byte compatibility with the current AgIO PGN framing or provide a deterministic bridge when introducing new transports.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】
- R-COMM-012 (SHOULD, transport-hardening): Establish latency budgets (<100 ms round-trip for control loops, <500 ms for monitoring) and error budgets (≤0.1% packet loss after retries) for any new gRPC/WebSocket channels so contributors know when the slice is ready to graduate from proposal to review.
- R-COMM-013 (SHOULD, security posture): Document optional encryption/authentication expectations (TLS 1.3, mutual certs or token auth) for modern transports while ensuring PGN bridges can operate offline when credentials are unavailable.

## Options
- O-COMM-0: Status quo — AgIO-managed UDP + serial PGN transports with optional NTRIP.
- O-COMM-1: Consolidate on a single binary framing library shared across serial/UDP/CAN.
- O-COMM-2: Introduce gRPC for high-level clients while tunneling legacy PGNs.
- O-COMM-3: Adopt MQTT or AMQP for telemetry fan-out.
- O-COMM-4: Embed a REST API around PGN state for web dashboards.
- O-COMM-5: [Versioned variable-rate PGN suite](../options/O-COMM-5_VariableRatePGNs.md) — Sequenced layer streams with schema handshakes.
- O-COMM-6: [PGN compatibility bridge layered over new APIs](../options/O-COMM-6_PGNCompatibilityBridge.md) — Legacy PGNs in, typed events out.
- O-COMM-7: gRPC/protobuf API surface published via `Aog.Abstractions` NuGet and consumed by Core/UI/Plugins while AgIO backends handle transport specifics.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L36】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-COMM-0 | Proven in-field behavior and tooling | No built-in sequencing beyond custom logic | Harder to scale beyond LAN | Current AgIO UDP/serial stack |
| O-COMM-1 | Shared codecs, easier testing | Migration effort for each module | Regression risk for older firmware | Existing PGN definitions |
| O-COMM-2 | Strong typing and streaming | Needs bridge to hardware PGNs | Service footprint grows | Field loggers + PGN spec |
| O-COMM-3 | Turnkey pub/sub | More infra to run | Broker outages impact steering | Use telemetry monitors |
| O-COMM-4 | Familiar web tooling | Polling overhead | Divergent auth story | AgDiag HTTP prototypes |
| O-COMM-5 | Adds sequencing, schema hashes, and layer registries | Firmware/app upgrades required | Bandwidth pressure if many layers stream | AgIO UDP monitor + layer registry plan |
| O-COMM-6 | Allows Core/API modernization without stranding modules | Bridge adds latency + new failure mode | Incorrect translation can break steering | PGN compatibility bridge |
| O-COMM-7 | Strong typing, shared contracts, works across Windows/Linux | Requires disciplined versioning + CI | Backend bug impacts every client | .NET 8 + Avalonia stack |

## Evaluation criteria
Deterministic latency, message integrity (CRC/sequencing), offline buffering, compatibility with existing AgIO channels, firewall friendliness.

## Current sentiment
- Keep PGNs flowing through AgIO while we inventory what hardening is required before layering a modern API facade.
- Community wants the layer PGN suite staged behind feature flags so existing rigs stay stable while richer telemetry rolls out.【F:docs/SRS/options/O-COMM-5_VariableRatePGNs.md†L43-L57】【F:docs/SRS/options/O-TEST-4_LayerReplayCI.md†L7-L27】
- The shared gRPC/protobuf surface is considered the preferred evolution path when paired with the PGN bridge because it keeps hardware compatibility while aligning Core, UI, and plugins on one contract package.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L79】
- There is appetite to prototype the compatibility bridge alongside the Core API so UDP/serial devices remain usable during a Linux migration.【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L44】

## Open questions
- Do we converge on a single heartbeat/watchdog strategy across transports?
- Should we adopt protobuf/FlatBuffers for higher-level APIs?

## Related specifications
- Device identity heartbeat and DFU orchestration: see [Section 17 — Device Firmware Updates](17_Device_Firmware_Updates.md).
