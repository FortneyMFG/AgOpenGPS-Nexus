# ADR-021: Timebase and clock synchronization

## Status
Drafting (target review window: 2025-12-18 week)

## Context
Deterministic PoseStream sequencing, automation timing, and telemetry diagnostics require a canonical timebase across Core, plugins, and firmware. Without defined drift detection and reconciliation, distributed nodes risk sequence breaks and latency violations. ADR-021 sets the clock authority, synchronization strategy, and diagnostic expectations aligned with ADR-007 PoseStream, ADR-016 firmware transport, and ADR-020 determinism checks.

## Decision
- Select the canonical time authority (GPS/PTP/RTK) and define drift detection, compensation, and sequence numbering policies for PoseStream and dependent services.
- Specify latency budgets per node, including diagnostics exposure so operators can monitor timing health.
- Integrate drift monitors and resynchronization routines with firmware transports to ensure mismatches fail safe and emit telemetry.
- Provide CI and simulation checks that validate monotonic sequence enforcement across distributed deployments.

## Consequences
- Timebase consistency improves determinism and coordination but requires investment in monitoring and firmware cooperation.
- Drift handling logic adds complexity to PoseStream ingestion and firmware transports, necessitating fault-injection coverage.
- Diagnostics surfaces must expand to include latency histograms and drift events, increasing UI and telemetry workload.

## Validation
- Drift monitors must detect ≥ 2 ms/minute drift within three minutes and trigger resynchronization routines validated via simulated skew scenarios.
- PoseStream sequence enforcement must guarantee monotonic numbering with ≤ 1 frame reorder incidents across 24-hour soak tests.
- Diagnostics UI must surface end-to-end latency histograms updating at least once per second covering 95% of nodes.

## References
- [Communications & transports requirements](../SRS/sections/03_Comm_Transports.md)
- [Control & automation requirements](../SRS/sections/09_Control_Automation.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-007: PoseStream and SectionState architecture](ADR-007-posestream-sectionstate-architecture.md)
- [ADR-016: Firmware and transport for variable-rate layer PGNs](ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-020: Determinism, replay, and CI guardrails](ADR-020-determinism-replay-ci.md)
