# AgOpenGPS.Nexus.Plugin.Cli.Abstractions

`AgOpenGPS.Nexus.Plugin.Cli.Abstractions` provides the contracts required to build
command modules that plug into the unified `nx` CLI host. Plugin teams reference
this package to contribute verbs without re-implementing host plumbing or
service discovery.

## Features

- `ICommandModule` interface used by the host to discover and configure plugin
  verbs.
- `CommandModuleContext` wrapper exposing the root `System.CommandLine`
  structure, dependency injection container, and shared output options.
- `PluginCommandModuleDescriptor` metadata supplied for adapter diagnostics and
  manifest correlation.
- Nullable reference types enabled with XML documentation for IDE assistance.

## Getting started

Install the package from the local artifacts feed or GitHub Packages and
register your command modules in the plugin manifest:

```bash
# From your plugin project
 dotnet add package AgOpenGPS.Nexus.Plugin.Cli.Abstractions --version 0.1.*
```

Implement the `ICommandModule` interface and use the provided
`CommandModuleContext` to attach commands to the CLI host. For guidance on
wiring adapters into plugin manifests, see `docs/plugins/CLIExtensions.md`.

## Local development

During repository development the package is produced into `./artifacts/nuget`.
Run the helper script to pack (and optionally publish) the package:

```powershell
./tools/ci/publish-cli-abstractions.ps1 -NoPush
```

Point your plugin project at the local feed via the included `nuget.config` or
configure `dotnet restore` with `-p:RestoreAdditionalProjectSources` if needed.
