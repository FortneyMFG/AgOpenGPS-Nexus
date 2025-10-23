# NX-343 UI Modernization Prompt Bundle

This bundle provides copy/paste prompts for automation agents that will assemble the UI
inventory, plugin mapping, component specifications, and migration backlog for the Nexus
UI modernization effort.

## Using the Bundle

1. Point your automation runner at the legacy repositories listed in the `source_repos`
   metadata (`../Legacy SourceCode -V6`, `../Legacy SourceCode -Dev`, and
   `../Legacy SourceCode -AgValoniaGPS`).
2. Execute the prompts sequentially. Each step produces artifacts consumed by the next.
3. Store generated files under `artifacts/` so they can be checked into the Nexus repo or
   shared with the team for review.

The canonical machine-readable definition lives in
[`ui-modernization-ai-prompts.json`](./ui-modernization-ai-prompts.json). Automation can
load that file, validate the schema, and drive task execution without manual prompt
editing.

## Prompt Sequence Overview

| Prompt ID           | Purpose                                                | Key Outputs                        |
| ------------------- | ------------------------------------------------------ | ---------------------------------- |
| `inventory-json`    | Crawl legacy repos and produce `ui-inventory.json`.    | `artifacts/ui-inventory.json`      |
| `inventory-csv`     | Convert the JSON inventory into CSV format.            | `artifacts/ui-inventory.csv`       |
| `mapping-yaml`      | Map UI elements to core or plugin hosts.               | `artifacts/ui-to-plugin.yaml`      |
| `design-system-spec`| Define @nexus/ui-core components and theme tokens.     | `artifacts/ui-core-spec.md`, `artifacts/ui-theme-tokens.json` |
| `backlog-json`      | Generate a GitHub-importable backlog of UI tasks.      | `artifacts/ui-backlog.json`        |
| `license-checklist` | Summarize license and attribution requirements.        | `artifacts/ui-license-checklist.md`|

Each prompt includes acceptance criteria so reviewers can verify completeness without
re-reading the entire plan.

## Acceptance Criteria for NX-343

- JSON schema is syntactically valid and references the required legacy repositories.
- Markdown companion explains how to consume the prompts and the outputs they produce.
- No assumptions are made about proprietary assets; license prompt enforces verification.

The repository now includes a repeatable generator at
`tools/generate_ui_artifacts.py` that crawls the legacy WinForms, guidance refactor,
and Avalonia configuration sources and emits the artifacts under `artifacts/`:

- `ui-inventory.json` and `ui-inventory.csv`
- `ui-to-plugin.yaml`
- `ui-core-spec.md`
- `ui-theme-tokens.json`
- `ui-backlog.json`
- `ui-license-checklist.md`
- `ui-screenshots/README.md`

Run `python tools/generate_ui_artifacts.py` whenever the legacy sources update to
refresh the bundle. The script records the generation timestamp inside the JSON
metadata so downstream automation can detect stale runs.

Once automation finishes, the resulting artifacts (inventory, mappings, backlog, design
system spec, and license checklist) can feed directly into issue creation and plugin
scaffolding work.
