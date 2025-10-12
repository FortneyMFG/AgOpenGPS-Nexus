# Plugin Packaging, Updates, and Catalog (Status: proposed)

## Scope
Defines the packaging contract, distribution workflows, verification, and catalog governance for AgOpenGPS plugins. Applies to both UI and backend extensions delivered as ZIP archives.

## Packaging standard
- Plugins ship as a `.zip` archive with a deterministic layout:
  - `plugin.json` manifest (required).
  - Optional folders: `/backend/`, `/ui/`, `/assets/`, `/contrib/` for runtime components, UI assets, shared media, and contribution payloads.
  - Recommended notices: `LICENSE`, `NOTICE`, changelog.
- Install targets use versioned directories with a `current` symlink per plugin for rollback:
  - System: `<data_root>/plugins/<id>/<version>/`
  - User: `~/.aog/plugins/<id>/<version>/`
- Plugins avoid shared dependencies in v1; each archive bundles everything it needs. Future dependency metadata may extend the manifest with `dependencies` but is out of scope for initial rollout.

## Manifest requirements
- `plugin.json` includes:
  - Identity: `id`, `name`, `version`, `description`, `author`, `license`.
  - Compatibility: `targets.core`, `targets.ui` with SemVer ranges aligned to host builds.
  - Entry points: `entry.backend` for out-of-process services (binary path, args, IPC protocol) and `entry.ui` for UI module metadata.
  - Permissions: structured capability requests covering serial, SocketCAN, network bindings, filesystem paths, etc.; displayed during install/enable.
  - Contributions: references to layers, gauges, menus, or other extension artifacts under `/contrib/`.
  - Signing: digital signature metadata (`alg`, `sig`, optional `pub`) applied to the ZIP payload, enabling trust tier classification.
  - Updates (optional): GitHub Releases integration metadata (`provider`, `repo`, `assetPattern`, `channel`).
- JSON Schema definitions for `plugin.json` and `catalog.json` live in the appendices for tooling validation.

## Installation & verification
- ZIP integrity validated via SHA-256 checksums supplied by the catalog or `checksums.txt` inside side-loaded archives.
- Optional Ed25519 signatures are verified against trusted public keys (catalog-published or bundled). Missing signatures prompt users when security policy allows unsigned plugins.
- Backend plugins run under restricted OS accounts; network binds default to localhost unless permissions grant broader access.
- The host enforces permissions and surfaces them in the Plugin Manager before activation.

## Updates & rollback
- Hosts support manual or automated update checks, controlled per plugin or globally.
- Sources include curated catalog indexes, GitHub Releases (using provided repo/asset patterns), and local filesystem paths (for offline media).
- Update flow:
  1. Discover newer versions matching declared target ranges.
  2. Download ZIP, verify checksum and signature.
  3. Stage into `<id>/<version>/`, retaining the previous version for rollback.
  4. Flip `current` symlink atomically on restart or via hot reload.
- Only one rollback version is retained; UI exposes "Revert" to switch back.
- Metadata caches obey HTTP caching headers (ETag/If-Modified-Since) and support mirrored indexes for restricted networks.

## Approved catalog
- Curated catalog(s) publish a signed `catalog.json` index containing plugin metadata, channels (stable/beta), download URLs, checksums, signatures, and compatibility declarations.
- Users can add enterprise or offline catalog URLs, including `file://` paths pointing to removable media with `catalog.json` plus assets.
- Governance relies on PR-based submissions validated by CI (icons, checksums, semver, compatibility, signatures).
- UI features:
  - Browse/search/filter catalog entries by category or permissions.
  - Install/update/uninstall directly from catalog entries.
  - Toggle channels globally or per plugin (stable/beta/pinned).
  - Display changelog, permission diffs, and trust tier (approved, third-party signed, unsigned) before updates.
  - Surface health/log links and backend status for installed plugins.

## Acceptance criteria
- Installations from catalog and side-loaded ZIPs succeed with checksum validation.
- Auto-update discovers higher versions, stages safely, and swaps via symlink; rollback restores prior build.
- Unsigned plugins prompt for confirmation; mismatched signatures or checksums fail installation.
- Permission changes are shown during updates and require user approval.
- Backend plugin crashes do not bring down the host; restart/backoff policies are observed and logged.

## References
- Plugin manifest schema: see Appendix `plugin_manifest.schema.json`.
- Catalog index schema: see Appendix `plugin_catalog.schema.json`.
