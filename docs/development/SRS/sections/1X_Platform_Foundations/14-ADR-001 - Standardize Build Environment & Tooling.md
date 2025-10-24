# 14-ADR-001 — Standardize Build Environment & Tooling

*(Status: Draft — Pending Review)*

**Authors:** Release & Tooling Working Group  
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-25  
**Last Updated:** 2025-10-25  
**Related SRS:** `14_Build_Tooling.md`

---

## 1) Context

Section 14 requires reproducible, secure builds that match CI pipelines across Windows
and Linux. Historical releases relied on ad-hoc developer environments, manual signing,
and inconsistent container images. With Section 11 now mandating Windows/Linux parity
and Section 12 adopting .NET 8, the build environment must guarantee deterministic
outputs, automated signing, and documented bootstrap workflows that match Avalonia UI
and headless services alike.

---

## 2) Proposed Decision

Standardize the Nexus build environment around shared scripts, container images, and
vault-managed secrets:

- Pin SDKs, toolchains, and container bases in repository-managed manifests so CI and
  local builds produce identical artifacts.
- Provide cross-platform bootstrap scripts (`tools/scripts/nexus.*`) that install the
  .NET 8 SDK, required native dependencies, and analyzers within 10 minutes.
- Run dual-lane smoke builds (Windows + Linux) on every PR, including signing
  verification and SBOM generation.
- Store signing keys and sensitive credentials in the secure build vault, issuing
  short-lived tokens to CI and never exposing secrets to developer machines.

The decision becomes Accepted when release engineering validates the bootstrap scripts
and dual-lane smoke builds for Section 11 packages.

---

## 3) Consequences

**Expected benefits**

- Builds become reproducible across contributors, CI, and release pipelines.
- Signing and SBOM validation happen automatically, reducing security risk.
- Onboarding improves via scripted bootstrap flows rather than manual guides.

**Trade-offs / mitigations**

- Additional upfront work to containerize build steps. Mitigated by reusing existing
  Linux packaging efforts from Section 11.
- Vault integration may complicate local testing. Mitigated by providing mock signing
  flows for developer environments.
- Dual-lane smoke tests increase CI time. Mitigated by caching and parallelization.

---

## 4) Rationale

Standardizing build tooling is essential for delivering the Windows/Linux parity defined
in `11-ADR-001` and enforcing the .NET 8 runtime from `12-ADR-001`. Without shared
scripts and secrets management, operators cannot trust release artifacts and developers
cannot reproduce builds locally.

---

## 5) Open Questions

| ID | Question | Owner | Resolution Path |
|----|----------|-------|-----------------|
| ADR14-Q1 | What macOS support is required for companion builds? | Platform Foundations WG | Coordinate with Section 13 companion roadmap. |
| ADR14-Q2 | Which container registry hosts official build images? | Release WG | Evaluate GitHub Container Registry vs. self-hosted. |
| ADR14-Q3 | How are SBOMs published and versioned? | Security WG | Integrate into release pipeline, documented in §96. |

---

## 6) Acceptance Checklist

- [ ] Bootstrap scripts validated on Windows and Linux fresh machines.  
- [ ] CI lanes publish signed artifacts with verification logs.  
- [ ] Container images documented with rebuild cadence and CVE tracking.  
- [ ] Vault integration playbook approved by security.

---

## 7) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-25 | Initial draft for review. | Release & Tooling WG |
