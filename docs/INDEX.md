---
owner: nexus-docs
status: active
last_reviewed: 2024-05-10
related_tickets: []
---

# Nexus Documentation Index

## Orientation

- [Repository README](../README.md) — project overview, support channels, and release vision.
- [System Requirements home](development/SRS/00_ReadMe.md) — how the SRS, options, and ADRs fit together.
- [SRS section template](development/SRS/03_SRS_Section_Template.md) — single source for requirements, options, and decision matrices.
- [Nexus glossary](development/GLOSSARY.md) — canonical terminology, including guidance-specific terms.
- [Developer guide](development/INDEX.md) — workstation setup, workflows, and contributor checklists.
- [Development documentation catalog](development/catalog.md) — theme-sorted index with summaries and audiences for every development resource.

## Core

- [Guidance orchestrator briefs](Core/guidance/README.md) — Live Field Builder through telemetry and hysteresis policies.
- [Linux core operations playbook](Core/linux-core-operations-playbook.md) — deployment guardrails and troubleshooting.
- [Core reference library](Core/reference/README.md) — data flow diagrams, layer registry notes, and migration studies.
- [Support playbooks](Core/support/README.md) — runtime guardrails and operational procedures.

## AgIO

- [AgIO subsystem overview](AgIO/README.md) — transports, bridge adapters, and rollout tooling.
- [AOG-Link bridge architecture](AgIO/aog-link-bridge-architecture-guide.md) — fastpath design and coexistence with legacy PGNs.
- [Transport rollout checklist](AgIO/aog-link-transport-rollout.md) — staged deployments and validation gates.
- [Bridging workflow knowledge base](AgIO/bridging-workflow-knowledge-base.md) — troubleshooting heuristics and field notes.

## Plugin Platform

- [Plugin architecture overview](Plugins/architecture.md)
- [Official plugin catalogue](Plugins/official/README.md)
- [Plugin lease & manifest governance](Plugins/plugin-lease-manifest-governance.md)
- [Plugin runbook collection](Plugins/README.md) — guidance, analytics, mapping, and automation briefs.

## UI

- [UI overview](UI/README.md) — Avalonia run modes, metadata-driven layout patterns, and lifecycle guidance.
- [Sidebar layout overview](UI/sidebar-layout-overview.md) — shell navigation conventions and workspace expectations.
- [Metadata-driven style guide](UI/metadata-driven-ui-style-guide.md) — theming, typography, and control inventories.
- [UI session lifecycle](UI/ui-session-lifecycle.md) — orchestration between Core, UI hosts, and plugins.

## Development & Operations

- [Project charter & guardrails](development/SRS/01_Project_Charter.md) — vision, scope, and stakeholders.
- [System slices](development/SRS/02_System_Slices.md) — map active sections by domain (UI, guidance, IO, etc.).
- [Active SRS sections](development/SRS/sections/) — normative requirements organized by slice.
- [ADR template](development/SRS/05_ADR_Template.md) — structure for recording final decisions.
- [How-to guides](development/howto/) — provisioning, telemetry, packaging, and governance runbooks.
- [Testing guidelines](development/testing.md) — entry points into QA harnesses and training scenarios.
- [Performance guidance](development/performance.md) — dashboards, budgets, and tuning checklists.
- [QA practices](development/qa/) — contract governance, plugin validation, and regression plans.
- [Training & scenarios](development/training/) — composite simulation drills and onboarding exercises.
