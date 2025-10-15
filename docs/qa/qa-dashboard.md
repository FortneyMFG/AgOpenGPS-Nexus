# NX-078 QA dashboard aggregation

To connect HIL runs, fault campaigns, and safety checks into a single status view the QA tool
aggregates metric sets from simulation and hardware runs. Each metric file declares a scenario
name, source, and a list of metrics with category, value, unit, and status (`pass`, `warn`, or
`fail`). The aggregator collates these into per-scenario summaries and metric aggregates—
counting passes/fails and computing min/average/max—so the results can feed web dashboards
or CI artefacts.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/QaDashboardAggregator.cs†L9-L103】

Sample metric sets live under `tools/qa/metrics` and mirror the structure emitted by the HIL
runner. A failing metric (e.g., section dropouts) is reflected in the aggregate with updated fail
counts and in the scenario status, allowing the UI to flag regressions at a glance.【F:tools/qa/metrics/scenario-fault.json†L1-L9】
The genetics plugin publishes its regression fixture as `genetics-regression.json`, surfacing
plan/variety counts, coverage gaps, and barcode scan latency so ADR-046 workflows stay wired
into QA automation.【F:tools/qa/metrics/genetics-regression.json†L1-L11】

## Aggregating metrics

```bash
# Aggregate all metric JSON files under tools/qa/metrics
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- dashboard aggregate \
  --input tools/qa/metrics \
  --output out/qa-dashboard.json
```

The resulting JSON contains:

- `scenarios` — one entry per metric set with scenario name, source, overall status, and the
  original metrics.
- `metrics` — aggregates keyed by category/name/unit with sample counts and pass/warn/fail
  counts.
- `allPassed` — boolean summarising whether any metric failed.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Dashboard/QaDashboardAggregator.cs†L15-L103】

Because the aggregator operates purely on JSON files it can run in CI to validate new HIL
runs or locally when QA engineers review fresh data logs. Exit code `2` signals at least one
failed metric, allowing pipelines to halt automatically.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L182-L210】

## Producing dashboard-ready data

1. Ensure each automation pass writes a metrics JSON file following the sample schema.
2. Collect the files under a common directory per run (`out/latest/metrics`).
3. Run the aggregator and publish the JSON output (or transform it into charts/HTML for the
   operator dashboard).

The aggregated output also feeds the post-run report generator (NX-079), keeping dashboards
and exported reports in sync without bespoke wiring.
