# NX-079 Post-run report generator

Field operators and QA leads can now export a Markdown summary that stitches together the
checklist sign-off, aggregated metrics, and executed fault schedule for each run. The report
generator reuses the dashboard aggregator to ensure consistency between automated dashboards
and archived reports. Optional inputs allow teams to embed the validated field checklist and the
fault schedule that drove the run.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Reports/PostRunReportGenerator.cs†L1-L115】

## Inputs

- `--metrics <dir>` — directory containing metric JSON files (see NX-078).
- `--checklist <file>` — optional validated field checklist JSON.
- `--faults <file>` — optional resolved fault schedule JSON.
- `--output <file>` — required path to write the Markdown report.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L212-L254】

Sample assets in `tools/qa` illustrate the expected inputs. The generator automatically creates
tables covering scenario status, aggregated metric statistics, checklist sections, and fault events.
At the end of the file the report summarises overall pass/fail state so auditors can quickly spot
runs that need investigation.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Reports/PostRunReportGenerator.cs†L19-L112】

## Generating a report

```bash
# Combine the sample metrics, checklist, and schedule into a Markdown report
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- report generate \
  --metrics tools/qa/metrics \
  --checklist tools/qa/checklists/completed-sample.json \
  --faults out/fault-schedule.json \
  --output out/post-run.md
```

The Markdown output is designed to live beside the zipped safety logs from NX-077, providing a
concise yet complete record of what happened during the run, who signed off on it, and how the
rig responded to injected faults.
