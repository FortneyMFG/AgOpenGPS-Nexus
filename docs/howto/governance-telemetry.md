# Governance Telemetry Automation

NX-610 introduces a lightweight reporting pipeline so governance stewards can
monitor the ADR roadmap, plugin dependencies, and review cadence without
hand-editing dashboards.

## Running the generator

```bash
python tools/scripts/generate-governance-telemetry.py
```

The script writes two artefacts under `artifacts/governance/`:

- `governance-telemetry.json` — machine-readable telemetry that backs dashboards
  and regression checks.
- `review-minutes.md` — human-readable digest suitable for publishing alongside
  monthly review notes.

Both files are regenerated on every run. Override the output locations or the
source backlog section via the CLI options:

```bash
python tools/scripts/generate-governance-telemetry.py \
  --section "Section 94 — Extensibility, Packaging & Updates" \
  --output-json custom/path/telemetry.json \
  --output-markdown custom/path/minutes.md
```

## Data sources

1. **Program board** — Entries are parsed from `tasks.md`. By default the
   generator focuses on `Section 94 — Extensibility, Packaging & Updates`, which tracks the
   ADR governance backlog. The JSON output records counts by state along with
   the individual tickets used to populate dashboards.
2. **Dependency digest** — The script loads manifest metadata from
   `docs/plugins/manifests/**`. Required APIs, transports, and declared
   capabilities are summarised so governance checks can detect drift in the
   official bundle.
3. **Review minutes** — Meeting notes stored as JSON files under
   `docs/SRS/sections/2X_System_Architecture/reviews/` are collated into the Markdown digest. Each entry can list
   decisions, action items, and reference links. The Markdown output renders the
   structured data with checkboxes for quick status reviews.

## Minutes schema

Each JSON file inside `docs/SRS/sections/2X_System_Architecture/reviews/` should follow this structure:

```json
{
  "meeting_date": "YYYY-MM-DD",
  "meeting_type": "Architecture Governance Review",
  "summary": "One or two sentences summarising the meeting.",
  "attendees": ["Core Owner", "Plugins Owner"],
  "decisions": [
    {"id": "ADR-031", "title": "Manifest governance automation", "status": "ratified", "notes": "Headline"}
  ],
  "action_items": [
    {
      "id": "NX-610-1",
      "owner": "Plugins Owner",
      "description": "Backfill dependency digests",
      "status": "open",
      "due_date": "2025-03-03",
      "completed_date": "2025-02-20"
    }
  ],
  "links": [{"label": "Roadmap", "url": "docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md"}]
}
```

The generator normalises action-item status values to `open`, `in_progress`,
`blocked`, or `done` before counting them in telemetry outputs.

Store new review minutes in separate files to keep the automation diff-friendly
and chronological.
