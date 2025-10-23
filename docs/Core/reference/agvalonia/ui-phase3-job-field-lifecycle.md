# NX-413 Job & Field Lifecycle Dialogs Port

## Objectives
- Rebuild job and field lifecycle dialogs described in [`artifacts/ui-backlog.json`](../../artifacts/ui-backlog.json) and [`artifacts/ui-inventory.json`](../../artifacts/ui-inventory.json).
- Align Avalonia implementations with Nexus job/session services and metadata-driven styling tokens.
- Preserve operator workflows from legacy WinForms, Dev branch, and AgValonia sources while satisfying plugin injection points defined in [`artifacts/ui-to-plugin.yaml`](../../artifacts/ui-to-plugin.yaml).

## Core Surfaces

### Job Manager (`job_manager_dialog`)
- **Legacy references:**
  - V6 WinForms: `SourceCode/GPS/Forms/FormJob.cs` (grid, filters, session mounting).
  - Dev branch: `SourceCode/GPS/Forms/FormJob.cs` (bulk actions, transfer buttons).
  - AgValonia: `Views/Dialogs/JobManager.axaml` (layout, responsive sizing).
- **Porting plan:**
  1. Model dialog state with `JobManagerDialogViewModel` exposing job roster, status tags, and season filters.
  2. Bind command surface to `JobsService` orchestration APIs and `SessionLifecyclePanelViewModel` so mounting a job pre-selects associated fields.
  3. Recreate transfer/import/export affordances via `JobTransferOperations` service adapters; emit telemetry hooks for audit logging.
  4. Validate sort/group behavior against backlog acceptance criteria: alphabetical season grouping, status-based colouring, quick search, and double-click to mount.
  5. Provide unit tests covering filter transitions, mounting flow, and job creation validation.
  6. Supply Storybook/sample state by extending `MainWindowViewModel.CreateSample()`.

### Field Directory (`field_directory_dialog`)
- **Legacy references:**
  - V6 WinForms: `SourceCode/GPS/Forms/Field/FormFields.cs`.
  - Dev branch: `SourceCode/GPS/Forms/Field/FormFields.cs` (cloud sync columns).
  - AgValonia: `Views/Dialogs/FieldDirectory.axaml`.
- **Porting plan:**
  1. Introduce `FieldDirectoryDialogViewModel` listing field metadata (area, last worked, coverage) and exposing folder management commands.
  2. Wire create/rename/delete to `FieldCatalogService` and map to plugin command points `dialog.jobs` for reuse across shell contexts.
  3. Implement quick filters (season, crop, status) and map preview integration by embedding the `FieldMapPreviewViewModel`.
  4. Persist column preferences via the metadata-driven settings infrastructure from NX-414.
  5. Add regression tests for rename validation, selection persistence, and preview toggles.

### Existing Field Picker (`field_existing_dialog`)
- **Legacy references:** `SourceCode/GPS/Forms/Field/FormExistingField.cs` (V6 + Dev), `Views/Dialogs/ExistingField.axaml` (AgValonia).
- **Porting plan:**
  1. Build `ExistingFieldDialogViewModel` focused on quick mounting from recent or favorite fields.
  2. Surface list virtualization for large catalogs using incremental loaders backed by `FieldCatalogService` queries.
  3. Respect backlog acceptance: highlight active session field, confirm with status preview, and surface coverage progress bars.
  4. Provide unit tests verifying favourite toggles and incremental loading states.

### Field Map Preview (`field_map_dialog`)
- **Legacy references:** `SourceCode/GPS/Forms/Field/FormMap.cs` (V6 + Dev), `Views/Dialogs/FieldMap.axaml`.
- **Porting plan:**
  1. Host the metadata-driven map canvas from NX-412 inside an Avalonia dialog with snapshot overlays.
  2. Bind to `FieldGeometryService` for boundary/polygon preview and use `CoverageLayerViewModel` for heuristics.
  3. Provide job context actions (open in map, export, share) via plugin injection `dialog.jobs`.
  4. Validate screenshot capture/copy flows required for operator communication.

### Field Save Confirmation (`field_save_dialog`)
- **Legacy references:** `SourceCode/GPS/Forms/Field/FormSaveAs.cs` (V6 + Dev).
- **Porting plan:**
  1. Mirror validation rules from legacy (naming constraints, overwrite prompts, folder selection) while aligning focus order per ADR-034 accessibility guidance.
  2. Integrate with `SeasonNavigatorViewModel` to refresh job/field rosters post-save.
  3. Emit structured telemetry events for save success/failure and cancellations to preserve audit history.

## Integration Notes
- Maintain data binding parity with `SessionLifecyclePanelViewModel` and `MultiFieldJobSelectorViewModel` introduced in NX-295/NX-296.
- Leverage the localization infrastructure (resource keys + `ILocalizationService`) for button labels and status banners.
- Respect theme tokens from `ui-theme-tokens.json` for typography, spacing, and border radius to guarantee appearance consistency.
- Provide deterministic sample data for UI previews to support documentation and QA captures.

## Testing & Validation
- Add unit tests in `Aog.UI.Avalonia.Tests` covering dialog view-model validation, selection flows, and service error handling.
- Extend simulator smoke scripts to mount jobs, edit fields, and verify dialog open/close flows without runtime exceptions.
- Capture golden screenshots for docs per [metadata-driven UI style guide](metadata-driven-ui-style-guide.md) once layouts stabilize.

## Deliverables
- Avalonia XAML and view-model implementations for all dialogs listed above.
- Updated documentation ([UI session lifecycle guide](ui-session-lifecycle.md)) describing new operator flows and any new sample helpers.
- Backlog linkage updates ensuring completion toggles for relevant entries in `artifacts/ui-backlog.json` during rollout.
