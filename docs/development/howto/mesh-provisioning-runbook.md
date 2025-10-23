# Mesh provisioning runbook (NX-321)

Rolling out the live telemetry mesh requires coordinated provisioning so devices join with the correct topic scopes, ACLs, and radio credentials defined in ADR-047 and ADR-048.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L17-L36】【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L16-L38】 This runbook guides dealer and fleet ops teams through preparing share/subscribe profiles, loading RadioBridge registries, and validating the mesh before handing rigs to operators.

## Audience & prerequisites

- **Audience:** Dealer integration specialists and fleet operations staff staging multi-machine crews.
- **Prerequisites:**
  - Nexus builds include the live mesh service and diagnostics described in ADR-047 and the Core mesh README.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L17-L43】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L3-L23】
  - RadioBridge-capable radios (ELRS/LoRa) are available with the framing and retry features documented in ADR-048.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L16-L38】
  - Share and subscribe profile templates are accessible to operators, following the schema requirements for topics, ACLs, and visibility presets.【F:schemas/ShareProfile.v1.json†L7-L57】【F:schemas/SubscribeProfile.v1.json†L7-L61】

## Provisioning workflow

### Phase 0 — Pre-flight planning

1. Collect the season and job identifiers that will be staged and map them to mesh topics using the `aog/live/{season}/{job}/{layer}` convention so profile grants reference valid scopes.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L17-L28】
2. Inventory each device's publish/subscribe responsibilities (presence only vs. trails/coverage/layer deltas) and confirm they align with fleet policy before any credentials are issued.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L23-L36】
3. Export the RadioBridge topic registry and firmware bundle that matches the manifest shipped with the build so device handshakes negotiate identical tier support.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L26-L33】

### Phase 1 — Profile creation

1. Draft share profiles that enumerate allowed mesh topics, default ACL policy, and per-device overrides using the schema contract; ensure IDs follow the `share:` namespace and only approved topics are listed.【F:schemas/ShareProfile.v1.json†L7-L40】
2. Populate subscribe profiles with requested topics plus allow/deny lists that reflect operator visibility policies; select presets (presence/trails/coverage/full) that match the UI expectations for each cab.【F:schemas/SubscribeProfile.v1.json†L7-L44】
3. Store both profile documents with change metadata so updates remain auditable during later security reviews.【F:schemas/ShareProfile.v1.json†L41-L57】【F:schemas/SubscribeProfile.v1.json†L45-L55】

### Phase 2 — Device enrollment

1. Register every radio or cab with the mesh service using normalized device IDs and capability summaries so ACL enforcement and diagnostics remain consistent across deployments.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L23-L36】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L8-L23】
2. Apply the prepared share and subscribe profiles during registration; the service normalizes grants and rejects devices that lack publish/subscribe permission, so load errors must be resolved before fielding the rig.【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L91-L177】
3. Capture a diagnostics snapshot after enrollment to verify no access denials or topic hygiene warnings are recorded, confirming the mesh registry is healthy before radio bring-up.【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L23】

### Phase 3 — RadioBridge bring-up

1. Flash RadioBridge firmware and load the topic registry so the device can translate mesh topics into numeric IDs during the handshake.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L26-L33】
2. Provision AES-CCM keys (when hardware supports it) and confirm authentication failures trigger quarantine mode in staging before deploying to the field.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L28-L32】
3. Monitor RadioBridge diagnostics for RSSI, packet loss, retransmissions, and FEC status, confirming telemetry surfaces in the mesh diagnostics topic for operator dashboards.【F:Nexus SourceCode/src/Aog.Agio/RadioBridge/RadioBridgeAdapterBase.cs†L364-L403】

### Phase 4 — Validation & handoff

1. Run a publish/subscribe smoke test for each provisioned tier (presence, trails, coverage, plugin layers) and confirm ACLs allow only the expected flows before signing off.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L23-L43】【F:Nexus SourceCode/src/Aog.Core/Mesh/LiveTelemetryMeshService.cs†L91-L240】
2. Verify presence heartbeats expire according to the five-second TTL default and that diagnostics capture any stale entries or denials for future troubleshooting.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L32-L36】【F:Nexus SourceCode/src/Aog.Core/Mesh/README.md†L10-L23】
3. Archive the completed profile set, RadioBridge registry, diagnostics export, and smoke test transcript with the deployment ticket so audit trails remain intact for retention and security reviews.【F:docs/development/howto/mesh-retention-privacy-operations-guide.md†L73-L101】

## Checklist snapshot

| Milestone | Owner | Evidence | Status |
| --- | --- | --- | --- |
| Mesh topics & job scopes confirmed | Deployment lead | Topic plan referencing ADR-047 scopes | |
| Share/subscribe profiles validated | Dealer integrator | Schema validation logs and diagnostics export | |
| RadioBridge firmware & keys provisioned | Hardware tech | Registry package checksum and key escrow record | |
| Mesh smoke tests logged | Fleet ops | Publish/subscribe transcript with diagnostics summary | |
| Deployment artefacts archived | Compliance owner | Ticket attachment bundle | |
