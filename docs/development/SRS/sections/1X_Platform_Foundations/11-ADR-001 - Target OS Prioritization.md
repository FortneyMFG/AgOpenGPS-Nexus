# 11-ADR-001 — Target OS Prioritization  
*(Status: Accepted — 2025-10-25)*

**Authors:** Nexus Team (Fortney)
**Reviewers:** Nexus Team
**Created:** 2025-10-20  
**Last Updated:** 2025-10-24  
**Related SRS:** `11_OS_Support.md`  
**Related Options:** 

---

## 1) Context

Section 11 establishes the requirement that Nexus must provide cross-platform builds
with functional parity across supported operating systems.

Historically, AgOpenGPS releases shipped only **Windows** desktop installers, while
community forks and experiments provided limited or manual **Linux** builds. Pilot
work for §11 confirmed that a unified **.NET 8 + Avalonia** stack can generate signed
**Windows** installers and **Linux** packages from a single solution.

To align platform teams, this ADR codifies the officially supported OS scope,
tiering, and governance rules for the initial Nexus release. Other operating systems
remain out of scope until baseline parity and stability are achieved.

---

## 2) Decision

For the **initial Nexus release** and **until further notice**, only **Windows** and **Linux**
will be developed, tested, and distributed as officially supported platforms.

These platforms constitute the **entire supported scope** for full-stack operation
(Core + UI) at this stage of development. All other operating systems are excluded
from active development and will be evaluated in future ADRs once the baseline is
stable.

### 2.1 Platform Scope

| Tier | Platforms | Purpose / Scope | Notes |
|------|------------|-----------------|-------|
| **Primary (Supported)** | **Windows 10/11 (x64)** and **Ubuntu LTS (x64 / ARM64)** | Full-stack execution of Core and UI. Defines official release quality baseline and all parity, performance, and verification tests. | All §11 “MUST” requirements apply. |
| **Future Consideration (Not in Scope)** | Other Linux variants, macOS, Android, iOS, or web environments | Potential expansion once baseline maturity and automation allow. | Will be addressed under separate ADRs. |

### 2.2 Governance Rules

- Each public release **must** include:
  - A **Support Matrix** listing the current officially supported platforms.  
  - A **Parity Checklist** confirming feature and I/O equivalence between Windows and Linux.  
  - A **Known Gaps** table identifying any temporary deviations.

- Adding or removing a platform from official support requires approval by the  
  **Platform Foundations Working Group (PFWG)** and an accompanying SRS §11 update.

- Experimental or community builds for other OSes **may exist**, but carry **no
  release guarantees** and **must not** interfere with primary release schedules.

> **Normative:**  
> This decision establishes Windows and Linux as the **only officially supported and
developed platforms** for Nexus at this time. Future OS expansions will be proposed
and ratified through new ADRs.

---

## 3) Consequences

### Positive Impacts
- Focuses runtime, UI, and tooling efforts on achieving **Windows / Linux parity**.  
- Concentrates resources on the **dual-platform build + test pipeline** for consistent installers and runtime behavior.  
- Establishes a clear baseline from which future expansions can proceed safely.  
- Keeps release engineering accountable for parity testing and packaging verification.

### Negative / Mitigated Impacts
- **Increased QA workload** across two OSes.  
  *Mitigation:* automated smoke suites, nightly parity dashboards, hardware-in-loop testing.  
- **Vendor SDK and driver gaps** between Windows and Linux.  
  *Mitigation:* AgIO abstraction layer and documented device-tier matrices.  
- **Community expectations for other OSes.**  
  *Mitigation:* communicate scope and gate expansions behind future ADRs.

### Follow-Up Actions
- Publish the §11 **Platform Matrix Template** (Windows x64, Ubuntu x64 / ARM64).  
- Integrate the **Parity Checklist** into CI so every PR executes Windows and Linux smoke tests.  
- Document **promotion criteria** for any new OS proposal and keep a current support matrix in release notes.

> **Outcome:**  
> Nexus 1.0 will ship and be officially supported on **Windows** and **Linux** only, forming
the foundation for future cross-platform growth.

---

## 4) Rationale

The decision matrix for §11 showed that focusing on Windows + Linux parity provides
the best balance between operator reach and engineering investment. Splitting stacks
by OS would double maintenance cost and delay parity. Codifying governance around
tiers keeps expansion visible without diluting the mandatory desktop experience.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|----------|---------|
| **Windows-only baseline** | Continue shipping Windows builds only. | **Rejected** — fails SRS §11 requirements R-OS-001 and R-OS-002. |
| **Independent Linux fork** | Maintain a Linux-specific runtime/UI separate from Windows. | **Rejected** — doubles maintenance and breaks parity guarantees. |
| **Web-first shell** | Replace desktop apps with browser UI. | **Rejected** — cannot meet hardware access and offline requirements in §11 / §13. |

---

## 6) Governance

- **Ownership:** Platform Foundations Working Group (PFWG).  
- **Review Cadence:** Bi-annual or when major Windows / Ubuntu LTS versions shift.  
- **Artifacts:** Published Support Matrix, Parity Checklist, Release Parity Dashboards.  
- **Exit Criteria:** Superseded by a future ADR that changes mandatory OS coverage or adds a new Primary tier.

---

## 7) Risks & Mitigations

| ID | Risk | Mitigation |
|----|------|-------------|
| **ADR11-R1** | Linux GPU drivers regress rendering performance. | Maintain benchmark dashboards; provide software-rendering fallback. |
| **ADR11-R2** | Vendor SDKs unavailable on Linux or ARM64. | Document device tiers; prioritize SocketCAN / AgIO shims. |
| **ADR11-R3** | Preview platform efforts divert focus from primary targets. | Gate preview investments on parity metrics and PFWG approval. |

---

> **Summary:**  
> ADR 11-001 defines the initial OS support boundary for Nexus.  
> Windows and Linux are the only officially developed and supported platforms at this time.  
> Future expansions will require separate ADRs and formal PFWG approval.

---

## 8) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial acceptance of Windows/Linux support policy. | Nexus Team (Fortney) |
| 2025-10-24 | Expanded preview/companion notes; aligned with Section 11 parity checklist. | Nexus Team (Fortney) |
