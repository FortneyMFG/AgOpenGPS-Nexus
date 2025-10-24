# 14 — Build Environment & Tooling

> **In plain terms:** Anyone can spin up the Nexus build in minutes, the CI
> machines use the exact same scripts, and secrets stay locked away so signed
> installers and packages are always trustworthy.

*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 14  
**Editors:** Release & Tooling Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 11 — OS Support, 12 — Development Language & Runtime, 96 — Quality Engineering & Release
**Related Decisions:** `14-ADR-001 — Standardize Build Environment & Tooling`, `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `11-ADR-001 — Establish Windows & Linux Support Baseline`
**Upstream Dependencies:** 12 — Runtime Governance, 96 — QE Policies  
**Downstream Impacts:** Release pipelines, developer onboarding

---

## 14.1 Purpose & Scope

Document the build toolchains, automation, signing, and secrets policies that ensure Nexus artifacts are reproducible and secure across Windows and Linux environments.

---

## 14.2 Context

- Windows installers and Linux packages must be produced from consistent pipelines (`11-ADR-001`).
- Containerized builds support simulation, testing, and remote deployments.
- Secrets (signing keys, credentials) must be centrally managed.
- Developers need bootstrap tooling to align with CI environments.

> **Quick start for new developers**
>
> 1. Clone the repo and run `tools/scripts/nexus.sh bootstrap` (Linux/macOS) or `tools/scripts/nexus.ps1 bootstrap` (Windows).
> 2. Execute `dotnet build` and `dotnet test` to confirm the toolchain matches CI.
> 3. Use `tools/scripts/nexus.sh run --help` to explore common workflows without memorizing project paths.
> 4. Never copy signing keys locally—CI retrieves them just-in-time from the vault.

---

## 14.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Toolchain Pinning | Manual tracking of SDK versions per developer. | Build drift across contributors and CI. | `global.json` + scripted environment setup. | Release retrospective |
| Signing | Ad-hoc signing for installers and packages. | Risk of unsigned builds shipping to users. | Automated signing pipeline with stored certificates. | QE policy draft |
| Containerization | Limited Dockerfiles for experimental Linux builds. | Inconsistent headless deployment parity. | Standardize container images for CI + field deployments. | Linux Core plan |

---

## 14.4 Definitions

| Term | Definition |
|------|-------------|
| Build Manifest | Artifact describing versions, dependencies, and checksums for a release. |
| Secure Build Vault | Managed secrets store providing ephemeral credentials for pipelines. |
| Smoke Build | Minimal build/test execution verifying pipeline health. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** ensures baseline conformance.
> - **SHOULD / SHOULD NOT** indicates strong recommendations requiring justification.
> - **MAY** denotes optional enhancements.

## 14.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|-----------------|-----------------------------|
| R-BUILD-000 | MUST | Reproducibility | Pin .NET SDK versions, native dependencies, and publish repeatable restore manifest (`14-ADR-001`). | QE policy | Build hash comparison |
| R-BUILD-001 | MUST | Signing | Sign installers, NuGet packages, plugin bundles with project certificates; verify signatures in CI. | Release governance | Signing verification step |
| R-BUILD-002 | SHOULD | Container Support | Provide Dockerfiles/base images for headless builds matching field deployments. | Linux Core roadmap | Container smoke build |
| R-BUILD-003 | SHOULD | Developer Ergonomics | Automate environment bootstrap via scripts/CLI. | Developer onboarding | `nexus setup` success rate |
| R-BUILD-004 | MUST | Secrets Handling | Store signing keys and credentials in managed vault with short-lived tokens. | Security policy | Secrets audit log |
| R-BUILD-005 | SHOULD | Cross-platform Validation | Run smoke builds on Windows + Linux for each PR. | Section 11 dependencies | Dual-lane CI completion |

> **Why it matters:** These rules keep every release repeatable, guarantee that a single command sets up a developer machine, and prevent leaked secrets by automating signing inside the vault-backed pipelines.

> **Everyday security translation:** “Secrets handling” here simply means you never email or store signing keys on disk. Pipelines fetch short-lived tokens, and developers use the bootstrap scripts without handling raw certificates.

### 14.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-BUILD-000 | QE policy draft | Avoid build drift and supply-chain risk. |
| R-BUILD-001 | Release governance | Ensure trust in distributed binaries. |
| R-BUILD-002 | Linux Core roadmap | Align CI and headless deployments. |
| R-BUILD-004 | Security policy | Protect sensitive credentials and signing assets. |

---

## 14.6 Acceptance Criteria & Verification

- Build pipelines produce signed artifacts with verification logs.
- Container images built nightly and pass smoke tests.
- Developer bootstrap scripts run successfully on Windows and Linux within 10 minutes.

### 14.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-BUILD-000 | Build audit | `qa/build-repeatability.md` | Hashes match baseline |
| R-BUILD-001 | CI check | `pipelines/signing-verify.yml` | All signatures validated |
| R-BUILD-002 | Container test | `pipelines/linux-container.yml` | Container executes smoke suite |
| R-BUILD-003 | Script telemetry | `tools/bootstrap/logs/` | ≥ 95% success rate |
| R-BUILD-004 | Security review | `security/vault-audit.md` | No secret leak incidents |
| R-BUILD-005 | CI integration | `pipelines/windows-build.yml`, `pipelines/linux-build.yml` | Both lanes succeed |

---

## 14.7 Constraints

- Signing certificates must be stored in hardware-backed vaults where possible.
- Build pipelines must complete within agreed SLAs to avoid release delays.
- Container base images require periodic CVE scans and patching.
- Release documentation MUST summarize bootstrap script updates so contributors can reproduce environments.
- Release artifacts MUST enumerate supported container image versions for operators and CI consumers.

### 14.7.1 Non-Functional Requirement Classes

- **Reliability:** Build success rate, retry policies.
- **Security:** Secrets management, signing, SBOM output.
- **Portability:** Tooling parity between Windows and Linux builds.
- **Maintainability:** Scripted environment setup, documented processes.

---

## 14.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-14-1 | Signing key compromise. | High | Hardware vault + incident response plan. | @security |
| RISK-14-2 | Container image drift vs. field deployments. | Medium | Automate rebuilds; publish version map. | @release |
| ISSUE-14-1 | Define bootstrap tooling for macOS contributors. | Medium | Evaluate cross-platform script support. | @platform |
| ISSUE-14-2 | Track SBOM publication pipeline. | Medium | Integrate SBOM step into CI. | @security |

---

## 14.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Reproducible builds | Deterministic outputs for trust and compliance. |
| C2 | Secure secret handling | Vault integration and audit logging. |
| C3 | Developer onboarding | Minimize manual setup friction. |
| C4 | Multi-OS pipelines | Windows + Linux parity for runtime validation. |
| C5 | Container parity | Align container images with supported deployments. |

### 14.9.1 Assumptions & Preconditions

- [A1] CI infrastructure can host Windows and Linux agents with necessary permissions.
- [A2] Security team provisions vault access workflows.
- [A3] Release team maintains documentation for bootstrap scripts.

---

## 14.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone options; build policies follow Section 11/12 runtime decisions. | — |

---

## 14.11 Comparison Matrix

| Attribute / Criteria | Standardized Toolchain (Current Plan) | Ad-hoc Developer-managed Setup |
|----------------------|---------------------------------------|--------------------------------|
| Reliability | High | Low |
| Security | High | Low |
| Developer Effort | Medium | High |
| Release Risk | Low | High |

---

## 14.12 Decision Matrix

Build tooling decisions derive from runtime/OS selections; emphasis is executing reproducibility and security policies.
Future alternatives (e.g., Bazel build system) would require dedicated option documents.

---

## 14.13 Evaluation & Verification

- Track build success rate and time-to-green metrics across pipelines.
- Audit signing logs and vault access quarterly.
- Review bootstrap script telemetry for onboarding friction.

---

## 14.14 Implementation Policy

*(Reserved — see `14-ADR-001` for implementation governance. This section remains
focused on normative requirements.)*

---

## 14.15 Community Sentiment

- Contributors request single-command setup; automation reduces onboarding friction.
- Release team prioritizes signed, reproducible builds before public betas.
- Security group emphasizes vault integration and SBOM publication for supply-chain trust.

### 14.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Reframed build tooling section using standardized template. | #0000 |

---

## 14.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-BUILD-000 | — | — | `qa/build-repeatability.md` | `global.json`, `packaging/` scripts |
| R-BUILD-001 | — | — | `pipelines/signing-verify.yml` | `packaging/signing/` |
| R-BUILD-002 | 11-O1 | 11-ADR-001 | `pipelines/linux-container.yml` | `deployment/linux-core/containers/` |

---

## 14.17 Conformance

Build tooling conforms when deterministic pipelines produce signed artifacts across Windows and Linux, container images match documented configurations, and secret management policies are enforced with audit evidence.

---

## Standards Context

Aligned with **SLSA** (Supply-chain Levels for Software Artifacts) level 2 requirements and **NIST SSDF** guidelines for secure build pipelines.
