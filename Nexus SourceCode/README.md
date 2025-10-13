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
