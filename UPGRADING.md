# Upgrading Nexus Releases

Nexus releases are assembled from self-contained component archives and optional bundles that combine Core, AgIO, UI, and plugins.
Follow these steps when moving to a newer tag:

## 1. Review the Release Notes
- Every GitHub Release is created by `.github/workflows/release.yml`.
- Components are always available even if bundle assembly fails.
- Bundles are marked `components-only` when required artifacts are missing so you can make an informed decision about waiting for a retry.

## 2. Download Required Artifacts
- Bundles (`Nexus-Base`, `Nexus-Headless`, `Nexus-UI`) include the most common combinations of services.
- If you only need a specific component or plugin, download the individual archive that matches your runtime (Windows `zip`, Linux `tar.gz`).
- Compare filenames against the manifest in `bundles/*.bundle.json` if you maintain a downstream mirror.

## 3. Verify Integrity
- Check `SHA256SUMS.txt` attached to the release for matching hashes.
- Optional Cosign signatures (`*.sig`/`*.crt`) can be verified with `cosign verify-blob`.
- SBOMs (`sbom-<tag>.spdx.json`) describe all bundle contents for compliance tracking.

## 4. Stage the Upgrade
- Extract archives into a staging directory rather than overwriting a live installation.
- Preserve your configuration files (`config/*.json`, `plugins/*/config.json`) before replacing binaries.
- On Linux, update systemd services (see `SERVICES.md`) to point to the new version directory, then restart.
- On Windows, the launcher (`nexus-launcher.cmd`) starts Core, AgIO, then UI; update any scheduled tasks or shortcuts to the new location.

## 5. Migrate Plugins
- Plugin directories live under `plugins/<Name>/` inside the bundle.
- If you maintain custom plugins, copy them into the new `plugins/` directory after verifying compatibility notes in `PLUGINS.md`.
- Restart the host to pick up plugin changes; hot-swapping is not yet supported.

## 6. Roll Back if Needed
- Keep the previous release directory until the new version passes your smoke tests.
- To roll back, stop services, switch symlinks or shortcuts to the previous directory, and restart.
- Report regressions with the release tag, bundle type, and affected runtime so CI can be updated.

Following this checklist keeps upgrades repeatable and aligned with the manifest-driven packaging pipeline introduced for NX-341.
