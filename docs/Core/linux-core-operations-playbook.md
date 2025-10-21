# Linux Core operations & observability playbook (NX-463)

> **Audience:** Operators and support engineers deploying the headless Linux
> "AOG Core" service alongside AGiO hardware bridges. The goal is to keep rigs
> supervised under systemd, surface health signals demanded by SRS §10, and
> reuse ADR-068 replay tooling for diagnostics.

## 1. Prerequisites

- Build the Linux packages with `tools/packaging/linux/build-packages.sh` or
  obtain them from the release pipeline. The script publishes Core + AGiO
  binaries, drops wrapper launchers under `/usr/bin`, and stages systemd units
  (`aog-core.service`, `aog-agio.service`) with hardened defaults.【F:tools/packaging/linux/build-packages.sh†L1-L214】【F:tools/packaging/linux/systemd/aog-core.service†L1-L27】
- Ensure the host satisfies O-BACKEND-6: Ubuntu/Debian or RPM-based distro with
  .NET 8 runtime, writable `/var/lib/aog` for state, and `/etc/aog` for
  configuration overrides.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L33】
- Confirm access to the `nexus` release bundle if you need to cross-check
  appsettings defaults or plugin payloads.【F:SERVICES.md†L9-L46】

## 2. Install & upgrade

### Debian/Ubuntu

```bash
sudo dpkg -i aog-core_<version>_amd64.deb
```

The `postinst` hook creates the `aogsvc` system account, ensures `/var/lib/aog`
and `/var/log/aog` exist, enables both systemd units, and restarts them on
upgrade.【F:tools/packaging/linux/build-packages.sh†L216-L269】

### RHEL/Fedora/openSUSE

```bash
sudo rpm -Uvh aog-core-<version>-1.x86_64.rpm
```

The RPM spec mirrors the Debian lifecycle: it adds the `aogsvc` user, reloads
systemd, and restarts the services so new bits take effect immediately while
retaining config under `/etc/aog`.【F:tools/packaging/linux/build-packages.sh†L271-L353】

### Rollback

Both package formats leave configuration untouched. To revert, reinstall the
previous version (`dpkg -i <old>.deb` or `rpm -Uvh <old>.rpm`). Services restart
with the older binaries while keeping local overrides intact.

## 3. Service operations

Systemd installs two units:

- `aog-agio.service` – hardware I/O bridge. Starts first, exposes GNSS, CAN,
  and RadioBridge backends.【F:tools/packaging/linux/systemd/aog-agio.service†L1-L25】
- `aog-core.service` – headless Core orchestration, depends on AGiO and emits
  health heartbeats for supervision.【F:tools/packaging/linux/systemd/aog-core.service†L1-L26】【F:Nexus SourceCode/src/Aog.Core.Host/CoreHealthService.cs†L8-L68】

When `aog-agio.service` loads the Linux backend it activates three independent
hardware watchers. Tune them via `/etc/aog/agio/appsettings.json` or the
matching `NEXUS_` environment overrides:

| Subsystem | Default behavior | Disable/tune via |
| --- | --- | --- |
| Serial NMEA auto-scanner | Probes `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/ttyAMA*`, `/dev/ttyS*`, and `/dev/serial/by-id/`, opening the first port that produces GGA/RMC/VTG sentences and streaming them over gRPC.【F:Nexus SourceCode/src/Aog.Agio.Linux/Serial/LinuxSerialPortEnumerator.cs†L17-L89】【F:Nexus SourceCode/src/Aog.Agio/Serial/NmeaAutoScanner.cs†L18-L118】 | Trim `AgioHost:Linux:Serial:DevicePrefixes` or set it to an empty array to pause enumeration. Adjust scan cadence under `AgioHost:Linux:Serial:Scan` (`ProbeDuration`, `ReadTimeout`, `BaudRates`, `MaxReadAttemptsPerPort`).【F:Nexus SourceCode/src/Aog.Agio.Linux/Serial/LinuxSerialPortEnumeratorOptions.cs†L10-L64】【F:Nexus SourceCode/src/Aog.Agio/Serial/NmeaSerialPortScanOptions.cs†L8-L44】 |

When overriding `AgioHost:Linux:Serial:DevicePrefixes`, provide absolute paths (optionally ending in `/` to treat the entry as a directory). The loader trims whitespace and ignores any non-rooted values before probing.【F:Nexus SourceCode/src/Aog.Agio.Linux/Serial/LinuxSerialPortEnumeratorOptions.cs†L10-L64】
| gpsd monitor | Connects to `/var/run/gpsd.sock`, issues a WATCH request, and logs TPV reports while the daemon is present.【F:Nexus SourceCode/src/Aog.Agio.Linux/Gpsd/GpsdBackgroundService.cs†L27-L98】 | Clear `AgioHost:Linux:Gpsd:SocketPath` (empty string or `null`) to skip gpsd integration entirely.【F:Nexus SourceCode/src/Aog.Agio.Linux/Gpsd/GpsdClientOptions.cs†L8-L36】 |
| SocketCAN bridge | Consumes frames from `can0` by default, republishes them through `SocketCanBusService`, and supports hot reconfiguration when the interface changes.【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanBackgroundService.cs†L19-L137】【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanBusService.cs†L14-L84】 | Set `AgioHost:Linux:SocketCan:InterfaceName` to a specific bus (for example `vcan0`), or leave it empty to keep the worker idle until an interface is provided.【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanOptions.cs†L21-L70】 |

Example serial scan overrides in `/etc/aog/agio/appsettings.json`:

```json
{
  "AgioHost": {
    "Linux": {
      "Serial": {
        "Scan": {
          "ProbeDuration": "00:00:07",
          "ReadTimeout": "00:00:01",
          "BaudRates": [4800, 230400],
          "MaxReadAttemptsPerPort": 10
        }
      }
    }
  }
}
```

Common commands:

```bash
sudo systemctl status aog-core
sudo systemctl status aog-agio
sudo systemctl restart aog-core
sudo systemctl disable --now aog-core aog-agio    # for maintenance windows
```

Logs stream into journald and `/var/log/aog` thanks to the unit `LogsDirectory`
settings. Tail recent activity with:

```bash
journalctl -u aog-core -fu
journalctl -u aog-agio -fu
ls -R /var/log/aog
```

If AGiO safety logging is left at its default relative path (`logs/safety`),
update `/etc/aog/agio/appsettings.json` to point at `/var/log/aog/agio-safety`
so rotation aligns with the package-managed directories.【F:Nexus SourceCode/src/Aog.Agio/appsettings.json†L4-L33】

## 4. Health & observability

### Core heartbeat & handshake

`CoreHealthService` emits “Core host heartbeat OK” every 30 seconds by default.
Adjust cadence with an environment override in `/etc/aog/core.env`:

```bash
NEXUS_COREHOST__HEALTH__INTERVALSECONDS=10
```

Reload the service (`systemctl restart aog-core`) to apply changes.【F:Nexus SourceCode/src/Aog.Core.Host/CoreHealthService.cs†L12-L54】

The startup sequence performs an AGiO capabilities handshake over gRPC and logs
accepted/rejected capabilities. Inspect the journal after boot to confirm both
sides negotiated the same descriptors or override `NEXUS_COREHOST__CAPABILITIES`
values in `core.env`/`appsettings.json` when introducing new telemetry.【F:Nexus SourceCode/src/Aog.Core.Host/Capabilities/CapabilitiesHandshakeService.cs†L16-L82】

### AGiO safety & timing probes

AGiO records failsafe events as JSONL files. With the service running you should
see timestamped entries under the configured safety log directory each time the
heartbeat window is exceeded.【F:Nexus SourceCode/tests/Aog.Agio.Tests/FailsafeIntegrationTests.cs†L19-L68】 Adjust
timeouts by editing `AgioHost:Safety` in `/etc/aog/agio/appsettings.json` or by
setting environment overrides (e.g. `NEXUS_AGIOHOST__SAFETY__HEARTBEATMS`).

Timing capabilities probes run automatically on Linux; failures surface in the
AGiO journal. Investigate persistent warnings before fielding builds on SBCs.

### Log shipping & offline retention

- Use `journalctl --since "-1 day" -u aog-core -o json` to export structured
  health logs and attach them to support tickets, satisfying the telemetry
  readiness goals in SRS §10.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L1-L37】
- Mirror `/var/log/aog` and replay bundles from `/var/lib/aog` onto a support
  workstation when diagnosing issues offline. Packaging keeps permissions owned
  by `aogsvc` to preserve provenance when copying across systems.【F:tools/packaging/linux/build-packages.sh†L231-L269】

## 5. Replay & smoke validation

For post-incident analysis or new deployments, run the deterministic replay
harness from ADR-068 against the freshly installed services:

1. Generate a replay bundle with `TelemetryParquetLogger` (e.g. via CI fixture).
2. Use `TelemetryReplayController` to stream samples into an in-memory bus while
   monitoring Core/AGiO logs for consistent processing.【F:Nexus SourceCode/tests/Aog.Core.Tests/Replay/TelemetryReplayControllerTests.cs†L17-L111】
3. Confirm the Core↔AGiO handshake succeeds via the integration test added in
   NX-464 (`HeadlessCoreAgioIntegrationTests`). Run locally with:

   ```bash
   dotnet test Nexus\ SourceCode/tests/Aog.Core.Host.Tests --filter HeadlessCoreAgioIntegration
   ```

   The test spins up a gRPC handshake server, boots the Core host, and asserts
   that capabilities are negotiated, giving a fast regression check before field
   updates.【F:Nexus SourceCode/tests/Aog.Core.Host.Tests/HeadlessCoreAgioIntegrationTests.cs†L1-L125】

These steps provide the “replay + smoke” circuit required by ADR-068 and
O-BACKEND-6 so operators can validate builds before rolling them to production
fleets.
