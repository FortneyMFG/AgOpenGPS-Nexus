# Job Tasks Season & Session Orchestrator

`JobSeasonSessionOrchestrator` provides the in-memory backbone for the Job Tasks
plugin while the external gRPC contracts from ADR-030/ADR-041 are finalized. The
orchestrator mirrors the expected runtime behaviour so other plugins and hosted
services can hydrate deterministic fixtures during early development:

- **Season awareness.** `TrackJobAsync` keeps the season index aligned with the
  latest `JobMetadata` so dashboards and importers can scope jobs by season
  before disk-backed stores are introduced.
- **Session lifecycle.** A single active session per job is enforced and every
  transition publishes a `JobSessionEvent`. Consumers can subscribe through
  `WatchAsync` to hydrate telemetry loggers, UI shells, or regression fixtures.
- **Deterministic IDs.** Session identifiers follow the `session:n` pattern
  described in ADR-041, making fixtures human readable and matching future gRPC
  responses.

The in-memory implementation is intentionally lightweight and free from I/O so
unit tests and simulation harnesses can exercise lifecycle flows deterministically.
Disk-backed persistence and TaskService integration will attach in follow-up
NX-289/NX-290 tasks.
