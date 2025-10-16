# Nexus Services Deployment Guide

The NX-341 packaging automation delivers self-contained archives for each host process. Use this guide to run them as services on Linux and Windows.

## Service Layout

```
<install-root>/
  core/               # Nexus.Core binaries and config
  agio/               # Nexus.AgIO binaries and config
  ui/                 # Nexus.UI binaries (optional on headless rigs)
  plugins/<Name>/     # Plugin payloads
  nexus-launcher.*    # Convenience launchers for Windows/Linux
  release.json        # Bundle metadata (name, version, timestamp)
```

## Linux (systemd)

1. Extract the desired bundle to `/opt/nexus/<version>/`.
2. Copy `packaging/common/nexus-launcher.sh` into the bundle root (already included by CI).
3. Create systemd service units similar to:

```
[Unit]
Description=Nexus Bundle Launcher
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/nexus/v0.3.0
ExecStart=/opt/nexus/v0.3.0/nexus-launcher.sh
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

4. Enable and start the service:

```
sudo systemctl daemon-reload
sudo systemctl enable --now nexus.service
```

5. Tail logs with `journalctl -fu nexus.service`.

For environments that prefer individual processes, create separate units that call `core/Nexus.Core`, `agio/Nexus.AgIO`, and `ui/Nexus.UI` with appropriate dependencies.

## Windows (Scheduled Task or Service Wrapper)

- Extract the bundle to `C:\Nexus\v0.3.0`.
- Use `nexus-launcher.cmd` to start the processes; it launches Core, AgIO, then UI.
- To run at login, create a scheduled task that runs `nexus-launcher.cmd` with "Run with highest privileges".
- For service-style behavior, use NSSM (Non-Sucking Service Manager) or `sc create` pointing to the launcher script. Ensure the task/service account has permissions to access GNSS devices and CAN adapters.

## Configuration Management

- Configuration files live next to the executables (`appsettings.json`, `*.yaml`). Store overrides under `config/` and mount them via symlinks if you rotate bundles.
- Keep secrets out of version control; use environment variables or external stores referenced in the configuration.

## Health & Monitoring

- Core and AgIO emit structured logs via Serilog; forward them to your preferred collector.
- The release workflow publishes SBOMs and checksums so you can pre-register binaries with allowlists.
- Use `release.json` to confirm the running version during support escalations.

These practices keep deployments aligned with the manifest-driven bundles produced by the Release workflow.
