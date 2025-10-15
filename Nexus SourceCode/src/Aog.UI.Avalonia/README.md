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

## Crop quick-select UI (NX-302)

`CropQuickSelectViewModel` models the crop quick-select card described in [ADR-045](../../../docs/ADR/ADR-045_CropTypePlugin.md).
Groups expose curated rotations, favorites, and recent assignments via `CropQuickSelectGroupViewModel`
and `CropQuickSelectOptionViewModel`. The MainWindow binds to the sample instance returned by
`CropQuickSelectViewModel.CreateSample()`, illustrating how plugins can publish crop context for field
envelopes without code-behind wiring.
