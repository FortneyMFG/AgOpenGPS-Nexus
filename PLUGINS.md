# Nexus Plugin Management

The Release workflow publishes official plugins alongside Core, AgIO, and UI components. Use this guide to add, update, or validate plugins in your deployment.

## Plugin Packaging
- Each plugin archive follows the pattern `Plugin-<Name>_v<VER>_<RUNTIME>.<ext>`;
  cross-runtime bundles may instead use `<manifest-id>-<ver>-<rid>.zip` when
  authored outside of MSBuild.
- Archives contain a `plugins/<Name>/` directory with binaries, manifests, and sample configuration.
- Bundles extract plugins into `<install-root>/plugins/<Name>/` based on manifest requirements in `bundles/*.bundle.json`.
- Cross-runtime bundles (Pumpkin Pi HAL, Nexus CLI sample) are packaged via
  `tools/ci/package-sidecar-plugins.ps1`, which emits manifest-first zip
  archives matching the MSBuild `PackPlugin` layout.

## Installing Plugins
1. Download the plugin archive for your runtime.
2. Verify the SHA256 checksum or Cosign signature provided in the release assets.
3. Extract into `<install-root>/plugins/<Name>/`.
4. Restart the host processes (Core/AgIO/UI) to load the plugin.

## Updating Plugins
- Follow the upgrade checklist in `UPGRADING.md` to stage updates.
- Preserve plugin-specific configuration files before overwriting binaries.
- If a plugin has schema updates, review the linked ADR/SRS references in the release notes.

## Custom Plugins
- Place custom plugins under `plugins/<Name>/custom/` to avoid collisions with official releases.
- Document compatibility expectations and version support inside a `README.md` within the plugin directory.
- Use the same directory layout as official plugins so packaging scripts and service launchers can treat them uniformly.

## Validation
- Run deterministic simulation scenarios relevant to the plugin after each upgrade.
- Monitor logs for `PluginManifest` entries; missing manifests indicate the plugin was not discovered.
- File issues with the release tag, plugin name, runtime, and logs to help reproduce problems.

Manifest-driven packaging ensures bundles are never partial—if a plugin is missing the Release workflow keeps the release in components-only mode until the gap is resolved.
