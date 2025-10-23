# Session lifecycle & multi-field selector (NX-295 / NX-296)

NX-295 refreshed the job session start/stop UX and NX-296 added
multi-field job selection support aligned with
[ADR-041](../development/SRS/sections/6X_Core_Domain_Services/62-ADR-041%20-%20Job%20Sessions%20Lifecycle.md) and
[ADR-043](../development/SRS/sections/3X_Data_Storage/31-ADR-043%20-%20Multi-Field%20Job%20Envelopes.md). The Avalonia UI layer now
exposes dedicated presentation models that can be bound inside the job drawer
or simulator surfaces.

## SessionLifecyclePanelViewModel

`SessionLifecyclePanelViewModel` orchestrates session state transitions
(Start, Pause, Resume, Complete) while maintaining a timeline of
`SessionTimelineEntryViewModel` rows. The panel accepts a job identifier, the
fields that can be mounted, and an optional clock delegate for deterministic
replay/testing. Each transition updates command availability and pushes status
messages that UI layers can surface in notification banners.

Typical usage:

```csharp
var panel = new SessionLifecyclePanelViewModel(
    jobId: "job:2025-plant-corn",
    fields: definitions,
    clock: () => DateTimeOffset.UtcNow);

panel.SessionNotes = "Wind 12 kph";
panel.StartSessionCommand.Execute(null);
```

Timeline entries capture mounted field snapshots, cumulative active duration,
pause/resume counters, and operator notes. Notes can be appended while a session
is active via `AddNoteToActiveSession` and the entry automatically records the
start/complete remarks supplied with `SessionNotes`.

## MultiFieldJobSelectorViewModel

`MultiFieldJobSelectorViewModel` tracks the field roster for a job, computes
aggregated selection state (counts, total hectares, average coverage) and
surfaces commands to select all/clear/invert the selection. Each field is
represented by a `JobFieldSelectionViewModel` that exposes toggles and formatted
labels for display. The selector provides snapshots of the currently mounted
fields so the session panel can record provenance alongside timeline entries.

The selector publishes friendly summary strings, for example:

- `SelectionSummary`: `"All 2 fields selected · 21 ha"`
- `SelectedFieldsDisplay`: `"North 40 & Driveway West"`
- `AverageCoverageDisplay`: `"35% covered"`

These pre-formatted strings allow binding without redundant formatting logic in
views.

## SeasonNavigator updates

`SeasonJobViewModel` now accepts a list of field names instead of a single
field. New helpers—`FieldSummary`, `FieldCountDisplay`, `PrimaryFieldName`, and
updated `Subtitle` logic—ensure multi-field jobs render readable summaries
throughout the season navigator.
