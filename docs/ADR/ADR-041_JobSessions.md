# ADR-041 — Job Sessions

- **Status:** Drafting
- **Date:** 2025-03-18
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-131 Field job session lifecycle ADR

## Context

Legacy workflows tracked a single "run" per job, forcing operators to pause and resume jobs without preserving weather, inputs,
or notes tied to each outing. Replay, analytics, and regulatory records need richer metadata and journaling around each visit to
a field. Plugins and the UI also require deterministic hooks when operators start, pause, or resume work so guidance, rate
control, and telemetry loggers can align their outputs.

## Decision

Replace the Run concept with `Session`. A session is automatically created when a job starts, carries renameable identity and
human-readable labels, and captures environmental metadata, input summaries, notes, and layer references alongside authoring
metadata (`createdBy`, `createdAt`, `lastModifiedAt`). Sessions may be stored inline within `job.json` or as dedicated
`sessions/<sessionId>.json` documents depending on storage preferences. Core emits plugin lifecycle events when sessions start,
end, or change metadata, and exposes a per-session `extensions` bag for plugin-authored data such as crop genetics snapshots or
profitability estimates. Lifecycle contracts are explicit: plugins subscribe to `onSessionStart`, `onSessionPause`,
`onSessionResume`, and `onSessionEnd` in addition to `onJobLoaded` and `onContextChanged` so they can checkpoint state without
polling job storage.

### Work order alignment

TaskService derives executable work orders from job templates. When a work order launches, Core records the originating
`workOrderId`, the assigned operator(s), and the preset/layout bundle resolved during orchestration inside the session metadata.
Checklist events coming from companion clients append to `Session.notes[]` with `type: "checklist"` so proof-of-work logs and
contractor billing exports can replay progress. Session lifecycle hooks surface the associated `workOrderId` and checklist
completion state so plugins (e.g., Profit, Telemetry Logging) can stamp provenance and calculate labor utilization without
scraping task queues. Cancelling or reassigning a work order emits `onSessionMetadataChange` updates with the new assignee and
task state, keeping provenance synchronized across mobile and desktop surfaces.

### Session payload

```json
{
  "id": "session:1",
  "name": "Morning warmup",
  "startedAt": "2025-05-05T07:15:00Z",
  "endedAt": null,
  "env": {"tempC": 18.2, "humidityPct": 62, "windKph": 12, "windDeg": 240},
  "inputs": {"notes": "Hopper 1 fill; burndown complete prior week"},
  "notes": "Soft spots near creek; resumed after 10:30.",
  "layerRefs": ["layer:coverage-2025-05-05"],
  "createdBy": "user:operator.maya",
  "createdAt": "2025-05-05T07:15:02Z",
  "lastModifiedAt": "2025-05-05T11:46:33Z",
  "extensions": {
    "cropType.actual": {
      "hybrid": "ZX-2045",
      "seedLot": "lot-4432A"
    }
  }
}
```

## Consequences

- Core must create Session 1 automatically on job start, persist it with autosave timers, and expose a "Start New Session" UI
  action that closes the current session and opens the next.
- Plugin APIs gain `onSessionStart`, `onSessionPause`, `onSessionResume`, and `onSessionEnd` hooks that carry `farmId`,
  optional `seasonId`, immutable `jobId`, immutable `sessionId`, and the mounted `fieldIds`. Core publishes the active context
  (`farm`, `season`, `job`, `session`) on an event bus so plugins can subscribe once and receive deterministic updates rather
  than polling. Metadata edits fire `onSessionMetadataChange` events including diff summaries to support selective recompute.
- Journaling expectations apply at the session level: coverage tiles, telemetry logs, and notes must checkpoint before declaring
  a session closed.
- Backwards compatibility: jobs with no `sessions` array are treated as a single implicit session when loaded.

## Alternatives considered

1. **Continue with implicit runs.** Rejected because metadata gaps prevent compliant reporting and analytics cannot disambiguate
   multiple outings.
2. **Use per-field sessions.** Would fragment multi-field envelopes and complicate plugin lifecycle management when a job spans
   adjacent fields.

## Migration & compatibility

- Importers should map existing `Run` references to sessions during upgrade while preserving timestamps and notes when available.
- UI strings, documentation, and telemetry labels must replace "Run" with "Session" to avoid operator confusion.
- Plugin manifests declaring run hooks must be updated to the new session events; Core should provide transitional logging when
  deprecated hooks are encountered.
- Session IDs remain stable within a job; migrating storage layers should preserve IDs to keep layer provenance intact.
