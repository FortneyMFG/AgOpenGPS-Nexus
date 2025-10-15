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

## Profit heatmap & analytics UI (NX-305)

The right-hand column now showcases the profit visualization workflow from
[ADR-050](../../../docs/ADR/ADR-050_CostProfitPlugin.md). The sample `ProfitAnalyticsViewModel` seeds
`layer:profit.net` map tiles, per-currency summaries, and hotspot callouts so plugins can verify how
profitability overlays surface inside the desktop shell. The view-model accepts a
`ProfitAnalyticsSnapshot` built from rollup outputs and exposes:

* Currency cards that list revenue, cost, profit, and margins using metadata derived from the profit
  layer export pipeline.
* Hotspot tiles that highlight gain/loss regions with notes operators can act on before exporting a
  `ProfitLayer.v1` document.
* A loss alert banner that mirrors plugin alerts when analytics exceed configured thresholds.

Populate `ProfitAnalyticsViewModel.ApplySnapshot` from the plugin orchestrator to keep the UI and map
heatmap in sync.

## Field health severity UX (NX-306)

Field health overlays from [ADR-052](../../../docs/ADR/ADR-052_FieldHealthPlugin.md) have a dedicated
panel that demonstrates severity distribution and history filters. `FieldHealthPanelViewModel`
consumes a `FieldHealthPanelSnapshot` that includes severity buckets, observation counts, and history
entries from the ingest pipeline. The Avalonia view binds to this snapshot to surface:

* Toggleable filters for **Active**, **Monitor**, and **Resolved** observations that update the history
  summary in real time.
* Severity pills with colour-coded counts and area totals so operators can prioritise scouting work.
* A history timeline that mirrors the plugin’s provenance records, making it easy to audit changes.

When the plugin raises critical events (e.g., `FieldHealthSeverity.Critical`), set the `Alert` property
to display the red banner and guide remediation workflows.
