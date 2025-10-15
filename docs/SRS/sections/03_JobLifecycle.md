# 03 — Job Lifecycle & Session Management (Status: Drafting)

## Overview

Job lifecycle services orchestrate navigation, plugin hooks, journaling, and resume behavior across the Season → Job → Session
hierarchy. Core maintains the authoritative context (`farmId`, `fieldIds[]`, `seasonId?`, `jobId`, `sessionId`) and broadcasts
updates to plugins so spatial renderers, rate controllers, and analytics stay aligned with operator actions.

## Lifecycle States

1. **Planned:** Job exists with metadata but no active session. Operators may attach prescriptions or stage assets.
2. **Mounted:** Job is selected, fields are mounted, and Session 1 auto-creates with environment snapshot.
3. **Active Session:** Operator performs work. Autosave/journaling persist coverage tiles, telemetry, and metadata checkpoints.
4. **Paused:** Operator intentionally stops the session (manual) or Core detects inactivity; session remains open until End Session
   action runs.
5. **Closed:** Session ended, job remains reopenable. Starting a new session increments the counter and triggers journaling.
6. **Completed:** Operator marks job finished; analytics/export flows run, but sessions remain immutable for audit.

## Core lifecycle events

| Event | Trigger | Payload | Plugin expectations |
| --- | --- | --- | --- |
| `onFarmLoaded` | Operator selects a farm in the navigator | `farmId`, farm metadata snapshot, mounted field roster | Preload geometry, imagery, and plugin extensions for the farm. |
| `onJobLoaded` | Job mounted or resumed | `farmId`, `seasonId?`, `jobId`, `fieldIds[]`, job metadata (core + `extensions`) | Initialize caches, stage per-field stats, prep overlays. |
| `onSessionStart` | Session opened (new or resume) | `farmId`, `seasonId?`, `jobId`, `sessionId`, `fieldIds[]`, env snapshot | Bind provenance context, prime journaling buffers. |
| `onSessionMetadataChange` | Operator updates session metadata | Updated session document | Persist changes, refresh dashboards, honor authoring metadata immutability. |
| `onSessionEnd` | Operator ends the session or job completes | `farmId`, `seasonId?`, `jobId`, `sessionId`, summary stats | Flush journals, finalize layers, update analytics snapshots. |

- Core emits `onFarmLoaded` → `onJobLoaded` → `onSessionStart` in order during mounts. Crash recovery replays `onJobLoaded` and resumes the active session before firing `onSessionStart`.
- Existing jobs without sessions surface as a single implicit session; UI prompts operators to create additional sessions when resuming legacy jobs.【F:docs/ADR/ADR-041_JobSessions.md†L12-L60】

## Autosave & Journaling

- **Autosave cadence:** Minimum every 60 seconds or when >5 MB of coverage tiles are written, whichever comes first.
- **Crash safety:** Journal entries persist to disk before acknowledging `onSessionEnd`. Recovery replays incomplete batches.
- **Metadata:** Session documents (embedded or `sessions/<id>.json`) update atomically. Notes and inputs include timestamps and
  user attribution where available.
- **Layer provenance:** Layers created during the session append provenance records referencing `jobId` and `sessionId`; reused layers keep the original `hash` and `source` while updating the mounting job. Authoring metadata flows into `Layer.v1` alongside plugin-provided `extensions`.【F:schemas/Layer.v1.json†L1-L117】

## Multi-Field Mount/Unmount

- When operators select multiple fields, Core emits a single `mountFields(fieldIds[])` call to mapping plugins, which respond with the union envelope and per-field indices. Job `extensions` supply optional crop/genetics metadata for plugins to render overlays alongside coverage.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L12-L68】
- Field unmounts occur only when jobs close or operators explicitly remove a field; Core updates `fieldIds` and notifies plugins
  prior to persisting changes.
- Per-field stats accumulate in `job.stats.fields[]`, retaining historical coverage even if a field is later unmounted.

## Resume & Last-Open Pointers

- Core tracks the last open job and active session per operator profile. On startup, the UI offers a "Resume Last Session" action
  that remounts the job, restores session state, and replays pending journaling batches.
- Plugins receive the same `onSessionStart` event during resume to refresh caches, restore telemetry subscriptions, and hydrate analytics from session `extensions`.
- Session IDs are deterministic (`session:1`, `session:2`, …) for automatically generated sessions; custom UUIDs remain supported
  for imported history.

## UX Requirements

- **Season-first flow:** Season list → Farm summary (fields participating in the season) → Job → Session timeline with notes.
- **Farm-first flow:** Farm → Fields → Job → Session timeline; seasons appear as badges when linked.
- **Start New Session:** Accessible during an active job; prompts for optional name and notes before closing the current session.
- **Session metadata panel:** Inline edits for env snapshot, inputs, notes, and layer references with autosave indicators.

## Open Questions

- How should automatic session segmentation behave when equipment idles in-field for extended periods?
- Do shared rigs need concurrent session support (multiple implements under one job)?
