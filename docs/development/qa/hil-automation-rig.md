# NX-075 Hardware-in-the-loop automation rig

The QA console includes a deterministic harness that executes hardware-in-the-loop (HIL)
scenarios against recorded metric snapshots. Configuration files describe the bench rig,
stream bindings, and per-scenario assertions; results are emitted as structured JSON so they
can be archived alongside safety logs. Sample configurations and metric captures live under
`tools/qa/hil` for quick adoption.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Hil/HilRigConfiguration.cs†L7-L35】【F:tools/qa/hil/rig-config-sample.json†L1-L24】

## Configuration schema

A rig configuration declares:

- **rigName** and **controllerEndpoint** to identify the bench fixture.
- **streamBindings** listing the telemetry channels that must be online before tests start.
- **scenarios** containing a human-readable name, duration, sample data path, and assertions
  for metrics captured during the run.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Hil/HilRigConfiguration.cs†L7-L35】

Sample metric captures (`tools/qa/hil/metrics/*.json`) map metric IDs to numeric values. When
a scenario is executed the runner loads the referenced sample, evaluates each assertion, and
summarises pass/fail status for downstream dashboards.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Hil/HilRigRunner.cs†L9-L88】

## Running the harness

Execute a configuration with the `hil run` command. The runner prints JSON to stdout or the
path supplied via `--output`:

```bash
# Evaluate the sample rig and capture results as JSON
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- hil run \
  --config tools/qa/hil/rig-config-sample.json \
  --output out/hil-results.json
```

Each scenario result lists the scenario name, the evaluated assertions, and whether the
scenario passed overall. A non-zero exit code indicates at least one failed assertion, allowing the
command to gate CI jobs or bench automation.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L94-L140】

## Extending scenarios

1. Record a new metric snapshot during a bench run and save it under `tools/qa/hil/metrics`.
2. Add a scenario pointing at the new sample and declare the expected min/max bounds.
3. Run `qa hil run` and review the generated JSON; any missing metrics are reported inline with
   the failing assertion for rapid triage.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Hil/HilRigRunner.cs†L41-L88】

This workflow transforms the bench rig into a repeatable automation harness that guards
against regression in GNSS quality, steering engagement times, and section control stability.
