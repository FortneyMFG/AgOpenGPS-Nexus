---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "2024-05-13"
---

# AgOpenGPS v6 Guidance Study Map

These notes extract the guidance pipeline implemented in the AgOpenGPS v6 legacy codebase so that we can mirror proven behaviours inside Nexus without copying code. The study is organised as nine deep dives plus a glossary. Start with the overview, then dip into the speciality topics as needed.

## How to read this pack

1. **Skim the overview** to understand how GNSS/IMU data flows through pose estimation, line planners, and the autosteer handshake.
2. **Jump to the topic of interest**—each chapter links back here and contains its own research notes with outstanding questions.
3. **Cross-reference symbol tables** in the glossary whenever a variable or PGN field shows up in multiple files.
4. **Mind the scope**: these are descriptive extracts only; we intentionally avoid prescribing Nexus designs.

## Chapters

- [10 — Guidance Overview](10_guidance-overview.md): Runtime loops, scheduling boundaries, and the GNSS→autosteer dataflow.
- [20 — Line Families & Creation](20_line-families-and-creation.md): How AB, curve, contour, recorded/adaptive, and headland lines are stored and regenerated.
- [30 — Line Selection & Reacquisition](30_line-selection-and-reacquisition.md): Logic for choosing the active pass, snap-to-nearest behaviour, and operator overrides.
- [40 — Toolbar Coverage & Effective Width](40_toolbar-coverage-and-effective-width.md): Section geometry, effective working width, and how coverage pixels drive section control.
- [50 — Path Planning from Toolbar to Axles](50_path-planning-from-toolbar-to-axles.md): Converting toolbar loci into pivot/steer references with hitch and articulation rules.
- [60 — Error Terms & Controllers](60_error-terms-and-controllers.md): Definitions, controller maths (Stanley, Pure Pursuit), and smoothing/limits.
- [70 — Autosteer Handshake & Contract](70_autosteer-handshake-and-contract.md): PGN layout, update rates, and state machines between guidance and the autosteer ECU.
- [80 — Previous Work, Boundaries & Headlands](80_previous-work-boundaries-and-headlands.md): Mapping patches, boundary fences, and headland-aware behaviours.
- [90 — Row Guidance & Row Alignment](90_row-guidance-and-row-alignment.md): Tramline, look-ahead, and row-alignment hooks.
- [95 — Edge Cases & Failure Modes](95_edge-cases-and-failure-modes.md): Low-speed oscillations, GNSS loss, articulated quirks, and other guardrails.
- [99 — Glossary & Symbols](99_glossary-and-symbols.md): Variable, frame, and unit references plus Nexus naming hints.

## Research Notes

- Code pointers are maintained per chapter; see the "Research Notes" block at the end of each file.
- AgOpenGPS Dev sources require credentials and were not accessible in this environment; all findings come from the public v6 tree at commit `cf5eafe25e7bc29b0023e81b14424a04645aca0e`.
