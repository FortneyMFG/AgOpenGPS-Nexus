# Windows Packaging for AgOpenGPS Nexus

Task **NX-061** introduces an automated packaging flow that produces a single-file publish, a
redistributable `.zip`, and a lightweight installer bundle for Windows rigs. This guide explains
how to run the packaging script and validate the generated artifacts.

## Prerequisites

- Windows 10 or newer (x64)
- PowerShell 7 (`pwsh`) or Windows PowerShell 5.1
- .NET SDK 8.0 or newer available on `PATH`

> Tip: set the `DOTNET` environment variable if you need to point the script at a custom `dotnet`
> installation.

## Running the packager

The helper script lives under `tools/ci/package-windows.ps1`. Run it from the repository root with
PowerShell:

```powershell
pwsh ./tools/ci/package-windows.ps1
```

Optional parameters:

| Parameter | Description | Default |
| --- | --- | --- |
| `-Configuration` | Build configuration supplied to `dotnet publish`. | `Release` |
| `-Runtime` | RID to publish (must be a Windows RID). | `win-x64` |
| `-Project` | UI project to package, relative to the repo root. | `Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj` |
| `-OutputRoot` | Destination folder for artifacts. | `artifacts/windows` |

The script performs the following steps:

1. Runs `dotnet publish` with `PublishSingleFile`, self-contained mode, and single-file compression.
2. Copies the generated executable to `artifacts/windows/AgOpenGPS.Nexus-win-x64.exe`.
3. Builds `AgOpenGPS.Nexus-win-x64.zip` containing the executable, license, and usage notes.
4. Assembles `AgOpenGPS.Nexus-win-x64-installer.zip` with `install.ps1` and documentation for an
   elevated install into `%ProgramFiles%\AgOpenGPS\Nexus`.

All intermediate folders are cleaned between runs so the artifacts represent the latest publish.

## Validating the artifacts

1. Extract the `.zip` archive and launch `AgOpenGPS.Nexus.exe`. It should start without requiring
   a local .NET runtime thanks to the self-contained publish.
2. For a kiosk-style install, extract the installer bundle and run `install.ps1` from an elevated
   PowerShell prompt:

   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   .\install.ps1
   ```

   The script copies the binary into `%ProgramFiles%\AgOpenGPS\Nexus` and creates a Start Menu
   shortcut under `AgOpenGPS`.
3. To uninstall, remove the installation directory and delete the Start Menu shortcut noted in
   `INSTALLING.txt`.

Keep a copy of the produced `.zip` and installer bundle as CI artifacts so Windows operators can
load the UI on clean machines (FZ-G1 baseline). Document the artifact location when handing builds
to testers or hardware pilots.
