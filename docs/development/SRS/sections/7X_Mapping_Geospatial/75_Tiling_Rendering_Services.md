# 75 — Tiling & Rendering Services
*(Status: Proposed)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 75
**Editors:** Rendering & Visualization Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 71 — Mapping Kernel Contracts, 72 — Mapping Layers Plugin, 91 — UI Shell Layout, 96 — Replay-driven CI
**Upstream Dependencies:** ADR-009 TileStore, ADR-096 Replay Rollout, Platform Foundations Section 11
**Downstream Impacts:** Desktop UI renderer, Headless map services, Reporting exports, CI replay harness

---

## 75.1 Purpose & Scope

Define the tiling, caching, and rendering services responsible for presenting mapping layers across interactive clients and headless exports. The section governs CPU/GPU budgets, tile storage, multi-resolution strategies, and deterministic replay expectations on constrained hardware.

---

## 75.2 Context

- TileStore persistence from ADR-009 provides canonical storage with provenance and schema versions for each raster/vector tile.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】
- UI Shell layout (Section 91) defines rendering frame budgets and instrumentation requirements for Avalonia-based clients.【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L70-L126】
- Replay-driven rollout (Section 96) depends on deterministic tile playback for CI, regression tests, and offline analytics exports.【F:docs/sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md†L7-L44】
- Remote gRPC/WebSocket clients consume headless rendering outputs for remote dashboards and reporting flows.【F:docs/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L12-L34】

---

## 75.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Tile Caching | WinForms builds stored tiles in bespoke folder hierarchies. | No eviction, provenance, or compression strategy. | Adopt TileStore with compression and eviction tuned for embedded storage. | Persistence audit |
| Rendering Pipeline | CPU, GPU, and draw operations intertwined in UI thread. | Plugins could starve rendering and stall UI. | Separate update, upload, draw phases with explicit budgets. | UI shell backlog |
| Replay | Layer replay relied on manual scripts. | Non-deterministic outputs; difficult to validate releases. | Journaled tile updates with deterministic playback. | Replay-driven CI proposal |

---

## 75.4 Definitions

| Term | Definition |
|------|-------------|
| TileStore | Persistent storage for raster/vector tiles with provenance metadata and compression. |
| LOD (Level of Detail) | Multi-resolution tiling strategy enabling efficient rendering across zoom levels. |
| Headless Renderer | Service that renders tiles or full map images without UI, used for exports and dashboards. |
| Frame Budget | Allocated CPU/GPU time slices for update, upload, and draw phases each frame. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory and testable.
> - **SHOULD / SHOULD NOT** = strong preference requiring waiver.
> - **MAY** = optional; document enabling conditions and telemetry coverage.

## 75.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-TILE-7500 | MUST | Persistence | TileStore MUST maintain provenance, schema version, compression, and eviction policies suitable for embedded devices. | ADR-009 | Integration tests verifying tile metadata + eviction. |
| R-TILE-7501 | MUST | Pipeline | Renderer MUST separate CPU update, GPU upload, and draw phases with configurable frame budgets and instrumentation. | UI Shell Section 91 | Frame timing benchmarks verifying budget adherence. |
| R-TILE-7502 | SHOULD | Multi-Resolution | System SHOULD support multi-resolution LOD tiles and stitch adjacent fields without seams. | Mapping plugin coordination | Rendering tests across large farms verifying seam-free LOD transitions. |
| R-TILE-7503 | MUST | Headless Rendering | Provide headless rasterization/export paths (PNG/GeoTIFF) using the same pipeline as interactive clients. | Remote client ADR | Automated export tests confirming parity with UI renders. |
| R-TILE-7504 | SHOULD | Platform Coverage | Rendering pipeline SHOULD validate on Windows, Linux desktop, and Raspberry Pi/CM5 GPUs using OpenGL ES3-compatible features. | Platform Foundations Section 11 | Cross-platform CI verifying feature flags. |
| R-TILE-7505 | MUST | Deterministic Replay | Tile updates MUST replay deterministically from journals for CI and regression tests. | Replay-driven CI Section 96 | Replay harness diffing rendered frames vs. baseline. |

### 75.5.1 Frame Budget Guidance

| Phase | Budget (Desktop) | Budget (Embedded) | Notes |
|-------|------------------|-------------------|-------|
| CPU Update | ≤ 4 ms | ≤ 6 ms | Build draw lists, apply tile diffs. |
| GPU Upload | ≤ 3 ms | ≤ 5 ms | Upload textures/buffers; avoid sync stalls. |
| Draw | ≤ 8 ms | ≤ 12 ms | Render overlays, UI chrome; maintain 60/30 FPS targets. |

### 75.5.2 Headless Export Requirements

- Support rendering to PNG and GeoTIFF with configurable bounds, layers, and resolution.
- Use identical shader paths and styling as interactive clients to avoid divergence.
- Emit provenance metadata (layer IDs, timestamps, styling presets) alongside exports for audit.

---

## 75.6 Acceptance Criteria & Verification

- TileStore integration tests validate compression, eviction, and provenance metadata across representative workloads.
- Frame timing benchmarks run on Windows, Linux, and Raspberry Pi hardware to ensure budgets hold across platforms.
- Replay harness replays recorded sessions and diffs rendered outputs against golden images.
- Headless export smoke tests produce PNG/GeoTIFF outputs compared against reference renders.

### 75.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-TILE-7500 | Integration test | `tests/integration/tilestore_cache.cs` | Provenance + eviction metrics match baseline. |
| R-TILE-7501 | Performance bench | `bench/rendering/frame_budget.md` | CPU/GPU phases remain within budget on target hardware. |
| R-TILE-7502 | Rendering test | `tests/rendering/lod_transition.spec` | No seams or artifacts across LOD transitions. |
| R-TILE-7503 | Export test | `tests/rendering/headless_export.cs` | PNG/GeoTIFF outputs match UI renders within tolerance. |
| R-TILE-7504 | Cross-platform CI | `pipelines/rendering-matrix.yml` | All matrix jobs pass using ES3-compatible path. |
| R-TILE-7505 | Replay harness | `tests/replay/tilestore_diff.cs` | Frame diffs ≤ tolerance (0.5% pixel variance). |

---

## 75.7 Constraints

- Rendering stack must avoid vendor-specific GPU extensions exceeding ES3 to remain portable.
- Embedded deployments limited to ≤ 512 MB GPU memory; tile cache eviction strategies must respect this limit.
- Replay artifacts must remain stable across builds; tooling must pin shader compilation inputs and asset hashes.

### 75.7.1 Non-Functional Requirement Classes

- **Performance:** Frame times, tile streaming throughput, memory footprint.
- **Reliability:** Deterministic replay, crash-safe tile writes, headless export parity.
- **Security:** Signed shader binaries or hash validation when distributed externally.
- **Operability:** Instrumentation for frame timing, tile cache health, replay diff tooling.
- **Maintainability:** Modular pipeline stages enabling targeted optimizations.

---

## 75.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-75-1 | Raspberry Pi GPU bandwidth insufficient for multi-layer renders. | High | Profile workloads; implement adaptive layer throttling. | @rendering |
| RISK-75-2 | Tile compression increases CPU load on embedded devices. | Medium | Support hardware-specific codecs; tune cache prefetch. | @storage |
| ISSUE-75-1 | Determine shader packaging strategy for headless deployments. | Medium | Evaluate precompiled binaries vs. runtime compilation. | @ops |
| ISSUE-75-2 | Define golden image tolerance levels for replay diffs. | Low | Telemetry + QA team to publish thresholds. | @qa |

---

## 75.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Deterministic Replay | Journals and rendering pipeline must reproduce frames reliably for CI/regression. |
| C2 | Resource Budgets | Explicit CPU/GPU budgets prevent plugins from starving rendering. |
| C3 | Multi-Platform Support | ES3-compatible pipeline keeps desktop and embedded clients aligned. |
| C4 | Headless Parity | Export services reuse interactive rendering paths to avoid divergence. |
| C5 | Tile Cache Efficiency | Compression and eviction tuned for limited storage. |
| C6 | Observability | Frame timing and cache metrics enable rapid diagnostics. |

### 75.9.1 Assumptions & Preconditions

- [A1] TileStore journals remain authoritative for layer diffs delivered to renderers.
- [A2] Hardware matrix (Windows, Linux, Raspberry Pi/CM5) remains available for CI verification.
- [A3] Plugin authors adhere to frame budget guidelines and instrumentation contracts.

---

## 75.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | Design trade-offs captured within §75.9 design considerations. | — |

---

## 75.11 Comparison Matrix

| Attribute / Criteria | Modern Rendering Pipeline | Legacy Rendering Approach |
|----------------------|---------------------------|---------------------------|
| Determinism | High — journal replay validated in CI | Low — manual scripting |
| Performance Governance | Explicit frame budgets & instrumentation | Ad-hoc performance tuning |
| Platform Coverage | Windows/Linux/ARM ES3 path validated | Desktop-centric, limited embedded support |
| Export Capability | Headless PNG/GeoTIFF parity with UI | Manual screenshot exports |
| Cache Management | Provenance-aware TileStore with eviction | Loose files with manual cleanup |

---

## Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-20 | Initial draft | Nexus Team (Codex) |  |

