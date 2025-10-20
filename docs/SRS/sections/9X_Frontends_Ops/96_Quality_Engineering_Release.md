# 96 — Quality Engineering & Release (Status: collecting proposals)

## Problem statement
Explain how the project is validated (unit, integration, field), packaged, and shipped to operators, along with desired automation.

## Requirements (from contributors)
- R-CI-000 (MUST, current-AgOpenGPS): Keep existing NUnit-based unit test projects (`AgLibrary.Tests`, `AgOpenGPS.Core.Tests`) building in CI.【F:SourceCode/AgLibrary.Tests/AgLibrary.Tests.csproj†L1-L28】【F:SourceCode/AgOpenGPS.Core.Tests/AgOpenGPS.Core.Tests.csproj†L1-L22】
- R-CI-001 (MUST, current-AgOpenGPS): Preserve the manual publishing workflow that runs `dotnet publish` on the solution for release packaging.【F:README.md†L35-L41】
- R-CI-002 (SHOULD): Add automated linting for SRS IDs, PGN schemas, and documentation as coverage grows.
- R-CI-003 (SHOULD): Provide reproducible build artifacts (signed installers/zips) without breaking today’s manual zip releases.
- R-CI-010 (MUST, proposed-variable-layer): Stand up deterministic replay tests, aggregation math checks, and performance benchmarks covering variable-rate layers before rollout.【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L1-L23】
- R-CI-011 (SHOULD, proposed-variable-layer): Gate new PGNs, UI flows, and persistence behind feature flags with CI validation plus operator documentation for toggles.【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L24-L46】
- R-CI-004 (SHOULD, proposed-LinuxCore): Add Linux (amd64/arm64) CI lanes that build/test the Core service, containers, and PGN bridge alongside Windows artifacts.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- R-CI-005 (COULD): Integrate hardware-in-the-loop smoke tests for steering/rate modules.
- R-CI-012 (SHOULD, release assurance): Define acceptance criteria for cross-platform build artifacts (checksums, signatures, SBOM availability) before they ship to operators.
- R-CI-013 (SHOULD, fixture governance): Document how hardware-in-the-loop rigs and replay fixtures are versioned and synchronized with firmware/controller changes so tests remain trustworthy over time.
- R-CI-020 (MUST, provenance & audit): Capture job/session identifiers, dataset hashes, and quality flags in CI artifacts so analytics outputs and exports remain traceable back to source PoseStreams and controller builds.
- R-CI-021 (SHOULD, derived product QA): Provide repeatable evaluation metrics (banding/clamping checks, target function validation, ROI masking) for prescription/analytics pipelines so agronomic decisions can be reviewed before release.
- R-CI-030 (MUST, golden replay determinism): Maintain golden PoseStream-to-tile replay suites with pass/fail thresholds on spatial/temporal deltas and performance budgets so regression gates catch non-deterministic changes.

## Options
- O-CI-0: Status quo — Manual release pipeline with ad-hoc CI builds.
- O-CI-1: GitHub Actions/ADO pipeline running unit tests + packaging per commit.
- O-CI-2: Nightly integration builds with simulated hardware harnesses.
- O-CI-3: Release train with automated changelog and installer signing.
- O-CI-4: Hosted CI with containerized build agents for Windows/Linux.
- O-CI-5: [Replay-driven CI and rollout for layers](../options/9X/O-TEST-4_LayerReplayCI.md) — Deterministic datasets + feature-flagged deployment.

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
- Replay-driven validation is viewed as mandatory before enabling layer telemetry by default, ensuring field confidence.【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L47-L64】
- Linux packaging and PGN bridge builds must be proven in CI before encouraging field pilots.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/options/4X/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】

## Upcoming ADR coverage
- **ADR-013 Derived products** will define the analytics-to-prescription recipes and QA metrics expected by R-CI-021, ensuring prescriptions remain auditable and repeatable.【F:docs/ADR/ADR-roadmap.md†L67-L73】
- **ADR-019 Provenance, audit, and QA** will formalize the provenance schema, quality scores, and audit trail requirements captured in R-CI-020, aligning storage and visualization expectations.【F:docs/ADR/ADR-roadmap.md†L115-L121】
- **ADR-020 Determinism, replay & CI** will enhance the golden dataset workflows required by R-CI-030, including hashing schemes, fixture formats, and performance gates.【F:docs/ADR/ADR-roadmap.md†L123-L129】

## Open questions
- How do we validate PGN compatibility in automation without physical hardware?
- What’s the minimal artifact set (zip, installer, checksums) we must publish per release?
