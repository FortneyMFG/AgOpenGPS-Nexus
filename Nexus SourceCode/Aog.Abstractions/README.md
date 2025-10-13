# AgOpenGPS.Aog.Abstractions

This package exposes the protobuf and gRPC contract surface shared by AgOpenGPS Nexus
components.  It contains the generated .NET types for the contracts together with the
original `.proto` files so that other languages can generate bindings when required.

## Package contents

- `AgOpenGPS.Aog.Abstractions` assembly with nullable-enabled generated types.
- Embedded XML documentation for IntelliSense support.
- `proto/core.proto` shipped inside the package for cross-language consumption.

## Versioning strategy

Package versions follow `0.1.x` while the API is in preview.  Continuous integration
builds append a `ci.<run>.<sha>` suffix so downstream consumers can opt into the latest
artifacts using the floating range `0.1.*`.  When the contracts stabilize, tags of the
form `v0.1.x` will produce stable NuGet releases.

## Local development

To work on the contracts locally while consuming them as a package:

```powershell
# From the repository root
./tools/ci/publish-contracts.ps1 -NoPush

# Restore/build using the packaged contracts
dotnet build "Nexus SourceCode/AgOpenGPS.Nexus.sln" -p:UseLocalAogAbstractions=false
```

The script packs the project into `./artifacts/nuget` and updates the local NuGet feed
configured in `nuget.config` so that consuming projects can restore without reaching
an external registry. Pass `-p:UseLocalAogAbstractions=true` during restore/build when
you explicitly want to reference the source project instead of the package.
