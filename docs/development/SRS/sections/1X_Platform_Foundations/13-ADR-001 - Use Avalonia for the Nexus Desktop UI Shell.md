# 13-ADR-001 — Adopt Avalonia for the Nexus Desktop UI Shell  
*(Status: Accepted — 2025-10-25)*  

**Authors:** Nexus Team (Codex)
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `13_UI_Framework_UX.md`  
**Related Decisions:** `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `11-ADR-001 — Establish Windows & Linux Support Baseline`  
**Upstream Sections:** §11 — OS Support, §12 — Development Language & Runtime  
**Downstream Impacts:** §9X — Frontends & Ops, Training & UX Guidelines  

---

## 1) Context

Section 13 defines how Nexus manages its presentation layer while modernizing beyond
the legacy WinForms UI used in AgOpenGPS v6. Operators still rely on that interface
daily, but the SRS requires a **cross-platform desktop shell**, **metadata-driven
dashboards**, and **run-mode parity** across Windows and Linux.  

Earlier community branches explored **Qt/C++** front-ends and partial web dashboards,
but maintaining those toolchains separately from Core added complexity and fractured
the contributor base. A shared, managed UI framework is required to unify the visual
and runtime layers under a single toolchain.

**Avalonia UI** provides a modern, .NET-native, cross-platform framework that runs on
Windows and Linux, aligning directly with the runtime chosen in `12-ADR-001` and
supporting future mobile and companion use cases without a full rewrite of existing
view models.

---

## 2) Decision

Adopt **Avalonia UI** as the **primary desktop shell** for Nexus.  
Legacy **WinForms** remains the fallback interface during the transition.

- Avalonia projects will host shared **view models**, **theming system**, and
  **metadata-driven dashboards** defined in §13.  
- Mobile or web companions **may** reuse shared view models and services, but all
  desktop work must target the Avalonia shell.  
- WinForms will continue receiving maintenance updates until Avalonia reaches feature
  parity for production use.

> **Scope:**  
> This ADR governs desktop UI architecture only. Companion apps, build tooling, and
> OS support are handled in their respective sections.

---

## 3) Consequences

### Positive Impacts
- Provides a **single UI codebase** for Windows and Linux using shared XAML and
  view-models.  
- Enables **metadata-driven dashboards** and run-mode toggles without duplicating
  logic across frameworks.  
- Keeps Nexus development within the **.NET 8 LTS** ecosystem, maximizing reuse of
  existing tools and contributor expertise.

### Negative / Mitigated Impacts
- **Learning curve:** Contributors must learn Avalonia patterns.  
  *Mitigation:* Provide official templates, sample dashboards, and migration guides.  
- **GPU/driver variance:** Render performance may differ by platform.  
  *Mitigation:* Benchmark input latency and FPS on representative hardware (§11).  
- **Dual maintenance:** WinForms remains active until parity is achieved.  
  *Mitigation:* Gate retirement on the §13 acceptance criteria.

### Follow-Up Actions
- Publish Avalonia scaffolds and example metadata widgets in `/docs/UI/`.  
- Document theming, accessibility, and layout policies alongside §13 reference
  material.  
- Schedule quarterly UX smoke tests on Windows + Linux covering multi-monitor layouts
  and run-mode toggles.

---

## 4) Rationale

Avalonia satisfies the modernization goals of §13 — portability, accessibility, and
metadata-driven expansion — without leaving the .NET ecosystem.  
Qt/C++ and hybrid web dashboards introduce additional toolchains and dependency
management overhead, while Avalonia delivers cross-platform parity within the same
language, runtime, and CI infrastructure already adopted for Core and AgIO.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|----------|---------|
| **Maintain WinForms only** | Continue shipping the existing Windows-only UI. | **Rejected** — fails cross-platform and modernization requirements (§13). |
| **Qt/C++ desktop rewrite** | Adopt Qt for a native cross-platform UI. | **Rejected** — high maintenance cost, separate toolchain, and poor reuse of existing code. |
| **Web/Electron shell** | Implement browser-based or hybrid UI. | **Rejected** — offline and hardware-access constraints unsuitable for in-cab operation. |

---

## 6) Governance

- **Ownership:** UI Working Group  
- **Review Cadence:** Quarterly UX reviews or upon Avalonia LTS releases  
- **Success Metrics:** Accessibility compliance, input latency/FPS benchmarks,
  metadata-widget adoption metrics  
- **Retirement Plan:** Deprecate WinForms once Avalonia meets parity and operator
  acceptance milestones; publish fallback documentation for legacy systems  

---

## 7) Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-20 | Initial draft adopting Avalonia as desktop shell. | Nexus Team (Codex) |  |
| 2025-10-25 | Replaced WPF references with Qt lineage, aligned context and governance. | Nexus Team (Codex) |  |
