# ADR-029: Mapping as a plugin with a minimal geospatial kernel in Core

## Status
Proposed

**Relevant Plugin(s):** Mapping, Variable Mapping, Rate Control, Section Control, Telemetry Logging



## Context
Guidance, section control, and variable-rate features all require spatial queries, yet many rigs operate without GNSS hardware or only need speed-based automation. Today the legacy mapping stack is bundled into the runtime, coupling UI overlays, layer math, and file import pipelines to Core releases.【F:docs/aog-v6-mapping-brief.md†L10-L53】 The team also wants to iterate on new layer formats (ISOXML, GeoJSON, GeoTIFF) and mapping algorithms without breaking Core’s determinism guarantees or bloating deployments that run headless. A dedicated ADR is needed to define the split between a lean geospatial kernel that ships with Core and pluggable mapping engines that can evolve independently.

## Decision
Create a two-part architecture:

1. **Core geospatial kernel (Aog.Core).** Embed CRS and unit conversion helpers, ENU tiling utilities, the monotonic timebase/frame counter, deterministic replay helpers, and null Pose/Mapping implementations so rigs without mapping still boot cleanly. Core also owns the frozen `Aog.Abstractions` protobuf contracts for pose, layers, mapping queries, and replay logs.【F:docs/ADR/ADR-002-grpc-contracts.md†L1-L28】【F:docs/SRS/sections/04_Backend_Services.md†L6-L20】
2. **Mapping plugins (Aog.Plugins.Mapping.*).** Implement pose ingestion, raster/vector/interpolation engines, layer import/export, tile caching, and rate/coverage publishing behind the Core contracts. Plugins run out-of-process, subscribe to the PoseBus, and publish RateHint, SectionMask, CoverageUpdate, and diagnostics via Core’s event bus. Multiple mapping engines may coexist, and presets select which plugin bundle to load.

### Responsibilities & contracts

| Component | Owns | Consumes | Publishes |
| --- | --- | --- | --- |
| **Core geospatial kernel** | Monotonic clock, frame IDs, CRS transforms (WGS84↔local ENU), tiling helpers, deterministic replay format, capability registry, NullPose/NullMapping providers. | PoseBus inputs from AgIO, plugin capability manifests. | PoseBus service, Mapping gRPC service stubs, capability availability events, replay/tap logs. |
| **Mapping plugin** | Pose ingestion, coverage tracing, raster/vector/prescription engines, interpolation (IDW/Kriging-lite), buffer/keep-out computations, import/export (ISOXML, GeoJSON, GeoTIFF, Shape/MBTiles), diagnostics. | PoseBus subscription, Mapping contracts, LayerRegistry metadata, capability registry. | RateHint, SectionMask, CoverageUpdate, PathHint streams; layer storage updates; health telemetry. |
| **Consumers (VRC, Sections, UI, analytics)** | Rate and section automation, visualization overlays, analytics, session exports. | Mapping RPCs/streams, LayerRegistry, capability registry. | Automation commands, operator UI overlays, QA summaries. |

### Message contracts

Define or extend the following protobuf contracts under `Aog.Abstractions` with versioned messages:

```proto
message Pose {
  uint64 frame_id = 1;
  int64 mono_time_ns = 2;
  double lat = 3;
  double lon = 4;
  double alt_m = 5;
  double heading_deg = 6;
  double speed_mps = 7;
  double accuracy_m = 8;
}

message LayerQuery {
  double lat = 1;
  double lon = 2;
  string layer_id = 3;
  uint64 frame_id = 4;
}

message LayerValue {
  string layer_id = 1;
  double value = 2;
  string unit = 3;
  bool has_value = 4;
}
```

Services:

- `PoseBus.Publish/Subscribe` – shared monotonic timebase for pose producers/consumers.
- `Mapping.GetLayerValue` – synchronous layer sampling (<2 ms p99, 20 Hz minimum cadence).
- `Mapping.StreamRateHints` – stream of rate recommendations for VRC plugins.
- `Mapping.StreamCoverage` – coverage state fan-out for Sections/UI.
- `Mapping.ImportLayer` – uniform ingest (ISOXML, GeoJSON, GeoTIFF, Shape) with CRS/unit normalization.

### Operational policies

- **Optionality:** Core boots with NullMapping; rigs without mapping plugins expose `mapping:unavailable` in the capability registry so consumers degrade gracefully.
- **Fault isolation:** Mapping plugins are restartable processes; Core tears down leases and marks data stale if health pings fail.
- **Version agility:** Mapping plugins declare capabilities (e.g., `mapping:raster@v1`, `mapping:vector@v2`). Core validates compatibility before activation and surfaces feature gaps to presets.
- **Deterministic replay:** Core mirrors Pose/Layer traffic into replay logs; plugins must honor frame IDs/mono time and supply deterministic outputs under replay.
- **Debugging:** Core exposes replay/tap endpoints so developers can feed recorded Pose logs into mapping plugins for regression tests.

### Degraded operation & operator messaging
- **NullMapping UX:** When NullMapping is active, Core publishes a `mapping:offline` state with explicit operator-facing messaging in the Device Manager and Preset Switcher. Sections and rate controllers continue to run using headland-only constraints, and UI overlays display a "No map data" banner rather than empty tiles.
- **Health flaps:** Repeated health failures transition mapping plugins into a quarantined state. Core pauses consumer subscriptions, renders historical coverage as read-only, and provides retry controls in the UI so operators can deliberately re-enable the plugin after addressing root causes.
- **Partial capability gaps:** If a plugin lacks optional capabilities (e.g., raster but not vector), presets and JobsService entries annotate the missing features and downgrade dependent automations. For example, a VRC preset shows "Variable rate paused — mapping:raster missing" while maintaining baseline/manual rates until the capability becomes available again.

## Consequences
- **Pros:** Optional mapping footprint for headless rigs, swappable engines for specialized workflows, isolated failures, and faster iteration on GIS features without Core releases.【F:docs/aog-v6-mapping-brief.md†L55-L97】
- **Cons & mitigations:** IPC latency managed through shared monotonic timebase/frame IDs; protobuf version drift mitigated by contracts freeze/versioning; debugging supported by standardized replay taps; state fan-out handled via Core’s event bus and capability registry.【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/ADR/ADR-018-plugin-api.md†L12-L34】

## Follow-up work
- Draft contract updates for `Pose`, `LayerQuery`, `LayerValue`, `RateHint`, `CoverageUpdate`, and related services under the contracts freeze process.
- Implement NullMapping and NullPose providers in Core to unblock headless deployments.
- Prototype a reference mapping plugin (grid-based) and add regression replay fixtures.
- Update presets to reference mapping capabilities and extend the plugin loader to enforce capability dependencies.

## Governance Updates
- **Certification program.** Mapping plugins undergo standardized latency, accuracy, and resource benchmarks using published replay scenarios. Results determine certification tiers that inform operator catalogs.
- **Multi-plugin coexistence.** Documentation now requires declaring hard dependencies and incompatibilities, and CI ensures preset bundles do not load conflicting plugins.
- **Upgrade rehearsals.** Plugin maintainers stage upgrades in sandboxed environments with automated regression reports before promoting releases.

## Validation
- **NullMapping readiness:** NullMapping provider start-up on reference hardware must complete in under 350 ms at the 95th percentile and publish a healthy capability state before sections/plugins request pose transforms.
- **Capability enforcement:** Integration tests must fail within 2 seconds when a plugin advertises incompatible `mapping:*` capabilities, with actionable diagnostics surfaced through the Device Manager contract.
- **Deterministic taps:** A 60-minute PoseStream replay must generate identical raster tile checksums when executed with and without mapping plugins loaded, demonstrating deterministic tap semantics for regression harnesses.
