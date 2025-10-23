# ADR-026: Performance budgets and instrumentation

## Status
Drafting (target review window: 2026-01-09 week)

**Relevant Plugin(s):** Full Stack


## Context
As Nexus integrates PoseStream, controllers, and visualization pipelines, performance regressions can undermine determinism and usability. The platform needs explicit CPU, IO, and FPS budgets tied to instrumentation and CI alerts. ADR-026 formalizes performance targets leveraging ADR-020 determinism guardrails, ADR-021 timebase insights, and ADR-025 data lifecycle expectations.

## Decision
- Publish reference hardware targets (CPU, IO, FPS) for core scenarios, including machine-readable budget catalogs consumed by CI.
- Instrument services to capture latency and utilization metrics, forwarding them to telemetry dashboards within tight intervals.
- Integrate alerts that trigger when budgets are exceeded across consecutive CI runs, with synthetic breach tests to validate paging.
- Coordinate with component teams to align measurement hooks and ensure budgets reflect cross-component dependencies (Core, UI, plugins).

## Consequences
- Teams gain clear performance expectations but must instrument code paths and maintain dashboards and alerts.
- Budget enforcement may block merges when regressions occur, increasing short-term friction but improving long-term stability.
- Telemetry and CI systems incur additional load to collect, store, and visualize performance data.

## Governance Updates
- **Metric schema standardization.** Shared telemetry schema defines metric names, units, and labels, enforced through CI to ensure comparability across Core and plugins.
- **Open exporters.** Standard exporters (OpenTelemetry collectors) ship with default dashboards, enabling consistent observability across environments.
- **Alert rehearsals.** On-call teams run semi-annual dry runs of alert playbooks, verifying paging routes, mitigation steps, and communication templates.

## Validation
- Budget catalog must enumerate CPU/IO/FPS thresholds and export JSON consumed by CI pipelines.
- Instrumentation must capture 95th percentile latency metrics and publish to dashboards within 60 seconds of run completion.
- Alerting pipeline must page on-call when budgets exceed thresholds for two consecutive runs, verified through synthetic breach exercises.

## References
- [Communications & transports requirements](../4X_Interprocess_Communications/42_Transports.md)
- [Backend services requirements](../2X_System_Architecture/21_System_Decomposition_Boundaries.md)
- [Telemetry & health requirements](../6X_Core_Domain_Services/64_Telemetry_Health.md)
- [Data model & storage requirements](../3X_Data_Storage/32_Persistence_Formats.md)
- [ADR-020: Determinism, replay, and CI guardrails](ADR-020-determinism-replay-ci.md)
- [ADR-021: Timebase and clock synchronization](ADR-021-timebase-clock-sync.md)
- [ADR-025: Data lifecycle and retention policy](ADR-025-data-lifecycle-retention.md)
