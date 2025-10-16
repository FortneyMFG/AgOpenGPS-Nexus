# Linux Core service packaging (NX-462)

This directory contains the packaging assets for the headless Linux "AOG Core"
service described by [O-BACKEND-6](../../../docs/SRS/options/O-BACKEND-6_LinuxCoreService.md).
The `build-packages.sh` helper builds `.deb` and `.rpm` installers that:

- Publish the Core and AGiO hosts for a chosen runtime (default `linux-x64`).
- Stage binaries under `/usr/lib/aog/{core,agio}` with wrapper launchers in
  `/usr/bin` so systemd can start them without `dotnet` on the PATH.
- Drop configuration and environment overrides in `/etc/aog/` and symlink the
  published `appsettings.json` files back to that location.
- Install `aog-core.service` and `aog-agio.service` units that honour the
  restart/health expectations from SRS §10 and ADR-068.
- Create `/var/lib/aog` as the data/log root for replay capture and diagnostics.

## Usage

```bash
./build-packages.sh \
  --version 0.8.0 \
  --runtime linux-x64 \
  --output ../../artifacts/packages \
  --format all
```

Options:

- `--version <semver>` – package version string (defaults to `0.0.0-dev`).
- `--configuration <name>` – build configuration (`Release` by default).
- `--runtime <rid>` – target RID (defaults to `linux-x64`).
- `--output <dir>` – destination folder for generated packages.
- `--format <deb|rpm|all>` – which package formats to build (default `all`).

The script requires `dotnet`, `dpkg-deb`, and (for RPM output) `rpmbuild`.
Installers ensure the `aogsvc` service account exists, reload systemd, and
restart both units on upgrade. The post-install and pre-uninstall hooks mirror
O-BACKEND-6's expectation that the Core service is supervised and restartable.

## Layout

The resulting packages install the following layout:

```
/usr/bin/aog-core            # Wrapper that executes the Core host
/usr/bin/aog-agio            # Wrapper that executes the AGiO host
/usr/lib/aog/core/           # Published Core binaries
/usr/lib/aog/agio/           # Published AGiO binaries
/etc/aog/core/appsettings.json
/etc/aog/agio/appsettings.json
/etc/aog/core.env            # Environment overrides for the Core host
/etc/aog/agio.env            # Environment overrides for the AGiO host
/lib/systemd/system/aog-core.service
/lib/systemd/system/aog-agio.service
/var/lib/aog/                # Replay logs, tiles, diagnostics
```

Symlinks keep `appsettings.json` next to each binary directory so existing
configuration loaders continue to work while giving operators a stable place to
apply overrides.
