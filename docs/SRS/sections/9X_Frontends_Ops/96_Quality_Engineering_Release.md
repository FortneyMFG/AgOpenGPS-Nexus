# 96 — Quality Engineering & Release
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 96  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-24
**Related Sections:** 21 — System Architecture, 62 — Job Lifecycle, 91 — UI Shell & Layout, 94 — Extensibility & Packaging Updates, 97 — Simulation & Replay
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture, 97 — Simulation & Replay
**Downstream Impacts:** Release cadence, operator confidence, regulatory traceability, CI infrastructure

---

## 96.1 Purpose & Scope

Define the validation, build, and release expectations for Nexus frontends and operational tooling. This section covers unit and integration testing, deterministic replay, cross-platform packaging, provenance, and feature-flagged rollout requirements so operators receive trustworthy builds with auditable lineage.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L129】

---

## 96.2 Context

- Legacy releases rely on manual `dotnet publish` steps and ad-hoc zip packaging.  
- Deterministic replay datasets from §97 and feature flags must govern metadata-heavy layer rollouts.
- Linux Core pilots require multi-architecture CI coverage and packaging parity with Windows installers.  
- Provenance records and SBOMs become essential for regulatory and agronomic traceability.

---

## 96.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| CI Coverage | Unit tests run locally; limited automated pipelines. | Manual errors and regressions slip through. | Automated pipelines with deterministic replay and feature flags. | Legacy scripts; replay roadmap【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L129】 |
| Packaging | Manual zip distribution. | No signing, SBOM, or reproducibility. | Automated packaging with checksums and signatures. | README publish flow【F:README.md†L35-L41】 |
| Provenance | Minimal artifact metadata. | Hard to trace analytics results to datasets/builds. | Capture job/session IDs, dataset hashes, and quality flags in artifacts. | ADR-019 provenance plan |

---

## 96.4 Definitions

| Term | Definition |
|------|-------------|
| Golden Replay | Canonical PoseStream-to-layer dataset with expected outputs and thresholds used for regression. |
| Feature Flag | Toggle gating new PGNs/UI flows/persistence to limit blast radius during rollout. |
| SBOM | Software Bill of Materials enumerating components included in released artifacts. |
| Release Train | Scheduled cadence with automated changelog, signing, and distribution workflows. |

---

> **Requirement Grammar (RFC-2119):**  
> - **MUST / MUST NOT** = mandatory; verification required.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.

## 96.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-CI-000 | MUST | Unit Testing | Maintain NUnit-based unit test projects in CI. | Legacy test projects【F:SourceCode/AgLibrary.Tests/AgLibrary.Tests.csproj†L1-L28】【F:SourceCode/AgOpenGPS.Core.Tests/AgOpenGPS.Core.Tests.csproj†L1-L22】 | CI run results |
| R-CI-001 | MUST | Manual Release Continuity | Preserve manual `dotnet publish` workflow while modernization proceeds. | README publish flow【F:README.md†L35-L41】 | Release checklist |
| R-CI-002 | SHOULD | Documentation Linting | Add automated linting for SRS IDs, PGN schemas, and documentation. | Docs tooling backlog | Linting CI job |
| R-CI-003 | SHOULD | Reproducible Artifacts | Provide signed installers/zips without breaking manual releases. | Release governance | Signing pipeline tests |
| R-CI-004 | SHOULD | Linux CI Coverage | Add Linux amd64/arm64 CI lanes building/testing Core, containers, PGN bridge. | Linux Core ADR【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L44】 | Multi-arch CI status |
| R-CI-005 | COULD | Hardware Smoke Tests | Integrate hardware-in-loop smoke tests for steering/rate modules. | Hardware QA backlog | HIL prototype results |
| R-CI-010 | MUST | Deterministic Replay | Stand up deterministic replay tests, aggregation math checks, performance benchmarks for variable-rate layers. | PoseStream roadmap【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L123-L129】 | Replay CI suite |
| R-CI-011 | SHOULD | Feature-flag Governance | Gate new PGNs, UI flows, persistence behind feature flags with CI validation and operator docs. | PoseStream roadmap【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L115-L129】 | Feature flag audit |
| R-CI-012 | SHOULD | Release Assurance | Define acceptance criteria (checksums, signatures, SBOM) before shipping artifacts. | Release governance | Release validation checklist |
| R-CI-013 | SHOULD | Fixture Governance | Document versioning for HIL rigs and replay fixtures. | QA backlog | Fixture registry documentation |
| R-CI-020 | MUST | Provenance Capture | Capture job/session IDs, dataset hashes, quality flags in CI artifacts. | ADR-019 provenance | Provenance metadata tests |
| R-CI-021 | SHOULD | Derived Product QA | Provide repeatable metrics for prescriptions/analytics (banding, ROI). | ADR-013 derived products plan | Analytics QA suite |
| R-CI-030 | MUST | Golden Replay Determinism | Maintain golden replay suites with thresholds on spatial/temporal deltas and performance budgets. | ADR-020 determinism plan | Golden replay CI gating |

### 96.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-CI-000 | Legacy NUnit suites | Preserve regression coverage while modernizing. |
| R-CI-004 | Linux Core pilots | Validate multi-architecture deployments before field trials. |
| R-CI-010 | Replay-driven option | Protect agronomic math and performance before enabling layers by default. |
| R-CI-020 | ADR-019 provenance | Ensure analytics outputs remain traceable. |
| R-CI-030 | ADR-020 determinism | Catch non-deterministic regressions before release. |

---

## 96.6 Acceptance Criteria & Verification

Quality and release workflows succeed when deterministic replay suites pass, packaging artifacts meet signing/SBOM requirements, and provenance metadata accompanies every build. CI must provide actionable feedback for Windows and Linux pipelines.

### 96.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-CI-000 | CI pipeline | `github.com/.../ci.yml` (placeholder) | All unit tests pass per commit |
| R-CI-004 | Multi-arch build | `tests/ci/LinuxCore.yml` | Linux amd64/arm64 builds succeed without regression |
| R-CI-010 | Replay CI suite | `tests/replay/VariableLayer.feature` | Replay outputs match expected metrics within tolerance |
| R-CI-020 | Provenance validation | `tests/ci/ProvenanceMetadata.feature` | Artifacts include job/session IDs, dataset hashes |
| R-CI-030 | Golden replay gating | `tests/replay/GoldenSuite` | Spatial/temporal deltas ≤ configured threshold |

---

## 96.7 Constraints

- Legacy manual release workflows must remain available until automated pipelines earn operator trust.  
- Replay datasets require storage, curation, and security practices to protect sensitive agronomic data.  
- Hardware-in-loop tests depend on lab availability and cannot block emergency hotfixes.

---

## 96.8 Interfaces & Dependencies

- Consumes plugin manifest governance (§94) for feature flag coordination.  
- Shares audit and provenance requirements with §95 Security & Permissions.  
- Coordinates with §91 UI Shell for release packaging and operator documentation.

---

## 96.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Replay-driven CI | Curate deterministic datasets, math checks, and feature flag gating before variable-layer rollout. |
| C2 | Multi-arch pipelines | Maintain Windows and Linux build agents with consistent tooling and caching strategy. |
| C3 | Release assurance | Automate checksums, signatures, SBOM, and provenance capture for every artifact. |
| C4 | Fixture governance | Version hardware rigs, replay data, and analytics metrics to keep QA reproducible. |
| C5 | Operator packaging | Continue producing simple zip packages alongside installers to support legacy workflows. |

### 96.9.1 Assumptions & Preconditions

- [A1] Replay datasets are curated and refreshed per release cycle.  
- [A2] Build infrastructure can access signing keys and secure storage for SBOMs.  
- [A3] Operators remain informed about feature flags and toggles via §91 documentation.

---

## 96.10 Option Overview

No competing release strategies are under review; modernization follows considerations C1–C5 while preserving manual workflows as a fallback.

---

## 96.11 Comparison Matrix

| Attribute / Criteria | Manual Pipeline | Automated Replay-driven Pipeline |
|----------------------|-----------------|----------------------------------|
| Coverage | Low | High — deterministic replay + multi-arch CI |
| Release Speed | Medium | High — automated packaging and checks |
| Reliability | Low | High — gating on replay and provenance |
| Operator Trust | Medium | High — signed artifacts + documentation |
| Maintenance Effort | Low | Medium — requires infrastructure investment |

---

## 96.12 Decision Matrix

> **Informative:** Weighted scoring pending ADR-020 completion; current direction follows replay-driven CI and provenance considerations.

---

## 96.13 Evaluation & Verification

- Run nightly replay suites covering high-risk datasets (variable rate, telemetry, PGN bridging).  
- Verify release artifacts (zip, installer) include checksums, signatures, SBOM, and provenance manifest.  
- Conduct post-release audits comparing operator feedback with CI metrics to refine thresholds.

**Acceptance Criteria**

- All **MUST** requirements verified through CI logs and release checklists.  
- Replay gating prevents regressions prior to feature flag activation.  
- Provenance and audit metadata accompany every release.

---

## 96.14 Implementation Policy

- Store replay datasets and fixture metadata in `artifacts/replay/` with versioned manifests.  
- Publish release notes documenting feature flags, replay coverage, and provenance summary in `docs/releases/<version>.md`.  
- Maintain SBOM and checksum outputs in `artifacts/releases/<version>/` consumed by UI/CLI surfaces.

---

## 96.15 Community Sentiment

- Operators expect simple packaging but support investment in deterministic CI before enabling new telemetry defaults.  
- Contributors emphasize Linux CI readiness and PGN compatibility prior to field pilots.  
- QA teams insist on traceable provenance and replay coverage to defend analytics decisions.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L123-L129】

### 96.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted to new SRS template; integrated replay-driven considerations into design table. | #0000 |

---

## 96.16 Traceability

| Requirement ID | Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|----------------|--------|-----------------------|--------------------------|
| R-CI-000 | C2 | — | CI pipeline logs | `tests/ci` directory |
| R-CI-004 | C2 | 21-O6 | `tests/ci/LinuxCore.yml` | Linux build scripts |
| R-CI-010 | C1, C4 | Replay roadmap | `tests/replay/VariableLayer.feature` | Replay dataset repository |
| R-CI-020 | C3 | ADR-019 | `tests/ci/ProvenanceMetadata.feature` | Provenance manifest generator |
| R-CI-030 | C1, C4 | ADR-020 | `tests/replay/GoldenSuite` | Golden replay harness |

---

## 96.17 Conformance

Implementations conform when replay-driven CI suites gate releases, provenance metadata accompanies artifacts, multi-arch pipelines remain green, and manual packaging continues to function for operators.

---

## Standards Context

Aligns with ISO/IEC/IEEE 29119 for software testing processes and NIST SP 800-204A guidance on SBOM and supply chain integrity, applied to Nexus agricultural deployments.
