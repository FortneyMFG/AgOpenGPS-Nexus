# Companion metadata-driven parity pass (NX-312)

ADR-034 requires the Avalonia desktop shell and the remote CompanionRemote
builds to share the same metadata-driven dashboards, legends, and inspector
surfaces. NX-312 delivers the first parity pass by capturing the desktop
metadata in a serialisable snapshot so Android/iOS companions can render the
exact same layout without bespoke wiring.

## Goals

- **Single metadata contract.** Companion clients reuse the same layer legend,
  inspector, dashboard, and replay metadata that powers the desktop shell.
- **Deterministic layouts.** Snapshot payloads include the formatted strings,
  numeric ranges, and colour ramps so remote shells do not guess at
  presentation details.
- **Streamlined DI wiring.** The snapshot is produced directly from the
  Avalonia view-models, avoiding duplicate data shaping layers for mobile.

## Companion snapshot contract

`CompanionMetadataSnapshot` lives in
`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels` and is populated by the
`MainWindowViewModel.CreateCompanionMetadataSnapshot()` helper. The snapshot
contains four sections:

| Section | Contents | Notes |
| --- | --- | --- |
| `Legend` | Layer IDs, range displays, mode badges, colour ramps | Mirrors `LayerLegendViewModel` entries. |
| `Inspector` | Pinned observation text, rate availability, transport/payload metadata | Derived from `LayerInspectorViewModel` including formatted strings. |
| `Dashboard` | Steering history series, PID gains, tuning parameter definitions | Captures sparkline samples and slider bounds from `SteerDashboardViewModel`. |
| `ReplayTimeline` | Speed/heading samples, export status, bookmarks | Reuses `ReplayTimelineViewModel` data for quick remote analysis. |

Colour values are emitted as `#AARRGGBB` strings so Xamarin/MAUI, Uno, or
web companions can recreate the gradients without Avalonia dependencies.
Numeric lists are duplicated into standalone arrays to avoid threading issues
when the desktop UI mutates collections after the snapshot is generated.

## Consuming the snapshot

1. Call `CreateCompanionMetadataSnapshot()` when the desktop shell enters a
   state that should be mirrored (initial load, layout save, or layer pin).
2. Serialise the snapshot to JSON (System.Text.Json handles records out of the
   box) and publish it across the existing CompanionRemote transport channel.
3. On the remote client, hydrate view-models/widgets using the serialised data
   instead of hand-maintained enums or layout definitions.
4. When the operator pins a different layer or dashboard preset, request a new
   snapshot so the companion updates in lock-step.

## Validation

- `dotnet test Nexus SourceCode/tests/Aog.UI.Avalonia.Tests` exercises the
  snapshot factory to guarantee legend, inspector, dashboard, and replay data
  stay in parity with the Avalonia view-models.
- Companion shells should render the sample snapshot stored in
  `Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json` during
  smoke tests to confirm gradients, formatted strings, and metadata badges
  remain consistent with desktop renders.

## Rollout checklist

- [ ] Wire the snapshot stream into the CompanionRemote transport host.
- [ ] Update Android/iOS clients to hydrate dashboards, inspectors, and legends
      from the snapshot instead of ad-hoc models.
- [ ] Capture updated "golden" screenshots for desktop vs companion parity and
      store them in the metadata-driven dashboard regression pack.
- [ ] Extend CI smoke jobs with a remote-parity assertion comparing the latest
      snapshot to the rendered mobile view.
