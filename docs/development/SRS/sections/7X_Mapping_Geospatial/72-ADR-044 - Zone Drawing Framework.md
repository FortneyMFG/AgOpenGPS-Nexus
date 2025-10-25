# ADR-044 — Zone Drawing Framework

- **Status:** Drafting
- **Date:** -
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Mapping, analytics, and agronomy plugins each implement their own geometry editors for crop zones, damage areas, and manual
adjustments. The lack of a shared toolset causes duplicate UX, inconsistent provenance, and conflicts when multiple plugins edit
the same layer. Core needs a unified drawing system that understands the Farm→Field→Job hierarchy, honors session provenance,
and exposes deterministic APIs so plugins can annotate spatial layers without reimplementing geometry math.

## Decision

Deliver a Core-owned Zone Drawing Framework that ships common geometry editing components, persistence hooks, and provenance
logging. The framework exposes a `LayerEditService` that orchestrates editing sessions and brokers events between the UI, Core
tile store, and interested plugins. It standardizes toolbar affordances (polygon, rectangle, brush, eraser), attribute editing,
undo/redo, and merge/split utilities. Plugins register which layer IDs and attribute schemas are editable; Core remains the
source of truth for storage, IDs, and audit trails.

### Core APIs

- `drawLayer(layerId, options)` — Creates a new vector layer and opens an editing session. Options include geometry type hints,
default attributes, and provenance seed metadata.
- `editFeature(layerId, featureId, operations)` — Applies geometry or attribute edits to an existing feature using structured
operations (move vertex, reshape, attribute patch).
- `mergeZones(layerId, featureIds[])` / `splitZone(layerId, featureId, strategy)` — Deterministic spatial operations with topology
validation and provenance entries.
- `LayerEditService` publishes lifecycle events:
  - `onLayerStartEdit(context)` — Fired when a layer enters edit mode; context includes farm/season/job/session IDs, layer
    metadata, and plugin-declared attribute schema.
  - `onFeatureCommit(event)` — Fired for every feature create/update/delete. Carries diff summaries, geometry hashes, actor, and
    timestamp.
  - `onUndo(event)` / `onRedo(event)` — Fired when the undo stack mutates so analytics plugins can reconcile deltas.

### Storage & Provenance

- All edits record `LayerEditEvent.v1` documents containing immutable IDs, actor, timestamps, operations, and provenance
  references to the affected layer tiles.
- Geometry is persisted via Core tile storage. Plugins may provide attribute schema descriptors but cannot override storage
  layout or ID assignment.
- Undo/redo uses an append-only journal with deterministic hashes so replays and collaborative edits remain consistent across
  devices.

### UI & Interaction

- Shared toolbar states (polygon, rectangle, brush, eraser) live in Core UI. Plugins contribute attribute panels via declarative
  metadata and may supply tag pickers, dropdowns, or numeric editors.
- The framework supports keyboard shortcuts, snapping, and field boundary awareness. Multi-field jobs surface all mounted fields
  during editing, enforcing envelope checks before commits.

### Plugin Integration

- Plugins declare editable layer IDs and attribute schemas through their manifests. Core validates declarations against
  capability policies before exposing editing.
- Zone-aware plugins (Crop Type, Genetics, Yield, Profit, Field Health) register event handlers to recompute analytics upon
  `onFeatureCommit`.
- Collaborative scenarios leverage the Multi-Machine telemetry mesh (ADR-047) to replicate edit journals across devices.

## Consequences

- Operators receive a consistent editing experience across plugins, reducing training time and duplicated UX.
- Provenance becomes auditable: every geometry change is linked to jobs, sessions, and actors with replayable journals.
- Plugin teams focus on attribute semantics instead of geometry math, accelerating new overlay development.
- Core must own tile-store concurrency, undo stack persistence, and eventual conflict resolution when offline edits merge.

## Governance Updates

- **Layer registry alignment.** Editable layer declarations must reference the canonical registry entries introduced with the
  multi-field envelope update so Crop, Genetics, Yield, Profit, Field Health, and Weather plugins remain provenance-compatible
  when sessions span multiple fields.【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L9-L112】【F:schemas/Layer.v1.json†L1-L120】
- **Undo/redo determinism gate.** `LayerEditEvent.v1` fixtures now ship alongside each plugin bundle. CI compares journal hashes
  during replay to block releases that diverge from the reference set, ensuring collaborative edits remain deterministic across
  mesh participants.【F:schemas/LayerEditEvent.v1.json†L1-L140】
- **Collaborative audit trail.** Mesh share profiles must declare which edit events they replicate. ZoneService rejects
  publications that omit provenance hashes or exceed the per-event payload budget defined in the manifest governance
  program.【F:schemas/ShareProfile.v1.json†L1-L120】【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L17-L70】

## Amendment — 2025 architecture refresh (NX-190)

- Editing sessions record the mounted `fieldIds[]` and union envelopes published by JobsService so replay, profit, and weather
  analytics can reconcile edits with the active job context without bespoke geometry joins.【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L13-L96】
- `LayerEditService` broadcasts session lifecycle hooks (`onSessionStart`, `onSessionEnd`) and mesh presence metadata, allowing
  Profit, Yield, and Weather plugins to coalesce analytics windows with the exact edit ranges captured in journals.【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L9-L70】【F:schemas/Session.v1.json†L1-L120】
- Collaborative edits over the Live Telemetry Mesh carry deterministic replay seeds so remote devices rebuild identical undo
  stacks even when RadioBridge transports introduce retries or resequencing during constrained connectivity.【F:docs/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L9-L60】

## Alternatives Considered

1. **Plugin-owned editors.** Rejected due to inconsistent UX, conflicting shortcuts, and divergent provenance stories.
2. **GIS library embedding.** Heavyweight desktop GIS frameworks add licensing risk and do not integrate with Nexus session
   provenance or telemetry meshes without substantial glue code.

## Dependencies

- Builds atop the Farm→Field→Season→Job→Session context defined in ADR-040/041/043.
- Supplies tooling required by ADR-045 (Crop Type), ADR-046 (Genetics), ADR-049 (Yield), ADR-050 (Profit), ADR-052 (Field Health),
  and ADR-053 (Weather overlays).

## SRS Impact

- Fulfills R-DATA-040 and R-DATA-043 journal/schema expectations in §08 Data Model & Storage.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L28-L33】
- Delivers shared toolbar and attribute panel experiences required by R-FE-070, R-FE-071, and R-FE-093 in §05 Frontends.【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L36】
- Binds editing lifecycle hooks referenced by R-FE-033 and related job lifecycle events in §03 Job Lifecycle.【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L48】【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L21-L35】
- Adds `LayerEditEvent.v1` schema under `/schemas` with examples for regression testing.

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

