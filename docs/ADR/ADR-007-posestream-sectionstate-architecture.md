# ADR-007: PoseStream and SectionState architecture

## Status
In Review (target sign-off window: 2025-10-24 week)

## Context
Nexus requires a single authoritative timeline that carries tractor, implement, toolbar, and section poses alongside diffed SectionState updates. Existing prototypes rely on disparate logs and cadence assumptions that break determinism, complicate automation gating, and make replay analysis inconsistent. ADR-007 formalizes PoseStream expectations so downstream ADRs for equipment hierarchy, persistence, guidance, and control share the same temporal guarantees while inheriting stack boundaries set by ADR-028.

## Decision
- Ship a unified PoseStream that sequences all pose-producing sources with an explicit cadence/decimation policy and shared time authority.
- Represent SectionState as diffs on the same timeline, including optional micro-streams when plugin cadence diverges, while enforcing monotonic ordering under drift.
- Define opportunity/event tally semantics and vector log expectations so replay determinism targets are measurable.
- Establish degraded-mode behaviors when optional feeds are absent, ensuring deterministic fallbacks and telemetry signaling.

## Consequences
- Downstream services (e.g., equipment hierarchy, persistence, guidance) can assume a stable pose timeline with bounded payload sizes and deterministic diff ordering.
- Replay tooling must ingest PoseStream outputs and preserve ordering to remain within determinism budgets.
- Legacy components will require shims to adapt multi-stream pose logs into the new combined format.

## Governance Updates
- **Schema evolution rules.** PoseStream payloads accept additive-only field changes with reserved IDs tracked in a central registry. Dual-write shims cover consumers for at least one minor release before removals and require downgrade verification in replay CI.
- **Chaos validation.** Integration suites now include packet duplication, reorder, and ±75 ms clock skew bursts. Contributors must publish deterministic expectations for each case and prove downstream tallies stay within tolerance.
- **Consumer sign-off.** Equipment hierarchy, guidance, and persistence teams run acceptance scripts on recorded agronomic traces before PoseStream schema changes graduate from feature flags.

## Validation
- PoseStream diff compression must maintain ≤ 2.5 KB median frame payload at 20 Hz for 48-section rigs under replay testing.
- Opportunity/event tallies derived from PoseStream must match analytical goldens within 1% per hectare across the regression suite.
- Forced clock skew of ±25 ms must not break monotonic SectionState sequencing when exercised in integration fault-injection tests.

## References
- [Communications & transports requirements](../SRS/sections/03_Comm_Transports.md)
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Control & automation requirements](../SRS/sections/09_Control_Automation.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-028: Stack boundaries](ADR-028-stack-boundaries.md)
