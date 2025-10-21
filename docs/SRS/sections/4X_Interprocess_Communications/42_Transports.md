# 42 — Transports (Status: collecting proposals)

## Problem statement
Define how field devices, guidance engines, and remote clients exchange data across serial, UDP, CAN, and higher-level transports with resiliency and observability.

## Requirements (from contributors)
- R-COMM-000 (MUST, current-AgIO): Preserve the serial port management that bridges GPS, IMU, steer, and machine modules through configurable baud/port settings.【F:SourceCode/AgIO/Source/Forms/FormCommSetGPS.cs†L20-L160】
- R-COMM-001 (MUST, current-AgIO): Maintain UDP discovery, scanning, and monitoring workflows used to find and supervise field modules.【F:SourceCode/AgIO/Source/Forms/FormUDP.cs†L13-L160】【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L100】
- R-COMM-002 (MUST, current-AgOpenGPS): Continue emitting and receiving CAN/UDP PGNs that drive auto-steer, machine control, and section data flows.【F:SourceCode/GPS/Forms/PGN.Designer.cs†L430-L491】
- R-COMM-003 (SHOULD, current-AgIO): Support NTRIP over TCP alongside UDP/serial routing for GNSS corrections.【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L22-L160】
- R-COMM-010 (MUST, proposed-variable-layer): Provide versioned PGNs, sequencing, and schema negotiation so layer definitions and feedback streams stay consistent across firmware and apps.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O5%20-%20Versioned%20variable-rate%20PGN%20suite.md†L1-L41】
- R-COMM-011 (SHOULD, proposed-variable-layer): Enforce monotonic timestamps, bounds checks, and bad-sample counters on layer transports to simplify diagnostics and retries.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O5%20-%20Versioned%20variable-rate%20PGN%20suite.md†L19-L41】【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md†L7-L22】
- R-COMM-004 (SHOULD, proposed-LinuxCore): Stand up a gRPC/WebSocket facade that coexists with legacy PGNs so new clients can attach without rewriting firmware.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L6-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L1-L34】
- R-COMM-005 (MUST, proposed-PGNBridge): Preserve byte-for-byte compatibility with the current AgIO PGN framing or provide a deterministic bridge when introducing new transports.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】
- R-COMM-012 (SHOULD, transport-hardening): Establish latency budgets (<100 ms round-trip for control loops, <500 ms for monitoring) and error budgets (≤0.1% packet loss after retries) for any new gRPC/WebSocket channels so contributors know when the slice is ready to graduate from proposal to review.
- R-COMM-013 (SHOULD, security posture): Document optional encryption/authentication expectations (TLS 1.3, mutual certs or token auth) for modern transports while ensuring PGN bridges can operate offline when credentials are unavailable.
- R-COMM-020 (MUST, PoseStream cadence): Publish a canonical PoseStream cadence/decimation policy with deterministic sequencing so Core, plugins, and firmware consume a single authoritative pose timeline during live runs and replays.
- R-COMM-021 (SHOULD, layer transport handshake): Extend the layer PGN/registry handshake with registry hashes, payload chunking rules, and retry/back-pressure signals so variable-rate controllers can negotiate capabilities before exchanging SectionState deltas.
- R-COMM-022 (MUST, spatial constraints service): Expose a ZoneService gRPC API (`ListZones`, `WatchZones`, `GetZonesInBounds`) that streams boundary, headland, keep-out, and work-disabled polygons with provenance metadata so guidance and section plugins share authoritative constraint geometry.
- R-COMM-023 (MUST, pose zone mask): Embed a `PoseZoneMask` in each PoseStream sample exposing `inside_boundary` (`insideBoundary` in JSON), `inside_headland`, `inside_keep_out`, and `inside_work_disabled` booleans plus the ordered list of intersecting zone identifiers and the `zone_registry_hash`. Replay, automation, and logging pipelines rely on the mask to reproduce gating decisions deterministically when transports relay pose data.

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
- **ADR-007 PoseStream & SectionState architecture** will standardize the pose timeline, SectionState diff rules, and replay guarantees that satisfy transport requirements R-COMM-010, R-COMM-011, and R-COMM-020 while aligning plugin/service expectations captured in Section 12.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L73】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L34】
- **ADR-027 Spatial constraints & zone policies** introduces the ZoneService, buffered zone masks, and constraint gating transports required by R-COMM-020…R-COMM-023 so guidance, section control, and telemetry share deterministic context.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L27-L41】
- **ADR-016 Firmware/Transport: Variable-Rate & Layer PGNs** will finalize payload packing, sequencing, and registry-handshake semantics for layer definitions, fulfilling R-COMM-010, R-COMM-011, and R-COMM-021 prior to firmware rollout.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L91-L97】
- **ADR-021 Timebase & clock sync** will establish the canonical clock, drift handling, and latency budgets that anchor R-COMM-020 and R-COMM-040…R-COMM-042 across Core, plugins, and firmware.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L131-L137】

## Multi-machine telemetry mesh (ADR-047)

- **Entities:** Devices broadcast `Presence` messages describing identity, capabilities, and profile hashes. Operators curate `ShareProfile` documents defining what data leaves the cab (presence only, trails, coverage, layer edits). `SubscribeProfile` policies declare what remote data a cab ingests.
- **Pub/Sub topics:** Core defines topics for `presence`, `poseTrail`, `coverage`, `layerEdit`, and `sessionState`. Each topic advertises QoS budgets: presence at 1 Hz, trails at ≤2 Hz, coverage at ≤1 Hz aggregated, layer edits immediately with deduplication, session state on change.
- **Store-and-forward:** Offline cabs queue shared payloads (coverage tiles, LayerEditEvent journals) up to 20 MB/device. When connectivity returns, queued payloads replay in order with hash validation. Mesh nodes drop stale payloads beyond 30 minutes unless explicitly marked archival.
- **Privacy & ACL:** Share profiles enforce allow/deny lists keyed by device IDs or organization tags. Sensitive feeds (layer edits, profitability) default to deny; operators opt-in per session.
- **Failure handling:** Mesh heartbeats include freshness timers. Receivers flag stale data when heartbeats exceed 5 seconds or when coverage deltas pause for >15 seconds, triggering UI warnings.

## RadioBridge abstraction (ADR-048)

- **Transports:** RadioBridge encapsulates ELRS, LoRa, XBee, or other low-bandwidth radios behind a binary framing layer. Frames use CBOR or FlatBuffers encoding with optional compression.
- **Acknowledgements & replay:** Commands mark `needsAck`; devices retry at exponential backoff up to 5 times. Replay windows allow 60 seconds of history for lossy links; recipients request retransmit by sequence number.
- **Rate governors:** Links expose configured bitrate ceilings. Core throttles coverage and telemetry streams to respect medium constraints (e.g., LoRa 56 kbps). Higher bandwidth topics fall back to store-and-forward bundles.
- **Mapping to mesh topics:** RadioBridge integrates with the multi-machine mesh; share profiles declare which topics traverse radio links. Layer edits compress to delta operations; pose trails decimate to 1 Hz for narrowband.

### AOG-Link MCU datagram protocol
AOG-Link standardizes MCU-to-host and MCU-to-MCU exchanges on compact protobuf
messages compiled with nanopb. Section 3A captures the full wire specification,
including the frame layout, discovery handshakes, transport bindings, and
latency targets.[^aoglink-srs]

Key expectations carried into this section:

- A single 8-byte header (`0xA5` prefix, version, flags, `msg_id`, length,
  service, method) precedes all `aoglink.v1` protobuf payloads; serial and CAN
  transports append CRC16-CCITT and use COBS framing where required.
- Commands set `FLAGS.ACK_REQUIRED` and retry until an `Ack{msg_id}` arrives;
  telemetry is fire-and-forget but embeds sequence numbers and monotonic
  microsecond clocks for loss detection.
- UDP, USB-CDC serial, and CAN(FD) share the same logical model while MQTT can
  mirror payloads for pub/sub fan-out without the binary header.
- A v0 bridge preserves legacy PGN interoperability during migration so the
  Bridge can translate between gRPC contracts, AOG-Link v1 frames, and existing
  UDP-only devices.

[^aoglink-srs]: See [Section 53 — AOG-Link Compatibility](03A_AOG_Link_v1.md).

## Options
- O-COMM-0: Status quo — AgIO-managed UDP + serial PGN transports with optional NTRIP.
- O-COMM-1: Consolidate on a single binary framing library shared across serial/UDP/CAN.
- O-COMM-2: Introduce gRPC for high-level clients while tunneling legacy PGNs.
- O-COMM-3: Adopt MQTT or AMQP for telemetry fan-out.
- O-COMM-4: Embed a REST API around PGN state for web dashboards.
- O-COMM-5: [Versioned variable-rate PGN suite](../4X_Interprocess_Communications/42-O5%20-%20Versioned%20variable-rate%20PGN%20suite.md) — Sequenced layer streams with schema handshakes.
- O-COMM-6: [PGN compatibility bridge layered over new APIs](../4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md) — Legacy PGNs in, typed events out.
- O-COMM-7: gRPC/protobuf API surface published via `Aog.Abstractions` NuGet and consumed by Core/UI/Plugins while AgIO/Bridge backends handle transport specifics.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L36】

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
- Community wants the layer PGN suite staged behind feature flags so existing rigs stay stable while richer telemetry rolls out.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O5%20-%20Versioned%20variable-rate%20PGN%20suite.md†L43-L57】【F:docs/SRS/sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md†L7-L27】
- The shared gRPC/protobuf surface is considered the preferred evolution path when paired with the PGN bridge because it keeps hardware compatibility while aligning Core, UI, and plugins on one contract package.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L79】
- There is appetite to prototype the compatibility bridge alongside the Core API so UDP/serial devices remain usable during a Linux migration.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L21-L44】

## Open questions
- Do we converge on a single heartbeat/watchdog strategy across transports?
- Should we adopt protobuf/FlatBuffers for higher-level APIs?

## Related specifications
- Device identity heartbeat and DFU orchestration: see [Section 55 — Firmware Interfaces & Updates](17_Device_Firmware_Updates.md).
