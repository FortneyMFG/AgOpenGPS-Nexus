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
