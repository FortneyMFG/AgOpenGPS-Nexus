# ADR-032: Presets and Layout Linking for Equipment Workflows

## Status
Proposed

## Context
Operators need to rapidly switch between tractor + implement combinations while preserving UI layouts and long-running preparation tasks. Current workflows require manual tweaking of machine profiles, implement settings, and screen layouts each time the job type changes (planting, spraying, harvest). This is error-prone, delays fieldwork, and causes layout drift when improvements are not propagated across machines. The UI also lacks a way to expose progress for the background jobs triggered when configurations change.

## Decision
Adopt a Preset model that binds Equipment, Implement, and Layout selections into reusable bundles with support for either live-linked or snapshot Layout references. Layouts will be versioned documents that can inherit from a parent, enabling organization-wide baselines with local overrides. The system will surface dependency graphs, diff tooling, and guardrails to manage changes. Applying a preset will trigger Task executions (e.g., loading implement profiles, warming up GNSS) whose progress is observable in the UI.

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

## Legacy Implementation Notes
### AgOpenGPS v6
- No formal preset system; operators manually adjust tractor, implement, and layout settings and rely on per-machine configuration files.
- Layout updates do not propagate automatically and there is no diff or dependency awareness tooling.
- Background tasks triggered by configuration changes are opaque and cannot be retried or rolled back.

### Legacy Dev Branch
- Similar to v6 with incremental UI persistence but without preset-linking, layout versioning, or surfaced task orchestration.

## References
- [SRS §2.8 Documentation](../SRS/NOTES.md#srs-28-documentation)
- [SRS §4.2 Safety & QA](../SRS/NOTES.md#srs-42-safety--qa)
- [SRS §12 Extensibility & Plugins](../SRS/sections/12_Extensibility_Plugins.md)
- Tasks spec excerpt provided by product stakeholders (internal notes)
