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
| `onSeasonLoaded` | Season selected or job references a season | `seasonId`, `name`, `jobIds[]`, date range, optimizer state, `extensions` | Prestage seasonal analytics, budget snapshots, and crop rotation context. |
| `onJobLoaded` | Job mounted or resumed | `farmId`, `seasonId?`, `jobId`, `fieldIds[]`, job metadata (core + `extensions`), immutable IDs | Initialize caches, stage per-field stats, prep overlays, and subscribe to envelope updates. |
| `onContextChanged` | Farm/season/job/session IDs change (field mount, season swap) | Diff summary + latest context snapshot | Reconcile caches idempotently; rebuild spatial indices, refresh analytics. |
| `onSessionStart` | Session opened (new or resume) | `farmId`, `seasonId?`, `jobId`, `sessionId`, `fieldIds[]`, env snapshot, crop/genetics context | Bind provenance context, prime journaling buffers, auto-fill crop/genetics defaults. |
| `onSessionPause` | Operator pauses work | Context snapshot + pause reason | Suspend live logging, mark telemetry streams paused without closing session. |
| `onSessionResume` | Operator resumes after pause | Context snapshot + resume timestamp | Resume coverage logging, refresh analytics caches. |
| `onSessionMetadataChange` | Operator updates session metadata | Updated session document with diff summary | Persist changes, refresh dashboards, honor authoring metadata immutability. |
| `onSessionWeatherUpdate` | Weather auto-logging records a new sample | Weather delta payload (temp, humidity, wind, rainfall, pressure, source) | Update session weather snapshot, notify spraying/analytics plugins. |
| `onSessionEnd` | Operator ends the session or job completes | `farmId`, `seasonId?`, `jobId`, `sessionId`, summary stats | Flush journals, finalize layers, update analytics snapshots. |
| `onLayerStartEdit` | Zone Drawing Framework enters edit mode | Layer context (layerId, jobId, sessionId?, editable attributes) | Prepare attribute editors, suspend conflicting automation. |
| `onFeatureCommit` | Geometry/attribute change committed | `LayerEditEvent` payload | Update analytics, sync collaborative meshes, refresh overlays. |
| `onLayerUndo`/`onLayerRedo` | Undo stack mutates | `LayerEditEvent` pointer + diff summary | Rollback/redo analytics caches, update UI history. |

- Core emits `onFarmLoaded` → `onSeasonLoaded` (when applicable) → `onJobLoaded` → `onSessionStart` in order during mounts. Crash recovery replays `onJobLoaded`, replays pending `onContextChanged` diffs, and resumes the active session before firing `onSessionStart`.
- Existing jobs without sessions surface as a single implicit session; UI prompts operators to create additional sessions when resuming legacy jobs.【F:docs/ADR/ADR-041_JobSessions.md†L12-L60】

## Autosave & Journaling

- **Autosave cadence:** Minimum every 60 seconds or when >5 MB of coverage tiles are written, whichever comes first. Layer edits trigger autosave when 10 or more `LayerEditEvent` entries are buffered.
- **Crash safety:** Journal entries persist to disk before acknowledging `onSessionEnd`. Recovery replays incomplete batches.
- **Metadata:** Session documents (embedded or `sessions/<id>.json`) update atomically. Notes and inputs include timestamps and
  user attribution where available.
- **Layer provenance:** Layers created during the session append provenance records referencing `jobId` and `sessionId`; reused layers keep the original `hash` and `source` while updating the mounting job. Authoring metadata flows into `Layer.v1` alongside plugin-provided `extensions`. Layer edits emit `LayerEditEvent.v1` journals with deterministic hashes for undo/redo and collaborative replication.【F:schemas/Layer.v1.json†L1-L117】【F:schemas/LayerEditEvent.v1.json†L1-L140】

## Multi-Field Mount/Unmount

- When operators select multiple fields, Core emits a single `mountFields(fieldIds[])` call to mapping plugins, which respond with the union envelope and per-field indices. Job `extensions` supply optional crop/genetics metadata for plugins to render overlays alongside coverage.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L12-L68】
- Field unmounts occur only when jobs close or operators explicitly remove a field; Core updates `fieldIds` and notifies plugins
  prior to persisting changes.
- Per-field stats accumulate in `job.stats.fields[]`, retaining historical coverage even if a field is later unmounted.
- `onContextChanged` fires after field mount/unmount, season reassignment, or crop context updates so plugins can rehydrate caches without redundant restarts.

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

## Work orders & task orchestration

- R-JOB-040 (MUST): Provide a TaskService-backed Work Order list that lets managers assign jobs with presets, implements, and planned inputs. Launching a work order must automatically open a session with the originating `workOrderId`, preset hash, and assignee captured in session metadata and provenance.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L17-L40】【F:docs/ADR/ADR-041_JobSessions.md†L33-L55】
- R-JOB-041 (SHOULD): Mobile/companion clients shall surface per-work-order checklists, notes, and completion toggles that sync into `Session.notes[]` entries with actor/timestamp data for proof-of-work exports.【F:docs/ADR/ADR-041_JobSessions.md†L46-L55】
- R-JOB-042 (MUST): Task state transitions (Assigned → In Progress → Completed/Cancelled) must emit lifecycle events so Profit, Telemetry Logging, and regulatory plugins can stamp provenance without polling queue state. Events include `workOrderId`, `jobId`, `sessionId`, `assignee`, and checklist completion percentage.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L32-L40】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L17-L34】
- R-JOB-043 (SHOULD): TaskService must reconcile work order material reservations with the Inventory Ledger, reducing on-hand quantity when sessions report consumption and flagging discrepancies for manual review.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L15-L34】

These requirements extend the session lifecycle so orchestration, crew scheduling, and audit logs align with field execution while preserving deterministic provenance across plugins.

## Open Questions

- How should automatic session segmentation behave when equipment idles in-field for extended periods?
- Do shared rigs need concurrent session support (multiple implements under one job)?
