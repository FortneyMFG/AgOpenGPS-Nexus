# AgOpenGPS Next SRS

Welcome to the Software Requirements Specification (SRS) workspace for the next generation of AgOpenGPS. This folder curates requirements, options, and discussions before the community makes architecture decisions.

## How this SRS is organized
- **Vision & Non-goals** capture what the next release aspires to solve and what is intentionally out-of-scope.
- **System slices** map every focus area (OS, UI, comms, storage, etc.) to an individual section document under `sections/`.
- **Sections** collect requirements and enumerate options. They are intentionally decision-neutral—decisions live in Architecture Decision Records (ADRs).
- **Options** can be expanded in dedicated files using the `/docs/templates/OPTION.md` template when deeper analysis is needed.
- **References** house canonical specs (e.g., PGN catalogs) that new options must remain compatible with unless an ADR says otherwise.
- **ADRs** document finalized decisions. Each ADR references the section(s) and options involved so we preserve traceability.

## Workflow expectations
1. Start discussion in the matching GitHub Discussion for the section.
2. Open PRs to add or refine requirements (R-IDs) and options (O-IDs).
3. Maintainers review for clarity and formatting; contributors stay neutral until an ADR is published.
4. Once the community agrees, capture the outcome in an ADR that links back to the relevant section table.

## Conventions
- **IDs**: `R-` for requirements, `O-` for options, `Q-` for open questions, and `ADR-` for decisions.
- **Status labels** in headings track whether a section is collecting proposals, under review, or decided.
- **Borrowables** highlight concrete code or assets from AgOpenGPS/AgIO or other projects that we can reuse.
- **Linting ideas**: unique IDs, table formatting, and link validation can be automated in CI.

## Glossary
- **AgIO**: Companion I/O service that provides network, CAN, and serial connectivity for AgOpenGPS.
- **ADR**: Architecture Decision Record capturing the context, choice, and consequences of an agreed solution.
- **Headless**: Running without a directly attached display, controlled remotely or via automation.
- **Kiosk mode**: Locked-down runtime experience intended for field operators with minimal UI.
- **Multi-monitor**: Use of two or more displays to show different dashboards or controls simultaneously.
- **Remote UI**: User interface accessed via another device (tablet, browser, thin client).
