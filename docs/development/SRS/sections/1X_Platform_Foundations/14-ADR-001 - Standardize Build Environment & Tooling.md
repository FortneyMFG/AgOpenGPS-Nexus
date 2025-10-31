# 14-ADR-001 — Standardize Build Environment & Tooling  
*(Status: Draft — Pending Review)*  

**Authors:** Nexus Team (Codex)
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-25  
**Last Updated:** 2025-10-25  
**Related SRS:** `14_Build_Environment_Tooling.md`  
**Related Decisions:** `11-ADR-001 — Establish Windows & Linux Support Baseline`, `12-ADR-001 — Adopt .NET 10 LTS Runtime`

---

## 1) Context  

Section 14 establishes the requirement for reproducible, secure builds that mirror the CI pipelines used across Windows and Linux.  
Historical AgOpenGPS builds relied on ad-hoc local environments, manual signing, and inconsistent container images.  

With Windows / Linux parity mandated in `11-ADR-001` and the unified .NET 10 runtime adopted in `12-ADR-001`, Nexus must now ensure deterministic outputs, automated signing, and documented bootstrap workflows that function identically in both developer and CI environments.

Current builds use **Visual Studio Code**, **.NET 10 SDK**, and **GitHub Actions** for continuous integration, making those the foundation for this standardization.

---

## 2) Decision  

Standardize the **Nexus build environment** around shared scripts, container images, and vault-managed secrets:  

- **Pinned toolchains:** Lock .NET SDK, analyzers, and container bases in repository manifests so CI and local builds produce identical artifacts.  
- **Cross-platform bootstrap:** Provide `tools/scripts/nexus.*` scripts that install .NET 10 SDK, required dependencies, and analyzers within ≈ 10 minutes.
- **Dual-lane CI:** Run Windows + Linux smoke builds on every PR via GitHub Actions, including signing verification and SBOM generation.  
- **Vault-based signing:** Store all signing keys and credentials in a secure vault issuing short-lived tokens to CI jobs only.  
- **Container parity:** Maintain a single Dev Container / base image reused by both developers and CI runners.  

This ADR becomes *Accepted* once release engineering verifies that bootstrap scripts and dual-lane builds successfully generate identical, signed packages for Section 11 deliverables.

---

## 3) Consequences  

### Positive Impacts  
- Reproducible builds across all environments.  
- Automated signing + SBOM validation improve supply-chain security.  
- Faster onboarding and reduced configuration drift.  

### Trade-offs / Mitigations  
- **Initial containerization effort:** Offset by reusing Section 11 Linux packaging work.  
- **Vault integration complexity:** Provide mock signing for developer tests.  
- **Longer CI times:** Use caching and job parallelization to mitigate.  

---

## 4) Rationale  

Standardizing build tooling enforces the same guarantees that runtime and OS governance provide: determinism, security, and traceability.  
GitHub Actions offers sufficient isolation and reproducibility for Nexus without maintaining separate build infrastructure.  
Unified scripts, container bases, and vault integration ensure that every contributor or CI job can produce verifiable artifacts identical to release builds.  

---

## 5) Open Questions  

| ID | Question | Owner | Resolution Path |
|----|-----------|--------|-----------------|
| **ADR14-Q1** | What level of macOS support is required for companion or cross-builds? | Platform Foundations WG | Coordinate with Section 13 companion roadmap. |
| **ADR14-Q2** | Which container registry will host official CI / release images? | Release WG | Compare GitHub Container Registry vs. self-hosted. |
| **ADR14-Q3** | How are SBOMs published and versioned across releases? | Security WG | Integrate SPDX generation into the §96 release pipeline. |

---

## 6) Acceptance Checklist  

- [ ] Bootstrap scripts validated on fresh Windows + Linux environments.  
- [ ] GitHub Actions lanes produce signed artifacts and publish verification logs.  
- [ ] Container images documented with rebuild cadence + CVE tracking.  
- [ ] Vault integration playbook approved by the Security WG.  

---

## 7) Governance  

- **Ownership:** Release & Tooling Working Group  
- **Review Cadence:** Semi-annual or upon new .NET LTS / GitHub Actions image revisions  
- **Artifacts:** `global.json`, `Dockerfile.dev`, `.github/workflows/*.yml`, `vault-policy.md`, and reproducibility dashboards  
- **Exit Criteria:** Superseded by a newer ADR expanding to additional CI platforms or alternative build systems (e.g., Bazel/Nix).  

---

## 8) Change Log  

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-25 | Initial draft defining standardized VS Code + GitHub Actions + Vault build environment. | Nexus Team (Codex) |  |
