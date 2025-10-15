# Telemetry Logging Plugin Requirements (Draft)

## Overview

Telemetry logging plugins capture session-scoped data for replay, analytics, and compliance. They must integrate with the session lifecycle, multi-machine mesh, and report tooling while guaranteeing deterministic replays.

## Runtime Responsibilities

- Subscribe to session lifecycle events to open/close log files per session, capturing environment metadata, equipment profiles, and provenance references.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L74】【F:docs/ADR/ADR-017-profiles-kinematics.md†L33-L86】
- Record telemetry topics (PoseStream, rate, section state, layer edits, mesh presence) using deterministic timestamping aligned with SimClock/SimBus expectations from ADR-004.【F:docs/ADR/ADR-004-composite-simulation.md†L21-L78】
- Persist logs in an append-only format with integrity hashes and session IDs. Store metadata for quick indexing (start/end timestamps, job/field IDs, active plugins).
- Stamp `jobId`, `sessionId`, and `seasonId` columns on every telemetry record so provenance survives export and replay boundaries per ADR-040/ADR-041.
- Integrate with the multi-machine mesh to capture collaborative events, ensuring share profiles govern whether remote data is included.【F:docs/plugins/MultiMachine.md†L1-L80】
- Capture equipment hour counters, fault codes, and implement usage metrics needed by the Equipment Health plugin, tagging logs with machine IDs so maintenance schedules stay accurate.【F:docs/plugins/EquipmentHealth.md†L1-L160】
- Record work order checkpoints (start, pause, checklist updates) so contractor billing and proof-of-work exports can replay crew progress.【F:docs/ADR/ADR-041_JobSessions.md†L46-L55】

## UX & Tooling

- Provide UI to mark sessions for archival/export, including size estimates and compression status.
- Surface alerts when disk space is low or when logging falls behind real time.
- Offer APIs for Replay and Report Builder plugins to query available logs and associated metadata.
- Expose capture toggles for Training Simulator scenarios so operators can tag recordings for benchmarking suites.

## Compatibility Notes

- Offline-first: logs write to local storage; cloud sync or transfer is optional and reconciles when connectivity returns.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Legacy Run terminology must be replaced with Session identifiers in filenames, metadata, and exports.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
