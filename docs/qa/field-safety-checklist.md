# NX-074 Field safety validation checklist

The field runbook now ships with a structured checklist that mirrors the operator hand-off
process used on production rigs. The checklist is encoded in the `Aog.Tools.Qa` console under
the `checklist` command and is distributed with ready-to-use templates and samples under
`tools/qa/checklists`. The template enforces four sections—pre-run readiness, hardware
validation, software and failsafe checks, and final sign-off—to mirror the gates required by
NX-074.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Checklist/FieldSafetyChecklistTemplate.cs†L7-L63】

## Sections and required outcomes

| Section | Focus | Outcomes |
| --- | --- | --- |
| Pre-run readiness | Crew coordination before autonomy is enabled. | PPE issued, hazards flagged, and comms check confirmed. |
| Hardware validation | Machine-side safety interlocks. | Manual overrides, GNSS quality, and controller engagement verified. |
| Software & failsafes | Runtime guardrails. | Watchdog heartbeats, deterministic baseline, and logging retention confirmed. |
| Approvals | Accountability. | Operator acknowledges controls; QA lead authorises the run. |

Each item records a status (`pending`, `completed`, or `notApplicable`) plus optional notes,
verifier, and timestamp. Completed items require both verifier identity and time; `notApplicable`
items capture context via `notes`. Validation fails if any item remains pending or a required field
is missing, ensuring sign-off is auditable.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Checklist/FieldSafetyChecklistValidator.cs†L9-L86】

## Exporting and validating checklists

Use the QA console to export the current template or validate a filled-in JSON file:

```bash
# Export a fresh template to tools/qa/checklists/field-safety-template.json
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- checklist template \
  --output tools/qa/checklists/field-safety-template.json

# Validate a completed checklist (returns non-zero if any gate fails)
dotnet run --project "Nexus SourceCode/tools/Aog.Tools.Qa/Aog.Tools.Qa.csproj" -- checklist validate \
  tools/qa/checklists/completed-sample.json
```

The repository includes a pre-filled example (`completed-sample.json`) that demonstrates the
expected notation for verifiers and timestamps so operators can cross-check their own forms.
Any validation error is emitted to stderr with the offending checklist item, making it easy to
address missing approvals or notes before a field run.【F:Nexus SourceCode/tools/Aog.Tools.Qa/Program.cs†L34-L92】

## Operational flow

1. Export the latest template and capture the day’s site/operator metadata.
2. Walk through each section with the on-site lead, recording verification notes.
3. Run `qa checklist validate` and resolve any reported gaps.
4. Archive the JSON alongside safety logs using the retention policies from NX-077 for audit
   readiness.【F:Nexus SourceCode/src/Aog.Agio/README.md†L1-L36】

Following this workflow guarantees a consistent, reviewable record of every autonomous field
session while integrating with the existing safety log export tooling.
