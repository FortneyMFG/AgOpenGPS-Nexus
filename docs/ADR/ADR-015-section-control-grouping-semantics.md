# ADR-015: Section control and grouping semantics

## Status
Drafting (target review window: 2025-11-08 week)

## Context
Nexus needs deterministic section control behavior that honors manual overrides, automation, and plugin contributions while managing overlapping groups and toolbar-level lookahead. Legacy systems rely on ad-hoc priority rules and offer limited observability. ADR-015 defines the control graph and arbitration rules aligned with ADR-008 hierarchy, ADR-007 PoseStream cadence, and ADR-018 plugin capabilities.

## Decision
- Establish a control graph covering On/Auto/Off states, master group actions, overlapping group arbitration, and toolbar-specific lookahead/overlap defaults.
- Define arbitration priorities (manual > plugin > auto) with safety interlocks and telemetry for overrides.
- Provide configuration UI updates and documentation so operators can understand and configure group behaviors.
- Integrate with plugin lifecycle hooks (ADR-018) ensuring permissions guard control extensions and degraded modes signal missing capabilities.

## Consequences
- Section control becomes predictable and testable across automation modes, improving safety and operator trust.
- Arbitration logic introduces complexity that requires thorough simulation, replay, and UI feedback loops.
- Plugins must adapt to new lifecycle hooks and permission gating to participate in control decisions.

## Validation
- Section control simulator must keep overlap error ≤ 8% against agronomic goldens across diverse replay fixtures.
- Manual overrides must pre-empt plugin commands within 100 ms and log actor plus duration in telemetry streams.
- Safety interlock tests must assert sections fail closed when heartbeat loss exceeds 300 ms, verified via integration harnesses.

## References
- [Control & automation requirements](../SRS/sections/09_Control_Automation.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-008: Equipment hierarchy](ADR-008-equipment-hierarchy.md)
- [ADR-007: PoseStream and SectionState architecture](ADR-007-posestream-sectionstate-architecture.md)
- [ADR-018: Plugin API and capability discovery](ADR-018-plugin-api.md)
