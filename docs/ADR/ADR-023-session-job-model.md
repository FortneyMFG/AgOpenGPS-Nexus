# ADR-023: Session and job model with provenance graph

## Status
Drafting (target review window: 2025-12-09 week)

**Relevant Plugin(s):** Job Tasks, Mapping, Variable Mapping, Telemetry Logging, UI Shell



## Context
Nexus must relate jobs, sessions, PoseStreams, and derived artifacts so provenance (ADR-019) and lifecycle services (ADR-030) operate consistently. Legacy flows treat sessions as ad-hoc folders without stable identifiers, hindering provenance and automation. ADR-023 defines session lifecycle semantics, identifiers, and graph relationships tying together jobs, equipment snapshots, PoseStreams, and derived outputs.

## Decision
- Establish session lifecycle phases (create, resume, switch, close) with deterministic identifiers and state transitions tied to JobsService (ADR-030).
- Capture equipment and profile snapshots for each session, ensuring reproducibility and compatibility with ADR-017 kinematics.
- Define how PoseStreams, layers, and derived artifacts attach to sessions within the provenance graph shared with ADR-019.
- Provide APIs, storage schemas, and UI hooks for managing sessions, including integrity checks that prevent orphaned references.

## Consequences
- Provenance and analytics systems can traverse session graphs to understand context, improving auditability.
- Session orchestration introduces coordination overhead but enables deterministic resume and multi-stream management.
- UI and automation pipelines must surface session awareness, requiring new UX patterns and telemetry events.

## Governance Updates
- **Transactional guarantees.** Session writes now leverage journaling with two-phase commit semantics between session state and storage backends. Crash recovery replays logs and reconciles provenance pointers automatically.
- **Consistency tooling.** Maintenance CLI scans provenance graphs for orphaned edges, offering guided repair actions that operators can run during scheduled downtime.
- **Operational playbooks.** Fleet administrators receive monthly health reports highlighting drift, reconciliation results, and outstanding repairs.

## Validation
- Session lifecycle tests must demonstrate create/resume/switch flows that maintain referential integrity with zero orphaned references.
- Snapshot storage must persist implement/equipment state with < 500 ms serialization latency and ≤ 5% storage overhead compared to raw configurations.
- Provenance graph builder must emit DAGs validated against schema, rejecting cycles and invalid attachments in integration tests.

## References
- [Backend services requirements](../SRS/sections/04_Backend_Services.md)
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Control & automation requirements](../SRS/sections/09_Control_Automation.md)
- [ADR-019: Provenance, audit, and QA governance](ADR-019-provenance-audit-qa.md)
- [ADR-030: Field job sessions and lifecycle services](ADR-030-field-job-sessions.md)
