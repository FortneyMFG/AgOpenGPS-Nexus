# Aog.UI.Avalonia

## Zone editor toolbar (NX-291)

The main window now hosts a sample toolbar that exercises the zone drawing framework defined in
[ADR-044](../../../docs/ADR/ADR-044_ZoneDrawingFramework.md). The toolbar lives inside the map card
and is backed by `ZoneEditorToolbarViewModel`, which:

* Drives tool selection for polygon, rectangle, brush, and eraser affordances.
* Publishes snapping toggles and attribute inspector visibility state.
* Emits sample `LayerEditEventAppendRequest` entries through `LayerEditEventJournalService` whenever
  operators press **Commit edit**. The resulting journal entries are summarized in the UI to model
  how provenance cards will surface telemetry.
* Offers Undo/Redo controls by tracking committed entries inside the view-model.

The toolbar is meant to demonstrate integration points for later plugin-owned attribute panels. UI
code-behind should not interact with it directly; bind to the view-model and rely on its commands and
properties.

## Device Manager compatibility dashboard (NX-309)

The shell includes a Device Manager compatibility card powered by
`DeviceManagerCompatibilityViewModel`. The card evaluates plugin manifests via
`PluginCompatibilityEvaluator`, surfaces summary health (`Healthy`, `Warnings`, `Blocked`), and lists
per-plugin issues that map to ADR-031 governance signals. When running inside the repository, the
view-model loads manifests from `docs/plugins/manifests`; packaged builds fall back to a representative
sample. UI bindings render capability badges, dependency issues, and a data-source banner so operators
understand what telemetry is driving the dashboard.
## Crop quick-select UI (NX-302)

`CropQuickSelectViewModel` models the crop quick-select card described in [ADR-045](../../../docs/ADR/ADR-045_CropTypePlugin.md).
Groups expose curated rotations, favorites, and recent assignments via `CropQuickSelectGroupViewModel`
and `CropQuickSelectOptionViewModel`. The MainWindow binds to the sample instance returned by
`CropQuickSelectViewModel.CreateSample()`, illustrating how plugins can publish crop context for field
envelopes without code-behind wiring.
## Zone constraint policies (NX-292)

`ZoneConstraintPolicyViewModel` exposes the ADR-027 gating contract and manual override workflow. The
panel renders four canonical zone toggles (boundary, headland, keep-out, work-disabled) with buffer
summaries, policy descriptions, and manual override commands. Overrides emit structured history entries
(`ZoneOverrideEventViewModel`) so automation, replay, and audit surfaces share the same provenance.

## Zone import/export workflows (NX-293)

`ZoneImportExportPanelViewModel` coordinates sample import/export pipelines for Shapefile, GeoPackage,
and ISOXML transfers. Each `ZoneImportWorkflowViewModel` simulates policy validation, progress updates,
and completion logging while `ZoneTransferEventViewModel` captures an activity timeline aligned with
ADR-027 interop requirements.

## Preset switcher and orchestration status (NX-297)

`PresetSwitcherViewModel` models the preset selection card described in
[ADR-032](../../../docs/ADR/ADR-032-presets-and-layout-linking.md). Presets expose dependency health,
background tasks, and orchestration progress through `PresetOptionViewModel` and
`PresetTaskStatusViewModel` records. The static `CreateSample()` helper wires the planter, sprayer, and
harvest fixtures into `MainWindowViewModel` so UI shells can exercise status messaging without
service dependencies.

## Radio provisioning UI flows (NX-311)

`RadioProvisioningPanelViewModel` surfaces the provisioning workflows aligned with
[ADR-048](../../../docs/ADR/ADR-048_RadioBridge.md). The panel assembles
`RadioProvisioningDeviceViewModel` records that track handshake, topic registry, key exchange, and
reliability steps for each bridge, while `RadioProvisioningProfileViewModel` and
`RadioProvisioningAuditEntryViewModel` expose generated keysets and operator-facing audit history. The
sample card in `MainWindow` binds to `RadioProvisioningPanelViewModel.CreateSample()` so designers can
exercise queue, diagnostics, and follow-up messaging without mesh hardware.
