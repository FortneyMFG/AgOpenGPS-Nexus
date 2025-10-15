# Aog.Core.Jobs

This folder contains the initial infrastructure for the Nexus job lifecycle described in
[ADR-030](../../../../docs/ADR/ADR-030-field-job-sessions.md) and
[ADR-041](../../../../docs/ADR/ADR-041_JobSessions.md). The `JobLifecycleOrchestrator`
provides an in-memory implementation that tracks created jobs, enforces a single active
job at a time, and emits lifecycle events for subscribers. Storage, journaling, and
plugin hooks will extend this surface in follow-up tasks (NX-222 and beyond).

`IJobLifecycleOrchestrator` can be injected into hosted services, gRPC endpoints, or
plugins that need to create or monitor jobs. The orchestrator normalises identifiers to
match the `aog.job.v1` schema and generates deterministic job identifiers that are easy
to correlate with persisted metadata once disk-backed stores are introduced.
