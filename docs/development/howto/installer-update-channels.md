# Nexus installer & update channels (operator guide)

> **Audience:** Operators and field technicians responsible for installing Nexus or
> keeping test/production rigs updated.
> **Outcome:** You can choose the right rollout channel, stage offline media, and
> apply updates on Windows laptops/tablets or Raspberry Pi/CM5 hosts without
> breaking existing installs.

## Release channels at a glance

The release pipeline promotes builds through three channels before publishing
artifacts. Each channel corresponds to the `-Channel` switch accepted by the
release orchestrator (`tools/ci/release.ps1`).【F:tools/ci/release.ps1†L4-L87】 Use
the matrix below to decide which feed suits your rig.

| Channel | Intended audience | Update cadence | Guidance |
| --- | --- | --- | --- |
| `nightly` | Lab/test benches validating fixes as they land. | Whenever CI succeeds. | Expect occasional regressions. Keep the previous bundle handy for rollbacks. |
| `beta` | Pilot machines and advanced operators proving upcoming releases. | Promoted after nightly burn-in and sign-off. | Log issues quickly so they can be fixed before the release cut. |
| `release` | Production tractors and kiosks that prioritise uptime. | Formal releases only. | Use when you need the highest stability and support guarantees. |

When a build is promoted, the script stages artifacts under
`artifacts/release/<channel>/<timestamp>/` and generates a `manifest.json` file
listing every package with its SHA-256 hash.【F:tools/ci/release.ps1†L58-L110】 The
`manifest.json` file is the source of truth for verifying downloads regardless of
how you obtain the media (shared folder, USB stick, or direct download).

## What packages to expect per platform

### Windows desktop/tablet

Packaging for Windows produces three deliverables from a single publish
operation.【F:docs/development/howto/windows-packaging.md†L14-L58】

| Artifact | When to use it |
| --- | --- |
| `AgOpenGPS.Nexus-win-x64.exe` | Self-contained single-file build for portable deployments or quick smoke tests. |
| `AgOpenGPS.Nexus-win-x64.zip` | Manual install/update. Extract to `C:\AgOpenGPS\Nexus` (or another folder you own). |
| `AgOpenGPS.Nexus-win-x64-installer.zip` | Guided install kit with `install.ps1` for elevated installs into `%ProgramFiles%\AgOpenGPS\Nexus`. |

All three bundles ship together in the release folder so operators can pick the
path that matches their IT policies.

### Raspberry Pi / Compute Module 5

The Pi packaging script builds a Debian package named
`nexus-pi_<version>_arm64.deb` (or `armhf` on 32-bit targets).【F:tools/packaging/pi/build-deb.sh†L1-L199】
Installing the package:

- Drops Core and AGiO binaries under `/opt/nexus/`.
- Installs default configuration into `/etc/nexus/` plus `.env` overrides for
  environment variables.
- Registers `nexus-core.service` and `nexus-agio.service` under systemd and
  starts them after install so the services come up automatically on boot.

Keep a copy of the previous `.deb` so you can revert with
`sudo dpkg -i <older-package>.deb` if an update misbehaves.

## Applying an update

### 1. Verify the media

Before installing, confirm that the files you received match the manifest.

**PowerShell (Windows):**

```powershell
Get-Content manifest.json | ConvertFrom-Json | ForEach-Object {
    foreach ($artifact in $_.Artifacts) {
        $hash = (Get-FileHash -Path $artifact.RelativePath -Algorithm SHA256).Hash
        if ($hash -ne $artifact.Sha256) {
            throw "Hash mismatch for $($artifact.Name)"
        }
    }
}
```

**Linux (Pi/CM5):**

```bash
jq -r '.Artifacts[] | "echo " + .Sha256 + "  " + .RelativePath' manifest.json | bash -x
```

The hashes originate from the release script, so a mismatch means the download or
copy is corrupt.【F:tools/ci/release.ps1†L58-L110】

### 2. Windows install/upgrade

1. Decide whether you are using the ZIP or installer bundle.
2. For the ZIP path, stop Nexus, back up the previous folder, and extract the new
   ZIP over `C:\AgOpenGPS\Nexus`. Launch `AgOpenGPS.Nexus.exe` to confirm it
   starts.【F:docs/development/howto/windows-packaging.md†L14-L58】
3. For the installer bundle, extract `AgOpenGPS.Nexus-win-x64-installer.zip`, run
   PowerShell as Administrator, and execute:

   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   .\install.ps1
   ```

   The script copies files into `%ProgramFiles%\AgOpenGPS\Nexus` and refreshes
   Start Menu shortcuts.【F:docs/development/howto/windows-packaging.md†L41-L58】
4. Keep the prior build somewhere safe. Rolling back is as simple as restoring
   the backup folder or rerunning the older installer.

### 3. Raspberry Pi / CM5 install/upgrade

1. Copy the `.deb` and manifest onto the Pi (direct download or removable media).
2. Verify the hash with `sha256sum` as shown above.
3. Install or upgrade:

   ```bash
   sudo dpkg -i nexus-pi_<version>_arm64.deb
   ```

   The package ensures the `nexus` system user exists, reloads systemd, enables
   both services, and restarts them so the update takes effect immediately.【F:tools/packaging/pi/build-deb.sh†L109-L198】
4. To roll back, reinstall the prior `.deb`. Debian keeps configuration files
   under `/etc/nexus/`, so keep notes on any local edits before upgrading.

## Offline-first workflow

Many rigs spend weeks offline, so plan updates around portable media in line with
the offline-first requirements captured in the SRS.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L1-L33】

1. On an Internet-connected machine, download the channel folder you intend to
   deploy (manifest + artifacts).
2. Copy the entire folder to a USB drive or rugged SSD. Label it with the
   channel, version, and date.
3. Before unplugging, run the hash checks above so you know the media is clean.
4. In the cab, stop running services/apps, copy the files locally, verify again,
   and perform the install steps for Windows or Pi.
5. Archive the previous version on the same drive so you can revert if the new
   build fails field validation.

## When connectivity is available

If the machine can briefly connect to the Internet (shop Wi-Fi, cellular hotspot)
you can poll for updates without pulling the full bundle:

1. Fetch the latest `manifest.json` for your chosen channel.
2. Compare the `Version` or `Timestamp` fields against the currently installed
   build. If they differ, download only the packages you need.
3. Apply the update using the steps above. Keep at least one known-good build on
   removable media in case connectivity drops mid-process.

This process preserves the manual ZIP workflow demanded by legacy users while
introducing verifiable manifests and clear channel guidance so staged rollouts
stay predictable.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L1-L33】
