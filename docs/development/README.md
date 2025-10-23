# Development Workspace Overview

This folder collects contributor-facing resources: the developer guide,
the SRS workspace, and QA playbooks. It complements the
[`docs/INDEX.md`](../INDEX.md) map and surfaces the authoritative
references for day-to-day engineering work.

## Directory map

- [`INDEX.md`](INDEX.md) — landing page for environment setup, workflows,
  and contributor checklists.
- [`SRS/`](SRS/) — normative requirements, options, and ADR templates.
  Start with [00_ReadMe](SRS/00_ReadMe.md) and follow the slice-specific
  sections under [`SRS/sections/`](SRS/sections/).
- [`qa/`](qa/) — testing playbooks covering automation harnesses,
  penetration tests, launch readiness reviews, and dashboard reporting.
- [`GLOSSARY.md`](GLOSSARY.md) — shared terminology aligned with the SRS
  and release documentation.

Cross-link these notes in PRs and runbooks so developers can hop between
the lightweight guides here and the normative SRS sections.
