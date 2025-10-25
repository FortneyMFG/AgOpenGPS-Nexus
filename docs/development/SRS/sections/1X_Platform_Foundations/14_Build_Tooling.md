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
**Last Updated:** 2025-10-25  
**Related Sections:** 11 — OS Support, 12 — Development Language & Runtime, 96 — Quality Engineering & Release  
**Related Decisions:** `14-ADR-001 — Standardize Build Environment & Tooling`, `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `11-ADR-001 — Establish Windows & Linux Support Baseline`  
**Upstream Dependencies:** §12 — Runtime Governance, §96 — QE Policies  
**Downstream Impacts:** Release pipelines and developer onboarding  

---

## 14.1 Purpose & Scope  

Define the **toolchains, automation, signing, and secrets policies** that keep all Nexus builds reproducible, portable, and secure across Windows and Linux.  
All development currently uses **Visual Studio Code** with the **.NET 8 SDK**, and **GitHub Actions** handles the Windows + Linux continuous-integration lanes.  
Local builds, containerized tests, and CI runs use identical scripts to guarantee environment parity.  

---

## 14.2 Context  

- Windows installers and Linux packages must come from **consistent, reproducible pipelines** (§11-ADR-001).  
- The same build scripts run locally and in GitHub Actions using Dev Containers or pinned SDK versions.  
- **Signing keys** and **credentials** are managed in a secure vault; developers never store them locally.  
- Bootstrap scripts allow new contributors to reproduce the CI toolchain automatically.  

> **Quick Start for Developers**
>
> 1. Clone the repo and run  
>    `tools/scripts/nexus.sh bootstrap` (Linux/macOS) or `tools/scripts/nexus.ps1 bootstrap` (Windows).  
> 2. Run `dotnet build` and `dotnet test` to confirm the environment matches CI.  
> 3. Use `tools/scripts/nexus.sh run --help` to explore common workflows.  
> 4. Never export or copy signing keys — CI fetches them just-in-time from the vault.

---

## 14.3 Legacy Comparison  

| Area / Theme | Legacy Behavior | Limitation | Modernization Opportunity | Source |
|---------------|----------------|-------------|---------------------------|---------|
| Toolchain Pinning | Manual SDK installs per developer. | Build drift across contributors & CI. | `global.json` + bootstrap scripts. | Release retrospective |
| Signing | Ad-hoc, often unsigned binaries. | Unverifiable public builds. | Automated signing via GitHub Actions + vault. | QE policy draft |
| Containerization | Partial Dockerfiles for test rigs. | Environment mismatch vs. field deployments. | Unified Dev Container + CI image base. | Linux Core plan |

---

## 14.4 Definitions  

| Term | Definition |
|------|-------------|
| **Build Manifest** | Machine-readable list of dependency versions + checksums per release. |
| **Secure Build Vault** | Secrets store issuing short-lived credentials to CI jobs. |
| **Smoke Run** | Minimal compile + test verifying pipeline health. |

---

## 14.5 Requirements  

> **Requirement Grammar (RFC 2119):** **MUST/MUST NOT** = mandatory; **SHOULD/SHOULD NOT** = strong recommendation; **MAY** = optional.  
> Implementation details live in `14-ADR-001`.  

| ID | Priority | Category | Summary | Source / C-IDs | Verification |
|----|-----------|-----------|----------|----------------|---------------|
| **R-BUILD-000** | **MUST** | Reproducibility | Pin .NET SDK and native deps; publish repeatable restore manifest. | QE policy | Hash comparison across builds |
| **R-BUILD-001** | **MUST** | Signing | Sign installers and packages with project certificates; verify in CI. | Release gov. | Signature check step |
| **R-BUILD-002** | **SHOULD** | Container Support | Provide Dockerfiles / Dev Containers matching field deployments. | Linux Core roadmap | Container smoke run |
| **R-BUILD-003** | **SHOULD** | Developer Ergonomics | One-command bootstrap for all platforms. | Onboarding | Script success rate ≥ 95 % |
| **R-BUILD-004** | **MUST** | Secrets Handling | Use vault-backed short-lived credentials for signing & release. | Security policy | Vault audit log clean |
| **R-BUILD-005** | **SHOULD** | Cross-Platform CI | Run Windows + Linux lanes in GitHub Actions per PR. | §11 dependency | Dual lane CI green status |

> **Intent:** Every developer and CI runner produces identical, signed artifacts without ever touching long-term secrets.

---

## 14.6 Acceptance Criteria & Verification  

- GitHub Actions pipelines produce signed, reproducible artifacts.  
- Dev Container image builds nightly and passes smoke tests.  
- Bootstrap scripts complete < 10 min on Windows and Linux.  

| Req ID | Verification Type | Artifact / Path | Pass Criteria |
|--------|--------------------|-----------------|----------------|
| R-BUILD-000 | Build audit | `qa/build-repeatability.md` | Hash matches baseline |
| R-BUILD-001 | CI check | `.github/workflows/signing-verify.yml` | All signatures valid |
| R-BUILD-002 | Container test | `.github/workflows/linux-container.yml` | Smoke suite pass |
| R-BUILD-003 | Script telemetry | `tools/bootstrap/logs/` | ≥ 95 % success |
| R-BUILD-004 | Security review | `security/vault-audit.md` | 0 leaks reported |
| R-BUILD-005 | CI integration | `.github/workflows/windows.yml`, `.github/workflows/linux.yml` | Both lanes green |

---

## 14.7 Constraints  

- Signing certificates reside in hardware-backed vaults only.  
- Build jobs must finish within release SLA.  
- Container bases require regular CVE scans and patching.  
- Bootstrap docs MUST mirror CI toolchain changes.  
- Published releases MUST list validated container image tags.  

### 14.7.1 Non-Functional Requirements  

- **Reliability:** ≥ 95 % build success rate per week.  
- **Security:** SBOM + signature required for every artifact.  
- **Portability:** Identical scripts for Windows and Linux.  
- **Maintainability:** Minimal manual steps; documented CI flows.  

---

## 14.8 Risks & Open Issues  

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| **RISK-14-1** | Signing key compromise. | High | Hardware vault + incident plan. | @security |
| **RISK-14-2** | Container drift vs field rigs. | Medium | Automate rebuilds + version map. | @release |
| **ISSUE-14-1** | macOS bootstrap support. | Medium | Evaluate cross-platform scripts. | @platform |
| **ISSUE-14-2** | SBOM pipeline finalization. | Medium | Integrate SPDX step in CI. | @security |

---

## 14.9 Design Considerations  

| ID | Consideration | Description |
|----|----------------|-------------|
| **C1** | Reproducible builds | Deterministic outputs for trust and debugging. |
| **C2** | Secure secret handling | Vault integration and audit logging. |
| **C3** | Developer onboarding | One-command setup mirrors CI. |
| **C4** | Multi-OS pipelines | Parity between Windows and Linux runs. |
| **C5** | Container parity | CI images match deployment images. |

**Assumptions:**  
- CI uses GitHub Actions Windows and Linux runners.  
- Security team maintains vault access rules.  
- Release team owns bootstrap documentation.  

---

## 14.10 Option Overview  

| Option ID | Status | Theme | Description | Reference |
|-----------|--------|--------|-------------|------------|
| **14-O1** | Proposed | CI/CD Standardization | Use GitHub Actions + Dev Containers with vault-based signing for reproducible builds. | 14-ADR-001 — Standardize Build Environment & Tooling |

---

## 14.11 Comparison Matrix  

| Attribute / Criteria | Standardized Toolchain (Current Plan) | Ad-hoc Local Setup |
|----------------------|--------------------------------------|--------------------|
| Reliability | **High** | Low |
| Security | **High** | Low |
| Developer Effort | Medium | High |
| Release Risk | **Low** | High |

---

## 14.12 Decision Matrix  

Build tooling decisions derive from runtime and OS policies (§11–§12).  
Future alternatives (e.g., Bazel or Nix build systems) require separate option documents.  

---

## 14.13 Evaluation & Verification  

- Track CI build success rate and time-to-green.  
- Audit signing and vault logs quarterly.  
- Review bootstrap telemetry for setup friction.  

**Acceptance Criteria:** All MUST requirements verified with no waivers.  

---

## 14.14 Implementation Policy  

*(Reserved — see `14-ADR-001` for implementation details. This SRS defines “what,” not “how.”)*  

---

## 14.15 Community Sentiment  

- Developers prefer VS Code + scripts over manual IDE setup.  
- Release team prioritizes signed, reproducible artifacts before public betas.  
- Security group supports GitHub Actions with vault integration and SBOM output.  

| Date | Summary | PR / Issue |
|------|----------|------------|
| 2025-10-20 | Standardized build tooling section drafted. | #0000 |
| 2025-10-25 | Updated to reflect VS Code + GitHub Actions workflow. | #0001 |

---

## 14.16 Traceability  

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-BUILD-000 | 14-O1 | 14-ADR-001 | `qa/build-repeatability.md` | `global.json`, `packaging/` |
| R-BUILD-001 | 14-O1 | 14-ADR-001 | `.github/workflows/signing-verify.yml` | `packaging/signing/` |
| R-BUILD-002 | 14-O1 | 11-ADR-001 | `.github/workflows/linux-container.yml` | `deployment/linux-core/containers/` |

---

## 14.17 Conformance  

Implementation conforms when:  
1. Deterministic pipelines produce signed artifacts for Windows and Linux.  
2. Container images match documented configs and pass smoke tests.  
3. Vault audit logs show compliant secret access.  

---

## Standards Context  

Aligned with **SLSA Level 2** (Supply-chain Levels for Software Artifacts),  
**NIST SSDF** secure build guidelines, and **ISO/IEC/IEEE 29148:2018** for requirement traceability.  
