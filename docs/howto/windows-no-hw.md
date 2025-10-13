# Windows quick start: build a map in two minutes (no hardware required)

This guide walks a new operator through launching the Nexus Windows UI, playing the
embedded simulation, and watching a map come alive without connecting any hardware.
It assumes you are working from a clean Windows 11 or Windows 10 (21H2+) machine.

## What you will do

1. Prepare a working folder and install the .NET runtime (one-time setup).
2. Launch the Avalonia desktop shell.
3. Start the bundled simulation and watch the map update in real time.
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

## Step 3 — Play the embedded simulation (45 seconds)

The shell ships with a deterministic bicycle-model scenario and synthetic IMU stream.
Use the built-in controls to start the run:

1. Press **Play** in the *Simulation controls* card. The status changes to
   `Playing`, and the highlighted vehicle dot begins moving on the map.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L37-L111】【F:Nexus SourceCode/src/Aog.UI.Avalonia/Controls/MapView.cs†L11-L168】
2. Use the radio buttons to switch between 0.5×, 1×, or 2× playback rates if you
   want to speed through the lap.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L54-L76】
3. Drag the scrubber to jump forward or backward in the run. The pose and heading
   update instantly because the simulation graph is deterministic.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L77-L88】【F:Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json†L1-L33】
4. Expand the *Stream routing* table to confirm that both the vehicle pose and IMU
   streams are sourced from the simulation provider graph bundled with the app.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L89-L123】【F:Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json†L1-L33】

As the timeline advances the map view recenters on the simulated tractor, giving
new users an immediate sense of orientation controls before real GNSS data is
available.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Controls/MapView.cs†L27-L168】

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

- A running Avalonia shell that renders vehicle pose updates on the map.
- A deterministic simulation graph you can replay at will.
- Saved connection settings for the future hardware handshake.

From here you can experiment with editing the simulation JSON in
`Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json` to practice
longer routes or multi-sensor setups before you step into the cab.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Resources/SimulationSample.json†L1-L33】

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `dotnet` is not recognized | Install the .NET 8 Desktop runtime and reopen PowerShell so the PATH updates.【F:Nexus SourceCode/README.md†L59-L83】 |
| `Unknown target 'ui'` from the helper script | Double-check you are running `nexus.ps1` from inside the repository so the relative project paths resolve.【F:tools/scripts/nexus.ps1†L11-L117】 |
| The window opens but the map stays still | Ensure the simulation is playing (status shows `Playing`) and the playback rate is not paused. You can also drag the scrubber to force an update.【F:Nexus SourceCode/src/Aog.UI.Avalonia/MainWindow.axaml†L47-L111】 |
| Saved settings disappear between runs | Verify you clicked **Save settings** and that your Windows profile has write permission to `%AppData%/AgOpenGPS/Nexus`. The UI persists the configuration there on Windows.【F:Nexus SourceCode/README.md†L79-L83】 |

## Next steps

- Read the Pi/CM5 quick start (NX-064) to rehearse the headless flow before you
  stage a tractor install.
- Pair this guide with the upcoming packaging tasks (NX-061/NX-062) so operators
  can download a ready-to-run bundle instead of cloning the repository.
