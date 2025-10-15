# NX-094 Field feedback telemetry aggregator

Support analysts can now roll weekly field feedback uploads into a structured JSON
summary using the `support-tool` CLI. The aggregator scans JSON or JSONL files, filters
out-of-window events, and tallies build, component, and issue trends so the results can feed
internal dashboards without exposing customer-identifying data.【F:Nexus SourceCode/tools/Aog.Tools.Support/FieldFeedbackAggregator.cs†L19-L129】 Tie the output back to the
[dealer escalation process](dealer-escalation-process.md) so every ticket reflects the same
snapshot of affected builds and components.

## Input format

Each feedback event is represented as JSON with a UTC `timestamp`, anonymised `tractorId`,
`support` build identifier, `component`, free-form `event` key, severity, and optional notes or
tags. Sample uploads live in `tools/support/feedback` for quick smoke checks.【F:tools/support/feedback/sample-2024-04.jsonl†L1-L4】

## Generating a dashboard snapshot

```bash
# Aggregate all feedback captured in tools/support/feedback
 dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Support/Aog.Tools.Support.csproj" -- \
   feedback aggregate \
   --input tools/support/feedback \
   --output out/field-feedback.json \
   --window-days 30
```

The resulting JSON includes:

- `totalEvents` and `uniqueMachines` to indicate coverage of the upload window.
- `builds` summarising affected versions with counts to guide release triage.
- `components` tallied by critical/warn/info severity to spotlight regressions.
- `topIssues` listing the most common event keys together with anonymised sample notes for
  fast repro context.【F:Nexus SourceCode/tools/Aog.Tools.Support/FieldFeedbackAggregator.cs†L86-L129】

## Dashboard + escalation integration

The aggregator emits the output path on success so CI jobs can publish the JSON as an artefact.
The weekly dealer escalation review (NX-095) pulls the latest snapshot to quantify severity and
assign follow-up owners before contacting regional support leads.【F:Nexus SourceCode/tools/Aog.Tools.Support/Program.cs†L26-L71】 Share the same file with the
[community preview program](community-preview-program.md) so preview fleets can compare their
telemetry against production trends before promoting a build.
