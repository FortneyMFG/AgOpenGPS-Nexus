# Plugin Catalog & Marketplace UI (Planned)

The Plugin Catalog plugin exposes signed manifests from the Nexus marketplace so operators can browse, install, and manage plugins safely.

## Core capabilities

- Fetches signed catalog indexes from trusted sources, verifying signatures and hash manifests defined in ADR-031.
- Displays plugin metadata (name, version, author, compatibility range, required capabilities, telemetry status) with search and filter support.
- Provides install, update, rollback, and uninstall actions with dependency resolution, capability validation, and disk space checks.

## Governance & telemetry

- Surfaces health telemetry from Core (load success, capability warnings, crash stats) alongside catalog entries.
- Highlights plugins that require additional permissions (e.g., Automation Engine access, hardware control) and routes operators to approval workflows.
- Records installation actions in audit logs with operator identity and timestamp for support and compliance.

## UX flows

- Catalog home groups plugins by category (Guidance, Mapping, Analytics, Automation, Hardware) and flags updates available.
- Detail pages show changelogs, screenshots, dependency graphs, and compatibility notes. Operators can preview manifest diffs before upgrading.
- Batch operations allow staging updates for downtime windows with rollback checkpoints stored for recovery.

## Integration points

- Works with Sync Dashboard to expose curated plugin bundles for remote monitoring deployments.
- Coordinates with Automation Engine and Regulatory plugins to ensure feature gates and compliance requirements are met before activation.
- Leverages TaskService to schedule restarts when core plugins require downtime for updates.
