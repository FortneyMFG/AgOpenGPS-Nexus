# Job Tasks Plugin

## Overview
The Job Tasks plugin manages job documents, tracks execution state, and coordinates season/session lifecycles. It provides the backbone for field operations workflows, ensuring that guidance, sections, rate control, and telemetry align with the active job.

## Capabilities
- `jobs.lifecycle` exclusive capability to open/close jobs and manage session state.
- `jobs.documents` shared capability for plugins that need read access to job metadata.
- `reporting.jobs` for contributing job-centric reports.

## Core Integration
- `JobSeasonSessionOrchestrator.cs` implements `IJobSeasonSessionOrchestrator` to coordinate session transitions (open, pause, resume, close).
- `JobDocumentFactory.cs` creates `JobDocument` instances from templates or external imports.
- `JobLifecycleStateExtensions.cs` houses helper logic for lifecycle transitions.
- Background services publish lifecycle events on the event bus, allowing dependent plugins to react deterministically.

## UI Integration
- Powers the Field Operations dialogs (`Views/FieldOperations/**`) and provides toolbar/menu commands for opening job wizards.
- When packaged as a zip, register windows (job manager, field chooser) via `IWindowProvider` and command handlers for the shell menus.
- Updates the status strip to show active job, field, and operator when a session is running.

## Dependencies
- Interacts with File IO for job import/export and Mapping for field boundary context.
- Downstream plugins (Sections, AutoSteer, Telemetry Logging) consume job state to scope their recordings.

## Packaging Notes
- Manifest should declare optional dependencies on File IO and Mapping to ensure rich job metadata.
- Include sample job templates under `assets/templates/` for onboarding.

## Related Resources
- `docs/plugins/FieldOperations.md` (if present) for UI details.
- `docs/plugins/tutorials/first-plugin.md` shows how to register windows similar to the job manager.
- ADRs concerning operations workflows provide additional background.

