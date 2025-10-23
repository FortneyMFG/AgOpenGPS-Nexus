# Nexus Documentation Index

## Orientation

- [Repository README](../README.md) — project overview, support channels, and release vision.
- [System Requirements home](SRS/00_ReadMe.md) — how the SRS, options, and ADRs fit together.
- [SRS section template](SRS/03_SRS_Section_Template.md) — single source for requirements, options, and decision matrices.
- [Nexus glossary](GLOSSARY.md) — canonical terminology, including guidance-specific terms.
- [Developer guide](development/INDEX.md) — workstation setup, workflows, and contributor checklists.

## System Requirements & Decisions

- [Project charter & guardrails](SRS/01_Project_Charter.md) — vision, scope, and stakeholders.
- [System slices](SRS/02_System_Slices.md) — map active sections by domain (UI, guidance, IO, etc.).
- [Active SRS sections](SRS/sections/) — normative requirements organized by slice.
- [ADR template](SRS/05_ADR_Template.md) — structure for recording final decisions.

## Guidance Orchestrator Delivery

- [01 — Live Field Builder](guidance/01_live-field-builder.md)
- [02 — Fields2Cover Planner Integration](guidance/02_fields2cover-orchestrator.md)
- [03 — Path Catalog & Sequencer](guidance/03_path-catalog-and-sequencer.md)
- [04 — Execution & Autosteer Contracts](SRS/references/guidance/execution-and-autosteer-contracts.md)
- [05 — Refresh Policies & Hysteresis](guidance/05_refresh-policies-and-hysteresis.md)
- [06 — Observability & Telemetry](guidance/06_observability-telemetry.md)

## Plugin Platform

- [Plugin architecture overview](plugins/architecture.md)
- [Official plugin catalogue](plugins/official/README.md)
- [Plugin lease & manifest governance](plugins/plugin-lease-manifest-governance.md)
- [Legacy plugin briefs](plugins/) — historical context for mapping, rate control, replay, and more.

## Reference & Operations

- [Coordinate reference normalization matrix](reference/crs-normalization-matrix.md)
- [Report template catalogue](reference/report-template-catalog.md)
- [Linux core operations playbook](Core/linux-core-operations-playbook.md)
- [How-to guides](howto/) — targeted runbooks for provisioning, telemetry, and validation.

## Support & QA

- [Support playbooks](support/) — runtime guardrails and operational procedures.
- [QA practices](qa/) — contract governance, plugin validation, and regression plans.
- [Training & scenarios](training/) — curated walkthroughs for onboarding and simulation drills.
