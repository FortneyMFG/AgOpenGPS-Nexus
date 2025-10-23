# NX-301 Dashboard automation harness

The dashboard harness converts UI automation logs into QA metrics so metadata-driven
layouts can be evaluated in CI alongside HIL, fault, and checklist tooling. A declarative
specification lists the widgets and telemetry series that must appear during a run. The
harness checks the observations against that spec, emits coverage findings, and serialises
a `QaMetricSet` that the existing dashboard aggregator and post-run report generator
consume.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/DashboardAutomationHarness.cs†L9-L168】

## Inputs

- `spec` — JSON describing the scenario name plus each widget and the series it should
  publish. Required series trigger errors when missing; optional ones downgrade to a
  warning. `minSampleCount` enforces baseline coverage so flaky bindings get flagged.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/DashboardAutomationHarness.cs†L47-L130】
- `observations` — Automation log from the UI run listing widget/series samples. The
  harness honours the recorded status (`pass`, `warn`, `fail`) and escalates when sample
  counts dip below thresholds. A scenario name mismatch between the spec and log is
  surfaced as a warning for quick triage.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/DashboardAutomationHarness.cs†L54-L118】
- `--output` (optional) — Directory where the metric set is written. The CLI appends `.json`
  automatically and reports the destination for downstream scripts.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/DashboardAutomationHarness.cs†L133-L153】【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L187-L220】

Sample fixtures that mirror a warm start smoke can be found under the QA test suite.
They demonstrate optional widgets (warning) and required widgets (failure when coverage
or status regresses).【F:Nexus SourceCode/tests/Aog.Tools.Qa.Tests/Data/dashboard/spec.json†L1-L21】【F:Nexus SourceCode/tests/Aog.Tools.Qa.Tests/Data/dashboard/observations.json†L1-L8】
Ready-to-run examples live alongside the other QA assets so automation pipelines can
reference them directly.【F:tools/qa/specs/dashboard-smoke.json†L1-L27】【F:tools/qa/observations/dashboard-smoke.json†L1-L8】

## Running the harness

```bash
# Evaluate observations against a dashboard spec and emit metrics next to the log
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- \
  dashboard harness tools/qa/specs/dashboard-smoke.json \
  --observations out/ui-observations.json \
  --output out/metrics
```

Exit code `0` indicates all metrics passed, `2` signals at least one failing widget, and
`1` surfaces usage errors. Findings are printed to stdout with severity prefixes so CI
logs remain easy to scan.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L187-L220】

The generated metrics drop straight into the existing aggregator and Markdown report
workflows, giving QA engineers a single pipeline for metadata-driven dashboards,
hardware rigs, and injected fault campaigns.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L175-L220】
