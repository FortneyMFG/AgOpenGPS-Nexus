# 12 — Development Language & Runtime  
*(Status: Drafting — Decision Agnostic)*

**Section ID:** 12  
**Version:** 0.1.0  
**Editors:** Nexus Team (Fortney)  
**Last Updated:** 2025-10-24
**Related Sections:** 11 — OS Support, 14 — Build Environment & Tooling  
**Related Decisions:** `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `11-ADR-001 — Establish Windows & Linux Support Baseline`  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 6X — Core Domain Services, 9X — Frontends & Ops  

---

## 12.1 Purpose & Scope  

This section defines the **language, runtime, and dependency policies** that govern all Nexus codebases.  
It ensures contributors use a common runtime environment and development toolchain, producing predictable, reproducible, and portable builds across Windows and Linux.  
It also defines boundaries for dependency management and contract versioning so that plugins, AgIO, and user interfaces can interoperate without breaking compatibility.  

> **Plain summary:**  
> Everyone writing Nexus code uses the same managed runtime, follows shared dependency rules, and relies on common interface contracts so everything behaves the same on all platforms.  

---

## 12.2 Context  

- All Nexus components share a managed runtime that supports both Windows and Linux as defined in §11 (`11-ADR-001`).
- Prior versions of AgOpenGPS mixed .NET Framework, WPF, and native utilities, leading to inconsistent build behavior.  
- The modern Nexus stack aims to unify runtime, language, and dependency handling across Core, AgIO, UI, and CLI tools.  
- Build environment and CI enforcement are covered in §14, while this section defines the policies that those builds must enforce.  

---

## 12.3 Legacy Comparison  

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Runtime Mix | Combination of .NET Framework, .NET Core 6, and native executables. | Inconsistent APIs and build pipelines. | Migrate all managed components to a single LTS runtime. | AOG v6 Source Analysis |
| Dependency Governance | Ad-hoc package additions per project. | Version drift, missing Linux validation. | Curated dependency allowlist reviewed through CI. | Contributor discussions |
| Plugin Contracts | Manual interface definitions shared by copy. | Frequent breakage across versions. | Centralized, versioned contract libraries. | Plugin WG notes |
| Build Reproducibility | No consistent toolchain pinning. | Builds vary between machines. | Enforce deterministic builds and SDK pinning. | Build WG proposal |

---

## 12.4 Definitions  

| Term | Definition |
|------|-------------|
| **Managed Runtime** | The long-term-support runtime (e.g., .NET LTS) used to execute compiled assemblies. |
| **Contract Package** | A shared, versioned interface definition library used by Core, UI, AgIO, and plugins. |
| **Deterministic Build** | A build process that produces identical artifacts from identical inputs. |
| **Allowlist** | A formally reviewed list of approved dependencies and their versions. |

---

## 12.5 Requirements  

> **Requirement Grammar (per RFC 2119)** —  
> **MUST / MUST NOT** = mandatory · **SHOULD / SHOULD NOT** = strong recommendation · **MAY** = optional.  
> Each requirement must be testable and traceable.

> See 12-ADR-001 (Runtime Selection) and 12-ADR-002 (Contract Governance) for specific implementation choices.


| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|----------------|-----------------------------|
| **R-STACK-001** | **MUST** | Runtime | Use a single long-term-support managed runtime across all Nexus projects (`12-ADR-001`). | Architecture WG | CI confirms all projects target the same TFM. |
| **R-STACK-002** | **MUST** | Language | Use C# as the implementation language for Core, UI, and AgIO. Shared contract packages MUST be consumable by other languages without modification. | Core WG | Contract build produces valid stubs for all languages. |
| **R-STACK-003** | **MUST** | Build Integrity | Pin SDK and dependency versions to ensure deterministic builds. | Build WG | Hash comparison between builds is identical. |
| **R-STACK-004** | **MUST** | Abstraction | Contain all OS-specific or hardware-specific logic behind dependency-injected interfaces. | AgIO WG | Swappable backend tests pass on both Windows and Linux. |
| **R-STACK-005** | **SHOULD** | Dependency Governance | Maintain a curated dependency allowlist verified across Windows and Linux CI lanes. | Release WG | CI pipeline rejects unapproved package additions. |
| **R-STACK-006** | **SHOULD** | Contract Versioning | Use semantic versioning and automated compatibility testing for shared interface packages. | Plugin WG | Compatibility tests pass across two consecutive versions. |
| **R-STACK-007** | **MAY** | Native Extensions | Allow optional native or FFI modules behind a stable abstraction layer with documented safety rules. | Core maintainers | Manual review and static analysis pass. |

> **Intent:**  
> These requirements ensure consistent development environments, predictable runtime behavior, and portable plugins.  

---

## 12.6 Acceptance Criteria & Verification  

**Verification expectations:**  
- CI pipelines build and test all managed components under the same target runtime.  
- Dependency scanners verify only allowlisted packages are used.  
- Contract compatibility tests confirm backward and forward support.  
- Build artifacts generated on different systems produce identical hashes.  

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-STACK-001 | CI Integration | `/pipelines/dotnet.yml` | All projects compile under unified TFM. |
| R-STACK-003 | Build Audit | `/qa/build_repeatability.md` | Hash and signature match baseline. |
| R-STACK-004 | Cross-Platform Backend Swap Test | `/tests/agio/driver_matrix/` | All hardware-facing interfaces pass the same behavioral tests on Windows and Linux. |
| R-STACK-005 | Policy Check | `/tools/dependency-allowlist.json` | No unapproved dependencies detected. |
| R-STACK-006 | Contract Test | `/tests/contracts/` | All API compatibility tests succeed. |

---

## 12.7 Constraints  

- Runtime and SDK versions must remain aligned with the project’s supported OS list (§11).  
- Build processes must be reproducible, signed, and verifiable.  
- All dependency changes require review under the dependency governance policy.  
- Breaking contract changes require migration notes and version increments.  

### 12.7.1 Non-Functional Requirements  

- **Performance:** Managed overhead shall not exceed 10% compared to native equivalents under test loads.  
- **Reliability:** CI must validate runtime behavior across at least two OS architectures.  
- **Security:** All builds must produce signed binaries and published SBOMs.  
- **Maintainability:** Shared code analyzers and style rules are mandatory for all repos.  
- **Portability:** All managed components MUST compile and run on all Primary OS targets defined in §11 / ADR 11-001.

---

## 12.8 Risks & Open Issues  

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| **RISK-12-1** | Legacy Framework components may not port cleanly. | Medium | Introduce adapters or gradual rewrite. | @core |
| **RISK-12-2** | Dependency allowlist process may slow merges. | Low | Automate checks; document review process. | @release |
| **ISSUE-12-1** | Define policy for native/FFI helpers. | Medium | Add to §14 build policy. | @platform |
| **ISSUE-12-2** | Confirm versioning cadence for shared contracts. | Medium | Establish per-release compatibility window. | @plugins |

---

## 12.9 Design Considerations  

| ID | Consideration | Description |
|----|----------------|-------------|
| **C1** | Single Runtime | Simplifies builds and reduces fragmentation. |
| **C2** | Language-Agnostic Contracts | Enables plugin developers to use other languages if desired. |
| **C3** | Deterministic Builds | Required for reproducibility, trust, and debugging. |
| **C4** | Dependency Hygiene | Keeps supply chain secure and licenses transparent. |
| **C5** | Native Hooks | Provides optional performance paths while keeping portability. |

**Assumptions:**  
- Contributors can install the chosen LTS runtime on Windows or Linux.  
- CI agents mirror contributor toolchains.  
- Plugin maintainers test against contract compatibility suites.  

---

## 12.10 Option Overview  

| Option ID | Status | Type / Theme | Description | Reference Document |
|------------|--------|--------------|-------------|--------------------|
| **12-O1** | Proposed | Runtime Policy | Use a single LTS managed runtime and unified toolchain. | 12-O1_Runtime_Policy.md |
| **12-O2** | Proposed | Contract Governance | Define semantic versioning and compatibility tests for shared packages. | 12-O2_Contract_Governance.md |

> **Informative:**  
> Options describe possible implementation policies. They are not normative until accepted via ADR.


---

## 12.11 Comparison Matrix  

| Attribute / Criteria | 12-O1 (Unified Runtime) | 12-O2 (Contract Governance) |
|----------------------|--------------------------|-----------------------------|
| Implementation Effort | Medium | Medium |
| Maintainability | High | High |
| Performance | High | Neutral |
| Extensibility | Medium | High |
| Risk Level | Medium | Medium |

---

## 12.12 Decision Matrix  

*(Reserved — to be completed when runtime and contract governance options are evaluated.)*

---

## 12.13 Evaluation & Verification  

- Verify build reproducibility by rebuilding from scratch and comparing artifact hashes.  
- Validate plugin API compatibility using automated diff tests across two versions.  
- Confirm all CI pipelines use identical SDK and dependency baselines.  

**Acceptance Criteria:**  
All MUST requirements pass their corresponding tests and audits with no waivers.  

---

## 12.14 Implementation Policy

*(Reserved — implementation specifics are governed by ADRs such as `12-ADR-001` and
`14-ADR-001`. This SRS remains focused on normative “what” requirements.)*

---

## 12.15 Community Sentiment  

Contributors broadly support a unified managed runtime and stricter dependency controls for reliability and onboarding simplicity.  
Plugin authors have requested clear documentation for contract versioning and migration guidance.  
Build maintainers emphasize reproducibility and security of the release pipeline.  



---


## 12.16 Traceability  

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-STACK-001 | 12-O1 | 12-ADR-001 (Runtime Selection) | `/pipelines/dotnet.yml` | `/global.json` |
| R-STACK-003 | 12-O1 | 14-ADR-001 (Build & Signing Policy) | `/qa/build_repeatability.md` | `/src/Build/` |
| R-STACK-004 | 12-O1 | 11-ADR-001 (Windows/Linux Baseline) | `/tests/agio/driver_matrix/` | `/src/AgIO/` |
| R-STACK-006 | 12-O2 | 12-ADR-002 (Contract Governance) | `/tests/contracts/` | `/src/Aog.Abstractions/` |


---

## 12.17 Conformance  

An implementation conforms to §12 when:  
1. All **MUST** requirements (R-STACK-001–R-STACK-004) are satisfied and verified;  
2. All **SHOULD** requirements (R-STACK-005–R-STACK-006) are satisfied or formally waived;  
3. No **MUST NOT** conditions are violated.  

---

## Standards Context  

Aligns with **ISO/IEC/IEEE 29148:2018** (Systems and Software Requirements Specification) and  
**IEEE 1016:2017** (Software Design Description).  
Also consistent with CNCF secure supply chain guidance (reproducible builds, dependency transparency, SBOM publishing).  
These references exist to ensure §12 can be used for audit, onboarding, and future certification without rewriting the requirements model.


| Date | Summary | PR / Issue |
|------|----------|------------|
| 2025-10-21 | Initial draft of runtime and dependency governance | #0000 |
| 2025-10-24 | Major structural review — added requirement grammar preamble, verification for R-STACK-004, traceability links to ADR 11-001/14-ADR-001, and minor consistency fixes. | #0001 |

