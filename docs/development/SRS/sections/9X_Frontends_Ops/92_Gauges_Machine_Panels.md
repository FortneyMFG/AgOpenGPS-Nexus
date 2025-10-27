# 92 — Blocks, Gauges & Machine Panels
*(Status: Draft — grid-aligned)*

**Authors:** Machine UX Crew  
**Version:** 1.0.0  
**Section ID:** 92  
**Related Sections:** 91 — UI Shell, 94 — Extensibility, 95 — Security  
**Upstream Dependencies:** 71 — Mapping Layers, 4X — Telemetry Transport  
**Downstream Impacts:** 96 — Quality Engineering, 97 — Simulation & Replay

---

## 92.1 Purpose

Describe gauge and machine panels as grid-aware blocks that surface machine telemetry, automation status, and quick actions. Blocks must resize predictably, respect capability gating, and allow plugin-driven contributions without compromising safety.

---

## 92.2 Block Taxonomy

| Type | Description | Examples |
|------|-------------|----------|
| **Telemetry Blocks** | Single-measure tiles (speed, heading, boom status) with optional sparkline or trend badge. Sized to 1×1 or 1×2 cells by default.【F:docs/UI/UI_Demo.html†L135-L221】 | Speed, Satellites, Accuracy. |
| **Group Blocks** | Composite tiles that host nested sub-grids for related data (e.g., hydraulic pressures). | Implement diagnostics, spray system summary. |
| **Action Blocks** | Blocks with primary action buttons (engage auto-steer, toggle sections). They expose confirm/undo affordances. | Section toggles, auto/manual steering. |
| **Context Bars** | Horizontal block strips pinned to grid edges for session metadata or alerts. They can spawn sub-sidebars for deeper drill-downs. | Session summary, warnings ticker. |

All block types follow shared styling tokens and can morph between compact, standard, or expanded layouts depending on available grid cells.

---

## 92.3 Machine Panels

Machine panels are panels (per §91) specialized for real-time machine control:

- **Machine Overview Panel.** Hosts nested grids for vehicle silhouette, implement state, and automation readiness.  
- **Section Manager Panel.** Renders a matrix of section blocks with drag-to-reorder and lasso activation.  
- **Diagnostics Panel.** Displays logs, alerts, and sensor health sorted by plugin-defined severity.

Each panel publishes machine-state data for recording and replay. Plugins may extend panels by registering additional sub-grids (e.g., boom width configuration) or context drawers.

---

## 92.4 Requirements

| ID | Priority | Requirement | Notes |
|----|----------|-------------|-------|
| R-BLOCK-01 | MUST | Blocks derive size, padding, and typography tokens from the workspace grid definition. | Ensures consistent scaling. |
| R-BLOCK-02 | MUST | Blocks support drag, resize, and duplication while preserving data bindings. | Tied to §91 drag contracts. |
| R-BLOCK-03 | MUST | Capability gating hides or disables action blocks when permissions are missing. | Coordinates with §95. |
| R-BLOCK-04 | SHOULD | Blocks expose configuration gear that launches plugin-provided editors in dockable prompts. | Uses Edit Prompt flow.【F:docs/UI/UI_Demo.html†L181-L221】 |
| R-BLOCK-05 | SHOULD | Provide block templates for common telemetry categories (guidance, spraying, vehicle health). | Accelerates plugin onboarding. |
| R-BLOCK-06 | MUST | Machine panels stream telemetry at configurable rates and smooth jitter without blocking UI thread. | Verified via simulation. |
| R-BLOCK-07 | SHOULD | Support snapshot + compare mode to highlight deviations from baseline presets. | QA & training use cases. |
| R-BLOCK-08 | MUST | Serialize per-block provenance (plugin, capability, data source) for auditing. | Stored with workspace manifest. |

---

## 92.5 Integration & Extensibility

- Blocks publish a `BlockDescriptor` manifest including slot, default size, data schema, and supported interactions.  
- Plugin manifests declare block contributions and optional sub-sidebars; the host validates schema compatibility before mounting.  
- Machine panels leverage `TelemetryMeshClient` to subscribe/unsubscribe when panels enter or exit view, avoiding unnecessary load.

---

## 92.6 Testing Strategy

- **Unit Tests:** Verify bindings, formatting, and state transitions for each block template.  
- **Integration Tests:** Drag/resize flows under simulated telemetry spikes.  
- **Replay Harness:** Run deterministic machine logs through panels to assert rendering parity across versions (ties into §97).  
- **Safety Review:** Validate action blocks respect capability checks and emit audit events.

---

## 92.7 Open Items

1. Define cross-plugin priority rules when multiple blocks target the same slot.  
2. Finalize animation guidelines for dynamic measurements (e.g., rate fluctuations).  
3. Decide on standard signal smoothing primitives shared across blocks.
