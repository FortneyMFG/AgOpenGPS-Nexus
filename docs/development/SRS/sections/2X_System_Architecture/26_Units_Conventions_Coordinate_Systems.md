# 26 — Units, Conventions & Coordinate Systems
*(Status: Draft)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-24
**Version:** 0.1.0
**Section ID:** 26
**Editors:** Platform Foundations Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 23 — Threading, Scheduling & Timing, 31 — Domain Data Model, 61 — Kinematics & Pose Fusion, 71 — Mapping Kernel & Registry Contracts, 92 — Gauges & Machine Panels
**Upstream Dependencies:** ADR-022, ADR-026, ADR-031, CRS normalization matrix
**Downstream Impacts:** Data persistence, telemetry normalization, operator UX, analytics pipelines

---

## 26.1 Purpose & Scope

Establish global policies for units, coordinate reference systems (CRS), timestamps, and orientation conventions. The section anchors cross-cutting assumptions so Core services, transports, and UI surfaces exchange data without repeated unit negotiations or conflicting CRS decisions.【F:docs/development/SRS/references/crs-normalization-matrix.md†L1-L74】

---

## 26.2 Context

- Legacy AgOpenGPS mixed imperial/metric unit defaults per screen, causing export mismatches and calibration drift.
- Pose fusion, mapping, and telemetry components consume shared timestamps and orientation standards; drifting conventions introduce replay and analysis errors.
- CRS selection previously lived in §76; formalizing it here simplifies mapping extensibility and downstream registry governance.

---

## 26.3 Definitions

| Term | Definition |
|------|------------|
| SI Baseline | Nexus default unit system using meters, liters, kilograms, and degrees Celsius. |
| Imperial Override | Operator-selected unit profile (feet, acres, gallons) applied at presentation layers without changing canonical storage. |
| CRS | Coordinate Reference System describing ellipsoid, datum, and projection parameters (e.g., EPSG:4326). |
| Heading Convention | Orientation policy defining 0° (true north) and rotation direction (clockwise). |
| Timestamp Policy | Resolution, timezone (UTC), and synchronization expectations for timestamps across transports. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong recommendation; document deviations.
> - **MAY** = optional capability enabled by policy compliance.

## 26.4 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Verification |
|----|-----------|----------|---------|-----------------|--------------|
| R-UNIT-2600 | MUST | Units | Core services MUST store and transmit values in SI units with explicit metadata when alternative unit views are required. | ADR-022 | Contract tests validate SI defaults across protobuf/schema definitions. |
| R-UNIT-2601 | MUST | Presentation | UI surfaces MUST apply operator-selected unit profiles at render time without mutating persisted SI values. | UI policy notes | UI regression suite validates conversions. |
| R-UNIT-2602 | MUST | CRS Policy | Mapping components MUST use the CRS selection matrix in appendix A, defaulting to EPSG:4326 unless field assets demand projected CRS documented in manifests. | CRS normalization matrix | Registry lint ensures CRS assignments match approved list. |
| R-UNIT-2603 | MUST | Orientation | Pose fusion and guidance subsystems MUST publish headings using true-north 0° and clockwise positive rotation. | ADR-026 | Simulation replay harness validates heading consistency. |
| R-UNIT-2604 | SHOULD | Timestamp Synchronization | Transports SHOULD emit timestamps in UTC with microsecond resolution; offsets MUST be documented when hardware limits apply. | Timing charter | Telemetry capture verifies UTC alignment. |
| R-UNIT-2605 | MAY | Custom Profiles | Site-specific unit bundles MAY be added when accompanied by schema updates and operator training material. | Governance policy | Release checklist ensures documentation published. |

---

## 26.5 Acceptance Criteria & Verification

- Contract tests confirm protobuf and JSON schema defaults stay in SI units.
- Mapping registry lint validates CRS assignments against the normalization matrix.
- UI regression tests verify operator profiles convert dashboard, gauge, and report values without drift.
- Replay harness checks pose orientation and timestamp invariants remain consistent across simulation and hardware logs.

---

## 26.6 Interfaces & Dependencies

- §23 defines shared clock synchronization mechanisms; timestamp policies in this section align transports and telemetry.
- §31 and §32 persist normalized units and CRS identifiers; schema governance enforces metadata completeness.
- §61 consumes orientation policies for pose fusion and guidance; §71 references CRS/units metadata when exposing layer contracts.
- §92 relies on unit profiles to present gauges and panels; overrides remain presentation-only.

---

## 26.7 Risks & Open Issues

| ID | Description | Impact | Mitigation |
|----|-------------|--------|-----------|
| RISK-26-1 | External data sources provide undocumented CRS/units. | Medium | Maintain ingestion wizards with operator prompts; log provenance for manual resolution. |
| RISK-26-2 | Imperial overrides drift from SI baselines over long sessions. | Low | Schedule periodic calibration prompts and compare telemetry against SI references. |
| ISSUE-26-1 | Pending ADR covering timebase alignment for third-party sensors. | Medium | Track under ADR backlog; update timestamp policy when accepted. |

---

## 26.8 Decision History

- CRS normalization policy moved from §76 to this section to centralize global conventions (2025-10-24).

---

## Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

