# Plugin Packaging, Updates, and Catalog (Status: collecting proposals)

## Problem statement
AgOpenGPS needs a consistent, auditable way to package, distribute, verify, and update plugins so operators can extend the system without risking downtime or unsafe behaviour. The specification must cover archive layout, manifest expectations, trust and permission models, offline installs, and catalog governance so that community and enterprise contributors can ship modules confidently.

## Requirements (from contributors)
- R-PKG-000 (MUST, packaging): Plugins ship as deterministic `.zip` archives with a top-level `plugin.json` manifest and optional `backend/`, `ui/`, `assets/`, and `contrib/` folders plus recommended notices (LICENSE/NOTICE/changelog).
- R-PKG-001 (MUST, manifest): `plugin.json` follows the published schema covering identity, compatibility targets, backend and UI entrypoints, permission declarations, contributions, signing metadata, optional GitHub update metadata, and reserved dependency metadata.【F:docs/SRS/appendices/plugin_manifest.schema.json†L1-L118】
- R-PKG-002 (MUST, install locations): Hosts install to `<data_root>/plugins/<id>/<version>/` (system) or `~/.aog/plugins/<id>/<version>/` (per-user) and maintain `<...>/plugins/<id>/current → <version>` for rollback.
- R-PKG-003 (MUST, integrity): Installers validate the ZIP against SHA-256 values supplied by the catalog or a bundled `checksums.txt` for side-loaded packages before activation.
- R-PKG-004 (SHOULD, signing): When `signing` metadata is present, verify the Ed25519 signature against trusted public keys; warn or block unsigned packages based on policy.【F:docs/SRS/appendices/plugin_manifest.schema.json†L118-L142】
- R-PKG-005 (MUST, permissions): The Plugin Manager surfaces requested permissions (serial, SocketCAN, network, filesystem, etc.) during install/enable and requires explicit approval.【F:docs/SRS/appendices/plugin_manifest.schema.json†L86-L117】
- R-PKG-006 (MUST, isolation): Backend plugins execute under restricted OS users and cannot bind beyond localhost without granted permissions; the host enforces declared capabilities.
- R-PKG-007 (MUST, manifests vNext): `plugin.json` captures `provides.capabilities[].version/features` and `provides.profiles[]` entries with CI-conformance attestations so capability swaps and profile equivalency stay machine-verifiable.【F:docs/SRS/appendices/plugin_manifest.schema.json†L153-L236】【F:docs/ADR/ADR-031-official-plugin-bundle.md†L25-L74】
- R-PKG-008 (MUST, dependency expressions): `requires.capabilities[]`, `requires.profiles[]`, and relationship arrays (`peerOf`, `conflictsWith`, `replaces`, `extends`) describe peer expectations and migration rules that Core enforces during graph resolution.【F:docs/SRS/appendices/plugin_manifest.schema.json†L237-L332】【F:docs/plugins/nexus-plugin-dependency-map.md†L88-L143】
- R-PKG-009 (SHOULD, provider policy): Plugin bundles may ship policy overlays (e.g., `[capability."mapping:vector"]`) documenting admin pins, allow-multiple flags, and provider priorities so deterministic selection is auditable and reproducible.【F:docs/plugins/nexus-plugin-dependency-map.md†L144-L205】
- R-PKG-010 (SHOULD, updates): Provide an optional auto-update utility that respects per-plugin channels (stable/beta/pinned), SemVer policies, and the declared compatibility ranges.
- R-PKG-011 (SHOULD, update flow): Updates download, verify, stage into versioned folders, retain the previous build, switch via the `current` symlink (restart or hot-reload), and expose a one-click rollback.
- R-PKG-012 (SHOULD, caching): Cache catalog indexes and release metadata locally, observe HTTP caching headers (ETag/If-Modified-Since), and support mirrored sources for restricted networks.
- R-PKG-020 (MUST, catalog): Approved catalogs publish signed `catalog.json` indexes that describe plugins, channels, checksums, signatures, compatibility, permissions, and icon URLs per the schema.【F:docs/SRS/appendices/plugin_catalog.schema.json†L1-L84】【F:docs/SRS/appendices/plugin_catalog.schema.json†L84-L120】
- R-PKG-021 (SHOULD, governance): Catalog entries are managed through PR-based workflows with CI validation of icons, checksums, signatures, SemVer ranges, and compatibility fields.
- R-PKG-022 (SHOULD, UI capabilities): The Plugin Manager supports browsing/searching catalog entries, filtering by category/permissions, installing/updating/uninstalling, toggling channels (global or per-plugin), viewing changelogs, highlighting permission diffs, and showing trust tiers plus health/log links.
- R-PKG-030 (MUST, side-loading): Users can install by dragging a plugin ZIP into the UI or pointing to a filesystem path; the host validates manifests, checksums, and warns for unsigned packages.
- R-PKG-031 (MUST, offline): Accept `file://` or removable-media indexes containing `catalog.json` plus assets to enable offline environments.
- R-PKG-040 (MUST, bundled deps): Initial releases forbid transitive dependencies; each plugin bundles what it needs. Future dependency metadata remains reserved in the schema.【F:docs/SRS/appendices/plugin_manifest.schema.json†L142-L152】
- R-PKG-050 (MUST, trust policy): Trust tiers differentiate approved (catalog + signed), third-party signed, and unsigned plugins; default policy accepts the first two and prompts on unsigned, while admin mode can enforce “approved only”.
- R-PKG-060 (MUST, runtime management): Core services validate archives, enforce permissions, sandbox processes, manage backend lifecycle (start/stop/retry with backoff), and expose health/log telemetry for the UI.
- R-PKG-070 (MUST, verification suite): Acceptance coverage includes catalog install success, side-loaded install with checksum validation, auto-update staging/switchover/rollback, unsigned warning flows, signature mismatch rejection, permission diff prompts, and backend crash containment.

## Options
- O-PKG-0: Status quo — Share ad-hoc binaries/assemblies without a manifest or catalog.
- O-PKG-1: Standardized ZIP packaging with manifest + catalog governance (proposed).
- O-PKG-2: Adopt external package managers (NuGet/MSIX) for plugins instead of custom ZIP archives.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-PKG-0 | Zero new tooling | No trust model, manual installs | Unsafe/unknown plugins | Current ZIP drops |
| O-PKG-1 | Deterministic installs, catalog UX, rollback | Requires signing + governance infra | Catalog compromise or tooling debt | VS Code extensions, Arduino Library Manager |
| O-PKG-2 | Leverages mature ecosystems | Heavyweight, platform-specific | Harder offline support, dependency drift | NuGet/MSIX packaging |

## Evaluation criteria
Safety, offline usability, rollback resilience, governance overhead, contributor ergonomics, and compatibility with existing release workflows.

## Current sentiment
The team prefers the standardized ZIP manifest approach (O-PKG-1) because it delivers predictable installs, optional automation, and a curated catalog while staying close to current ZIP distribution habits and allowing offline media workflows.

## Open questions
- What tooling ships with the SDK to help authors build/sign ZIPs?
- How are enterprise catalogs authenticated and distributed to fleets with no outbound internet?
- Do we need to predefine additional permission scopes (e.g., CAN bus filters, camera access) before beta release?

## Acceptance criteria
- Catalog and side-loaded installs succeed only when manifests and checksums validate; unsigned packages prompt users for confirmation.
- Auto-update discovers a newer version, stages it, switches via `current`, and supports a one-click rollback.
- A signed plugin with a mismatched checksum is rejected; permission deltas block updates until the operator approves them.
- Backend plugin crashes do not bring down the host; restart/backoff policies are logged and surfaced in the Plugin Manager.

## References
- Plugin manifest schema: Appendix `plugin_manifest.schema.json`.【F:docs/SRS/appendices/plugin_manifest.schema.json†L1-L152】
- Catalog index schema: Appendix `plugin_catalog.schema.json`.【F:docs/SRS/appendices/plugin_catalog.schema.json†L1-L120】
