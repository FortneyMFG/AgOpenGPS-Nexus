# ADR-020: Determinism, replay, and CI guardrails

## Status
Drafting (target review window: 2025-12-16 week)

**Relevant Plugin(s):** Simulation & Replay providers, Mapping, Autosteer, Section Control, Rate Control, Telemetry Logging


## Context
Nexus relies on deterministic replays to validate PoseStream, TileStore, and control behaviors across platforms. Without explicit hashing schemes, fixtures, and CI gates, regressions can slip into production unnoticed. ADR-020 defines determinism guardrails building on ADR-007 PoseStream, ADR-009 persistence, and ADR-026 performance budgets.

## Decision
- Establish golden replay fixtures and hashing strategies that cover PoseStream, TileStore, and derived artifacts across operating systems.
- Integrate replay tooling into CI pipelines with pass/fail criteria and trend reporting for runtime and determinism metrics.
- Provide performance budgets tied to ADR-026 so replay runs enforce CPU, IO, and size thresholds while remaining reproducible.
- Publish machine-readable reports so teams can monitor determinism health across merges and releases.

## Consequences
- Determinism regressions become visible and actionable but require investment in fixture maintenance and CI runtime.
- Teams must update workflows to address determinism failures promptly, potentially slowing merges but improving reliability.
- Replay tooling must support hash comparisons and diagnostics, increasing complexity but aiding investigations.

## Governance Updates
- **Unified dashboards.** Determinism hashes, runtime budgets, and hardware telemetry feed a shared Grafana board with release gating thresholds. Deviations beyond two cycles trigger automatic release holds.
- **Escalation criteria.** Reliability charter defines Sev2 when replay drift exceeds tolerance for two consecutive builds, escalating to the program review board within 24 hours.
- **Budget alignment.** Performance budget ADR alignment ensures replay CI reports include CPU, GPU, and memory deltas compared to baselines, enabling quick root-cause triage.

## Validation
- Replay harness must execute 60-minute fixtures in under 12 minutes wall-clock on CI agents while maintaining determinism across OS variants.
- Hash verification tooling must detect single-sample perturbations and fail CI within one minute of regression detection.
- CI pipeline must publish determinism trend reports (hash deltas, runtime budgets) for every merge to `main` with 30-day retention.

## References
- [Testing & CI requirements](../SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md)
- [Data model & storage requirements](../SRS/sections/3X_Data_Storage/32_Persistence_Formats.md)
- [ADR-007: PoseStream and SectionState architecture](ADR-007-posestream-sectionstate-architecture.md)
- [ADR-009: PoseStream vector logs and layer TileStore persistence](ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR-026: Performance budgets](ADR-026-performance-budgets.md)
