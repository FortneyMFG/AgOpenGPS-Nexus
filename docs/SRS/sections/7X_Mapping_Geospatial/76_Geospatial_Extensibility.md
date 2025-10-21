# 76 — Geospatial Extensibility
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 76
**Editors:** Mapping Platform Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 71 — Mapping Kernel Contracts, 72 — Mapping Layers Plugin, 75 — Tiling & Rendering Services, 94 — Extensibility Packaging Updates
**Upstream Dependencies:** ADR-022 CRS Precision, ADR-044 Zone Drawing Framework, ADR-010 Layer Registry, Offline Sync Section 33
**Downstream Impacts:** Plugin SDK, Registry automation, UI shells, Collaborative sync services

---

## 76.1 Purpose & Scope

Establish policies and tooling that let third parties introduce new geospatial layer types, projections, and analytics while remaining compatible with Nexus Core registries, tiling services, and UI clients. The section governs CRS support, custom layer registration, editing, import/export, and capability discovery.

---

## 76.2 Context

- CRS utilities and precision policies in ADR-022 define acceptable EPSG codes, unit conversions, and coordinate tolerances for all layers.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-022 - CRS units and precision policy.md†L1-L44】
- Mapping kernel contracts (Section 71) and Layer Registry schemas provide the authoritative catalog for built-in layers and provenance metadata.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L1-L132】
- LayerEditService (ADR-044) delivers shared editing tools, journals, and undo/redo flows that plugins must extend for custom layers.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】
- Extensibility packaging (Section 94) governs plugin manifests, capability discovery, and compatibility metadata for UI and automation consumers.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】

---

## 76.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| CRS Handling | Plugins embedded custom projection math or assumed WGS84. | Spatial precision drifted across layers and devices. | Centralize CRS utilities with ADR-022 precision policies. | CRS audit |
| Layer Registration | Custom layers required code changes in Core. | Slow iteration; risk of registry collisions. | Allow metadata-driven registration with validation hooks. | Registry backlog |
| Collaborative Sync | Third-party layers synced via ad-hoc file copies. | Conflicts and divergence across machines. | Reuse journal-based sync and conflict resolution. | Offline sync RFC |

---

## 76.4 Definitions

| Term | Definition |
|------|-------------|
| CRS Utility | Library functions that convert between EPSG codes, enforce precision, and provide projection metadata. |
| Custom Layer Manifest | Plugin-supplied metadata describing geometry type, attributes, schema version, and registry integration. |
| Capability Descriptor | Manifest section advertising feature availability, version, and compatibility constraints. |
| Collaborative Journal | Append-only log of layer edits used to synchronize multi-machine sessions. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong guidance subject to documented waiver.
> - **MAY** = optional capability requiring capability advertisement and telemetry coverage.

## 76.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-GEO-7600 | MUST | CRS Support | Provide CRS utilities (EPSG lookup, unit conversions, precision enforcement) aligned with ADR-022. | ADR-022 | CRS unit tests covering supported EPSG codes. |
| R-GEO-7601 | SHOULD | Layer Registration | Allow plugins to register custom schema definitions with validation hooks and provenance metadata. | Mapping plugin SRS | Registry integration tests ensuring custom schemas tile correctly. |
| R-GEO-7602 | MUST | Editing Tooling | Extend LayerEditService to support custom geometry editors, attribute panels, and undo/redo for plugin layers. | ADR-044 | Editing automation verifying journal replay. |
| R-GEO-7603 | SHOULD | Import/Export | Provide adapters to map custom layers to open formats while preserving provenance and schema versions. | Persistence formats | Import/export regression tests verifying metadata retention. |
| R-GEO-7604 | MUST | Capability Discovery | Plugin manifests MUST advertise new capabilities, compatibility ranges, and graceful degradation paths. | Extensibility packaging | Manifest lint verifying required descriptors. |
| R-GEO-7605 | SHOULD | Multi-Machine Consistency | Custom layers SHOULD synchronize via journals and conflict resolution rules used by built-in layers. | Offline Sync Section 33 | Sync simulation verifying conflict resolution. |

### 76.5.1 Custom Layer Registration Workflow

1. Plugin submits manifest including layer ID namespace, schema hash, CRS requirements, and attribute metadata.
2. Registry automation validates uniqueness, schema compatibility, and provenance expectations.
3. LayerEditService extensions register geometry editors, attribute panels, and validation hooks.
4. Capability descriptors published so UI shells enable/disable related features dynamically.

---

## 76.6 Acceptance Criteria & Verification

- CRS test suite verifies coordinate conversions, precision tolerances, and supported EPSG codes across plugins.
- Registry integration tests ingest custom layer manifests and ensure TileStore persists/tiles payloads correctly.
- Editing automation replays custom layer journals to validate undo/redo and collaborative behavior.
- Import/export tests confirm adapters retain provenance metadata across open formats.
- Capability discovery tests verify UI shells react gracefully when capabilities are missing.

### 76.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-GEO-7600 | Unit tests | `tests/crs/crs_precision.cs` | Coordinate conversions within tolerance specified by ADR-022. |
| R-GEO-7601 | Integration test | `tests/registry/custom_layer_registration.cs` | Custom layer tiles generated with valid schema + provenance. |
| R-GEO-7602 | Functional test | `tests/integration/custom_layer_editing.cs` | Undo/redo + collaborative replay succeed. |
| R-GEO-7603 | Import/export test | `tests/integration/custom_layer_roundtrip.cs` | Provenance + schema version preserved. |
| R-GEO-7604 | Manifest lint | `tools/plugins/manifest_lint.py` | 100% manifests include capability descriptors + compatibility ranges. |
| R-GEO-7605 | Sync simulation | `tests/sync/custom_layer_collaboration.cs` | Journals converge across peers with no data loss. |

---

## 76.7 Constraints

- Registry namespaces must be coordinated to avoid collisions; plugin submissions require review before publication.
- CRS utilities must avoid heavy runtime dependencies; precompute lookup tables where possible.
- Custom geometry editors must conform to UI shell accessibility and localization requirements.

### 76.7.1 Non-Functional Requirement Classes

- **Performance:** CRS conversions, tiling throughput for custom layers.
- **Reliability:** Journal replay accuracy, registry validation coverage.
- **Security:** Manifest signing, namespace approval, and sandboxing of custom tooling.
- **Usability:** Consistent editing UX, clear capability messaging, localization support.
- **Operability:** Monitoring for registry submissions, sync diagnostics, adapter health.

---

## 76.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-76-1 | CRS library expansion increases bundle size on embedded targets. | Medium | Provide modular CRS packages; include only required EPSG sets. | @platform |
| RISK-76-2 | Custom layer conflicts during collaborative editing. | High | Enforce journal conflict resolution hooks; add telemetry alerts. | @mapping |
| ISSUE-76-1 | Define review SLA for new namespace submissions. | Medium | Governance working group to publish policy. | @registry |
| ISSUE-76-2 | Clarify fallback behavior when capability descriptor missing in manifest. | Low | Document safe defaults in Section 94. | @product |

---

## 76.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Registry Governance | Namespace approval and schema hashing maintain catalog integrity. |
| C2 | CRS Precision | Enforcing ADR-022 prevents spatial drift across plugins. |
| C3 | Extensible Editing | Shared LayerEditService extensions reduce duplicated tooling. |
| C4 | Interoperable Imports | Adapters ensure custom layers remain portable across ecosystems. |
| C5 | Capability Signaling | Manifests allow UI/automation to adapt to missing providers. |
| C6 | Collaborative Sync | Journals and conflict resolution keep layers consistent across machines. |

### 76.9.1 Assumptions & Preconditions

- [A1] Plugin manifests are signed and distributed through approved channels.
- [A2] Registry automation processes submissions before plugins activate new layers.
- [A3] Collaborative sync infrastructure (Section 33) remains available for multi-machine scenarios.

---

## 76.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All prior options integrated as design considerations in §76.9. | — |

---

## 76.11 Comparison Matrix

| Attribute / Criteria | Extensible Geospatial Platform | Legacy Plugin Extensions |
|----------------------|--------------------------------|--------------------------|
| CRS Management | Central ADR-022 utilities + precision policies | Plugin-specific math |
| Layer Registration | Metadata-driven with registry validation | Core code changes required |
| Editing Experience | Shared LayerEditService extensions | Custom, inconsistent tooling |
| Import/Export | Adapters with provenance preservation | Manual scripts with limited metadata |
| Capability Discovery | Manifest-driven compatibility metadata | Implicit, error-prone assumptions |
