# 12 — Development Language & Runtime

> **In plain terms:** Everyone writing Nexus code installs the same .NET 8 SDK,
> follows the same dependency rules, and uses shared contracts so plugins and
> tools behave the same on Windows and Linux.

*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 12  
**Editors:** Platform Foundations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 11 — OS Support, 14 — Build Environment & Tooling  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 6X — Core Domain Services, 9X — Frontends & Ops

---

## 12.1 Purpose & Scope

Define the managed language, runtime, and dependency policies that keep Nexus Core, AgIO, plugins, and tooling aligned on a coherent stack.
Clarify how runtime governance supports cross-platform deployments and plugin compatibility while enabling deterministic builds.

---

## 12.2 Context

- Legacy code spans .NET Framework, .NET 6, and native helpers, complicating modernization.
- Contributors target .NET 8 LTS to unify runtime behavior across Windows and Linux.
- Shared gRPC contracts (`Aog.Abstractions`) coordinate Core, UI, AgIO, and plugins.
- Build tooling (Section 14) must pin SDK versions, dependencies, and signing assets to guarantee reproducibility.

> **Quick start for newcomers**
>
> 1. Install the .NET 8 SDK listed in `global.json`.
> 2. Clone the repo and run `tools/scripts/nexus.sh bootstrap` (or `nexus.ps1` on Windows) to restore dependencies.
> 3. Run `dotnet build` followed by `dotnet test`; if both succeed, you are ready to contribute.
> 4. Keep the dependency allowlist handy—new packages require a governance review before merge.

---

## 12.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Runtime Mix | .NET Framework WinForms, .NET 6 WPF, native utilities. | Fragmented build chain; divergent APIs. | Standardize on .NET 8 with unified project structure. | Source tree inventory |
| Dependency Governance | Ad-hoc NuGet additions per project. | Unverified Linux compatibility; inconsistent versions. | Curated allowlist with dual-OS CI validation. | Contributor discussions |
| Plugin Contracts | Manual interface definitions; no versioning plan. | Hard to maintain compatibility across releases. | Package shared gRPC/contract libraries with semantic versioning. | Plugin WG backlog |

---

## 12.4 Definitions

| Term | Definition |
|------|-------------|
| Managed Runtime | .NET runtime (CLR/CoreCLR) executing managed assemblies. |
| Contract Package | NuGet bundle exposing shared interfaces/protobuf definitions. |
| Deterministic Build | Repeatable compilation with identical outputs given pinned dependencies. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

## 12.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|-----------------|-----------------------------|
| R-STACK-000 | MUST | Runtime | Standardize on .NET 8 as baseline for Core, UI, AgIO, CLI. | Section 11 dependencies | CI ensures all projects target .NET 8 |
| R-STACK-001 | MUST | Language | Use C# as primary implementation language; expose language-agnostic contracts. | Architecture WG | Contract linting + API docs |
| R-STACK-002 | SHOULD | Dependency | Maintain curated dependency allowlist validated on Windows + Linux builds. | Release governance | Automated dependency diff + dual-OS builds |
| R-STACK-003 | MUST | Build Integrity | Pin toolchain versions via `global.json`, sign assemblies, ensure repeatable restore. | Build policy | Build reproducibility check in CI |
| R-STACK-004 | SHOULD | ABI Governance | Version shared contracts with runtime updates to keep plugins compatible. | Plugin WG | Semantic versioning policy + compatibility tests |
| R-STACK-005 | MUST | Hardware Abstraction | Keep OS-specific device bindings behind DI interfaces to avoid forks. | AgIO maintainers | Integration tests verifying backend swaps |
| R-STACK-006 | SHOULD | Observability | Provide logging, metrics, and tracing primitives consistent across runtime hosts. | Ops feedback | `nexus sim smoke` + telemetry verification |

> **Why it matters:** These guardrails stop surprise runtime drift, help plugin authors know which APIs are safe, and make sure a new contributor can match the CI environment in an afternoon.

### 12.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-STACK-000 | ADR-001 (.NET 8 runtime) | Enables cross-platform parity and long-term support. |
| R-STACK-002 | Release WG notes | Prevent regressions from incompatible dependencies. |
| R-STACK-003 | Supply chain policy | Guard against tampering and build drift. |
| R-STACK-004 | Plugin backlog | Keep third-party integrations stable across releases. |

### 12.5.2 Governance quick reference

| If you need to… | Talk to… | Where it lives |
|-----------------|-----------|----------------|
| Add or upgrade a NuGet package | Release governance lead | Dependency allowlist PR + Section 14 tooling |
| Ship a new plugin contract | Plugin working group | `Aog.Abstractions` package + contract tests |
| Update the runtime SDK version | Platform foundations WG | `global.json` change with rollout checklist |
| Introduce native helpers (C++/Rust) | Core/AgIO maintainers | Section 14 FFI guidance + security review |

---

## 12.6 Acceptance Criteria & Verification

- CI enforces .NET 8 target frameworks and runs on Windows + Linux lanes.
- Dependency diff workflow flags unapproved packages before merge.
- Signed artifacts verified in pipelines before publishing.

### 12.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-STACK-000 | CI integration | `pipelines/dotnet-build.yml` | All projects compile against .NET 8 |
| R-STACK-002 | Automated policy | `tools/dependency-allowlist.json` | No unauthorized packages detected |
| R-STACK-003 | Build audit | `qa/build-repeatability.md` | Hash comparison matches baseline |
| R-STACK-004 | Contract tests | `tests/contracts/versioning/` | All required compatibility tests pass |

---

## 12.7 Constraints

- Align runtime upgrades with .NET LTS cadence; avoid mid-cycle runtime shifts without ADR review.
- Maintain compatibility with plugin SDK requirements; breaking changes require migration guides.
- Keep cross-platform build durations within release pipeline SLAs (< 30 minutes per lane).

### 12.7.1 Non-Functional Requirement Classes

- **Performance:** Startup and runtime overhead of managed services.
- **Reliability:** Deterministic restore/build process; CI gating.
- **Security:** Signed binaries, SBOM publication, vulnerability scanning.
- **Maintainability:** Code style, analyzer enforcement, shared libraries.
- **Portability:** Ensure runtime features available on Windows x64, Linux x86_64, Linux ARM64.

---

## 12.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-12-1 | Legacy .NET Framework components resist upgrade. | Medium | Provide shims; schedule incremental rewrites. | @core |
| RISK-12-2 | Dependency allowlist slows contribution velocity. | Low | Automate approvals with cross-OS smoke builds. | @release |
| ISSUE-12-1 | Define policy for native helper utilities (C++/Rust). | Medium | Document in Section 14; require FFI guidelines. | @platform |
| ISSUE-12-2 | Determine cadence for contract version bumps. | Medium | Align with release calendar; publish roadmap. | @plugins |

---

## 12.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Unified runtime adoption | .NET 8 LTS provides consistent language features and tooling. |
| C2 | Legacy compatibility | Transition strategy for WinForms/WPF and native helpers. |
| C3 | Dependency governance | Allowlist + CI gating to ensure cross-platform compatibility. |
| C4 | Contract versioning | Maintain stable APIs for plugins and remote clients. |
| C5 | Tooling ergonomics | Provide setup scripts to install SDKs and analyzers consistently. |
| C6 | Simulation parity | Ensure runtime supports deterministic sim/replay frameworks. |

### 12.9.1 Assumptions & Preconditions

- [A1] Contributors can install .NET 8 SDK and required workloads on development machines.
- [A2] Build infrastructure supports Windows and Linux agents with identical toolchains.
- [A3] Plugin maintainers participate in compatibility validation.

---

## 12.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents for Section 12. See Section 11 Option 11-O1 for cross-platform runtime baseline. | — |

---

## 12.11 Comparison Matrix

| Attribute / Criteria | Unified .NET 8 Stack (11-O1) | Mixed Runtime Legacy |
|----------------------|------------------------------|---------------------|
| Implementation Effort | Medium | Low |
| Maintainability | High | Low |
| Performance | High | Medium |
| Extensibility | High | Low |
| Risk Level | Medium | Medium |

---

## 12.12 Decision Matrix

Section 12 defers to OS Support §11.12 for quantitative scoring; runtime governance inherits the selected Option 11-O1.
Focus for this section is policy execution (allowlists, signing, tooling).

---

## 12.13 Evaluation & Verification

- Maintain automated build repeatability report per release.
- Validate cross-platform CLI/tooling by running `nexus sim smoke` on Windows and Linux lanes.
- Track dependency vulnerability scan results (SCA) with remediation SLAs.

---

## 12.14 Implementation Policy

- Commit `global.json` updates via governance review; document SDK upgrades.
- Require dependency proposals via pull request template referencing allowlist updates.
- Publish plugin contract version map each release with migration notes.

---

## 12.15 Community Sentiment

- Contributors support moving to .NET 8 to align with Avalonia UI roadmap.
- Plugin authors request clear versioning policy to avoid unexpected breaking changes.
- Build/release team emphasizes reproducibility and secret hygiene across pipelines.

### 12.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Rebaselined runtime governance using standardized template. | #0000 |

---

## 12.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-STACK-000 | 11-O1 | 11-ADR-001 | `pipelines/dotnet-build.yml` | `global.json` |
| R-STACK-002 | — | — | `tools/dependency-allowlist.json` | `packaging/dependency-policy.md` |
| R-STACK-004 | — | — | `tests/contracts/versioning/` | `Nexus SourceCode/src/Aog.Abstractions/` |

---

## 12.17 Conformance

Implementations conform when managed components target .NET 8, dependency allowlists are enforced, and contract versioning policies are followed with documented verification artifacts.

---

## Standards Context

Aligns with **ISO/IEC/IEEE 29148:2018** for requirement traceability and **CNCF Secure Supply Chain** guidelines for build integrity.
