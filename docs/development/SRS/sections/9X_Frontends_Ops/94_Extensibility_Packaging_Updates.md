# 94 — Extensibility, Packaging & Workspace Governance
*(Status: Draft — grid-aware rewrite)*

**Authors:** Plugin Council  
**Version:** 1.0.0  
**Section ID:** 94  
**Related Sections:** 91 — UI Shell, 92 — Blocks, 93 — CLI, 95 — Security  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** Plugin SDK, Marketplace Services, Release Engineering

---

## 94.1 Purpose

Establish governance for plugins that contribute blocks, panels, prompts, and automation to the grid-based workspace. Packaging must ensure compatibility, predictable updates, and safe rollback while letting operators assemble modular UIs tailored to their equipment.

---

## 94.2 Manifest Requirements

| ID | Priority | Requirement | Notes |
|----|----------|-------------|-------|
| R-EXT-01 | MUST | Slot Declarations | Plugins declare the grid slots they target (`block.sidebar.left`, `panel.main`, `prompt.editor`). Conflicts resolve via priority + capability rules. |
| R-EXT-02 | MUST | Capability Scopes | Each contribution lists required permissions (read telemetry, write settings, control actuators). Ties into §95 enforcement. |
| R-EXT-03 | MUST | Layout Metadata | Provide default size, min/max spans, and recommended templates to ensure predictable scaling. |
| R-EXT-04 | SHOULD | Sub-Grid Schema | Panels that host sub-grids must ship schema describing internal layout, enabling host-managed resizing. |
| R-EXT-05 | MUST | Versioning | Declare semantic version and compatibility ranges for workspace schema (`WorkspaceLayout`) and required host APIs. |
| R-EXT-06 | MUST | Health Checks | Supply liveness probes for UI contributions (e.g., data availability) so the host can gray out unavailable blocks. |
| R-EXT-07 | SHOULD | CLI Extensions | Optionally register CLI verbs that mirror UI actions (layout apply, prompt automation). |

Manifests bundle with signed packages; CI rejects unsigned or incompatible submissions.

---

## 94.3 Packaging & Distribution

- **Official Catalog:** Host-signed bundles distributed via Nexus marketplace with dependency graph validation and staged rollout lanes.  
- **Offline Kits:** Provide exportable bundles (plugin + layout presets) so remote farms can install updates without connectivity.  
- **Delta Updates:** Support patch packages that update assets without re-downloading base media.  
- **Rollback:** Maintain multi-version cache; operators can roll back plugin contributions and associated layouts atomically.

---

## 94.4 Workspace Governance

1. **Preset Validation.** Before activating a workspace, the host validates all slot bindings, ensuring blocks map to available plugins and permissions.  
2. **Conflict Resolution.** When two plugins target the same slot, the host applies priority rules (`system` > `official` > `third-party`) then prompts operators to resolve ties.  
3. **Audit Trail.** Every change to layouts or plugin packages records provenance (who, when, source package) for compliance.  
4. **Sandboxing.** Plugins render UI through a declarative bridge (XAML/Avalonia or web view) without direct access to host visuals; data flows through sanitized contracts.  
5. **Degraded Mode.** If a plugin fails health checks, associated blocks collapse into safe fallback tiles with actionable remediation.

---

## 94.5 Tooling & CI

- Schema validators enforce manifest, slot, and capability definitions.  
- Automated UI smoke tests spin up host shell with plugin contributions and verify drag/resize compatibility.  
- Package pipelines sign bundles and publish compatibility reports for operators.  
- Regression dashboards track layout adoption and plugin health across fleets.

---

## 94.6 Open Questions

- How do we expose user-created presets to the marketplace while preserving authorship and safety review?  
- Should plugins declare optional vs. required sub-sidebars to better manage density?  
- What telemetry is necessary to rank plugin reliability for operators selecting bundles?
