# Job Tasks & Work Order Plugin (Planned)

The Job Tasks plugin operationalizes TaskService work orders by bridging job templates, presets, and session lifecycle events.

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
