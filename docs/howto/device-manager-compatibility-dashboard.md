# Device Manager Compatibility Dashboard

The Device Manager compatibility dashboard visualises the manifest governance
signals defined in [ADR-031](../ADR/ADR-031-official-plugin-bundle.md) so
operators can verify that the official plugin bundle is healthy before field
work. It consumes the dependency matrix and capability exports produced by the
plugin manifest tooling and renders pass/warn/fail states with remediation
steps for each plugin.

## Data sources and refresh cadence

1. **Manifest lint results.** CI and local workflows run
   `./tools/scripts/nexus.sh plugin lint` to validate each manifest against the
   dependency schema before publishing capability data.
2. **Capability export feed.** `./tools/scripts/nexus.sh plugin capabilities`
   produces the lease and capability catalogue that the dashboard ingests to
   map compatibility issues to affected subsystems. See
   [Plugin Manifest Compliance & Capability Reporting](plugin-manifest-compliance.md)
   for usage.
3. **Compatibility digests.** Signed digests generated during bundle releases
   capture dependency ranges and hard/soft/suggest classifications. The
   dashboard stores the latest digest locally so it can compare live manifests
   against the authoritative release snapshot even when the operator is
   offline.

The dashboard refreshes automatically whenever a new digest arrives or when a
plugin manifest changes on disk. Operators can also trigger a manual refresh
from the action menu when preparing a rig for deployment.

## UI layout and status semantics

- **Summary banner.** Displays aggregate state for the bundle (`Healthy`,
  `Warnings`, `Blocked`). `Healthy` requires zero failing hard dependencies and
  no unknown manifests. `Warnings` indicates only soft/suggest issues. `Blocked`
  is raised when a hard dependency fails or the runtime version falls outside a
  manifest range described in ADR-031.
- **Plugin matrix.** Each plugin row shows:
  - Identity (name, version, channel) sourced from the manifest.
  - Compatibility badges for Core, AgIO, and UI shells based on the recorded
    ranges.
  - Dependency status pills grouped by Hard/Soft/Suggest classifications.
  - Quick actions for remediation (enable dependency, download update, open
    docs).
- **Filters.** Operators can filter by severity, capability (e.g. `devices.*`),
  or runtime surface to match ADR-031’s governance views. A “Show bundle only”
  toggle limits the matrix to the official first-party plugins when diagnosing
  bundle health.
- **Details drawer.** Selecting a plugin expands a drawer with:
  - Full dependency graph highlighting the failing edges.
  - Recent manifest history with commit metadata.
  - Links to QA evidence recorded during the
    [Plugin QA handshake](../qa/plugin-qa-handshake.md).

## Remediation workflow

1. **Investigate failing dependency.** The drawer surfaces the incompatible
   version or missing plugin and links to supporting documentation.
2. **Apply fix.** Operators can launch installers, enable plugins, or roll back
   updates directly from the quick actions.
3. **Re-validate.** After remediation, rerun the manifest lint to update the
   capability feed, then refresh the dashboard. Soft dependency warnings can be
   acknowledged for 7 days; hard failures must be resolved or explicitly tagged
   as running in compatibility mode per ADR-031.
4. **Escalate.** If remediation fails, export the digest and lint logs via the
   dashboard so support or QA can reproduce the configuration.

## Telemetry and audit trail

The dashboard emits structured telemetry events (`device-manager.compatibility`)
that include the plugin ID, dependency classification, and operator action.
Telemetry is forwarded to the governance analytics pipeline described in
ADR-031 so release reviews can confirm bundle readiness. Audit exports bundle
these events with the manifest digest to create a traceable history of operator
acknowledgements and remediations.

## Integration checkpoints

- Bundle releases must publish updated capability exports before the dashboard
  changes ships so the UI reflects new dependencies on day one.
- Regression tests seed simulated manifests with failing Hard, Soft, and
  Suggest dependencies to ensure the UI state machine, filters, and exports
  behave as documented.
- Companion shells reuse the same capability feed; ensure regression snapshots
  cover Avalonia and web targets so parity stays intact.
