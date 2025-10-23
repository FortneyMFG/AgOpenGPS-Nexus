# Aog.UI.Avalonia

## Legacy shell layout parity (NX-1116)

The main window shell now mirrors the V6 WPF layout, including the chrome header, left/right
button strips, and bottom quick actions. `AppShellView` integrates `SidebarButtonViewModel`
collections exposed by `MainWindowViewModel` so Avalonia renders the same legacy-inspired
menus and strip buttons while retaining Nexus theming. The central workspace continues to host
the boundary tool sample while the right-hand column preserves the diagnostics/summary panel.

## Zone editor toolbar (NX-291)

The main window now hosts a sample toolbar that exercises the zone drawing framework defined in
[ADR-044](../../../docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md). The toolbar lives inside the map card
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
view-model loads manifests from `docs/Plugins/manifests`; packaged builds fall back to a representative
sample. UI bindings render capability badges, dependency issues, and a data-source banner so operators
understand what telemetry is driving the dashboard.

## Radio provisioning UI flows (NX-311)

`RadioProvisioningFlowViewModel` models the RadioBridge provisioning workflow described in
[ADR-048](../../../docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md) and the
[`radiobridge-provisioning` how-to](../../../docs/Core/howto/radio/radiobridge-provisioning.md). The card summarises
prerequisites, CLI usage, device configuration, and validation checkpoints so operators can stage ELRS/LoRa
bridges without switching back to documentation. The view-model also exposes the provisioning profile schema
fields to reinforce how generated JSON maps onto adapter options and security practices.
## Crop quick-select UI (NX-302)

`CropQuickSelectViewModel` models the crop quick-select card described in [ADR-045](../../../docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md).
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
[ADR-032](../../../docs/development/SRS/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md). Presets expose dependency health,
background tasks, and orchestration progress through `PresetOptionViewModel` and
`PresetTaskStatusViewModel` records. The static `CreateSample()` helper wires the planter, sprayer, and
harvest fixtures into `MainWindowViewModel` so UI shells can exercise status messaging without
service dependencies.

## Field health severity UX (NX-306)

`FieldHealthSeverityPanelViewModel` captures the severity scale required by
[ADR-052](../../../docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md), including layer provenance, persisted history
filters, and the severity colour ramps that align with the `FieldHealthRiskLayer.v1` schema. The sample
panel used by `MainWindowViewModel` highlights critical, high, moderate, low, and none severities with
recommended operator actions so future plugins can populate the same structure without bespoke UI code.
## Radio provisioning UI flows (NX-311)

`RadioProvisioningPanelViewModel` surfaces the provisioning workflows aligned with
[ADR-048](../../../docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md). The panel assembles
`RadioProvisioningDeviceViewModel` records that track handshake, topic registry, key exchange, and
reliability steps for each bridge, while `RadioProvisioningProfileViewModel` and
`RadioProvisioningAuditEntryViewModel` expose generated keysets and operator-facing audit history. The
sample card in `MainWindow` binds to `RadioProvisioningPanelViewModel.CreateSample()` so designers can
exercise queue, diagnostics, and follow-up messaging without mesh hardware.

## Diagnostics workspace (NX-416)

`DiagnosticsWorkspaceViewModel` aggregates GPS, loop, network, and serial telemetry so the sidebar can
mirror AGiO health. The workspace feeds off `TelemetryPrivacyViewModel` and
`DeviceManagerCompatibilityViewModel` to summarise opt-in status, plugin health, UDP throughput, loop
frequency, and recent diagnostics events. Sample data mirrors NX-309/NX-311 fixtures, providing realistic
metadata for designers while keeping the panel free from code-behind glue.

## Simulation and companion parity automation (NX-417)

`CompanionMetadataSnapshot` now serialises simulation bar state (playback rate, routed streams, scenario
metadata) plus diagnostics payloads from the workspace so CompanionRemote builds render identical
controls. The parity tests in `MainWindowViewModelTests` assert route counts, playback settings, and
diagnostics lists all match the Avalonia view-models.

## Documentation and QA updates (NX-418)

`DiagnosticsWorkspaceViewModelTests` exercises the new workspace sample data, while
`CompanionMetadataSnapshot` coverage verifies simulation and diagnostics parity. Supporting docs describe
the new snapshot sections so release checklists include diagnostics and simulation parity captures.
