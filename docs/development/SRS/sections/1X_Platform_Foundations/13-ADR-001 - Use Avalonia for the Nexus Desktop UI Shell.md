# 13-ADR-001 — Use Avalonia for the Nexus Desktop UI Shell

*(Status: Accepted — 2025-10-25)*

**Authors:** UI Working Group  
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `13_UI_Framework_UX.md`
**Related Decisions:** `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `11-ADR-001 — Establish Windows & Linux Support Baseline`

---

## 1) Context

Section 13 documents how Nexus manages its presentation layer while modernising
beyond the legacy WinForms UI. Operators still rely on WinForms today, but the
SRS requires a cross-platform shell, metadata-driven widgets, and run modes that
work on Windows and Linux desktops with future expansion to Android (full-stack)
and optional iOS/web companions. Previous WPF experiments stalled, and
alternatives such as Qt/C++ or web shells would fragment engineering skill sets
and slow the transition. Avalonia provides a .NET-friendly toolkit that runs on
Windows, Linux, and the mobile platforms targeted for future expansion, matching
the runtime adopted in `12-ADR-001` without forcing a rewrite of view models or
bindings.

---

## 2) Decision

Use Avalonia as the primary desktop shell for Nexus while WinForms remains the
fallback UI during the transition period. Avalonia projects host the shared view
models, theming system, and metadata-driven dashboards described in Section 13.
Mobile or web companions MAY share view models or services, but desktop polish
must wrap the Avalonia shell rather than reanimate retired WPF panels.

**Scope limitations.** This ADR governs desktop UI policy for Section 13 only.
Mobile companions, build tooling, and OS support matrices are addressed in their
respective sections.

---

## 3) Consequences

**Positive impacts**

- Enables a single UI codebase across Windows and Linux today while supporting
  future Android builds using the same XAML/view-model stack.
- Supports metadata-driven dashboards and run-mode toggles without maintaining
  multiple UI frameworks.
- Preserves C# and XAML expertise already present in the contributor base.

**Negative/mitigated impacts**

- Contributors must learn Avalonia patterns; mitigated by providing templates,
  guidelines, and samples alongside Section 13 assets.
- GPU capabilities differ per platform; mitigated by benchmarking touch, input
  latency, and render performance for the devices listed in Section 13.
- WinForms must stay healthy until Avalonia reaches feature parity; mitigated by
  gating WinForms retirement on the acceptance criteria documented in Section 13.
  Companion/mobile/web shells may lag desktop features and require bridging.

**Follow-up actions**

- Publish Avalonia project scaffolds with metadata-driven widget examples.
- Document theming, accessibility, and layout policies in the Section 13
  reference material.
- Schedule quarterly UX smoke tests that exercise multi-monitor layouts and
  run-mode toggles on both Windows and Linux.

---

## 4) Rationale

Avalonia satisfies the Section 13 goals of portability, metadata-driven UI
expansion, and accessible theming without abandoning existing tooling. Qt/C++
introduces a full-stack rewrite, and a web-first approach cannot meet the
hardware access requirements of desktop operators. Staying in the .NET ecosystem
also lets the UI reuse shared libraries, validation logic, and telemetry hooks.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|---------|---------|
| Maintain WinForms only | Keep WinForms as the sole UI and incrementally patch UX gaps. | Rejected — fails portability and metadata-driven goals in Section 13. |
| Qt/C++ desktop rewrite | Adopt Qt for cross-platform UI. | Rejected — high rewrite cost and limited reuse of existing view models. |
| Web/Electron shell | Host the UI in a browser engine. | Rejected — hardware access and offline requirements cannot be met without complex bridges. |

---

## 6) Governance

- **Ownership:** UI Working Group.
- **Review cadence:** Quarterly UX assessments or when Avalonia LTS versions are
  released.
- **Success metrics:** Accessibility checklist compliance, benchmarked input
  latency/FPS, and metadata widget adoption metrics captured for Section 13.
- **Retirement plan:** Once Avalonia meets parity milestones, deprecate WinForms
  with a documented fallback path for operators who need the legacy shell.

---

## 7) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial adoption of Avalonia shell. | Codex |
| 2025-10-25 | Scoped ADR to Section 13 and updated governance. | UI Working Group |

