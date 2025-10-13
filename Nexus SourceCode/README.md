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
- `tests/Aog.UI.Avalonia.Tests` — unit tests covering the DI registration helpers for the UI shell.

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

The bootstrap window displays the current operating system description to confirm cross-platform execution. Dependency injection wires the application, main window, and view-model using `Microsoft.Extensions.Hosting`.

## Testing

Execute the unit tests with:

```bash
dotnet test AgOpenGPS.Nexus.sln
```

The tests validate that the shell registration helper adds the Avalonia application types to the service collection exactly once.
