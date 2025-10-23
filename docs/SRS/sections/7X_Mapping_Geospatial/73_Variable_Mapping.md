# 73 — Variable Mapping
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 73
**Editors:** Agronomy & Automation Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 32 — Persistence & Formats, 61 — Kinematics & Pose Fusion, 71 — Mapping Kernel & Registry Contracts, 72 — Mapping Layers Plugin, 77 — Variable Rate Control, 91 — UI Shell & Layout
**Upstream Dependencies:** ADR-010, ADR-014, ADR-044, ADR-068
**Downstream Impacts:** Analytics & Reporting pipelines, UI preview surfaces, §77 controller inputs

---

## 73.1 Purpose & Scope

Define how Nexus represents agronomic intent as variable mapping layers, transformations, and authoring workflows. The section aligns schemas, provenance, and preview pipelines so controllers (§77), analytics, and UI clients consume consistent rate surfaces without bespoke conversions or duplicated tooling.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L11-L53】

---

## 73.2 Context

- Variable mapping relies on registry-governed schemas that encode agronomic units, provenance metadata, and controller hints for downstream consumers.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L224-L229】
- Import/export pipelines convert ISOXML, Shapefile, and GeoJSON prescriptions into canonical layer structures with audit trails captured in §63 journals.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L1-L43】【F:docs/SRS/sections/6X_Core_Domain_Services/63_Layers_Registry_Journal.md†L18-L74】
- Authoring tools leverage zone editing (§72) and unit conventions (§26) to produce consistent rate surfaces and preview maps across desktop and companion clients.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L64-L118】【F:docs/SRS/sections/2X_System_Architecture/26_Units_Conventions_Coordinate_Systems.md†L15-L64】
- Controllers described in §77 consume the normalized outputs from this section; telemetry (§64) compares planned vs. executed rates for analytics and QA.

---

## 73.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Rate Metadata | Prescriptions relied on shapefile attributes interpreted per controller. | Inconsistent units and scaling across devices. | Adopt schema-governed metadata with controller hints. | Metadata-driven VR RFC |
| Layer Authoring | Edits performed in bespoke tools with limited provenance tracking. | No shared undo history or audit trail for agronomy teams. | Use §72 zone framework with journaled edits and provenance metadata. | ADR-044 zone framework |
| Import/Export | Format conversion handled by per-project scripts. | Divergent scaling and CRS conversions caused mismatched rates. | Centralize conversion through registry-aware import/export services. | ADR-014 interop plan |

---

## 73.4 Definitions

| Term | Definition |
|------|-------------|
| Variable-Rate Layer | Layer registry entry describing planned or actual application rate per spatial cell. |
| Controller Hint | Metadata guiding ramp rates, smoothing windows, or nozzle timing for §77 consumers. |
| Authoring Session | Workspace that captures edits, provenance, and validation events for a prescription. |
| Preview Surface | Rendered visualization combining planned rates, cutlines, and context layers for validation. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong guidance; deviations documented.
> - **MAY** = optional extension gated by telemetry + ADR sign-off.

## 73.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-VM-7300 | MUST | Schema Governance | Variable-rate layers MUST include units, controller hints, valid ranges, and provenance metadata as defined in Layer Registry schemas. | ADR-010, Metadata VR RFC | Schema validation & layer lint tooling. |
| R-VM-7301 | MUST | Transform Pipeline | Import workflows MUST normalize spatial data, CRS, and units per §26 before committing layers to the registry. | ADR-014 | Import regression tests verify normalized outputs. |
| R-VM-7302 | SHOULD | Authoring Workspaces | Authoring sessions SHOULD capture provenance (who/when/why), undo history, and validation status for agronomic reviews. | ADR-044 | UI/editor tests ensure journals persist metadata. |
| R-VM-7303 | SHOULD | Preview & QA | UI clients SHOULD render preview surfaces combining planned rates, context layers, and keep-outs with simulated rate statistics. | UI shell roadmap | Preview regression harness validates overlays. |
| R-VM-7304 | MUST | Export Fidelity | Exporters MUST round-trip canonical layers into ISOXML/Shapefile/GeoJSON without losing units, bounds, or provenance metadata. | ADR-014 | Import/export smoke tests verifying metadata retention. |
| R-VM-7305 | MAY | Derived Layers | Derived agronomic analytics MAY annotate variable-rate layers when tagged with provenance and transformation metadata. | Analytics ADRs | Analytics lint ensures derived layers record origin IDs. |

---

## 73.6 Acceptance Criteria & Verification

- Schema lint tooling confirms registry entries populate required metadata, units, and provenance fields.
- Import/export regression tests confirm canonical layers survive ISOXML/Shapefile/GeoJSON round-trips without drift.
- Authoring previews demonstrate synchronized edits across desktop and companion clients with matching analytics overlays.

### 73.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-VM-7300 | Schema lint | `tools/registry/vr_layer_lint.py` | 100% of VR layers populate required metadata fields. |
| R-VM-7301 | Import regression | `tests/integration/prescription_roundtrip.cs` | Normalized layers match canonical baseline. |
| R-VM-7302 | UI regression | `tests/ui/variable_rate_authoring.feature` | Authoring sessions persist provenance + undo history. |
| R-VM-7303 | Preview harness | `tests/ui/preview_variable_rate.md` | Preview overlays match canonical layer statistics. |
| R-VM-7304 | Export regression | `tests/integration/prescription_export.cs` | Units + provenance preserved after round-trip. |
| R-VM-7305 | Analytics lint | `tools/registry/derived_layer_guard.py` | Derived layers record origin layer IDs. |

---

## 73.7 Constraints

- Layer transformations must complete within workstation-class budgets so previews stay interactive (≤ 250 ms for 1M-cell layers).
- Import pipelines must operate offline for remote agronomy teams; cache required CRS/units tables from §26 locally.
- Registry governance prohibits direct mutation of canonical layers without journal entries; all edits must route through authoring workflows.

### 73.7.1 Non-Functional Requirement Classes

- **Performance:** Transform runtime, preview responsiveness, bulk import throughput.
- **Reliability:** Provenance completeness, journal integrity, schema validation success rates.
- **Safety:** Keep-out honoring, metadata validation preventing erroneous rates.
- **Operability:** Multi-client previews, undo/redo availability, conflict resolution UX.
- **Maintainability:** Schema-governed metadata, automation for derived layer tagging.

---

## 73.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-73-1 | External prescriptions lack metadata for normalization. | Medium | Provide ingestion wizard + advisory warnings; encourage schema export from advisors. | @product |
| RISK-73-2 | Large derived layers inflate TileStore storage costs. | Medium | Offer on-demand generation and pruning policies. | @mapping |
| ISSUE-73-1 | Define collaborative editing conflict resolution rules for concurrent agronomy teams. | Medium | Track under ADR backlog; prototype merge strategies. | @agronomy |
| ISSUE-73-2 | Align analytics-derived overlays with §77 controller hints without duplication. | Low | Document mapping between derived metadata and controller inputs. | @analytics |

---

## 73.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Metadata Governance | Layer schemas drive interoperability; registry automation prevents drift. |
| C2 | Provenance First | Authoring sessions capture who/what/when to support audits and collaboration. |
| C3 | Telemetry Insight | Planned vs. actual comparisons require consistent metadata and journal links. |
| C4 | Format Interoperability | Supporting ISOXML/GeoJSON/Shapefile preserves compatibility with agronomy partners. |
| C5 | Preview Confidence | Operators need high-fidelity previews and statistics before committing rates. |
| C6 | Controller Alignment | Outputs must package controller hints for §77 without duplicating safety logic. |

### 73.9.1 Assumptions & Preconditions

- [A1] Mapping plugins deliver normalized union envelopes and keep-out geometries in real time.
- [A2] Telemetry mesh remains available with sufficient bandwidth for planned vs. actual comparisons.
- [A3] Agronomy teams provide canonical units/CRS metadata when importing third-party prescriptions.

---

## 73.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All trade-offs incorporated as design considerations in §73.9. | — |

---

## 73.11 Comparison Matrix

| Attribute / Criteria | Metadata-Driven Mapping Pipeline | Legacy Prescription Handling |
|----------------------|--------------------------------|-------------------------------|
| Provenance | Journaled edits with author + rationale | Manual notes or none |
| Unit/CRS Consistency | Registry-governed normalization per §26 | Controller-specific assumptions |
| Preview Confidence | Multi-layer previews with statistics | Static shapefile overlays |
| Import/Export | ISOXML/GeoJSON/Shapefile with provenance | Vendor-specific shapefile variants |
| Controller Alignment | Embedded hints + bounds for §77 | Manual retuning per implement |
