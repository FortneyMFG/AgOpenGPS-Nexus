# SRS Reference Library

Canonical specifications, capability catalogs, and interoperability guides that inform Nexus requirements live here. Each entry supports the corresponding SRS section and ADR set, giving engineers an authoritative contract to reference during design and review.

## Core runtime
- [Core data flow architecture](core/data-flow.md) — End-to-end pose, control, and analytics flows with links back to transport ADRs.
- [Capability registry](core/capability-registry.md) — Canonical capability identifiers and attributes used during negotiation.

## Guidance stack
- [Execution & autosteer contracts](guidance/execution-and-autosteer-contracts.md) — 25 Hz orchestrator loop semantics and `SteerTargets` schema.

## Mapping platform
- [Mapping architecture](mapping/mapping-architecture.md) — Division of responsibility between deterministic Core services and the mapping plugin.
- [AgOpenGPS v6 mapping brief](aog-v6-mapping-brief.md) — Historical context and compatibility guardrails for the v6 stack.

## Hardware & transport references
- [AgIO PGN baseline](AgIO_PGN_Baseline.md) — Canonical message catalog for AgIO interoperability.
- [AgOpenGPS hardware platforms](AgOpenGPS_Hardware_Platforms.md) — Supported hardware SKUs and constraints.
- [ISOBUS section control](ISOBUS_Section_Control.md) — Task controller interoperability notes and compliance guardrails.
