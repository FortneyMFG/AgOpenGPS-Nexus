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
