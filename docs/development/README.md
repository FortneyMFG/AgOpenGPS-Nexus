# Development Resources

The development tree hosts the SRS, developer workflows, QA practices, and
training scenarios. Start with the [Development Documentation Catalog](catalog.md)
for a theme-sorted view of every resource, then use the table below to jump into
the right material.

| Folder | Purpose |
| --- | --- |
| [`catalog.md`](catalog.md) | Theme-sorted catalog covering every development document. |
| [`INDEX.md`](INDEX.md) | Contributor onboarding, tooling, and workflow expectations. |
| [`SRS/`](SRS/) | System Requirements Specification, ADRs, and option matrices. |
| [`howto/`](howto/) | Operational runbooks for provisioning, packaging, and governance. |
| [`testing.md`](testing.md) | Entry point into QA harnesses and validation workflows. |
| [`performance.md`](performance.md) | Performance budgets, dashboards, and tuning checklists. |
| [`qa/`](qa/) | Validation plans, automation harnesses, and review checklists. |
| [`training/`](training/) | Simulation drills and scenario libraries used for onboarding. |
| [`GLOSSARY.md`](GLOSSARY.md) | Canonical terminology shared across teams. |

Manifest authors should start with the
[Plugin Manifest Workflow](howto/plugin-manifest-workflow.md) for governance
roles, lint commands, capability exports, and escalation guidance.

## Documentation front matter

All new and updated development docs should begin with the following YAML front
matter block. This metadata keeps ownership and review cadences visible across
the tree.

```
---
owner: <team-or-individual>
status: <draft|in_review|active|deprecated>
last_reviewed: <YYYY-MM-DD>
related_tickets:
  - <NX-### or NX-PP-###>
---
```

Guidelines:

- `owner` captures the accountable maintainer (team slug or individual).
- `status` reflects the document's lifecycle. Use `active` for canonical
  references and `in_review` when pending approval.
- `last_reviewed` records the most recent verification date in ISO-8601
  format.
- `related_tickets` lists Nexus tasks that drove the latest update. Use an
  empty list (`[]`) when there are no active tickets.

Run `tools/scripts/lint-doc-front-matter.sh` before submitting a PR to ensure
each touched Markdown file includes the required metadata.

Link changes back to the appropriate SRS section and capture workflow
impacts in `tasks.md` when updating these resources.
