# Job Tasks & Work Order Plugin

The Job Tasks plugin operationalizes TaskService work orders by bridging job templates, presets, and session lifecycle events.
General availability introduces a canonical persistence surface that writes <code>job.json</code>, manages session summaries, and
maintains the legacy <code>Resume.txt</code> marker so Drive-In and V6 compatibility flows continue to function.【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/JobTasksPersistence.cs†L1-L92】【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/ResumeFileWriter.cs†L1-L69】

## Scope

- Author and schedule work orders (planting, spraying, harvest) with assigned operators, implements, and planned inputs.
- Resolve presets/layout bundles on assignment, validating capabilities before dispatch and journaling provenance per ADR-032.
- Spawn or resume sessions automatically when operators accept work orders, injecting `workOrderId`, preset hash, and checklist defaults into session metadata.
- Provide checklist, notes, and attachment flows for mobile companions that sync into `Session.notes[]` for audit and contractor billing.

## Runtime contracts

- Consumes TaskService APIs to create, assign, and update work orders while emitting lifecycle events (`Assigned`, `InProgress`, `Completed`, `Cancelled`) to interested plugins (Profit, Regulatory, Telemetry Logging).
- Integrates with Inventory Ledger to reserve materials for planned work and reconcile consumption when sessions close.
- Publishes progress telemetry (percent complete, checklist counts, elapsed time) so dashboards and remote companions surface crew status in real time.

## UX considerations

- Desktop UI provides Kanban and calendar views with drag-and-drop assignment, dependency warnings, and preset readiness indicators.
- Mobile companions display per-order checklists, attachments (photos, QR receipts), and quick actions (Start Session, Add Note) optimized for offline use.
- Operators can batch-complete repetitive checklist items and record variances that flow to regulatory exports.

## Dependencies

- ADR-032 Presets & Layout Linking for preset resolution and task orchestration.
- ADR-041 Job Sessions for session metadata alignment and lifecycle hooks.
- ADR-050 Cost & Profit for labor/material reconciliation and contractor billing hooks.

## Persistence & Resume lifecycle

- `JobTasksPersistence` serialises `JobSnapshot` instances to disk, ensuring directories exist, writing `job.json`, and updating
  the legacy `Resume.txt` marker with deterministic metadata for autosave and Drive-In flows.【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/JobTasksPersistence.cs†L16-L92】
- `JobDocumentFactory` converts between in-memory models and the schema-conformant manifest, preserving spatial hints, asset
  references, and plugin extensions when round-tripping saves.【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/JobDocumentFactory.cs†L1-L262】
- `ResumeFileWriter` emits human-readable resume markers containing timestamps, active session identifiers, and operator roster
  data, allowing hardware companions to restore work without parsing the full JSON manifest.【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/ResumeFileWriter.cs†L1-L69】

### Usage

```csharp
var layout = new JobStoreLayout(jobRoot, Path.Combine(jobRoot, "data"), Path.Combine(jobRoot, "Resume.txt"));
var snapshot = new JobSnapshot(metadata, layout, sessions);
var persistence = new JobTasksPersistence();
await persistence.SaveAsync(snapshot, cancellationToken);
```

Loading a job from disk simply calls `LoadAsync(jobRoot)`, which resolves file-system defaults when the manifest omits optional
paths and returns a hydrated snapshot ready for orchestration or UI consumption.【F:Nexus SourceCode/src/Aog.Plugins/JobTasks/JobTasksPersistence.cs†L57-L92】
