# Plugin QA Handshake Checklist

The QA handshake formalises how plugin teams and governance stewards validate
manifest changes introduced under [ADR-031](../ADR/ADR-031-official-plugin-bundle.md).
Follow this checklist whenever a manifest diff affects dependencies, capability
leases, or runtime compatibility.

## Prerequisites

- Manifest lint and capability export logs attached to the pull request per the
  [Plugin Manifest Governance Playbook](../howto/plugin-manifest-governance.md).
- Updated dependency map entries landed or queued in
  [docs/plugins/nexus-plugin-dependency-map.md](../plugins/nexus-plugin-dependency-map.md).
- Simulation fixtures prepared for any new capabilities or degraded-mode paths.

## Handshake steps

1. **Kick-off review**
   - Governance steward confirms scope, affected plugins, and required hardware.
   - QA partner schedules lab time and assigns owners for simulation vs. hardware
     coverage.
2. **Deterministic simulation pass**
   - Run `nexus sim smoke` with manifests from the candidate branch to validate
     dependency resolution, capability routing, and telemetry output.
   - Capture capability exports and attach them to the QA log for traceability.
3. **Device Manager validation**
   - Load the candidate manifests and refresh the
     [Device Manager compatibility dashboard](../howto/device-manager-compatibility-dashboard.md).
   - Verify pass/warn/fail states match expectations for Hard/Soft/Suggest
     dependencies, including manual acknowledgement behaviour for soft
     degradations.
4. **Hardware spot checks**
   - Exercise at least one representative rig per affected capability.
   - Confirm firmware update flows and lease arbitration behave as documented.
5. **Sign-off**
   - QA partner records results in the task tracker (`tasks.md`/`AOG Nexus - Tasks.csv`).
   - Governance steward signs the manifest digest and schedules release notes.
   - Plugin owner merges once all defects are resolved or waived with explicit
     operator guidance.

## Evidence package

Archive the following artefacts with the QA log:

- Simulation output (logs, telemetry snapshots, lint results).
- Dashboard screenshots demonstrating final compatibility state.
- Hardware validation notes with firmware versions and observers.
- Incident follow-ups when issues are discovered post-handshake.

Maintaining a rigorous handshake ensures plugin releases remain predictable and
operators always have a verified compatibility story.
