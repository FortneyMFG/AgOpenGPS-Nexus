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
- R-COMM-020 (MUST, PoseStream cadence): Publish a canonical PoseStream cadence/decimation policy with deterministic sequencing so Core, plugins, and firmware consume a single authoritative pose timeline during live runs and replays.
- R-COMM-021 (SHOULD, layer transport handshake): Extend the layer PGN/registry handshake with registry hashes, payload chunking rules, and retry/back-pressure signals so variable-rate controllers can negotiate capabilities before exchanging SectionState deltas.
- R-COMM-022 (MUST, spatial constraints service): Expose a ZoneService gRPC API (`ListZones`, `WatchZones`, `GetZonesInBounds`) that streams boundary, headland, keep-out, and work-disabled polygons with provenance metadata so guidance and section plugins share authoritative constraint geometry.
- R-COMM-023 (MUST, pose zone mask): Attach a zone bitmask (`insideBoundary`, `insideHeadland`, `insideKeepOut`, `insideWorkDisabled`) to PoseStream samples so replays, plugins, and logs can reproduce constraint context deterministically when transports relay pose data.

### R-COMM — Plugin transport & leases
- R-COMM-030 (MUST, plugin transport): Define how Core exposes gRPC endpoints, discovery directories, and lease heartbeats so plugins can register/renew capabilities without restarting Core or the UI.
- R-COMM-031 (MUST, capability permissions): Require every plugin connection to negotiate an authenticated session (local policy or certificates) and enforce per-capability permissions (pose.read, section.command, storage.write) before streaming data.
- R-COMM-032 (SHOULD, health semantics): Publish health/metrics RPC expectations (Ping, GetStatus, GetMetrics) and degraded-state signaling so operators can see when transports or plugins fall behind without guesswork.

### R-TIME — Timebase & clock sync
- R-COMM-040 (MUST, canonical timebase): Establish a canonical time authority (GPS, PTP, or system clock fallback) with documented drift tolerances for PoseStream sequencing and cross-node coordination.
- R-COMM-041 (SHOULD, timestamp reconciliation): Require firmware-ingested samples to include capture timestamps and sequence numbers so Core can reconcile device clocks against the canonical timebase and surface drift metrics.
- R-COMM-042 (SHOULD, latency budgets): Document maximum end-to-end latency budgets per topic (pose ingest, section commands, tile flush) to guide scheduling and CI alerts across transports and plugins.

## Adopted architecture (ADR alignment)
- [ADR-002](../../ADR/ADR-002-grpc-contracts.md) establishes gRPC/protobuf as the authoritative **inter-process** API between Core, UI, plugins, automation tooling, and the Bridge/AgIO hosts. All desktop/server processes share the generated `Aog.Abstractions` clients while transports below the Bridge remain opaque to them.
- [ADR-006](../../ADR/ADR-006-aog-link-mcu-communications.md) defines **AOG-Link** as the MCU communications layer using nanopb datagrams over Ethernet, RS-485/serial, or CAN. The Bridge service translates between gRPC contracts, AOG-Link frames, and legacy PGN flows so firmware evolution does not alter higher-layer APIs.

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** will standardize the pose timeline, SectionState diff rules, and replay guarantees that satisfy transport requirements R-COMM-010, R-COMM-011, and R-COMM-020 while aligning plugin/service expectations captured in Section 12.【F:docs/ADR/ADR-roadmap.md†L67-L73】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L34】
- **ADR-027 Spatial constraints & zone policies** introduces the ZoneService, buffered zone masks, and constraint gating transports required by R-COMM-020…R-COMM-023 so guidance, section control, and telemetry share deterministic context.【F:docs/ADR/ADR-roadmap.md†L27-L41】
- **ADR-016 Firmware/Transport: Variable-Rate & Layer PGNs** will finalize payload packing, sequencing, and registry-handshake semantics for layer definitions, fulfilling R-COMM-010, R-COMM-011, and R-COMM-021 prior to firmware rollout.【F:docs/ADR/ADR-roadmap.md†L91-L97】
- **ADR-021 Timebase & clock sync** will establish the canonical clock, drift handling, and latency budgets that anchor R-COMM-020 and R-COMM-040…R-COMM-042 across Core, plugins, and firmware.【F:docs/ADR/ADR-roadmap.md†L131-L137】

### AOG-Link MCU datagram protocol
AOG-Link standardizes MCU-to-host and MCU-to-MCU exchanges on compact protobuf messages compiled with nanopb. Every packet begins with a fixed header of `{version, class, type, seq, src, dst, len}` followed by the protobuf payload; serial links append a CRC-16 after the payload. The fields mirror the IDs exposed through the gRPC contracts so the Bridge can map between the layers without lossy transforms.

**Transports:**

- **UDP (Ethernet/Wi-Fi):** Telemetry, discovery, and MCU-to-MCU data publish on multicast `239.10.6.1:16666`, while command/ack flows use unicast with retry/timeout handling. Payloads stay ≤600 B to avoid fragmentation, and the same packet framing is shared with the serial variant for firmware simplicity.
- **RS-485/serial:** Packets reuse the common header/payload, wrapped in COBS with a CRC-16 trailer. Deployments target 115200–1Mbaud multi-drop links with a simple token or host-directed slot every ~5 ms to prevent collisions.
- **CAN / CAN-FD:** Extended 29-bit identifiers follow `priority (3) | class (2) | type (10) | dest (8) | src (8)`, enabling silicon filtering by message type. CAN-FD single frames encode `[ver][seq][len][flags][protobuf…]`; larger messages either segment through ISO-TP (works on CAN 2.0 and FD) or a lightweight fragment header `[ver][seq][frag_idx][frag_cnt][payload…]` when `flags.fragmented` is set.

**Reliability and arbitration:** Commands mark `flags.needs_ack` and expect an acknowledgement echoing the original `type` and `seq`. Telemetry remains fire-and-forget, but listeners drop stale sources once sequence gaps exceed 300 ms and declare failover at 500 ms. Heartbeat messages advertise `{role, priority, capabilities}` at 1 Hz so multiple MCUs can self-elect producers or consumers per data class.

**MCU-to-MCU data sharing:** Speed, rate, section-state, and override information reuse the same message types used between the host and MCUs. Arbitration favors the highest-priority publisher for each class while still exposing lower-priority data for diagnostics. Suggested cadences: speed 10–20 Hz, section-state 2–5 Hz plus on change, rate 5 Hz, heartbeat 1 Hz.

**Firmware guidance:** Nanopb options should prefer fixed-width numeric fields and compact enums to keep payloads ≤48 B where possible, ensuring single-frame delivery on CAN-FD and minimal ISO-TP fragmentation on classical CAN. Modules log `{device_id, seq, stale_ms}` per data class for diagnostics and fall back to fail-safe outputs if inputs remain stale beyond 500–1000 ms.

Discovery, heartbeat, and time-sync flows originate from the Bridge, which also exposes conversion shims for legacy PGN UDP devices to remain operational during migration. MCU firmware reuses the shared `.proto` schemas from `Aog.Abstractions`, enabling the Bridge to translate losslessly between gRPC topics and AOG-Link datagrams while keeping PGN expansion frozen to maintenance-only fixes.

## Options
- O-COMM-0: Status quo — AgIO-managed UDP + serial PGN transports with optional NTRIP.
- O-COMM-1: Consolidate on a single binary framing library shared across serial/UDP/CAN.
- O-COMM-2: Introduce gRPC for high-level clients while tunneling legacy PGNs.
- O-COMM-3: Adopt MQTT or AMQP for telemetry fan-out.
- O-COMM-4: Embed a REST API around PGN state for web dashboards.
- O-COMM-5: [Versioned variable-rate PGN suite](../options/O-COMM-5_VariableRatePGNs.md) — Sequenced layer streams with schema handshakes.
- O-COMM-6: [PGN compatibility bridge layered over new APIs](../options/O-COMM-6_PGNCompatibilityBridge.md) — Legacy PGNs in, typed events out.
- O-COMM-7: gRPC/protobuf API surface published via `Aog.Abstractions` NuGet and consumed by Core/UI/Plugins while AgIO/Bridge backends handle transport specifics.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L36】

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
