# Performance budget telemetry dashboards (NX-323)

ADR-026 establishes Nexus performance budgets so CI, operators, and plugin teams
can catch regressions before they impact autonomy. NX-323 delivers the telemetry
side of that plan: a canonical set of dashboards built on the instrumentation
landed in NX-231 and the ADR-026 metric catalog. This guide documents how to
provision, validate, and maintain those dashboards across environments. For the
official thresholds and instrumentation contracts, refer to
[ADR-026 in the SRS](../SRS/sections/9X_Frontends_Ops/96-ADR-026%20-%20Performance%20budgets%20and%20instrumentation.md).

## Audience & prerequisites

- **Audience:** Observability/telemetry engineers, CI maintainers, and feature
  leads responsible for Core, UI, and plugin performance.
- **Prerequisites:**
  - NX-231 performance instrumentation is deployed and emitting the
    ADR-026 metric catalog (`aog.budget.*`).
  - OpenTelemetry collector tier (OTLP gRPC) is reachable from CI workers and
    fleet environments.
  - Grafana 10.2+ (or equivalent) with access to the Nexus metrics store
    (Prometheus, Timescale, or Loki via the metrics bridge).
  - Alert channel integrations (PagerDuty/Teams) configured per on-call matrix.

## Dashboard objectives

1. **Track budget conformance.** Surface CPU, IO, GPU, and frame-rate metrics
   with overlays for ADR-026 budgets so breaches are immediately visible.
2. **Accelerate investigations.** Provide correlated traces/log pivots for any
   budget breach without leaving the dashboard bundle.
3. **Enforce rollout gates.** Embed annotations tied to CI builds and synthetic
   breach rehearsals so auditors can confirm guardrails remain active.

## Architecture & data flow

```
Sim/Daemon metrics → OpenTelemetry exporters → OTLP collector → Metrics store
                                                       ↓
                                              Budget annotator job
                                                       ↓
                                                Grafana dashboards
                                                       ↓
                                           Alertmanager / PagerDuty
```

- **Sim/Daemon metrics:** Core simulation, AGiO hosts, and UI clients export
  `aog.budget.*` metrics with labels `component`, `scenario`, `hardware`, and
  `budget`. NX-231 wraps publish loops and render ticks to emit the series.
- **Budget annotator job:** A scheduled job (`tools/Aog.Tools.BudgetAnnotator`)
  compares the metric stream against `tools/schemas/performance-budget.json` and
  pushes Grafana annotations for CI runs, manual benchmarks, and synthetic
  breaches. Store credentials in the existing `nexus-observability` secret.
- **Dashboards:** Grafana folders `Nexus/Performance/Core`, `.../UI`, and
  `.../Plugins` ship via JSON provisioning. Each dashboard references the
  shared variable template (`datasource`, `scenario`, `hardware`).
- **Alertmanager:** Budget alerts route through `alertmanager.yml` in the
  observability Terraform. Two consecutive breaches at the 95th percentile fire
  PagerDuty incidents and post in `#nexus-ops`.

## Provisioning steps

1. **Import datasources.**
   ```bash
   terraform apply -target=module.observability.grafana
   ```
   Confirm the `nexus-metrics` datasource resolves queries for
   `aog_budget_cpu_seconds` and `aog_budget_render_frame_ms`.
2. **Load dashboards.** Import the Grafana JSON bundle published with the
   NX-323 release artifacts (stored alongside the observability Terraform
   module). Keep the folder hierarchy intact so RBAC matches the Terraform
   policy packs.
3. **Configure annotations.** Deploy the budget annotator job as a Kubernetes
   CronJob:
   ```bash
   kubectl apply -f ops/k8s/budget-annotator.yaml
   ```
   Ensure the service account has access to the Grafana HTTP API token stored in
   `grafana-secret`.
4. **Wire alerts.** Apply the Alertmanager configuration update and reload the
   stack:
   ```bash
   kubectl apply -f ops/k8s/alertmanager/budgets.yaml
   kubectl rollout restart deployment/alertmanager
   ```
   Validate the `performance-budget-breach` receiver points to the correct
   PagerDuty key and Teams webhook.

## Dashboard bundle overview

| Dashboard | Purpose | Key panels |
| --- | --- | --- |
| **Core Budget Compliance** | Tracks sim loop CPU, IO, and message latency per scenario/hardware matrix. | Budget trend heatmap, 95th percentile latency, top offenders table. |
| **UI Rendering Health** | Monitors frame timing, GPU utilization, and input latency for Avalonia and Android clients. | Frame time histogram, GPU % stacked area, input lag sparkline. |
| **Plugin Utilization Deep-Dive** | Focuses on plugin `component` labels to highlight budget spikes tied to optional features. | Budget breach waterfall, plugin vs baseline comparison, trace links. |

Each dashboard includes quick links to CI build annotations, runbook pages in
the on-call handbook, and "data dogleg" overlays comparing the current signal to
the prior passing build. Update the links during rollout if folder structures
shift.

## Validation & synthetic breaches

1. **CI smoke.** The nightly `nexus sim smoke --budget` job must populate the
   Core dashboard with at least three scenarios and attach a green "CI"
   annotation. Failing to see the annotation within five minutes is a release
   blocker.
2. **Synthetic breach drill.** Monthly, trigger the scripted overload:
   ```bash
   nexus sim smoke --budget --inject cpu-spike --duration 90s
   ```
   The dashboards should show a budget breach, Alertmanager should page the
   on-call rotation, and the drill should be tagged `synthetic=true`.
3. **Hardware parity check.** Quarterly, run the instrumentation on reference
   hardware (ADR-026) and export a Grafana report. Store PDFs in the
   `observability/perf-budgets/YYYY-QN/` folder for audits.

## Maintenance checklist

- Review metric catalog diffs when ADR-026 is updated and bump dashboard JSON
  accordingly.
- Re-run Terraform after upgrading Grafana or Alertmanager to ensure provisioned
  folders and contact points persist.
- Update on-call contact routing when plugin teams assume budget ownership for
  their components.
- Capture learnings from incidents in the performance budget postmortem catalog
  maintained by the on-call lead so mitigations feed back into instrumentation
  and budgets.
