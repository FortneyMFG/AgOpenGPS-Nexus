# Multi-Machine Plugin Requirements (Draft)

## Overview

The Multi-Machine plugin implements the live telemetry mesh described in ADR-047. It manages presence beacons, share/subscribe profiles, and collaborative overlays (trails, coverage, layer edits) across rigs while respecting privacy controls and bandwidth limits.

## Runtime Responsibilities

- Publish `Presence` messages at 1 Hz containing device identity, profile hashes, and capability summaries. Consume `ShareProfile` documents to determine which topics (presence, trails, coverage, layer edits, session state) leave the cab.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L84】
- Honor `SubscribeProfile` ACLs and visibility presets. Operators may choose presets such as `Presence only`, `Trails`, or `Full coverage`; the plugin must adjust subscriptions and UI overlays accordingly.
- Queue outbound payloads when offline (store-and-forward up to 20 MB/device) and replay them when connectivity returns, maintaining order and verifying hash signatures per ADR-047.
- Bridge Zone Tool events to collaborators by forwarding `LayerEditEvent.v1` journals with deduplication and replay protection.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】【F:schemas/LayerEditEvent.v1.json†L1-L140】
- Integrate with RadioBridge when narrowband links are active. Throttle high-bandwidth topics, decimate trails to ≤1 Hz, and prefer store-and-forward bundles for coverage layers.【F:docs/ADR/ADR-048_RadioBridge.md†L15-L52】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L86-L110】

## UX Requirements

- Surface a Machines drawer listing nearby devices, connection state, stale timers, and visibility presets. Operators must be able to toggle topic subscriptions and request diagnostic logs from peers.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L48-L76】
- Display collaborator cursors/presence chips during zone edits, including attribution for undo/redo stacks.
- Provide alerts when share/subscribe profiles block requested data (e.g., coverage hidden). Alerts should include remediation guidance.

## Privacy & Safety

- Sensitive topics (layer edits, profitability) default to deny. Operators explicitly opt-in per session, and share profiles log author/time when permissions change.
- Remote dashboards receive monitor-only streams unless the operator grants a control lease through the cab UI, aligning with SRS §09 control policies.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L33-L60】

## Compatibility Notes

- Mesh communications must function offline on LAN without cloud dependencies. Cloud sync is optional and reconciles on landing per ADR-030.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Session IDs replace legacy Run identifiers in all payloads to stay aligned with ADR-041.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
