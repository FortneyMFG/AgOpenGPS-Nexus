# 41 — Inter-Application API (Status: collecting proposals)

## Problem statement
Define the data contracts between AgOpenGPS, AgIO, companion tools, and external integrations, covering PGNs, UDP packets, and potential JSON/gRPC schemas.

## Requirements (from contributors)
- R-API-000 (MUST, current-AgOpenGPS): Maintain the PGN catalog used for steer, machine, relay, and section messaging.【F:SourceCode/GPS/Forms/PGN.Designer.cs†L430-L491】
- R-API-001 (MUST, current-AgIO): Keep UDP monitor tooling that surfaces raw PGNs for diagnostics and third-party reuse.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L100】
- R-API-002 (SHOULD, current-AgIO): Continue exposing GNSS correction settings (NTRIP credentials, targets) via config dialogs until a new API replaces them.【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L58-L160】
- R-API-003 (SHOULD): Provide versioning/compat guidance if we add protobuf/JSON schemas so legacy PGN clients remain supported.
- R-API-010 (MUST, proposed-variable-layer): Publish versioned layer definitions, units registries, quality rules, and schema hashes so remote clients and plugins stay aligned with firmware-emitted telemetry.【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md†L1-L31】
- R-API-011 (SHOULD, proposed-variable-layer): Reserve ID ranges, exchange monotonic timestamps + validity bitmaps, and fail fast on schema mismatches to prevent silent drift.【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md†L32-L49】
- R-API-004 (SHOULD, proposed-PGNBridge): Publish the canonical PGN reference and formalize how the bridge exposes versioning, validation, and translation hooks for new APIs.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】
- R-API-005 (COULD): Document handshake messages for capability discovery across processes.
- R-API-012 (SHOULD, release management): Adopt semantic versioning, deprecation periods, and schema compatibility tests for every published API/registry so contributors know when breaking changes are permitted and how long legacy clients are supported.
- R-GEO-000 (MUST, implement geometry): Publish a canonical equipment → implement → toolbar → section hierarchy with stable IDs, offsets, and working widths so Core, UI, plugins, and firmware target consistent geometry metadata.
- R-GEO-001 (SHOULD, section grouping): Describe overlapping SectionGroup semantics, master group overrides, and per-toolbar lookahead/overlap policies so control logic, simulators, and analytics derive identical gating behavior.
- R-GEO-002 (SHOULD, profile discovery): Expose manifest metadata (e.g., hitch profiles, attachment points, kinematic parameters) through the shared registries so plugin capability discovery can populate control and visualization surfaces without bespoke wiring.

### R-API — Plugin contracts & lifecycle
- R-API-020 (MUST, capabilities service): Provide a Capabilities gRPC service for plugin registration, manifest exchange, lease renewal, and version negotiation so Core can enforce compatibility at runtime.
- R-API-021 (MUST, service catalog): Publish typed gRPC contracts for Pose, Equipment, SectionControl, LayerRegistry, TileQuery, Config, EventBus, Guidance, and Health services with explicit streaming semantics and error codes.
- R-API-022 (SHOULD, schema negotiation): Include feature flags and semantic version ranges in plugin manifests so Core can downgrade or reject plugins when contracts diverge.
- R-API-023 (SHOULD, UI manifest contract): Define a declarative schema for plugin UI contributions (panels, config pages, overlays) consumed by the frontend APIs without embedding arbitrary UI code.
- R-API-024 (SHOULD, audit logging): Require control-affecting RPCs (section commands, rate commands, pose writes) to include operator/plugin identity and timestamps so audit trails align with Section 61 control requirements.

## Options
- O-API-0: Status quo — Binary PGNs over UDP/serial with tooling to inspect.
- O-API-1: Wrap PGNs in protobuf definitions for typed consumption.
- O-API-2: Design a JSON/REST surface for high-level interactions.
- O-API-3: Introduce gRPC streams with PGN bridges.
- O-API-4: Adopt OPC-UA or similar industrial protocol for sensors.
- O-API-5: [Versioned layer schemas and quality metadata](../4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md) — Shared catalogs + schema hashes for telemetry.
- O-API-6: [PGN compatibility bridge with typed APIs](../4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md) — Legacy PGNs translated into gRPC/WebSocket contracts.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-API-0 | Stable and widely deployed | Hard to evolve contracts | Binary parsing burden | PGN definitions + monitors |
| O-API-1 | Strong typing | Requires translators for legacy modules | Schema drift with firmware | Field log captures |
| O-API-2 | Easy for web tooling | Verbose payloads | Latency overhead | AgDiag exports |
| O-API-3 | Streaming-friendly | New infra to host | Requires bridging PGNs | PGN wrappers |
| O-API-4 | Industrial ecosystem | Complex stack | Overkill for small rigs | Existing PGNs as base |
| O-API-5 | Shared metadata, early mismatch detection | Additional serialization + documentation work | Version skew between firmware and apps | Current config serialization + registry plan |
| O-API-6 | Keeps PGNs usable while modern APIs evolve | Translation layer to maintain | Drift between spec + implementation | PGN compatibility bridge |

## Evaluation criteria
Compatibility with firmware, tooling support, latency, schema governance, ease of extension.

## Current sentiment
- Keep PGNs as the source of truth while defining how typed APIs can layer on top without fragmenting the ecosystem.
- Schema hashing + registry publishing is seen as a prerequisite before exposing new APIs or plugins to the layer data.【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5%20-%20Versioned%20layer%20schemas%20and%20quality%20metadata.md†L50-L64】
- Bridging PGNs to typed APIs is viewed as the safest path toward Linux/Core pilots without stranding current firmware.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L1-L35】【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】

## Upcoming ADR coverage
- **ADR-008 Equipment hierarchy** will settle the canonical geometry/tree metadata, satisfying new requirements R-GEO-000 through R-GEO-002 and feeding the control semantics defined in Section 61.【F:docs/ADR/ADR-roadmap.md†L27-L33】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L1-L80】
- **ADR-017 Profiles & kinematics** will extend the manifest expectations with hitch linkages, pivot tongues, and sensor fusion priorities so pose outputs align with the PoseStream architecture (ADR-007).【F:docs/ADR/ADR-roadmap.md†L99-L105】

## Open questions
- How do we synchronize schema changes with firmware releases?
- What’s the minimum metadata (IDs, versions) that every PGN must expose going forward?
