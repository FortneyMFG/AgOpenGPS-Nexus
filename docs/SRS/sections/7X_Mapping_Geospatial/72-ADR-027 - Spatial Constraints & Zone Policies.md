# ADR-027: Spatial Constraints & Zone Policies

## Status
Accepted — 2025-05-17 architecture guild review

**Relevant Plugin(s):** Mapping, Autosteer, Section Control, Rate Control, Variable Mapping, UI Shell


## Context
Guidance, section control, and mapping teams need a shared way to represent field boundaries, headlands, keep-out areas, and work-disabled regions so automation respects legal and agronomic constraints. Today, plugins each interpret shapefiles or ad hoc polygons independently, which prevents Core from enforcing safety policies, leads to non-deterministic replays, and offers no way to coordinate guidance line trimming, section gating, or operator alerts. Upcoming guidance work ("Smarter AB") depends on headland-aware cost maps, auto-extend behavior at boundaries, and deterministic recovery when re-entering a field, while section control must hard-stop product in no-spray zones and log overrides for audits.

## Decision
Adopt a first-class zone model owned by Core:

- **Zone types:** `boundary`, `headland`, `keepout`, and `work_disabled`, each stored as buffered vector polygons with optional holes. Buffers distinguish drive vs. work clearance margins.
- **ZoneStore + ZoneService:** Persist zones in the geometry layer store, index them with an R-tree, and expose gRPC APIs (`ListZones`, `WatchZones`, `GetZonesInBounds`) for plugins and UIs. Zones share the project CRS defined in ADR-022.
- **PoseStream mask:** Extend PoseStream samples with a zone bitmask for the active implement footprint so automation and logging reproduce operator context during replay.
- **Constraint gating:** Insert a constraint gate in the control arbiter. Keep-Out intersections inhibit guidance/autosteer engagement and force sections off. Work-Disabled zones allow driving but force product off while logging the gate event. Boundaries and headlands trim guidance terminals and bias planning strategies.
- **Common UX contract:** Frontends render zones with canonical symbology (boundary outlines, headland hatching, keep-out red fill, work-disabled amber cross-hatch), expose enable/disable toggles, per-zone buffers, and override policy switches.
- **Interop:** Normalize imported/exported polygons (Shapefile, GeoPackage, ISOXML) into the shared schema, preserving provenance, priority, and buffers.

### PoseStream mask contract

PoseStream samples embed a `PoseZoneMask` message that captures the constraint state for the implement footprint. The mask exposes four canonical boolean flags—`inside_boundary`, `inside_headland`, `inside_keep_out`, and `inside_work_disabled`—that line up with policy decisions surfaced to automation and UI clients. Implementations also include the ordered list of intersecting zone identifiers and the SHA-256 (hex) hash of the zone registry snapshot (`zone_registry_hash`) that produced the evaluation so replay and controller pipelines can confirm they are operating on the same catalog. Consumers must treat unspecified flags as `false` for backwards compatibility and ignore unknown fields when newer registry data adds context.

Example JSON representation:

```json
{
  "zoneId": "9f4b9b2a-7f84-4d06-958a-0d6453af46a1",
  "type": "keepout",
  "label": "Rock pile",
  "priority": 90,
  "enabled": true,
  "buffers": { "drive_m": 2.0, "work_m": 5.0 },
  "validWhen": { "crop": "corn", "season": "2026", "conditions": [] },
  "provenance": { "source": "shp", "timestamp": "2025-02-14T18:22:03Z" }
}
```

## SRS Impact

- Satisfies spatial constraint storage, buffering, and indexing requirements R-DATA-026…R-DATA-028 in §08 Data Model & Storage.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L21-L23】
- Enables zone gating visibility and override workflows described in §05 Frontends (R-FE-041…R-FE-042).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L24】

## Consequences
- **Positive impacts**
  - Guidance plugins gain deterministic access to headland and keep-out geometry for intent inference, auto-extend, and recovery logic while remaining bounded by Core policies.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L25-L35】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L53-L76】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L37-L49】
  - Section control benefits from uniform gating semantics, ensuring product shutoff in no-work/keep-out zones with auditable logs and operator notifications.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L50-L60】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L34-L41】
  - UIs and interop flows display and edit the same zone metadata, reducing divergence across desktop, headless, and import/export tooling.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L37-L47】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L77-L88】
- **Negative/mitigated impacts**
  - Maintaining buffered polygons and R-tree indexes adds CPU cost; mitigated by caching last-known zone state and querying only candidate polygons per PoseStream sample.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L36-L45】
  - Operators need training on override policies; mitigated through UI toggles, alerts, and audit logs described in the Telemetry & Health section.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L48-L54】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L42-L49】
- **Follow-up actions**
  - Implement ZoneService, storage, and pose mask plumbing in Core.
  - Update guidance and section plugins to subscribe to zones, integrate keep-out costs, and honor constraint gates.
  - Ship UI editors/importers aligned with the shared schema and provenance logging.

## Governance Updates
- **Policy engine.** Constraints evaluate through a policy engine that supports conditional rules (crop stage, weather) without recompiling plugins. Policies are versioned and auditable.
- **Offline editors.** Operators receive offline editing tools with simulation previews that validate constraint changes before deployment.
- **Change management.** Constraint updates require sign-off from agronomy and safety leads, with provenance entries capturing rationale and expected outcomes.

## Validation
- **Zone propagation latency:** 95th percentile zone-mask propagation latency must remain ≤ 120 ms from ingest to section arbiter under a 20 Hz PoseStream load on the reference simulation fixture.
- **Constraint fault injection:** Forced keep-out toggles must block section enable within two PoseStream frames and emit override telemetry with actor, reason, and expiry populated for audit.
- **Audit retention:** Crash-recovery replay covering 30 minutes of operation must retain all but at most one override log entry when exercising autosave/journaling paths.

## Legacy Implementation Notes
### AgOpenGPS v6
- Field assets track boundaries and headlands through text exports (`Boundary.txt`, `Headland.txt`), and the WinForms runtime draws those polygons for lift cues, but there is no formal notion of keep-out or work-disabled zones beyond manual operator overrides.【F:docs/reference/agopengps-v6/porting/V6-Functionality-Gap-Analysis.md†L16-L25】【F:docs/SRS/references/aog-v6-mapping-brief.md†L23-L34】

### Legacy Dev Branch
- The dev branch inherits the same boundary/headland-only model, leaving constraint gating requirements such as zone masks and automated keep-out enforcement unsatisfied, which is why new SRS items call for a ZoneService and arbiter gating around keep-out/work-disabled areas.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L19-L24】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L23-L24】

## References
- [Section 42 — Transports](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Section 91 — UI Shell & Layout](../SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [Section 32 — Persistence & Formats](../SRS/sections/3X_Data_Storage/32_Persistence_Formats.md)
- [Section 61 — Kinematics & Pose Fusion](../SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [Section 64 — Telemetry & Health](../SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md)
