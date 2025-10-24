# 11-ADR-001 — Establish Windows & Linux Support Baseline

*(Status: Accepted — 2025-10-25)*

**Authors:** Platform Foundations Working Group  
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `11_OS_Support.md`  
**Related Options:** `11-O1_Unified_DotNet8_Avalonia.md`

---

## 1) Context

Section 11 raises the requirement that Nexus must ship Windows and Linux builds with
identical capabilities. Legacy AgOpenGPS releases targeted Windows-only installers
and relied on bespoke Linux experiments. Pilot work for Section 11 proved we can
produce signed Windows installers and Linux packages from the same solution, but the
support tiers, distribution list, and parity expectations must be codified so all
platform teams align.

Upcoming scope also includes Android (full-stack) explorations, a lightweight iOS
companion, and potential web dashboards. Those expansions must not jeopardize the
primary Windows and Linux deliverables required for Section 11 compliance.

---

## 2) Decision

Establish a Windows & Linux support baseline with clearly defined tiers, parity
expectations, and release governance:

- **Primary platforms:** Windows 10/11 x64 and Ubuntu LTS x64; Debian-based ARM64
  images for compact hardware (e.g., CM5/industrial Pi).
- **Preview platforms:** Fedora/RHEL derivatives and Linux ARM64 container builds for
  headless deployments; Android full-stack builds once Avalonia Android reaches the
  stability required by Section 13 run modes.
- **Companion platforms:** iOS companion app and web UI surfaces MAY evolve, but must
  integrate via published APIs without blocking desktop releases.

Every release must publish a support matrix, parity checklist status, and known gaps.
Changes to the platform list require Platform Foundations WG approval and updated SRS
references.

---

## 3) Consequences

**Positive impacts**

- Aligns runtime, UI, and tooling investments toward the mandatory Windows/Linux
  parity goal.
- Provides a clear roadmap for expanding to Android, iOS companions, and web surfaces
  without diluting desktop commitments.
- Keeps release engineering accountable for dual-platform smoke tests and installer
  verification.

**Negative/mitigated impacts**

- Additional QA load across Windows and Linux. Mitigated by automated smoke suites
  and parity dashboards.
- Vendor SDK availability still varies per OS. Mitigated through AgIO adapter design
  and device tier documentation.
- Preview platforms may create expectations before tooling stabilizes. Mitigated by
  clearly labeling preview tiers and gating support on Section 11 metrics.

**Follow-up actions**

- Publish Section 11 platform matrix template (Windows x64, Ubuntu x64, Debian ARM64,
  preview tiers, companion targets).
- Integrate parity checklist into CI so each PR exercises Windows and Linux smoke
  runs.
- Track Android/iOS/web companion explorations under Section 13/9X without impacting
  Section 11 ship gates.

---

## 4) Rationale

The decision matrix for Section 11 showed that focusing on Windows + Linux parity
provides the best balance between operator reach and engineering investment. Splitting
stacks by OS would double maintenance cost and delay parity. Codifying preview and
companion tiers keeps expansion work visible without diluting the mandatory desktop
experience.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|---------|---------|
| Windows-only baseline | Continue shipping Windows builds only. | Rejected — fails SRS §11 requirements R-OS-001 and R-OS-002. |
| Independent Linux fork | Maintain a Linux-specific runtime/UI separate from Windows. | Rejected — doubles maintenance and breaks parity guarantees. |
| Web-first shell | Replace desktop apps with a browser UI. | Rejected — cannot meet hardware access and offline requirements in §11/§13. |

---

## 6) Governance

- **Ownership:** Platform Foundations Working Group.
- **Review cadence:** Twice yearly or when major OS/LTS milestones shift.
- **Artifacts:** Published support matrix, parity checklist, release parity dashboards.
- **Exit criteria:** Superseded by a future ADR that changes mandatory OS coverage or
  elevates preview tiers to primary.

---

## 7) Risks & Mitigations

| ID | Risk | Mitigation |
|----|------|------------|
| ADR11-R1 | Linux GPU drivers regress rendering performance. | Maintain benchmark dashboards; provide software rendering fallback. |
| ADR11-R2 | Vendor SDKs unavailable on Linux or ARM64. | Document device tiers; prioritize SocketCAN/AgIO shims. |
| ADR11-R3 | Preview platforms divert focus from primary OS targets. | Gate preview investments on parity metrics and WG approval. |

---

## 8) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial acceptance of Windows/Linux support policy. | Platform Foundations WG |
| 2025-10-25 | Expanded preview/companion notes; aligned with Section 11 parity checklist. | Platform Foundations WG |
