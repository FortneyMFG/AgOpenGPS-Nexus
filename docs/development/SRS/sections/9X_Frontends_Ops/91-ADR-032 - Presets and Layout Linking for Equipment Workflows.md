# ADR-032: Presets and Layout Linking for Equipment Workflows

## Status
Accepted

NX-130 advanced this ADR through review, locking the preset/layout model as the
authoritative workflow for equipment orchestration across desktop and companion
clients.

**Relevant Plugin(s):** UI Shell (Presets), Device Manager, Mapping, Autosteer, Section Control, Rate Control, Job Tasks, Variable Mapping



## Context
Operators need to rapidly switch between tractor + implement combinations while preserving UI layouts and long-running preparation tasks. Current workflows require manual tweaking of machine profiles, implement settings, and screen layouts each time the job type changes (planting, spraying, harvest). This is error-prone, delays fieldwork, and causes layout drift when improvements are not propagated across machines. The UI also lacks a way to expose progress for the background jobs triggered when configurations change.

## Decision
Adopt a Preset model that binds Equipment, Implement, and Layout selections into reusable bundles with support for either live-linked or snapshot Layout references. Layouts will be versioned documents that can inherit from a parent, enabling organization-wide baselines with local overrides. The system will surface dependency graphs, diff tooling, and guardrails to manage changes. Applying a preset will trigger Task executions (e.g., loading implement profiles, warming up GNSS) whose progress is observable in the UI.

## Governance Updates
- **Conflict resolution.** Preset editor introduces optimistic locking with merge UI for concurrent edits, and conflicts are logged with operator attribution.
- **Execution logging.** Preset applications write success/failure outcomes into provenance records so automation drift can be audited per job.
- **Dependency gating.** Applying presets now checks capability and constraint readiness, blocking activation when prerequisite services are degraded and surfacing actionable remediation guidance.

## Amendment — 2025 architecture refresh (NX-190)

- TaskService orchestration will be reused by future Work Order flows (NX-170). This ADR documents the dependency but leaves work order scope out-of-bounds for this pass.
- Work Order intents authored through the TaskService inherit preset/layout bindings. When managers create a work order that references a job template, the PresetsService resolves the matching preset bundle, records its layout version hash, and exposes the dependency list to TaskService so crew assignments can validate implements before dispatch. Checklist progress coming back from mobile companions updates the same preset application record, keeping provenance aligned between preparation tasks and field execution.

## Degraded operation & messaging
- **Missing dependencies:** When required dependencies (e.g., Sections or Mapping plugins) are absent or unhealthy, the Preset Switcher exposes a disabled state with inline reasons sourced from the dependency matrix (ADR-031). Operators can still review presets/layouts, but task orchestration is paused until dependencies recover. Background retries are surfaced as toast notifications rather than silent failures.
- **Job service offline:** Presets that rely on job context degrade to snapshot-only mode. The UI labels the active job as "Local-only" and automatically journals preset/layout selections so the JobsService can reconcile once it returns. Drive-In prompts continue to function using cached presets, but any automation that normally journaled to the job metadata emits warnings in the Activity pane.
- **Layout link breakage:** When a live-linked layout version becomes unavailable (missing file, failed migration), the operator is prompted to either re-link to the last known good version or convert to a snapshot. Changes remain local until the operator explicitly resolves the broken link, preventing silent divergence across machines.

## Consequences
- Positive impacts
  - One-click preset switching reduces setup friction and maintains consistency across machines through live layout linking.
  - Snapshot mode supports experimentation without impacting shared layouts, and makes rollback easy.
  - Versioned layouts with inheritance encourage reusable baselines while allowing site-specific tweaks.
  - Task orchestration exposes progress and failure handling to operators, improving trust in automation flows.

- Negative/mitigated impacts
  - Layout versioning and diff tooling add complexity; provide UI prompts when editing linked layouts to steer users toward safe actions.
  - Live links risk unexpected changes; banner notifications, lock flags, and optional conversion to snapshots mitigate surprises.
  - Inheritance resolution requires deterministic overlay rules (parent ⊕ overrides with block-level last-write-wins) and CAS-guarded saves to prevent race conditions.

- Follow-up actions
  - Implement PresetsService and LayoutsService gRPC endpoints, along with SDK helpers (`sdk.presets.*`, `sdk.layouts.*`, `sdk.tasks.*`).
  - Build Preset Switcher, Editor, and Layout Diff Viewer UI surfaces including multi-screen awareness and hotkeys.
  - Deliver TaskService with in-memory queue, progress streaming, and integration with preset application flows.
  - Seed catalog fixtures (e.g., "Planting – 12R", "Sprayer – 120ft") to validate multi-screen behavior and linked/snapshot flows.
- Provide migration tooling for layout JSON schemas (`migrate(LayoutJson, fromVersion)`), and regression tests covering inheritance, linking, and task execution.
- Coordinate with the layer registry hash handshake draft to ensure controller boot flows validate registry hashes before activating presets ([reference](../../../../Core/reference/layer-registry-handshake.md)).

## Accepted scope & invariants
- Preset bundles must persist dependency graphs that align with the manifest
  governance matrix so Device Manager and PresetsService present identical health
  states during orchestration.【F:docs/development/SRS/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L15-L62】
- Layout inheritance and live-link semantics are now normative for UI pods; updates
  must respect the deterministic overlay rules and provenance requirements captured
  in the frontend SRS section.【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L88】
- Task orchestration for preset application is officially bound to the JobsService
  lifecycle contracts, ensuring preset provenance is journaled alongside job/session
  metadata for auditing.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L20-L96】

## Legacy Implementation Notes
### AgOpenGPS v6
- No formal preset system; operators manually adjust tractor, implement, and layout settings and rely on per-machine configuration files.
- Layout updates do not propagate automatically and there is no diff or dependency awareness tooling.
- Background tasks triggered by configuration changes are opaque and cannot be retried or rolled back.

### Legacy Dev Branch
- Similar to v6 with incremental UI persistence but without preset-linking, layout versioning, or surfaced task orchestration.

## References
- [SRS §2.8 Documentation](../../NOTES.md#srs-28-documentation)
- [SRS §4.2 Safety & QA](../../NOTES.md#srs-42-safety--qa)
- [SRS §12 Extensibility & Plugins](../9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- Tasks spec excerpt provided by product stakeholders (internal notes)
