# Plugin Performance Guidelines

Plugins share CPU, memory, and I/O budgets with Core and AgIO services.
Use these guidelines to keep your contributions within the supported
footprint.

## Budgets

- Follow the [core performance dashboards](../Core/performance-budget-telemetry-dashboards.md)
  for CPU and memory thresholds.
- Respect the scheduling and determinism requirements captured in
  [SRS §9.6 Quality Engineering & Release](../development/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md).
- When targeting hardware transports, align with the [AgIO rollout
  checklist](../AgIO/aog-link-transport-rollout.md) so serial and CAN
  workloads stay within tolerances.

## Best Practices

1. Avoid blocking the host — use async APIs provided by `IHostServices`.
2. Batch telemetry to reduce chatter on the shared mesh.
3. Profile using the deterministic simulation harness before releasing.
4. Capture metrics in the QA dashboard packages when changes impact
   performance-sensitive code paths.

Coordinate with the Core and UI owners before introducing new background
threads or long-running tasks so shared budgets remain stable.
