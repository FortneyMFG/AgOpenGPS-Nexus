# Nexus Documentation Index

## Orientation

- [Repository README](../README.md) — project overview, support channels, and release vision.
- [Developer guide](development/INDEX.md) — workstation setup, workflows, and contributor checklists.
- [System Requirements workspace](development/SRS/00_ReadMe.md) — how the SRS, options, and ADRs fit together.
- [SRS section template](development/SRS/03_SRS_Section_Template.md) — single source for requirements, options, and decision matrices.
- [Nexus glossary](development/GLOSSARY.md) — canonical terminology, including guidance-specific terms.

## Core

- [Core overview](Core/README.md) — anchors for orchestrator services, telemetry, and deterministic operations.
- **Guidance orchestrator delivery**
  - [01 — Live Field Builder](Core/guidance/01_live-field-builder.md)
  - [02 — Fields2Cover Planner Integration](Core/guidance/02_fields2cover-orchestrator.md)
  - [03 — Path Catalog & Sequencer](Core/guidance/03_path-catalog-and-sequencer.md)
  - [04 — Execution & Autosteer Contracts](development/SRS/references/guidance/execution-and-autosteer-contracts.md)
  - [05 — Refresh Policies & Hysteresis](Core/guidance/05_refresh-policies-and-hysteresis.md)
  - [06 — Observability & Telemetry](Core/guidance/06_observability-telemetry.md)
- **Operations & reference**
  - [Linux core operations playbook](Core/linux-core-operations-playbook.md)
  - [Reference library](Core/reference/README.md)
  - [Coordinate reference normalization matrix](Core/reference/crs-normalization-matrix.md)
  - [Report template catalogue](Core/reference/report-template-catalog.md)
- **Runbooks & support**
  - [How-to guides](Core/howto/)
  - [Support playbooks](Core/support/)
  - [Multi-machine sync checklist](Core/multi-machine-sync.md)
  - [Performance budget telemetry dashboards](Core/performance-budget-telemetry-dashboards.md)

## AgIO

- [AgIO overview](AgIO/README.md) — transport services, bridges, and hardware coordination.
- [Bridge architecture guide](AgIO/aog-link-bridge-architecture-guide.md)
- [AOG-Link transport rollout](AgIO/aog-link-transport-rollout.md)
- [Pumpkin Pi CM5 integration](AgIO/cm5.md)
- [Bridging workflow knowledge base](AgIO/bridging-workflow-knowledge-base.md)

## UI & Training

- [UI overview](UI/README.md) — Avalonia shell, layout strategy, and plugin surfaces.
- [Avalonia run modes](UI/avalonia-run-modes.md)
- [Sidebar layout overview](UI/sidebar-layout-overview.md)
- [Metadata-driven UI style guide](UI/metadata-driven-ui-style-guide.md)
- [UI session lifecycle](UI/ui-session-lifecycle.md)
- [Training scenario library](UI/training/README.md)
- [Composite simulation fabric checklist](UI/training/composite-simulation-fabric.md)

## Plugins

- [Plugin architecture overview](Plugins/architecture.md)
- [Official plugin catalogue](Plugins/official/README.md)
- [Plugin lease & manifest governance](Plugins/plugin-lease-manifest-governance.md)
- [Plugin dependency map](Plugins/nexus-plugin-dependency-map.md)
- [Legacy plugin briefs](Plugins/README.md) — historical context for mapping, rate control, replay, and more.

## Development & QA

- [Development landing zone](development/INDEX.md)
- [SRS sections](development/SRS/sections/) — normative requirements organized by slice.
- [ADR template](development/SRS/05_ADR_Template.md) — structure for recording final decisions.
- [QA practices and dashboards](development/qa/) — contract governance, plugin validation, and regression plans.
