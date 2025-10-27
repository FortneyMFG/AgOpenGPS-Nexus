# 91 — UI Shell, Grid Layout & Workspace Orchestration
*(Status: Draft — grid-first rewrite)*

**Authors:** Nexus Working Group 9X  
**Created:** 2024-04-15 (rewritten 2025-10-20)  
**Version:** 1.0.0  
**Section ID:** 91  
**Editors:** Frontend & Operations Guild  
**Related Sections:** 13 — UI Framework & UX, 72 — Zone Drawing Framework, 94 — Extensibility & Packaging Updates  
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 92 — Gauges & Machine Panels, 95 — Security & Permissions, 97 — Simulation & Replay

---

## 91.1 Purpose & Scope

Define the Nexus shell as a grid-native workspace where every surface—maps, gauges, editors, and plugin contributions—exists as a configurable block or panel. The layout system must support full-page grids, nested sub-grids, drag-and-drop composition, and live reconfiguration so operators can scale between kiosks, tablets, and remote companions without bespoke wiring.【F:docs/UI/UI_Demo.html†L1-L263】

---

## 91.2 Modernization Drivers

- Legacy WinForms windows embed fixed panels, preventing reusable layouts across machines or seasons.
- Operators request predictable scaling between cab displays, office monitors, and remote browsers.
- Plugin authors need deterministic slots to add dashboards, inspectors, and controls without editing the host.
- QA needs deterministic serialization of workspaces to replay safety-critical screens.

---

## 91.3 Layout Primitives

| Primitive | Description | Notes |
|-----------|-------------|-------|
| **Root Grid** | Full-screen CSS-style grid that defines responsive columns and rows. All content snaps to grid coordinates rather than free-floating pixel math.【F:docs/UI/UI_Demo.html†L54-L134】 | Exposed through metadata schema (`GridDefinition`) and editable at runtime. |
| **Blocks** | Lightweight data tiles (gauges, status readouts) with optional quick actions. They occupy a rectangular grid span and can be stacked in dock columns or floated inside the root grid.【F:docs/UI/UI_Demo.html†L135-L221】 | Support opacity, sizing, and collision detection to avoid overlap. |
| **Panels** | Rich canvases (map, timeline, inspector) that host nested grids, charts, or canvases. Panels expose resize handles, docking edges, and context actions.【F:docs/UI/UI_Demo.html†L66-L108】【F:docs/UI/UI_Demo.html†L181-L221】 | Provide sub-grid definitions for consistent internal layout. |
| **Sidebars & Sub-Sidebars** | Vertical collections of blocks or launchers that can slide, pin, or collapse. “Sub-menu” surfaces from legacy docs are now “sub-sidebars” and are plugin-driven.【F:docs/UI/UI_Demo.html†L109-L180】 | Support keyboard traversal and manifest-based ordering. |
| **Prompts & Editors** | Modal or dockable panels for configuration (e.g., Edit Prompt). They obey the same grid sizing contract but may pin to edges for quick access.【F:docs/UI/UI_Demo.html†L181-L221】 | Must serialize state so edits replay during QA. |

---

## 91.4 Interaction Model

1. **Drag & Drop Composition.** All blocks and panels support grab handles; drop targets highlight available grid cells. Collision warnings prevent overlapping placements.【F:docs/UI/UI_Demo.html†L135-L221】  
2. **Responsive Scaling.** Grid definitions include minimum/maximum span, snap increments, and breakpoints so the same workspace renders on 11" tablets or 32" monitors without manual tweaks.  
3. **Sub-Grid Awareness.** Panels may host their own grids for toolbars or inspectors. Sub-grid constraints inherit from the parent layout to maintain consistent spacing.  
4. **Configuration Mode.** Unlock flows toggle edit affordances (gear buttons, resize handles). Configuration changes emit declarative diffs stored alongside presets for auditing.【F:docs/UI/UI_Demo.html†L16-L55】【F:docs/UI/UI_Demo.html†L181-L221】  
5. **Plugin Slots.** Each grid cell advertises capabilities (e.g., `map.primary`, `sidebar.left`, `config.prompt`). Plugins register surfaces by declaring compatible slots; the shell resolves conflicts via priority rules.

---

## 91.5 Requirements

| ID | Priority | Theme | Requirement | Verification |
|----|----------|-------|-------------|--------------|
| R-GRID-01 | MUST | Grid Engine | Provide runtime-editable root grid with persisted column/row definitions, gutters, and snap increments. | Workspace serialization & load/regression. |
| R-GRID-02 | MUST | Drag & Drop | Blocks and panels expose drag handles, collision detection, and snapping feedback. | UI integration test harness covering block moves. |
| R-GRID-03 | MUST | Nested Grids | Panels support sub-grids (e.g., map HUD, inspector stack) without bespoke per-panel logic. | Panel component tests. |
| R-GRID-04 | SHOULD | Templates | Ship default templates (Guidance, Coverage, Diagnostics) that operators can clone or remix. | Template smoke + preset diff tests. |
| R-GRID-05 | MUST | Accessibility | All grid interactions are keyboard accessible and announce resize/move events via accessibility APIs. | Accessibility regression. |
| R-GRID-06 | MUST | State Persistence | Layout updates serialize into preset/config manifests with versioning for replay. | Configuration replay tests. |
| R-GRID-07 | SHOULD | Remote Sync | Companion clients receive workspace diffs over the telemetry mesh; slow links coalesce changes. | Remote sync soak. |
| R-GRID-08 | SHOULD | Multi-Session | Different jobs or operator profiles load tailored grids while sharing system defaults. | Preset swap test. |
| R-GRID-09 | MUST | Plugin Slots | Define slot taxonomy and schema so plugins safely contribute blocks/panels/sub-sidebars. | Manifest validation. |

---

## 91.6 Integration Points

- **Gauges & Machine Panels (§92):** Provide block definitions, measurement vocab, and shared styling tokens for data tiles.
- **Extensibility (§94):** Governs plugin manifests declaring grid contributions, slot compatibility, and configuration editors.
- **Security (§95):** Enforces capability gating so unauthorized plugins cannot mount control panels or override layouts.
- **Simulation (§97):** Supplies deterministic datasets for layout stress tests (drag, resize, sync) during QA.

---

## 91.7 Data & Persistence

- Workspaces serialize into `WorkspaceLayout.v1` documents containing grid definitions, block placements, and plugin slot bindings.  
- Config mode writes diff patches so upgrades can merge operator customizations rather than overwrite them.  
- System presets live alongside plugin packages and declare compatibility ranges (screen size, capability scopes).

---

## 91.8 Constraints & Risks

- Must remain functional in offline cabins; all layout assets ship with the installer.  
- Remote clients may lag; UI must reconcile out-of-order layout diffs without corrupting state.  
- Drag-heavy interfaces risk input overload in rough environments—provide magnetic alignment, snap-back, and undo.

---

## 91.9 Open Questions

1. How do we expose grid editing APIs to automation without compromising security?  
2. What telemetry is required to understand layout drift across fleets?  
3. Should presets inherit from equipment profiles or remain operator-scoped?

---

## 91.10 Next Steps

- Finalize `WorkspaceLayout` schema and slot taxonomy.  
- Implement Avalonia grid editor prototype mirroring the HTML demo interactions.  
- Integrate plugin manifest validation into CI using §94 governance.
