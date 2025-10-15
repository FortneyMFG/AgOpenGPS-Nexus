# Plugin Lease & Manifest Governance

ADR-018 introduces lease-based access control and manifest enforcement for plugins so Core can guarantee compatibility and safe migrations from legacy AgIO integrations. This guide summarises the expectations for plugin authors, reviewers, and CI.

## Lease Model

- **Lease issuance.** Core grants leases per capability (e.g., GuidanceControl, SectionCommand) with an expiry timestamp. Plugins must renew leases before expiry or surrender control gracefully.
- **Conflict resolution.** If multiple plugins request the same capability, the arbiter selects the highest-priority lease holder and notifies others via capability events.
- **Revocation.** Core may revoke leases during fault conditions. Plugins must listen for revocation messages and stop commanding hardware immediately.

## Manifest Requirements

1. **Metadata.** Include plugin ID, version, supported capabilities, required transports, and minimum Nexus runtime version.
2. **Lease declarations.** For each capability, specify acquisition mode (exclusive/shared), timeout budgets, and recovery strategies.
3. **AgIO migration hooks.** Declare the AgIO backend bindings or bridge adapters needed when running side-by-side with legacy services.
4. **Validation scripts.** Provide automated checks (PowerShell/Bash) that validate environment prerequisites before activation.

## CI & Release Gates

- **Manifest lint.** `./tools/scripts/nexus.sh plugin lint` (or the PowerShell variant)
  verifies schema compliance, lease declarations, and dependency constraints before
  manifests are promoted.
- **Capability export.** `./tools/scripts/nexus.sh plugin capabilities` publishes the
  capability/lease matrix consumed by Device Manager and CI dashboards.
- **Lease simulation.** Plugin regression suite runs simulated Core lease arbitration scenarios to ensure plugins back off/resume correctly.
- **Bridge compatibility.** For plugins migrating from AgIO, CI runs dual-stack tests where both legacy and Nexus transports operate simultaneously.
- **Human QA.** Record hardware validation (who/when) in `tasks.md` before promoting the plugin bundle.

## Reviewer Checklist

- [ ] Manifest checked into source control with version bump.
- [ ] Lease handling code responds correctly to grant, renew, revoke, and expiry events.
- [ ] Plugin telemetry surfaces lease state for operators (UI dashboard or logs).
- [ ] Documentation updated with activation instructions and migration notes.

Following these rules keeps the plugin ecosystem predictable while Core transitions to ADR-018 lease enforcement.
