# Pi / CM5 Quick Start: Simulation First, Then GPS

This guide walks through preparing a Raspberry Pi 4/5 or Compute Module 5 (CM5) to run
Nexus in simulation and then attach a live GNSS receiver. The goal is to confirm the
software stack on the bench before heading to the field.

> **Audience:** Builders bringing up Nexus on a Pi-class device for the first time.
> **Outcome:** You can boot the Pi, run the headless simulator, and then switch the AGiO
> host to a real GPS feed without reinstalling anything.

## 1. Hardware & Accounts Checklist

| Item | Notes |
| --- | --- |
| Raspberry Pi 4B (4 GB+) or CM5 IO board | 64-bit Raspberry Pi OS (Bookworm) recommended |
| 32 GB+ microSD or NVMe (CM5) | Use high-endurance media for field units |
| Wired Ethernet or Wi-Fi with Internet | Needed for package installs and git clones |
| USB-C power supply (3 A min) | Prefer official Pi PSU |
| USB GNSS (u-blox, Ardusimple, etc.) or serial HAT | Must present as `/dev/ttyACM*` or `/dev/ttyUSB*` |
| Optional: USB-to-CAN/section control hardware | Keep disconnected during the first boot |
| GitHub account with repo access | Clone via HTTPS or SSH |

## 2. Flash the Operating System

1. Download **Raspberry Pi OS Lite (64-bit)** via Raspberry Pi Imager.
2. Use Imager to pre-configure:
   - Hostname (for example `nexus-pi`).
   - SSH enabled with user `nexus` (or your choice).
   - Wi-Fi credentials if Ethernet is unavailable.
   - Locale/time zone (UTC recommended for logs).
3. Flash the microSD/NVMe and boot the Pi. Confirm you can SSH in: `ssh nexus@nexus-pi.local`.

## 3. First-Boot Updates & Packages

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y git curl libicu72 libkrb5-3 libssl3 unzip
```

Install the .NET 8 runtime and SDK (required until packaging lands in NX-062):

```bash
# Microsoft package feed
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0
```

Reboot to pick up kernel and firmware updates:

```bash
sudo reboot
```

## 4. Clone Nexus & Build Once

```bash
mkdir -p ~/src && cd ~/src
git clone https://github.com/agopengps-official/AgOpenGPS-Nexus.git
cd AgOpenGPS-Nexus

# Optional: checkout a specific tag/branch if instructed
# git checkout feat/NX-064-pi-quickstart

dotnet restore
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build
```

A successful build confirms the SDK is installed correctly. Subsequent runs can use the
`tools/scripts` helpers without rebuilding.

## 5. Run the Simulation (No Hardware)

1. Stay in the repo root and point the helper at the Core host project (a dedicated
   sim host is still on the roadmap), then launch the composite simulator with
   [`nexus.sh`](../../tools/scripts/nexus.sh):

   ```bash
   export NEXUS_SIM_PROJECT="Nexus SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj"
   ./tools/scripts/nexus.sh sim
   ```

   The helper script reads the `NEXUS_SIM_PROJECT` override before falling back to its
   defaults, so exporting the path guarantees a valid project target. The project file
   lives at [`Aog.Core.Host/Aog.Core.Host.csproj`](../../Nexus%20SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj).【F:tools/scripts/nexus.sh†L18-L69】

2. The script runs `dotnet run` for the simulation host. You should see log lines for the
   SimClock, SimBus, and virtual sensors. Let it run for ~60 seconds.【F:tools/scripts/nexus.sh†L1-L99】
3. Capture a baseline log for troubleshooting later by re-running with `tee`:

   ```bash
   mkdir -p ~/logs
   ./tools/scripts/nexus.sh sim |& tee ~/logs/nexus-sim-baseline.log
   ```

   Stop the second run with `Ctrl+C` once you have ~30 seconds of data.

4. Subsequent runs can omit `tee`; keep `~/logs/nexus-sim-baseline.log` as a reference when
   comparing future builds.

> **Tip:** If you plan to use the Pi headless, pair the simulator with a desktop PC running
> the Avalonia UI over gRPC once NX-040 lands. For now, log output verifies health.

## 6. Prepare for Live GPS

1. Plug in the USB GNSS receiver. Verify the kernel sees it:

   ```bash
   dmesg | tail
   ls /dev/ttyACM* /dev/ttyUSB*
   ```

2. Grant your user access to dialout devices and re-login:

   ```bash
   sudo usermod -aG dialout $USER
   ```

3. (Optional) Install `gpsd` for quick sanity checks:

   ```bash
   sudo apt install -y gpsd gpsd-clients
   sudo systemctl stop gpsd.socket gpsd.service
   sudo gpsd -n -D2 /dev/ttyACM0
   cgps -s
   ```

   Exit `cgps` with `Ctrl+C` and stop the manual `gpsd` run before proceeding.

## 7. Switch Nexus to GPS Input

Until the AGiO packaging (NX-062) ships, run the AGiO host directly. Export the project
path override because the host project still lives under [`Aog.Agio`](../../Nexus%20SourceCode/src/Aog.Agio/Aog.Agio.csproj).
Select the Linux backend so serial, gpsd, and SocketCAN services all light up:

```bash
export NEXUS_AGIO_PROJECT="Nexus SourceCode/src/Aog.Agio/Aog.Agio.csproj"
export NEXUS_AGIOHOST__BACKEND__ASSEMBLY="Aog.Agio.Linux"
export NEXUS_AGIOHOST__BACKEND__TYPE="Aog.Agio.Linux.LinuxAgioBackend"
./tools/scripts/nexus.sh run agio
```

The Linux backend wires three subsystems; use the knobs below to tune or disable them as
needed:

| Subsystem | Default behavior | How to tweak or disable |
| --- | --- | --- |
| Serial NMEA auto-scanner | Enumerates `/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/ttyAMA*`, `/dev/ttyS*`, and `/dev/serial/by-id/` to locate GNSS receivers, then publishes the first stream that emits GGA/RMC/VTG sentences.【F:Nexus SourceCode/src/Aog.Agio.Linux/Serial/LinuxSerialPortEnumerator.cs†L17-L89】【F:Nexus SourceCode/src/Aog.Agio/Serial/NmeaAutoScanner.cs†L18-L118】 | Set `AgioHost:Linux:Serial:DevicePrefixes` to a narrower list (or an empty array) in `/etc/aog/agio/appsettings.json` or via `NEXUS_AGIOHOST__LINUX__SERIAL__DEVICEPREFIXES__0=...` overrides to skip specific ports.【F:Nexus SourceCode/src/Aog.Agio.Linux/Serial/LinuxSerialPortEnumeratorOptions.cs†L10-L37】 |
| gpsd monitor | Connects to `/var/run/gpsd.sock`, logs TPV updates, and retries when the daemon is unavailable.【F:Nexus SourceCode/src/Aog.Agio.Linux/Gpsd/GpsdBackgroundService.cs†L27-L98】 | Leave the socket blank by exporting `NEXUS_AGIOHOST__LINUX__GPSD__SOCKETPATH=` (or setting the value to `null`/`""` in appsettings) to disable the worker entirely.【F:Nexus SourceCode/src/Aog.Agio.Linux/Gpsd/GpsdClientOptions.cs†L8-L36】 |
| SocketCAN bridge | Opens `can0` (or another configured interface), republishes frames over gRPC, and exposes them through the `SocketCanBusService` stream.【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanBackgroundService.cs†L19-L122】【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanBusService.cs†L14-L84】 | Point `AgioHost:Linux:SocketCan:InterfaceName` at `vcan0`/`can1`, or set it to an empty string to pause the monitor until a non-empty value is provided.【F:Nexus SourceCode/src/Aog.Agio.Linux/SocketCan/SocketCanOptions.cs†L21-L70】 |

If you need the legacy single-port serial backend, pass explicit arguments instead of
selecting the Linux bundle:

```bash
./tools/scripts/nexus.sh run agio -- --backend Serial --port /dev/ttyACM0 --baud 115200
```

Leave the AGiO host running and, in a second SSH session, start the simulator again to
exercise the pipeline with mixed simulated and live data. Once the Core host is available,
substitute `./tools/scripts/nexus.sh run core` to consume the GNSS stream.

## 8. Persist as Services (Optional)

If you want the Pi to auto-start Nexus components:

1. Create user-level systemd units under `~/.config/systemd/user/` using the templates that
   will ship with NX-062.
2. Enable lingering for your user so services survive SSH logout: `sudo loginctl enable-linger $USER`.
3. Enable and start the units:

   ```bash
   systemctl --user enable nexus-sim.service
   systemctl --user start nexus-sim.service
   ```

   Repeat for `nexus-agio.service` once you are satisfied with the configuration.

## 9. Troubleshooting

| Symptom | Fix |
| --- | --- |
| `dotnet: command not found` | Re-run the .NET install commands; confirm `/usr/bin/dotnet` exists. |
| Simulator exits immediately | Check `~/.nexus/logs/` for stack traces. Missing `DOTNET_ROOT` can cause this—export `DOTNET_ROOT=/usr/lib/dotnet`. |
| No `/dev/ttyACM*` device | Confirm the GNSS is powered and supports USB CDC. Try another cable or port. |
| `UnauthorizedAccessException` when opening serial | Ensure your user is in the `dialout` group and log out/in. |
| GPS shows `NO FIX` indoors | Move the antenna near a window or use an active antenna outdoors. |

## 10. Next Steps

- Document your exact hardware (receiver model, antenna, power notes) in the team wiki.
- Pair this guide with the [Windows no-hardware quick start](windows-no-hw.md) to rehearse
  UI validation before heading to the cab.
- Contribute logs and feedback to `#nexus-dev` so packaging tasks (NX-062) can bundle the
  right dependencies.

You now have a Pi or CM5 configured to run Nexus in simulation and ready to ingest live
GPS data. Keep the baseline logs handy to compare against future builds.
