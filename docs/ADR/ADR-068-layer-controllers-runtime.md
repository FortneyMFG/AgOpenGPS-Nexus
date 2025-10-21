# ADR-068 — Layer Controllers & Aggregation Runtime

- **Status:** Accepted — 2025-05-17 core & mapping guild review
- **Date:** 2025-05-17
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-215 Layer controller runtime scaffolding
- **Relevant Plugin(s):** Mapping, Sections, Rate Control, Variable Mapping, Analytics, Telemetry Logging

## Context

The Nexus refactor introduces metadata-driven layers, deterministic replay, and season/session provenance that depend on a
common aggregation runtime. Legacy AgOpenGPS streamers intermix IO ingestion, accumulation, and rendering, which makes it hard
to add new sensors, keep overlap math deterministic, or replay controller outputs headlessly. SRS §04 backend requirements and
Option O-BACKEND-4 call for per-section controllers that buffer GNSS-aligned samples, maintain rolling accumulators, and emit
immutable snapshots so PoseStream, SectionState, and TileStore stay consistent.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L27】【F:docs/SRS/sections/2X_System_Architecture/21-O5%20-%20Layer%20controllers%20with%20aggregation%20pipelines.md†L1-L46】 Layer registry work (ADR-010) and metadata-driven layer schemas
(O-DATA-5) assume controllers can normalize values, units, and quality weights while journaling provenance to satisfy analytics
and regulatory workloads.【F:docs/SRS/sections/3X_Data_Storage/32-O5%20-%20Metadata-driven%20variable-rate%20layers.md†L1-L58】 Without a canonical runtime, plugins
duplicate logic, the UI sees inconsistent values, and replay fixtures cannot guarantee deterministic hashes for coverage tiles
or rate history.

## Decision

Adopt a layered aggregation runtime owned by Core:

- **LayerController contract:** Controllers implement `ILayerController` with `OnSample`, `Tick`, `GetSnapshot`, and `Reset`
  methods. `OnSample` ingests raw hardware/plugin samples tagged with `SourceId`, `timestamp`, `quality`, and optional `pose`
  references. `Tick` aligns buffered samples to the PoseStream cadence, performing interpolation or hold-last-value policies to
  produce deterministic frame-aligned aggregates. `GetSnapshot` emits an immutable DTO (protobuf + JSON) containing the latest
  values, accumulators, quality weights, and provenance hashes.
- **Controller registry:** Controllers register via dependency injection with metadata describing the layer namespace, units,
  compatible implements, smoothing parameters, and required capabilities. The registry enforces uniqueness per
  `(sectionId, layerNamespace)` and provides lookup helpers for plugins and TileStore writers.
- **Pose alignment:** Controllers subscribe to PoseStream updates via a deterministic event bus. Each tick receives the active
  pose (vehicle + implement), zone mask (ADR-027), and section mask so coverage math respects spatial constraints and implements
  can compute overlap area consistently.【F:docs/ADR/ADR-027-spatial-constraints.md†L11-L53】
- **Snapshot pipeline:** `LayerSnapshotWriter` writes controller outputs into TileStore segments and publishes `LayerSnapshot`
  events for UI/analytics consumers. Snapshots carry `sessionId`, `jobId`, `seasonId?`, `fieldIds[]`, and `poseHash` so replay
  and offline analytics can reconstruct provenance without additional lookups. Snapshots include per-layer `contentHash`
  computed from canonical JSON to guarantee determinism across runs.
- **Quality & diagnostics:** Controllers maintain running statistics (`samples`, `min`, `max`, `mean`, `variance`,
  `lastUpdateLatencyMs`). `LayerDiagnosticsService` exposes `/diagnostics/layers` for health dashboards and publishes warnings
  when latency or quality thresholds drift beyond budgets defined in ADR-026 performance guardrails.【F:docs/ADR/ADR-026-performance-budgets.md†L19-L66】
- **Plugin extension points:** Plugins can contribute additional controllers by shipping assemblies that implement
  `ILayerControllerFactory`. Factories declare capability requirements and fallback behaviour (e.g., degrade to binary
  coverage only). Controllers may expose extra analytics via `extensions` objects stored alongside snapshots; Core treats the
  field as plugin-owned while keeping base metrics canonical.
- **Journaling & replay:** Every controller tick appends entries to `LayerJournal.v1` with deterministic ordering. Replay loads
  journals, rehydrates controller state, and verifies snapshot hashes. Regression fixtures (`nexus sim replay --slice layer`)
  assert identical hashes across three replays before releasing controller changes.

## Data flow overview

1. Hardware/firmware samples arrive through AGiO providers or plugin simulators. Each provider normalizes payloads into
   `LayerSample` messages with engineering units and quality metadata.
2. `LayerControllerHost` routes samples by `(sectionId, layerNamespace)` to registered controllers using the capability registry.
3. Controllers buffer samples until the next PoseStream tick, then call `PoseAlignedAccumulator.Update(pose, zoneMask,
   sectionMask)` to compute coverage areas, distance, and dwell times.
4. Snapshots publish to the event bus and TileStore writers. Mapping/rate plugins receive updates via dependency injection and
   update UI overlays or control loops accordingly.
5. `LayerDiagnosticsService` monitors latency, drift, and sample gaps, surfacing alerts and metrics to Telemetry & Health
   dashboards per SRS §10.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L49】
6. Journals persist to disk using ADR-020 deterministic replay policies. Recovery replays journals to rebuild controller state
   before acknowledging `onSessionResume`.

## SRS Impact

- Delivers the aggregation runtime mandated by §04 Backend Services (R-BE-010…R-BE-013) so ingestion, accumulation, and
  rendering are decoupled and deterministic.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L35】
- Satisfies §08 Data Model expectations for per-layer metadata, accumulators, and provenance by emitting canonical snapshots and
  journals aligned with O-DATA-5 metadata-driven layers.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L10-L33】【F:docs/SRS/sections/3X_Data_Storage/32-O5%20-%20Metadata-driven%20variable-rate%20layers.md†L1-L58】
- Provides telemetry hooks and health metrics required in §10 Telemetry & Health, enabling alerting when controller latency or
  quality falls outside targets.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L41】
- Enables deterministic replay goals from §03 Communications & Transports and ADR-020 by producing repeatable hashes and
  journals for PoseStream-aligned controllers.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L25-L45】【F:docs/ADR/ADR-020-determinism-replay-ci.md†L11-L40】

## Consequences

- **Positive:**
  - Plugins consume a consistent snapshot model instead of bespoke accumulators, reducing duplicated math and easing onboarding
    for new layer types.
  - Replay and simulation share the same pipeline, so controller regressions surface in CI before field deployment.
  - Diagnostics surfaces (UI dashboards, telemetry exporters) gain structured metrics for coverage completeness, sample latency,
    and quality weights.
- **Negative / mitigations:**
  - Controllers introduce per-tick CPU overhead; mitigated by caching geometry lookups, streaming accumulators, and enforcing
    performance budgets with ADR-026 instrumentation.
  - Plugin authors must migrate from legacy callbacks; Core ships adapter shims that translate old events into controller samples
    for one release and logs upgrade guidance.
  - Additional storage for journals and snapshots is bounded via configurable retention and compaction policies in
    `LayerJournalService`.

## Validation

- `nexus sim replay --slice layer` fixture must reproduce identical snapshot hashes across three runs using recorded field data.
- Reference rigs (12-row planter, 36-section sprayer, 60-ft toolbar) must keep controller latency ≤ 40 ms p95 under 20 Hz
  PoseStream input while maintaining ≤ 1 frame of lag between PoseStream and controller snapshots.
- Fault injection (drop 10% samples, delay GNSS by 150 ms) must trigger quality warnings on `/diagnostics/layers` while keeping
  replayed hashes unchanged once data recovers.
- TileStore outputs generated from controller snapshots must match legacy overlap math within ±1% area error across regression
  datasets.

## Migration & rollout

- Legacy streamer code is wrapped in adapter controllers that emit matching snapshots. Incremental migration replaces adapters
  with native controllers per layer until all critical layers (coverage, rate actual/target, population, downforce, etc.) run on
  the new runtime.
- Documentation updates include developer guides for implementing controllers, schema references for snapshots, and operator
  notes explaining diagnostics panels.
- GA requires Linux headless and Windows desktop hosts to pass the same controller regression suite before enabling the runtime
  by default.

## References

- [O-BACKEND-4 — Layer Controllers](../SRS/sections/2X_System_Architecture/21-O5%20-%20Layer%20controllers%20with%20aggregation%20pipelines.md)
- [O-DATA-5 — Metadata-driven layers](../SRS/sections/3X_Data_Storage/32-O5%20-%20Metadata-driven%20variable-rate%20layers.md)
- [O-TEST-4 — Layer replay CI harness](../SRS/sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md)
- [ADR-010 — Layer registry & variable rate](ADR-010-layer-registry-variable-rate.md)
- [ADR-027 — Spatial constraints & zone policies](ADR-027-spatial-constraints.md)
- [ADR-020 — Determinism & replay CI](ADR-020-determinism-replay-ci.md)
