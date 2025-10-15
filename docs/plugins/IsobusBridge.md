# ISOBUS Bridge Plugin Requirements (Draft)

## Overview

The ISOBUS Bridge plugin implements ISO11783 Task Controller (TC) and Universal Terminal (UT) interoperability, translating between CAN/UDP transports, ISOXML payloads, and Nexus lifecycle events. It aligns with ADR-014 for TaskData handling and ADR-006/ADR-016 for transport compatibility.

## Runtime Responsibilities

- Support TC/UT roles with capability negotiation. Advertise supported task elements, section control channels, and rate control capabilities to connected implements.
- Translate between AgIO transports (CAN, UDP, serial) and ISO11783 messages, using RadioBridge or AOG-Link framing when necessary. Maintain deterministic sequencing and acknowledgements per ADR-006 and ADR-048.【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L21-L80】【F:docs/ADR/ADR-048_RadioBridge.md†L15-L52】
- Map Nexus job lifecycle events to ISOXML constructs: job start/end to Task activation, session IDs to Part-Field IDs, and layer references to datasets within TaskData bundles.【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
- Export planned prescriptions (`vr.planned.*`), as-applied data, and yield logs to ISOXML TaskData, embedding recipe hashes and layer IDs for round-trip fidelity.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】【F:docs/plugins/VariableMapping.md†L1-L70】
- Handle section arbitration in concert with the Sections module, ensuring TC commands respect Core’s constraint gate and work-disabled zones.【F:docs/ADR/ADR-015-section-control-grouping-semantics.md†L1-L74】【F:docs/SRS/sections/09_Control_Automation.md†L12-L60】

## UX & Diagnostics

- Provide status dashboards showing implement connection state, TaskController status, outstanding acks, and last exported datasets.
- Surface translation warnings (e.g., unsupported elements, unit mismatches) with actionable remediation steps.
- Offer manual export/import controls for ISOXML packages, including validation summaries and provenance hashes.

## Compatibility Notes

- Offline operation is supported: TaskData exports cache locally and sync when connectivity returns. Remote dashboards remain monitor-only unless granted control leases.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】【F:docs/SRS/sections/09_Control_Automation.md†L33-L60】
- Session IDs replace legacy Run identifiers in TaskData naming and metadata to align with ADR-041.
