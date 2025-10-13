# Nexus Source Code

## Core Host (NX-010)

The Nexus Core host is a generic-host based console application that bootstraps dependency
injection, configuration, and structured logging for the headless guidance engine.

### Running the host

```bash
dotnet run --project "src/Aog.Core.Host/Aog.Core.Host.csproj"
```

The host reads configuration from `appsettings.json` and environment variables prefixed with
`NEXUS_`. A background health service emits periodic heartbeat logs (`Core host heartbeat OK.`)
that higher-level orchestration or smoke tests can watch for successful startup/shutdown.
Changes to `CoreHost:Health:IntervalSeconds` are applied without restarting the process, and the
service logs whenever the heartbeat cadence is updated so operators can confirm the new interval.

When the host starts it performs a capabilities handshake with the configured AGiO endpoint.
The handshake endpoint and advertised capabilities can be customised under the
`CoreHost:Agio` and `CoreHost:Capabilities` sections. The following excerpt demonstrates the
available settings:

```json
{
  "CoreHost": {
    "Agio": {
      "Endpoint": "https://localhost:5105"
    },
    "Capabilities": {
      "NodeId": "core-host",
      "SessionPrefix": "core-",
      "AdvertisedCapabilities": [
        "nav.pose",
        "nav.imu"
      ]
    }
  }
}
```

### Configuration

`CoreHost:Health:IntervalSeconds` controls how frequently the heartbeat message is emitted. The
value must be greater than zero; validation runs when the host starts.
# AgOpenGPS Nexus Source Code

This directory contains the modern Nexus solution for AgOpenGPS. The initial milestone delivers the cross-platform Avalonia UI bootstrap described in task NX-040.

## Projects

- `AgOpenGPS.Nexus.sln` — solution file that groups the UI and accompanying tests.
- `src/Aog.UI.Avalonia` — Avalonia desktop application providing the shell window and dependency injection bootstrap.
- `src/Aog.Plugins` — Shared manifest loader and metadata contracts for managed plugins.
- `tests/Aog.UI.Avalonia.Tests` — unit tests covering the DI registration helpers for the UI shell.
- `tests/Aog.Plugins.Tests` — tests validating the plugin manifest loader and schema expectations.

## Prerequisites

- .NET SDK 8.0 or newer

Install the .NET SDK from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) if it is not already available on your machine.

## Build and run

Restore dependencies, build the solution, and launch the Avalonia UI shell:

```bash
dotnet restore AgOpenGPS.Nexus.sln
dotnet build AgOpenGPS.Nexus.sln
dotnet run --project src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj
```

The shell displays the current operating system description and exposes a connection settings panel for configuring AgIO. Dependency injection wires the application, main window, and view-model using `Microsoft.Extensions.Hosting`.

### Connection settings panel

- Choose the AGiO endpoint URI, backend target, and GPS source policy from the shell.
- Settings are persisted to `%AppData%/AgOpenGPS/Nexus/connection-settings.json` on Windows and `$XDG_CONFIG_HOME/AgOpenGPS/Nexus/connection-settings.json` (or `~/.config/AgOpenGPS/Nexus/connection-settings.json`) on Linux.
- The **Save settings** button activates when changes are detected and the endpoint field is populated.

### UI theming, layout, and telemetry consent

- Switch between light and dark themes using the theme selector in the main window header. Theme choices persist via `%AppData%/AgOpenGPS/Nexus/ui-preferences.json` so the shell reopens with the same look and feel.
- Window size, position, and state are captured on shutdown and restored on the next launch so multi-monitor layouts stay intact between sessions.
- A privacy card in the right-hand column exposes crash telemetry controls. Operators can opt in to sharing crash diagnostics, review pending reports, upload them immediately, or clear the queue entirely.

An embedded simulation sample is parsed at startup and the window prints the ordered provider graph so contributors can verify the new configuration loader logic without additional tooling. The shell also exposes a simulation control bar (NX-043) that provides play/pause, scrub, and playback rate controls alongside combo boxes for selecting the source and mode for each routed stream.

Replay controls are wired to an `IReplayController` service, enabling real telemetry sessions to drive the bar. The default dependency injection registration supplies a no-op controller for design-time usage, while the new `TelemetryReplayController` can be registered to stream Parquet logs with play, pause, seek, and playback rate support.
### Scenario editor

- Launch the **Edit scenarios...** dialog from the Simulation controls panel to review available simulation presets.
- Choose provider routes and override seed/time scale options before applying them to the running simulation.
- Save the active scenario to JSON or load an existing scenario file. Imported scenarios are validated against the simulation schema before being applied.

## Testing

Execute the unit tests with:

```bash
dotnet test AgOpenGPS.Nexus.sln
```

The tests validate that the shell registration helper adds the Avalonia application types to the service collection exactly once.
