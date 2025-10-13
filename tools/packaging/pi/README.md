# Nexus Raspberry Pi Packaging

This directory contains tooling to build a `.deb` package that installs the
headless Nexus services (Core and AGiO) on Raspberry Pi OS or other Debian-based
systems. The package configures systemd units so the services start at boot.

## Prerequisites

- .NET 8 SDK (for `dotnet publish`)
- `dpkg-deb`
- `bash` and common GNU utilities (`coreutils`, `findutils`)

On Raspberry Pi OS you can install the runtime dependencies with:

```bash
sudo apt-get install -y dpkg-dev
# Install the .NET 8 SDK from Microsoft package feeds before running the build.
```

## Building the package

Run the packaging script from the repository root:

```bash
./tools/packaging/pi/build-deb.sh --version 0.1.0
```

Key options:

- `--version <semver>` – package version string (required for release builds).
- `--configuration <Debug|Release>` – defaults to `Release`.
- `--runtime <RID>` – defaults to `linux-arm64` for Raspberry Pi 4/5.
- `--output <dir>` – directory to place the resulting `.deb` (defaults to
  `artifacts/` under the repo root).

The script will:

1. Publish the Core and AGiO hosts for the specified runtime.
2. Stage the binaries under `/opt/nexus/{core,agio}`.
3. Copy default configuration to `/etc/nexus/` and create environment overrides
   (`core.env`, `agio.env`).
4. Install systemd units (`nexus-core.service`, `nexus-agio.service`).
5. Build `nexus-pi_<version>_<arch>.deb` in the chosen output directory.

## Installing on a device

```bash
sudo dpkg -i nexus-pi_0.1.0_arm64.deb
```

The installer will create a dedicated `nexus` system user, reload systemd, and
enable the services. Configuration files live in `/etc/nexus/`:

- `/etc/nexus/core.env`
- `/etc/nexus/agio.env`
- `/etc/nexus/core/appsettings.json`
- `/etc/nexus/agio/appsettings.json`

After editing configuration, reload services:

```bash
sudo systemctl restart nexus-core.service nexus-agio.service
```

## Service overview

- `nexus-core.service` runs the Core host (`Aog.Core.Host`).
- `nexus-agio.service` runs the AGiO host (`Aog.Agio`).

Both services publish structured logs to `journalctl -u nexus-*.service` and
restart automatically on failure.

