# Plugin Manifest Workflow

ADR-031 established lease enforcement and governance for the official Nexus plugin
bundle. This workflow consolidates the governance playbook with the lint and
capability export procedures so plugin authors, reviewers, and QA partners share a
single source of truth.

## Roles and ownership

- **Plugin owners** keep manifests accurate, respond to dependency failures, and
  coordinate with downstream teams when introducing new capabilities.
- **Governance stewards** curate the dependency matrix, sign release digests, and
  manage the compatibility dashboard rollout.
- **QA partners** verify bundle health across deterministic simulations, hardware
  rigs, and Device Manager dashboards before release milestones.

Escalations flow through the governance steward on duty, who mobilises the
relevant plugin owner and QA partner to triage production-impacting regressions.

## Daily workflow

### 1. Pre-submit hygiene

1. Validate manifests locally to catch schema or lease issues before review:
   ```
   ./tools/scripts/nexus.sh plugin lint
   ```
   - Add `--manifests` or `--baselines` when linting external bundles or feature
     branches.
   - Windows hosts can call `./tools/scripts/nexus.ps1 plugin lint ...` with the
     same arguments.
2. Export capability snapshots for reviewers:
   ```
   ./tools/scripts/nexus.sh plugin capabilities --format json > capabilities.json
   ```
   - Use `--plugin <id|name>` to focus on a single manifest and `--output` to
     store the report alongside the pull request artefacts.
   - `--format text` generates a quick human-readable diff when collaborating in
     chat.
3. Update supporting docs (capability matrix, release notes) so governance
   stewards can audit intent quickly.

These commands honour the `NEXUS_PLUGIN_TOOL_PROJECT` environment variable when
pointing at an alternate project location and respect `DOTNET` overrides for
multi-SDK hosts.

### 2. Review gate

- Governance stewards confirm manifest diffs align with the published dependency
  map and update [docs/Plugins/nexus-plugin-dependency-map.md](../Plugins/nexus-plugin-dependency-map.md)
  if relationships change.
- QA partners schedule the [Plugin QA handshake](../qa/plugin-qa-handshake.md)
  whenever runtime compatibility, leases, or capability exposure is affected.
- Pull requests must link successful manifest lint and capability export runs in
  CI so reviewers can trace the automation trail.

### 3. Release readiness

- Bundle approved manifests into a signed compatibility digest.
- Publish the latest capability export so the
  [Device Manager compatibility dashboard](device-manager-compatibility-dashboard.md)
  reflects launch-day contracts.
- Announce the change in release notes with guidance for operators who must
  install additional plugins or update hardware firmware.

## Automation and artefacts

- **Signed compatibility digests** capture manifest versions, dependency ranges,
  and signature metadata for each bundle release.
- **Governance telemetry** records dashboard state changes and lint outcomes for
  compliance analytics described in ADR-031.
- **Baseline manifests** under
  `Nexus SourceCode/tests/Aog.Plugins.Tests/Compatibility/Baselines/` guard
  against accidental drift.

## Escalation and incident response

If a manifest update breaks compatibility:

1. Revert or pin the offending manifest in the bundle digest to restore a known
   good state.
2. File an incident report linking telemetry, lint history, and QA findings.
3. Coordinate with impacted teams via the governance channel, providing
   remediation timelines and interim operator guidance.

Following this workflow keeps manifest governance predictable, auditable, and in
lockstep with the capability tooling that ships the official plugin bundle.
