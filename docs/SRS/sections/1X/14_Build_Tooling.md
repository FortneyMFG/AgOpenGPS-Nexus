# 14 — Build Environment & Tooling (Status: collecting proposals)

## Problem statement
Document the toolchains, automation, and signing requirements that keep the Nexus stack reproducible across developer machines, CI agents, and release pipelines.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L1-L147】

## Requirements (from contributors)
- R-BUILD-000 (MUST, reproducibility): Pin .NET SDK versions via `global.json`, track native dependencies with checksums, and publish a repeatable restore manifest so contributors and CI obtain identical toolchains.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L19-L74】
- R-BUILD-001 (MUST, signing): Sign desktop installers, NuGet packages, and plugin bundles with project-maintained certificates before public release; verify signatures during CI to catch tampering.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L76-L147】
- R-BUILD-002 (SHOULD, container support): Provide Dockerfiles and cached base images for headless builds and test automation so Linux CI lanes match field deployments.【F:docs/SRS/sections/1X/11_OS_Support.md†L14-L45】
- R-BUILD-003 (SHOULD, developer ergonomics): Automate environment bootstrapping through `nexus setup` or PowerShell equivalents that install required SDKs, emulators, and git hooks without manual steps.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L109-L147】
- R-BUILD-004 (MUST, secrets handling): Keep signing keys, store credentials, and service endpoints in centrally managed vaults with short-lived tokens consumed by the build pipeline; never commit secrets to repos.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L94-L147】
- R-BUILD-005 (SHOULD, cross-platform validation): Run smoke builds on Windows and Linux for every PR to guarantee Avalonia, AgIO, and plugin SDK binaries remain cross-platform.【F:docs/SRS/sections/1X/11_OS_Support.md†L30-L45】

## Tooling landscape
- `dotnet build/test` for all managed components.
- `nexus sim smoke` for deterministic simulation health checks.
- `git-version` and release pipelines to stamp semantic versions and changelogs.
- Signing pipeline (Windows code signing, Authenticode, and plugin bundle signatures).

## Current sentiment
Investing in a unified build toolchain is essential before enabling broader community plugin contributions or Linux-first deployments; gaps in signing or dependency pinning are currently the largest blockers for public beta builds.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L76-L147】【F:docs/SRS/sections/9X/94_Extensibility_Packaging_Updates.md†L121-L211】
