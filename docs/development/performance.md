---
owner: performance-working-group
status: active
last_reviewed: -
related_tickets:
  - NX-PP-017
---

# Performance Guidance

Use this page to navigate the performance budgets, telemetry dashboards,
and optimization checklists that govern Nexus releases. The goal is to
maintain deterministic behaviour across Windows and Linux targets while
staying within the Raspberry Pi resource envelope.

## Core References

- [Core performance dashboards](../Core/performance-budget-telemetry-dashboards.md)
  — canonical metrics and alert thresholds.
- [SRS §9.6 Quality Engineering & Release](SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md)
  — formal performance requirements and verification criteria.
- [Linux core operations playbook](../Core/linux-core-operations-playbook.md)
  — runtime tuning, telemetry capture, and troubleshooting guidance.
- [Mesh provisioning runbook](howto/mesh-provisioning-runbook.md)
  — provisioning steps that keep transport performance within budget.

## Expectations

1. Benchmark changes against the dashboards above and attach updated
   captures in your PR.
2. Record any deviations from the documented budgets in `tasks.md` and
   open follow-up tickets if mitigation is required.
3. Coordinate with the plugin and UI owners when performance regressions
   cross subsystem boundaries so dashboards stay aligned.
