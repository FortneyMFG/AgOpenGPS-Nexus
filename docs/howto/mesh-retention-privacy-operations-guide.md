# Mesh retention & privacy operations guide (NX-324)

The live telemetry mesh shares presence, coverage, and layer edits across machines working the same job, but doing so safely requires retention guardrails and privacy controls that match the service design in ADR-047.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L10-L43】 This guide packages the operational steps for field deployments so mesh archives stay compliant while giving collaborators the data they need.

## Audience & prerequisites

- **Audience:** Fleet operators, dealer support leads, and privacy/compliance owners who manage Nexus mesh deployments.
- **Prerequisites:**
  - Live telemetry mesh service and RadioBridge transport from ADR-047 are enabled in the target build, with devices registered under the `aog/live/{season}/{job}/{layer}` topic scheme.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L41】
  - Mesh retention worker and store (NX-229) are deployed so publications can be journaled for offline analysis and sync workflows.【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L25-L33】
  - Retention planner automation from ADR-025 is available to enforce configurable 30/90/365-day policies and surface lifecycle telemetry.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】

## Mesh data tiers & privacy classes

| Tier | Contents | Privacy posture | Baseline retention |
| --- | --- | --- | --- |
| Presence | Pose, session metadata, online/offline state broadcast at 2 Hz on `.../presence` topics.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L23-L33】 | Operator identifiable; encrypt radio links and scope subscribers via ACLs before exporting beyond the cab.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L30-L36】 | Mirror live window only; purge after 30 days unless local policy demands longer audit trails.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】 |
| Trails | Breadcrumb geometry describing recent movement.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L34】 | Sensitive when combined with field names; mark exports with privacy tags so redaction cascades through downstream products.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L16-L27】 | Retain 90 days for collaboration reviews; archive older traces to cold storage if compliance rules require it.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】 |
| Coverage & layer deltas | Coverage tiles and plugin-specific layers shared over mesh topics.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L43】 | Treat as agronomic history subject to regulatory audits; enforce share/subscribe ACLs and encryption when keys exist.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L30-L36】 | Retain 365 days on-device to satisfy agronomic retention expectations before moving to archival workflows.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】 |
| Layer edit journals | `LayerEditEvent` entries captured for audit and offline replay.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L37-L43】 | Contains operator actions; link privacy tags to provenance so redaction requests propagate automatically.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L16-L27】 | Align with coverage retention (365 days) so replays stay deterministic.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】 |

## Operational phases

### Phase 0 — Policy alignment

1. Map regulatory requirements to retention windows using ADR-025 defaults (30/90/365 days) as the baseline and document any stricter jurisdictional rules before enabling the mesh for production crews.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】
2. Classify each subscribing device with a ShareProfile/SubscribeProfile so only permitted tiers flow to the cab; verify ACL evaluations against the mesh diagnostics before rollout.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L36】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L23】

### Phase 1 — Retention pipeline deployment

1. Deploy the `MeshRetentionWorker` alongside Core services and confirm it journals mesh publications into the bounded store, emitting telemetry events for downstream loggers.【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L25-L33】
2. Configure retention planner automation with tier-specific windows and compaction triggers so disk utilization stays below operational budgets while preserving deterministic replays.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L14-L33】
3. Enable lifecycle telemetry dashboards that surface storage pressure and retention breaches to operators so crews can act before data is purged unexpectedly.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L17-L33】

### Phase 2 — Privacy controls & key management

1. Provision encryption keys for RadioBridge transports whenever hardware supports it, ensuring payloads leave the cab encrypted and aligned with ADR-047 privacy expectations.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L30-L36】
2. Tag mesh exports (USB sync bundles, cloud uploads) with privacy metadata so redaction cascades through derived datasets without manual intervention.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L16-L27】
3. Schedule quarterly access reviews that reconcile Share/Subscribe profiles with fleet rosters, documenting variances for compliance audits.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L24-L33】

### Phase 3 — Monitoring & incident response

1. Watch mesh diagnostics for ACL denials or expired presence entries to catch misconfigured profiles or downed devices early.【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L20】
2. When retention breaches occur (e.g., compaction falling behind), trigger the retention planner's remediation workflow and capture evidence for quarterly audits.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L24-L33】
3. For privacy incidents, replay LayerEditEvent journals from the retention store to confirm scope, then execute redaction through the privacy propagation pipeline defined in ADR-025.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L37-L43】【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L16-L33】

## Checklist snapshot

| Milestone | Owner | Status notes |
| --- | --- | --- |
| Retention windows approved vs. ADR-025 defaults | Compliance lead | |
| Share/Subscribe profiles validated with diagnostics | Mesh operator | |
| MeshRetentionWorker journaling confirmed | Core ops | |
| Privacy tags applied to export workflows | Data governance | |
| Lifecycle telemetry dashboards monitored weekly | Fleet ops | |
| Quarterly access & retention audit logged | Compliance lead | |

Keep the completed checklist with the NX-324 rollout ticket and attach retention planner reports plus mesh diagnostics exports as audit artefacts.
