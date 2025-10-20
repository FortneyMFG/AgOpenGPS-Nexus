# ADR-041 — Job Sessions Lifecycle

- **Status:** Accepted — 2025-05-17 lifecycle working group
- **Date:** 2025-03-18
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-131 Field job session lifecycle ADR
- **Relevant Plugin(s):** Job Tasks, Telemetry Logging, Mapping, Variable Rate, UI Shell

## Context

Legacy workflows only tracked a single "run" per job folder. Operators paused and resumed work without creating a new lifecycle
record, forcing analytics, reporting, and compliance exports to infer what happened from sparse timestamps and handwritten notes.
The Core host introduced in ADR-030 centralises job orchestration but still treats the operator's time in the field as a single
blob. Plugins that control guidance, sections, prescriptions, or telemetry need deterministic hooks when work starts, pauses, or
finishes so they can checkpoint data, flush coverage tiles, or rotate logs. Season organisers (ADR-040) and job autosave policies
(ADR-030) also expect more granular provenance. Without an explicit session model, Core cannot safely coordinate Drive-In
resume flows, cloud synchronisation, or plugin automation.

## Decision

We replace the legacy Run concept with first-class **Sessions** that capture each contiguous block of work inside a job and are
explicitly orchestrated by Core, UI, and plugins.

### Session model

- Sessions are created whenever a job is opened for active work. The default `Session 1` is created automatically when a job is
  started and can be renamed by the operator.
- Session state flows through `Active → Paused → Active → Completed`. Completed sessions become immutable snapshots; only
  metadata notes may be edited by authorised users.
- Each session carries stable identifiers (`sessionId`, `jobId`, optional `seasonId`, `farmId`, `fieldIds`), timestamps
  (`startedAt`, `endedAt`, `lastModifiedAt`), operator roster, environmental measurements, equipment bundle references,
  and a plugin-owned `extensions` object for structured payloads (crop genetics, profitability, guidance QA, etc.).

### Storage layout

- `job.json` gains a top-level `sessions[]` array storing lightweight session descriptors (IDs, names, current state,
  started/ended timestamps, `activeOperators[]`, and summary stats).
- Detailed session payloads live in `sessions/<sessionId>.json` with the schema shown below. Autosave writes deltas atomically
  to avoid corrupting session history; Core rolls back partial writes using the journaling facilities from ADR-030.
- Sessions reference layers, telemetry logs, prescriptions, and attachments via stable IDs so controllers can trace provenance
  without scanning the filesystem.

```json
{
  "id": "session:2025-05-05T07:15Z",
  "name": "Morning warmup",
  "state": "active",
  "startedAt": "2025-05-05T07:15:00Z",
  "endedAt": null,
  "operators": ["user:operator.maya"],
  "workOrderId": "work:alpha-93",
  "context": {
    "seasonId": "season:2025",
    "fieldIds": ["field:home-quarter"],
    "presetId": "preset:planter-16r",
    "layoutId": "layout:spring-rows"
  },
  "environment": {"tempC": 18.2, "humidityPct": 62, "windKph": 12, "windDeg": 240},
  "inputs": {"seed": {"hybrid": "ZX-2045", "seedLot": "lot-4432A"}},
  "notes": [
    {"at": "2025-05-05T07:32:11Z", "author": "user:operator.maya", "type": "freeform", "text": "Soft spots near creek."},
    {"at": "2025-05-05T09:05:00Z", "author": "user:taskrunner", "type": "checklist", "payload": {"item": "Pre-start"}}
  ],
  "layerRefs": ["layer:coverage-2025-05-05"],
  "telemetry": {"logId": "telemetry:session-01", "segments": ["segment:123", "segment:124"]},
  "extensions": {
    "cropType.actual": {"hybrid": "ZX-2045", "seedLot": "lot-4432A"},
    "profit.analytics": {"operatingHours": 3.6, "fuelLiters": 42.3}
  },
  "createdBy": "user:operator.maya",
  "createdAt": "2025-05-05T07:15:02Z",
  "lastModifiedAt": "2025-05-05T09:45:18Z"
}
```

### Lifecycle API & events

- `JobsService` adds gRPC verbs: `StartSession`, `PauseSession`, `ResumeSession`, `CompleteSession`, `StartNextSession`,
  `UpdateSessionMetadata`, and `ListSessions`. Calls require an active job context and enforce role-based access per ADR-031.
- Core publishes lifecycle events (`onSessionStart`, `onSessionPause`, `onSessionResume`, `onSessionEnd`,
  `onSessionMetadataChange`) on the event bus alongside the existing `onJobLoaded` and `onContextChanged` notifications.
- Plugins declare support for session hooks in their manifests. Core buffers lifecycle events until each subscribed plugin
  acknowledges or times out, then logs per-plugin results for determinism.

### Work orders & UI integration

- TaskService launches sessions when work orders transition to `InProgress`. Session metadata stores `workOrderId`, assigned
  operators, preset/layout bundle resolved during orchestration, and checklist status snapshots so billing/reporting can reconcile
  effort with contractual requirements (ADR-043).
- The UI replaces "Run" with "Session" in all surfaces. Operators can open a Job Drawer to start/pause/resume/complete sessions,
  review notes, and add metadata (weather, inputs, attachments). Drive-In prompts trigger `StartSession` when geofence proximity
  matches an archived job session template.

### Autosave, journaling, and observability

- Autosave timers flush session documents alongside coverage tiles; Core treats session state transitions as critical sections
  that require a successful checkpoint before acknowledging completion.
- Session transitions emit structured audit logs (operator, reason, plugin acknowledgements, persisted state version) and metrics
  (duration, pause counts) to support fleet observability and SLA monitoring.
- Offline rigs queue lifecycle events for later upload; conflict resolution merges notes and attachments while rejecting
  conflicting state transitions (e.g., two clients completing the same session). Conflicts surface in the Activity pane with
  resolution guidance.

### Degraded operation

- If session-aware plugins are unavailable, Core proceeds but annotates the session summary with skipped hooks. Operators receive
  toasts explaining that automation did not run so they can retry once dependencies recover.
- When the job store detects read-only media or low disk space, Core falls back to append-only session journals and warns before
  pausing/completing a session. Operators can export the job bundle to removable storage and resume once space is available.

## SRS Impact

- Aligns the operational hierarchy and session payload requirements in §02 Data Model, ensuring Season → Job → Session orchestration has a canonical schema and journaling policy.【F:docs/SRS/sections/3X/31_Domain_Data_Model.md†L1-L160】
- Implements the lifecycle states, events, and autosave expectations defined in §03 Job Lifecycle, replacing the implicit run model with deterministic session hooks.【F:docs/SRS/sections/6X/62_Job_Lifecycle.md†L1-L120】
- Provides backend services with the deterministic checkpoints and health metrics called out in §04 Backend Services for layer controllers, journaling, and automation coordination.【F:docs/SRS/sections/2X/21_System_Decomposition_Boundaries.md†L6-L40】

## Consequences

- **Positive:**
  - Deterministic lifecycle events allow plugins to checkpoint state, rotate logs, and stamp provenance without polling.
  - Operators gain explicit session history for compliance, analytics, and Drive-In resumptions, reducing guesswork when
    returning to a field.
  - Session metadata enables season organisers and report builder workflows to associate work orders, labor, and inputs with
    discrete outings.
- **Negative / mitigations:**
  - Additional UI complexity is mitigated with guided flows (Start Session, Pause, Complete) and contextual education banners
    during rollout.
  - Plugins must adopt new hooks; a compatibility shunt logs deprecated run hooks and provides upgrade guidance for one release.
- **Follow-up actions:**
  - Implement gRPC contracts and client SDK helpers for session verbs (NX-221/222).
  - Update plugin manifests/tests to assert session hook handling (NX-157/NX-288/NX-290).
  - Extend cloud sync and telemetry exporters to respect session boundaries (NX-226/NX-285).

## Alternatives considered

1. **Continue with implicit runs.** Rejected because metadata gaps prevent compliant reporting and automation cannot react to
   pauses/resumes deterministically.
2. **Use per-field sessions.** Rejected because multi-field job envelopes would fragment into unrelated records and complicate
   plugin lifecycle management when a job spans adjacent fields.
3. **Treat sessions as external work orders only.** Rejected because offline rigs need to create sessions without TaskService and
   because plugins require a canonical on-disk representation for autosave.

## Migration & compatibility

- Importers map legacy `Run` references or timestamped coverage folders into session documents while preserving order, notes, and
  operators when known.
- Jobs lacking explicit sessions load with an implicit `Session 1`; Core prompts operators to complete it or start a new session
  before logging additional activity.
- Documentation, UI copy, and telemetry labels replace "Run" with "Session" to avoid operator confusion; legacy run hooks log
  warnings directing integrators to the new session APIs.

## Governance & rollout

- Architecture guild owns schema evolution for session payloads. Changes require compatibility reports generated by the
  JobsService diff tool introduced in ADR-030.
- Release gates require rehearsing session lifecycle flows (start, pause, resume, complete, metadata edits) across Windows/Linux
  targets with at least one offline-to-online sync scenario before GA.
- Operational dashboards track session creation/completion latency, plugin acknowledgement success rates, and autosave
  consistency; regressions block promotion.

## Validation

- **Crash recovery:** Restarting within 8 seconds after an unexpected shutdown must restore the active session and replay at most
  one PoseStream segment (ADR-027) per journal entry.
- **Lifecycle integrity:** Replaying recorded session transitions must produce identical coverage/journal artefacts across three
  consecutive replays (determinism requirement from ADR-020).
- **Drive-In accuracy:** Geofence-triggered session starts must fire within 50 cm spatial error against recorded RTK datasets,
  matching ADR-030 validation thresholds.

## Legacy Implementation Notes

### AgOpenGPS v6
- Stores field work as folders with a single implicit run; operators rely on `Resume.txt` and manual notes, and automation hooks
  cannot detect pauses or metadata changes.

### Legacy Dev Branch
- Mirrors V6 behavior with ad-hoc Drive-In logic and no central session metadata; plugins poll job folders directly and risk
  race conditions during autosave.

## References

- [Job lifecycle architecture](../ADR/ADR-030-field-job-sessions.md)
- [Season organisers & context bus](../ADR/ADR-040_SeasonOrganizers.md)
- [Job session schema requirements](../SRS/sections/6X/62_Job_Lifecycle.md)
- [Deterministic replay policy](../ADR/ADR-020-determinism-replay-ci.md)
- [Spatial constraints & Drive-In](../ADR/ADR-027-spatial-constraints.md)
- [Plugin lifecycle contracts](../ADR/ADR-018-plugin-api.md)
