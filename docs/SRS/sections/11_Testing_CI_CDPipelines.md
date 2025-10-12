# Testing, CI & CD Pipelines (Status: collecting proposals)

## Problem statement
Explain how the project is validated (unit, integration, field), packaged, and shipped to operators, along with desired automation.

## Requirements (from contributors)
- R-CI-000 (MUST, current-AgOpenGPS): Keep existing NUnit-based unit test projects (`AgLibrary.Tests`, `AgOpenGPS.Core.Tests`) building in CI.【F:SourceCode/AgLibrary.Tests/AgLibrary.Tests.csproj†L1-L28】【F:SourceCode/AgOpenGPS.Core.Tests/AgOpenGPS.Core.Tests.csproj†L1-L22】
- R-CI-001 (MUST, current-AgOpenGPS): Preserve the manual publishing workflow that runs `dotnet publish` on the solution for release packaging.【F:README.md†L35-L41】
- R-CI-002 (SHOULD): Add automated linting for SRS IDs, PGN schemas, and documentation as coverage grows.
- R-CI-003 (SHOULD): Provide reproducible build artifacts (signed installers/zips) without breaking today’s manual zip releases.
- R-CI-010 (MUST, proposed-variable-layer): Stand up deterministic replay tests, aggregation math checks, and performance benchmarks covering variable-rate layers before rollout.【F:docs/SRS/options/O-TEST-4_LayerReplayCI.md†L1-L23】
- R-CI-011 (SHOULD, proposed-variable-layer): Gate new PGNs, UI flows, and persistence behind feature flags with CI validation plus operator documentation for toggles.【F:docs/SRS/options/O-TEST-4_LayerReplayCI.md†L24-L46】
- R-CI-004 (SHOULD, proposed-LinuxCore): Add Linux (amd64/arm64) CI lanes that build/test the Core service, containers, and PGN bridge alongside Windows artifacts.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- R-CI-005 (COULD): Integrate hardware-in-the-loop smoke tests for steering/rate modules.
- R-CI-012 (SHOULD, release assurance): Define acceptance criteria for cross-platform build artifacts (checksums, signatures, SBOM availability) before they ship to operators.
- R-CI-013 (SHOULD, fixture governance): Document how hardware-in-the-loop rigs and replay fixtures are versioned and synchronized with firmware/controller changes so tests remain trustworthy over time.

## Options
- O-CI-0: Status quo — Manual release pipeline with ad-hoc CI builds.
- O-CI-1: GitHub Actions/ADO pipeline running unit tests + packaging per commit.
- O-CI-2: Nightly integration builds with simulated hardware harnesses.
- O-CI-3: Release train with automated changelog and installer signing.
- O-CI-4: Hosted CI with containerized build agents for Windows/Linux.
- O-CI-5: [Replay-driven CI and rollout for layers](../options/O-TEST-4_LayerReplayCI.md) — Deterministic datasets + feature-flagged deployment.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-CI-0 | Minimal effort | Manual errors, slower cadence | Missed regressions | Current script snippets |
| O-CI-1 | Fast feedback | Needs Windows build runners | Secrets/signing management | dotnet publish flow |
| O-CI-2 | Catches integration issues | Requires simulators | Maintenance overhead | ModSim project |
| O-CI-3 | Predictable releases | More governance | Slower hotfixes if rigid | Release notes process |
| O-CI-4 | Consistent envs | Hosting cost | Complex to debug hardware needs | Container builds |
| O-CI-5 | Protects agronomic math + perf with reproducible datasets | Curating/hosting replays adds overhead | Risk of stale fixtures missing field edge cases | AgDiag replay tooling + feature-flag plan |

## Evaluation criteria
Coverage, release reliability, effort to maintain, reproducibility, compatibility with manual workflows.

## Current sentiment
- Keep basic tests running but invest in CI pipelines that can still emit the simple zip packages operators expect.
- Replay-driven validation is viewed as mandatory before enabling layer telemetry by default, ensuring field confidence.【F:docs/SRS/options/O-TEST-4_LayerReplayCI.md†L47-L64】
- Linux packaging and PGN bridge builds must be proven in CI before encouraging field pilots.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】

## Open questions
- How do we validate PGN compatibility in automation without physical hardware?
- What’s the minimal artifact set (zip, installer, checksums) we must publish per release?
