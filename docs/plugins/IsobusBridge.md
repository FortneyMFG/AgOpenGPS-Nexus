# ISOBUS Bridge Plugin Requirements

## Overview

The ISOBUS Bridge plugin implements ISO11783 Task Controller (TC) and Universal Terminal (UT) interoperability, translating between CAN/UDP transports, ISOXML payloads, and Nexus lifecycle events. It aligns with ADR-014 for TaskData handling and ADR-006/ADR-016 for transport compatibility.

## Runtime Responsibilities

- Support TC/UT roles with capability negotiation. Advertise supported task elements, section control channels, and rate control capabilities to connected implements. The bridge exposes three declarative capabilities in the manifest: `isobus.task-controller`, `isobus.universal-terminal`, and `bridge.udp-mirror`, each backed by an explicit lease so Core can arbitrate control responsibilities.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L23-L60】
- Translate between AgIO transports (CAN, UDP, serial) and ISO11783 messages, using RadioBridge or AOG-Link framing when necessary. Maintain deterministic sequencing and acknowledgements per ADR-006 and ADR-048.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L21-L80】【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L15-L52】 The GA manifest locks these transports to the CAN hardware interface (`can:can0`) and multicast mirror (`udp:239.255.76.67:8888`) so deployment tooling can verify permissions up front.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L62-L69】
- Map Nexus job lifecycle events to ISOXML constructs: job start/end to Task activation, session IDs to Part-Field IDs, and layer references to datasets within TaskData bundles.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L12-L56】【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L55-L73】
- Export planned prescriptions (`vr.planned.*`), as-applied data, and yield logs to ISOXML TaskData, embedding recipe hashes and layer IDs for round-trip fidelity.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L33-L58】【F:docs/plugins/VariableMapping.md†L1-L70】 Use the `isobus.router` simulation provider declared in the manifest to regression-test these flows against captured PGNs before shipping new builds.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L71-L87】
- Handle section arbitration in concert with the Sections module, ensuring TC commands respect Core’s constraint gate and work-disabled zones.【F:docs/SRS/sections/6X_Core_Domain_Services/61-ADR-015 - Section control and grouping semantics.md†L1-L74】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L60】

## UX & Diagnostics

- Provide status dashboards showing implement connection state, TaskController status, outstanding acks, and last exported datasets.
- Surface translation warnings (e.g., unsupported elements, unit mismatches) with actionable remediation steps.
- Offer manual export/import controls for ISOXML packages, including validation summaries and provenance hashes.

## Compatibility Notes

- Offline operation is supported: TaskData exports cache locally and sync when connectivity returns. Remote dashboards remain monitor-only unless granted control leases.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L33-L86】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L33-L60】
- Session IDs replace legacy Run identifiers in TaskData naming and metadata to align with ADR-041.

## Configuration & Leases

- **Source identity.** Operators can tune the default source address and ISO NAME the bridge claims via `sourceAddress` and `isoName` manifest settings. These defaults match the GA baseline so headless deployments remain deterministic across rigs.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L10-L40】
- **UDP mirror.** The `udpMulticastEndpoint` setting fixes the multicast group used for PGN mirroring. Disable or retarget this endpoint when legacy consumers are absent to minimise unnecessary traffic.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L34-L40】
- **Lease behaviour.** TC/UT command surfaces operate under exclusive leases; the UDP mirror participates as a shared lease so multiple observers can subscribe without blocking each other. Plugins must surrender leases gracefully when Core revokes access or timeouts elapse.【F:docs/plugins/manifests/isobus-bridge/1.0.0.json†L88-L113】
