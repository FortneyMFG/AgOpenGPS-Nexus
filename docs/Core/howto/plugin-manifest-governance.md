# Plugin Manifest Governance Playbook

ADR-031 establishes the governance model for the official Nexus plugin bundle.
This playbook documents the roles, artefacts, and automation that keep manifests
trustworthy from pull request to release.

## Roles and responsibilities

- **Plugin owners** maintain manifest accuracy, respond to dependency failures,
  and coordinate with downstream teams when introducing new capabilities.
- **Governance stewards** curate the dependency matrix, sign release digests,
  and manage the compatibility dashboard rollout.
- **QA partners** verify bundle health across deterministic simulations,
  hardware rigs, and Device Manager dashboards before release milestones.

## Lifecycle checkpoints

1. **Pre-submit hygiene**
   - Run `./tools/scripts/nexus.sh plugin lint` locally to verify schema,
     dependency, and lease declarations before sending a review.
   - Export capabilities with
     `./tools/scripts/nexus.sh plugin capabilities --format json` and attach the
     diff to the PR description.
   - Update relevant documentation (e.g. capability matrix entries, release
     notes) so governance stewards can audit intent quickly.
2. **Review gate**
   - Governance stewards confirm the manifest diff aligns with the published
     dependency map and that any new dependencies are represented in
     [docs/Plugins/nexus-plugin-dependency-map.md](../../Plugins/nexus-plugin-dependency-map.md).
   - QA partners schedule the [Plugin QA handshake](../qa/plugin-qa-handshake.md)
     if the change affects runtime compatibility or leases.
   - Pull requests must include links to successful manifest lint and capability
     export runs in CI.
3. **Release readiness**
   - Bundle the approved manifests into a signed compatibility digest.
   - Publish updated capability exports so the
     [Device Manager compatibility dashboard](device-manager-compatibility-dashboard.md)
     reflects the latest contract on launch day.
   - Announce the change in release notes with explicit guidance when operators
     must install additional plugins or update hardware firmware.

## Automation and artefacts

- **Signed compatibility digests** capture the manifest version, dependency
  ranges, and signature metadata for each bundle release.
- **Governance telemetry** records dashboard state changes and lint outcomes to
  power compliance analytics described in ADR-031.
- **Baseline manifests** stored under
  `Nexus SourceCode/tests/Aog.Plugins.Tests/Compatibility/Baselines/` provide
  regression coverage against inadvertent drift.

## Escalation path

If a manifest update breaks compatibility:

1. Revert or pin the offending manifest in the bundle digest.
2. File an incident report linking telemetry and QA findings.
3. Coordinate with impacted teams via the governance channel, providing
   remediation timelines and interim operator guidance.

Adhering to this playbook ensures that manifest governance remains predictable
and auditable as Nexus expands its plugin ecosystem.
