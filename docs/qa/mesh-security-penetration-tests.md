# Mesh security and penetration tests (NX-329)

This test plan validates that the live telemetry mesh and RadioBridge transport satisfy the security posture described in ADR-047 and ADR-048 before broad deployment.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L48】【F:docs/ADR/ADR-048_RadioBridge.md†L16-L38】 The focus areas cover ACL enforcement, encrypted radio links, diagnostics telemetry, and evidence capture for audit readiness.

## Scope & prerequisites

- Mesh service and diagnostics must be deployed in a staging environment with the share/subscribe schemas enforced by tooling.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L43】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L3-L23】【F:schemas/ShareProfile.v1.json†L7-L57】【F:schemas/SubscribeProfile.v1.json†L7-L61】
- RadioBridge adapters and radios under test must expose the framing, retry, and encryption capabilities required by ADR-048 plus diagnostics surfaced through the mesh topic bridge.【F:docs/ADR/ADR-048_RadioBridge.md†L16-L33】【F:Nexus SourceCode/src/Aog.Agio/RadioBridge/RadioBridgeAdapterBase.cs†L364-L403】
- Retention tooling from NX-324 (mesh retention operations guide) is available to archive penetration artefacts without violating privacy baselines.【F:docs/howto/mesh-retention-privacy-operations-guide.md†L9-L101】

## Test plan

### 1. Access control & topic hygiene

1. Attempt to register a device with malformed IDs or topics outside `aog/live/{season}/{job}/{layer}` and confirm the mesh service rejects the registration/publish with diagnostics increments, proving normalization works.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L17-L36】【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L84-L124】
2. Load a share profile missing permission for a target layer and verify publish attempts are denied with explicit errors while diagnostics record the violation for audit review.【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L91-L124】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L23】
3. Configure subscribe profiles with allow/deny permutations and confirm unauthorized subscriptions fail while permitted filters succeed, demonstrating filter enforcement.【F:schemas/SubscribeProfile.v1.json†L21-L44】【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L133-L177】

### 2. Radio transport security

1. With encryption keys provisioned, capture radio traffic over the air to confirm payload confidentiality and observe quarantine behavior when authentication fails.【F:docs/ADR/ADR-048_RadioBridge.md†L28-L32】
2. Induce packet loss or RSSI degradation and ensure selective-repeat retries, FEC toggles, and diagnostic counters react according to the RadioBridge adapter implementation.【F:docs/ADR/ADR-048_RadioBridge.md†L20-L24】【F:Nexus SourceCode/src/Aog.Agio/RadioBridge/RadioBridgeAdapterBase.cs†L364-L403】
3. Validate that diagnostics publications include bridge identifiers and metrics so operators can trace security events to specific hardware endpoints.【F:Nexus SourceCode/src/Aog.Agio/RadioBridge/RadioBridgeAdapterBase.cs†L370-L403】

### 3. Presence & replay abuse resistance

1. Replay stale presence beacons beyond the five-second TTL and confirm the mesh service prunes entries while diagnostics record denials, preventing ghost machines.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L32-L36】【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L179-L240】
2. Attempt to publish forged layer deltas from an unregistered device ID and ensure the mesh rejects them with diagnostics evidence.【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L91-L124】
3. Flood the mesh with rapid subscription churn and validate the system maintains deterministic behavior without dropping ACL enforcement or diagnostics telemetry.【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L23】

### 4. Evidence capture & retention

1. Export mesh diagnostics, RadioBridge telemetry, and penetration test transcripts, then archive them using the retention operations workflow so privacy tags and lifecycle policies remain intact.【F:docs/howto/mesh-retention-privacy-operations-guide.md†L9-L101】
2. Produce a final report summarizing denied events, quarantine triggers, and mitigation actions with references to stored artefacts for future audits.【F:docs/howto/mesh-retention-privacy-operations-guide.md†L73-L101】

## Evidence checklist

| Area | Responsible team | Required artefacts | Status |
| --- | --- | --- | --- |
| ACL enforcement validated | Core QA | Diagnostics export with denied publish/subscribe attempts | |
| Radio encryption & retries verified | AGiO QA | Air capture, RadioBridge logs, key provisioning notes | |
| Presence/replay abuse mitigations | Security QA | Replay test transcript, diagnostics snapshot | |
| Evidence archived per retention policy | Compliance | Archive bundle + retention ticket reference | |
