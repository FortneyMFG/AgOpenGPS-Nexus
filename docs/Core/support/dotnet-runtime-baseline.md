# .NET 10 Runtime Baseline Playbook

Nexus standardises on .NET 10 across Core, AgIO, plugins, tooling, and the Avalonia UI per [12-ADR-001](../development/SRS/sections/1X_Platform_Foundations/12-ADR-001 - Adopt .NET 10 LTS Runtime.md). This playbook records the enforcement knobs that keep the baseline in place and how to validate them during reviews and CI.

## Repository Guardrails

1. **SDK pinning.** `global.json` locks contributors to the .NET 10 SDK family and rolls forward only within that feature band so we can coordinate upgrades. A blocked restore is an actionable signal that the local SDK needs to match the published baseline.
2. **Shared build props.** `Nexus SourceCode/Directory.Build.props` sets `TargetFramework` to `net10.0` for every project and enables deterministic builds. Individual projects may multi-target (for example, Windows-specific assets) but cannot drop the .NET 10 target.
3. **Solution auditing.** Both `AgOpenGPS.Nexus.sln` and `Nexus.sln` load only projects that inherit the shared props. Adding a project that bypasses the props file is treated as a policy violation.

## CI Verification

- **dotnet --info drift check.** CI agents log the active SDK during each build. The release checklist requires comparing the reported version to the baseline recorded in `global.json` before cutting a release tag.
- **TargetFramework sweeps.** The `dotnet build` and `dotnet test` steps run with warnings-as-errors to surface any project that tries to downgrade the target framework. Repository maintainers can also run `dotnet msbuild -t:CollectFrameworkInfo` locally when large refactors land.
- **Template validation.** Tooling projects regenerate package templates during nightly builds. The artifacts list shows the `TargetFramework` metadata for each template so changes can be audited without unpacking NuGet packages.

## Review Checklist

- [ ] New projects or templates import `Directory.Build.props` or explicitly set `TargetFramework` to `net10.0`.
- [ ] Dependencies resolve against packages compiled for .NET 10 (or multi-target packages that include `net10.0`).
- [ ] CI logs confirm the SDK version pinned in `global.json`.
- [ ] Release notes mention any planned SDK roll-forward with impact analysis and a mitigation plan.

Maintaining these guardrails ensures we stay aligned with the cross-platform deployment story outlined in ADR-001 while catching drift early in development cycles.
