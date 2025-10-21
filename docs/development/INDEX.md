# Developer Guide

This guide covers development setup, building, testing, and contribution workflows for Nexus. For architecture details, refer to our [System Requirements Specification](../SRS/INDEX.md) and [Architecture Decision Records](../ADR/INDEX.md).

## Development Environment Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022+ or VS Code
- Git
- Docker (optional, for containerized testing)

### Getting Started
1. Clone the repository
```powershell
git clone https://github.com/FortneyMFG/AgOpenGPS-Nexus.git
cd AgOpenGPS-Nexus
```

2. Install dependencies
```powershell
dotnet restore
```

3. Build the solution
```powershell
dotnet build
```

## Project Structure

```text
Nexus SourceCode/
├── src/
│   ├── Aog.Core/        # Core guidance engine
│   ├── Aog.Agio/        # Hardware integration
│   ├── Aog.Plugins/     # Plugin system
│   └── Aog.UI.Avalonia/ # Desktop UI
├── tests/
│   ├── unit/           # Unit tests
│   ├── integration/    # Integration tests
│   └── simulation/     # Simulation tests
└── tools/              # Development tools
```

## Build System

### Local Build
```powershell
# Full solution build
dotnet build

# Build specific project
dotnet build src/Aog.Core

# Build with configuration
dotnet build -c Release
```

### Test Execution
```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/unit/Aog.Core.Tests

# Run with filter
dotnet test --filter "Category=Integration"
```

### Simulation Testing
```powershell
# Run smoke tests
nexus sim smoke

# Run full regression
nexus sim regression

# Run specific scenario
nexus sim scenario guidance-calibration
```

## Development Workflow

1. **Task Selection**
   - Choose task from `tasks.md`
   - Create branch: `feat/NX-###-description`

2. **Development**
   - Follow [coding standards](coding-standards.md)
   - Maintain test coverage
   - Update documentation

3. **Testing**
   - Run unit tests
   - Execute integration tests
   - Verify simulation scenarios

4. **Pull Request**
   - Create PR with NX-### reference
   - Include test results
   - Add documentation updates

## Debugging

### Local Debugging
- VS Code launch configurations provided
- Full source map support
- Hot reload enabled

### Remote Debugging
- SSH debugging support
- Remote symbol loading
- Performance profiling

## Performance Testing

As specified in [ADR-026: Performance Budgets](../SRS/sections/9X_Frontends_Ops/96-ADR-026 - Performance budgets and instrumentation.md):

1. **Profiling**
   - CPU usage monitoring
   - Memory allocation tracking
   - Network latency measurement

2. **Benchmarking**
   - Standard test scenarios
   - Performance regression tests
   - Load testing

## Code Quality

### Static Analysis
- Code style enforcement
- Security scanning
- Dependency auditing

### Testing Requirements
- Unit test coverage: >80%
- Integration test coverage: >60%
- Performance test baseline

## Contribution Process

1. **Setup**
   - Fork repository
   - Configure development environment
   - Review contribution guidelines

2. **Development**
   - Create feature branch
   - Implement changes
   - Add tests and docs

3. **Review**
   - Submit pull request
   - Address feedback
   - Update documentation

4. **Integration**
   - Pass CI checks
   - Merge to develop
   - Monitor deployment

## Related Documentation

- [Architecture Overview](../architecture/INDEX.md)
- [Plugin Development](../plugins/INDEX.md)
- [Testing Guide](testing.md)
- [Contribution Guidelines](contributing.md)