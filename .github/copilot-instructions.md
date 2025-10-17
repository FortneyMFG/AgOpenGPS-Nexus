# AgOpenGPS-Nexus AI Agent Instructions

## Project Overview
AgOpenGPS-Nexus is a cross-platform precision agriculture guidance system built on .NET 8. The codebase is organized into several key components:

- Core guidance engine (`/Nexus SourceCode/src/Aog.Core/`)
- Hardware integration layer (`/Nexus SourceCode/src/Aog.Agio/`)
- Plugin system (`/Nexus SourceCode/src/Aog.Plugins/`)
- Desktop UI (`/Nexus SourceCode/src/Aog.UI.Avalonia/`)

## Essential Patterns

### Contract-First Development
1. Proto contracts (`/Nexus SourceCode/proto/`) define all inter-service APIs
2. Generated code lives in `/Nexus SourceCode/src/Aog.Abstractions/`
3. Schema definitions for data models in `/tools/schemas/`
4. Hardware-specific code must be isolated in AgIO backends

### Architecture Guidelines
- Services communicate via gRPC/protobuf contracts through `Aog.Abstractions`
- Platform-specific code belongs only in AgIO backends
- Plugin APIs follow strict versioning and compatibility rules
- Simulation code must be deterministic and seed-locked

### Development Workflow
1. Changes must reference an NX-### task from `tasks.md`
2. Use feature branches: `feat/NX-###-description`
3. Keep PRs focused (<200 LOC excluding tests)
4. Include tests and documentation
5. Validate changes on both Windows x64 and Linux arm64

### Build & Test
```powershell
# Build entire solution
dotnet build

# Run unit tests
dotnet test

# Run smoke tests
nexus sim smoke
```

### Critical Files
- `Nexus SourceCode/AgOpenGPS.Nexus.sln` - Main solution
- `Directory.Build.props` - Global build properties
- `tasks.md` - Task tracking and human QA signoffs
- `bundles/*.bundle.json` - Component bundling configs

## Common Pitfalls
1. Don't modify contracts during freeze periods without ADR approval
2. Ensure hardware-specific code stays in AgIO backends
3. Test changes on both Windows and Linux targets
4. Include regression vectors for simulation code
5. Update documentation when modifying plugin APIs

## Project-Specific Conventions
- Task IDs follow NX-### format
- ADRs required for breaking contract changes
- Proto packages follow `Aog.*` namespace
- Plugin configuration requires validating samples
- Human QA required for hardware-touching changes

For detailed architecture decisions, refer to `/docs/ADR/` directory.