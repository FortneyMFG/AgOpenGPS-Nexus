# Windows quick start: build a map in two minutes (no hardware required)

This guide walks a new operator through launching the Nexus Windows UI, exploring the
placeholder simulation controls, and saving defaults without connecting any hardware.
It assumes you are working from a clean Windows 11 or Windows 10 (21H2+) machine.

## What you will do

1. Prepare a working folder and install the .NET runtime (one-time setup).
2. Launch the Avalonia desktop shell.
3. Inspect the bundled simulation scaffolding so you know what will change once
   playback is wired up.
4. Save the default connection profile for when you graduate to real hardware.

The hands-on portion (steps 2–4) routinely completes in under two minutes once the
runtime is installed.

## Prerequisites

- Windows 10 21H2 or Windows 11 on a 64-bit CPU.
- A user account with permission to install applications.
- [.NET 8 Desktop runtime](https://dotnet.microsoft.com/download) — the Nexus UI is
a .NET 8 Avalonia application.【F:Nexus SourceCode/README.md†L59-L83】
- PowerShell 7+ (preinstalled on current Windows builds).
- ~1 GB of free disk space for the binaries and log/config files.

> **Tip:** If you plan to keep the package on a removable drive for in-field use,
> create a `C:\AgOpenGPS\Nexus` folder now; the instructions below assume that
> location.

## Step 1 — Download or build Nexus (one-time)

Choose whichever path matches how you received Nexus:

- **From a release ZIP:** download the latest `Nexus-Windows-x64.zip`, extract it to
  `C:\AgOpenGPS\Nexus`, and continue to Step 2.
- **From source:** clone this repository, open Windows Terminal, `cd` into the repo,
  and run `pwsh ./tools/scripts/nexus.ps1 run ui` once to restore dependencies.
  Subsequent runs start instantly because the script shells out to `dotnet run` with
  cached packages.【F:tools/scripts/nexus.ps1†L11-L117】【F:Nexus SourceCode/README.md†L69-L83】

Once you have a `Nexus SourceCode` folder (either extracted or cloned) the remaining
steps are identical.

## Step 2 — Launch the Windows UI shell (30 seconds)

1. Open Windows Terminal or PowerShell.
2. Change into the Nexus folder (`cd C:\AgOpenGPS\Nexus`).
3. Start the UI with the helper script: `pwsh ./tools/scripts/nexus.ps1 run ui`.
4. Within a few seconds the Avalonia window opens with a dark map pane on the left
   and setup controls on the right.【F:tools/scripts/nexus.ps1†L11-L117】【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L21-L175】

## Step 3 — Explore the placeholder simulation controls (45 seconds)

The current Avalonia shell seeds the map with a fixed sample pose while the replay
plumbing is under construction. Use the controls to understand what will change when
the simulation loop is connected:

1. Press **Play** in the *Simulation controls* card. The status flips between
   `Playing` and `Paused`, but the map stays on the seeded pose because the
   view-model does not stream updates yet.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/MainWindowViewModel.cs†L62-L122】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L21-L118】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L240-L276】
2. Switch between the 0.5×, 1×, and 2× radio buttons to watch the playback-rate
   label update. The selection only affects UI state until a replay controller is
   registered.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L19-L118】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L277-L320】
3. Drag the scrubber to change the timestamp readout. Because no controller feeds the
   map yet, the tractor icon remains in place even as the position label updates.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L119-L223】
4. Expand the *Stream routing* table to see which simulated routes the shell will wire
   up once playback arrives. This helps you understand the data flow even before live
   telemetry is available.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L89-L123】【F:Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json†L1-L33】

## Step 4 — Save your connection defaults (15 seconds)

Even in simulation mode it helps to prime the AGiO connection settings so Nexus is
ready the first time you plug in a controller:

1. Enter the AGiO endpoint you expect to use in the cab (for example,
   `http://192.168.5.1:5105`).
2. Leave **Backend** set to **Simulation** until NX-061 packaging drops real
   binaries, then flip it to **AgIO Host** when you connect hardware.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L90-L170】【F:Nexus SourceCode/src/Aog.UI.Avalonia/Settings/ConnectionSettings.cs†L11-L77】
3. Click **Save settings**. Nexus persists the file to your profile so the UI comes
   back with the same defaults next time.【F:Nexus SourceCode/README.md†L79-L83】

## Done — You have a working map

You now have:

- A running Avalonia shell seeded with sample layers and a placeholder vehicle pose.
- A deterministic simulation configuration you can inspect before playback wiring lands.
- Saved connection settings for the future hardware handshake.

From here you can experiment with editing the simulation JSON in
`Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json` to practice
longer routes or multi-sensor setups before you step into the cab.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json†L1-L33】

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `dotnet` is not recognized | Install the .NET 8 Desktop runtime and reopen PowerShell so the PATH updates.【F:Nexus SourceCode/README.md†L59-L83】 |
| `Unknown target 'ui'` from the helper script | Double-check you are running `nexus.ps1` from inside the repository so the relative project paths resolve.【F:tools/scripts/nexus.ps1†L11-L117】 |
| The window opens but the map stays still | This is expected until the replay controller is hooked up. The UI seeds a sample pose so you can explore layers and settings before live data is available.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/MainWindowViewModel.cs†L62-L122】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SimulationBarViewModel.cs†L240-L276】 |
| Saved settings disappear between runs | Verify you clicked **Save settings** and that your Windows profile has write permission to `%AppData%/AgOpenGPS/Nexus`. The UI persists the configuration there on Windows.【F:Nexus SourceCode/README.md†L79-L83】 |

## Next steps

- Read the Pi/CM5 quick start (NX-064) to rehearse the headless flow before you
  stage a tractor install.
- Pair this guide with the upcoming packaging tasks (NX-061/NX-062) so operators
  can download a ready-to-run bundle instead of cloning the repository.
