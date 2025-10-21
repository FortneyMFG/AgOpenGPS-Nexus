# 13 — UI Framework & UX Language
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 13  
**Editors:** UI Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 11 — OS Support, 9X — Frontends & Ops  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 9X — Frontends & Ops, Training materials

---

## 13.1 Purpose & Scope

Define the presentation technologies, layout systems, and UX policies for Nexus desktop and companion clients.
Balance legacy WinForms/WPF expectations with modernization via Avalonia and remote client strategies.

---

## 13.2 Context

- WinForms remains the production UI for operators; WPF shell in development.
- Remote clients and metadata-driven dashboards require cross-platform components.
- Avalonia pilots aim to share view models across Windows/Linux and mobile shells.
- UI stack must coexist with headless Core deployments connected through gRPC or WebSockets.

---

## 13.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| WinForms UI | Primary operator interface with OpenGL panels. | Windows-only, limited touch support. | Maintain compatibility while introducing cross-platform Avalonia shell. | Production UI |
| WPF Shell | Modernized Windows shell prototype. | Incomplete; still Windows-only. | Reuse view models within Avalonia for dual-first strategy. | WPF branch |
| Configuration UX | Manual wiring of dashboards and inspectors. | Slow to surface new layers/metrics. | Adopt metadata-driven dashboards (O-UI-5). | UX backlog |

---

## 13.4 Definitions

| Term | Definition |
|------|-------------|
| Companion Mode | Remote UI connecting to headless Core via network transport. |
| Metadata-driven UI | Dynamic dashboards derived from schema/metadata rather than hard-coded panels. |
| Run Mode | Operating mode toggles: CompanionRemote, LocalInProc, LocalOutOfProc. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** specify mandatory UI obligations.
> - **SHOULD / SHOULD NOT** highlight strong recommendations.
> - **MAY** identifies optional capabilities or roadmap items.

## 13.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|-----------------|-----------------------------|
| R-UI-000 | MUST | Legacy Support | Keep WinForms desktop UI shipping with mapping, PGN tools, OpenGL panels. | Legacy operators | Windows regression suite |
| R-UI-001 | MUST | Modern Shell | Continue WPF shell development targeting Windows desktop. | UI WG | WPF build + smoke tests |
| R-UI-002 | SHOULD | AgIO Config | Maintain AgIO Windows Forms dialogs for device setup. | AgIO maintainers | UI automation on dialogs |
| R-UI-003 | SHOULD | Multi-monitor | Preserve window placement helpers for multi-monitor cabs. | Operator feedback | UI layout tests |
| R-UI-004 | SHOULD | Metadata Widgets | Provide metadata-driven widgets to surface new layers without code rewrites. | Metadata dashboards backlog | Prototype dashboards hitting feature checklist |
| R-UI-005 | SHOULD | Remote Clients | Enable frontends that attach to headless Core via gRPC/Web transport. | Remote client plan | End-to-end remote client demo |
| R-UI-006 | COULD | Cross-platform Stacks | Evaluate kiosk-friendly cross-platform stacks (Qt, Avalonia, Web). | Linux Core roadmap | Comparative spike reports |
| R-UI-007 | SHOULD | Accessibility | Support high-DPI scaling, contrast presets, localization hooks. | Accessibility WG | Accessibility test matrix |
| R-UI-008 | MUST | Shared Mobile Shell | Keep Avalonia project free of platform-specific forks for mobile builds. | ADR-003 Avalonia UI | Mobile CI builds |
| R-UI-009 | SHOULD | Run-mode Toggles | Provide configuration surface for run-mode switching. | ADR-003 Avalonia UI | QA scenarios covering run modes |

### 13.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-UI-000 | Production deployments | Preserve current operator workflows during transition. |
| R-UI-004 | Metadata-driven dashboards option (9X) | Accelerate UI iteration without code changes. |
| R-UI-005 | Linux Core roadmap | Ensure headless deployments still deliver UX. |
| R-UI-008 | ADR-003 Avalonia UI | Keep shared codebase across desktop/mobile. |

---

## 13.6 Acceptance Criteria & Verification

- WinForms and WPF builds pass smoke tests with multi-monitor layout validation.
- Metadata dashboard prototypes demonstrate dynamic widget loading.
- Remote client demo proves gRPC transport viability for CompanionRemote mode.

### 13.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-UI-000 | Regression suite | `tests/ui/winforms-smoke/` | All scenarios pass |
| R-UI-004 | Prototype demo | `demos/ui/metadata-dashboard/` | Checklist complete |
| R-UI-005 | Integration test | `tests/ui/remote-client/` | Connects to headless Core without errors |
| R-UI-008 | CI build | `pipelines/ui-avalonia.yml` | Android/iOS builds succeed |

---

## 13.7 Constraints

- Maintain compatibility with existing WinForms OpenGL renderer until Avalonia reaches parity.
- Ensure UI toolkits comply with cross-platform GPU requirements (OpenGL 3.3+).
- Keep localization and accessibility requirements consistent across shells.

### 13.7.1 Non-Functional Requirement Classes

- **Performance:** Input latency, render FPS, UI startup time.
- **Usability:** Touch ergonomics, multi-monitor behavior, accessibility.
- **Portability:** Windows x64, Linux x86_64/ARM64, Android/iOS companion builds.
- **Maintainability:** Shared view models, limited platform-specific forks.

---

## 13.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-13-1 | Avalonia theming/performance gaps. | Medium | Run pilots on Windows + Linux; keep WinForms fallback. | @ui |
| RISK-13-2 | Metadata-driven dashboards overwhelm operators. | Low | Provide presets + training materials. | @ux |
| ISSUE-13-1 | Decide timeline for WPF shell support level. | Medium | Capture milestones in release plan. | @ui |
| ISSUE-13-2 | Validate run-mode toggles UX for QA. | Medium | Prototype configuration workflow. | @qa |

---

## 13.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | WinForms continuity | Keep existing UI operational during modernization. |
| C2 | Cross-platform shell | Avalonia path to share UI across OS/mobile. |
| C3 | Metadata dashboards | Dynamic layer discovery and inspector UX. |
| C4 | Remote/companion UX | Support remote clients with acceptable latency. |
| C5 | Accessibility baseline | High-DPI, color contrast, localization hooks. |
| C6 | Run-mode management | Streamline toggles between CompanionRemote/Local modes. |

### 13.9.1 Assumptions & Preconditions

- [A1] Rendering performance goals from Section 11 are achieved on Windows + Linux hardware.
- [A2] Metadata describing layers is maintained by mapping teams.
- [A3] Remote transport (gRPC/Web) remains consistent with Section 4X decisions.

---

## 13.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No dedicated Section 13 option documents; modernization leverages Option 11-O1 for runtime stack and 9X options for dashboard/remote UX. | — |

---

## 13.11 Comparison Matrix

| Attribute / Criteria | Legacy WinForms/WPF | Avalonia-based Shell (11-O1) |
|----------------------|---------------------|-----------------------------|
| Implementation Effort | Low (status quo) | Medium (new toolkit + theming) |
| Maintainability | Medium | High (shared code) |
| Touch/UX | Low | High |
| Portability | Low | High |
| Risk Level | Low | Medium |

---

## 13.12 Decision Matrix

UI framework selection follows the runtime decision (11-O1). Section 13 focuses on UX policies, metadata-driven dashboards, and remote client support.
Any future toolkit alternatives will require standalone option documents under Section 13.

---

## 13.13 Evaluation & Verification

- Conduct usability studies comparing WinForms vs. Avalonia shells.
- Validate metadata-driven UI flows in staging environment with operator feedback.
- Record latency metrics for remote clients (target ≤ 120 ms input round-trip).

---

## 13.14 Implementation Policy

- Maintain dual-build pipeline for WinForms and Avalonia until parity is declared.
- Publish UI theming guidelines and accessibility checklist for all shells.
- Require metadata schema updates to include presentation hints for dashboards.

---

## 13.15 Community Sentiment

- Operators request gradual transition; WinForms must remain stable until Avalonia proves parity.
- Contributors endorse Avalonia due to shared C# skill set and mobile ambitions.
- UX working group emphasizes metadata-driven approach to reduce manual dashboard wiring.

### 13.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted UI framework section to standardized template with updated considerations. | #0000 |

---

## 13.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-UI-000 | — | — | `tests/ui/winforms-smoke/` | `Legacy SourceCode -V6/SourceCode/GPS/` |
| R-UI-004 | 9X Consideration C3 | — | `demos/ui/metadata-dashboard/` | `docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations` |
| R-UI-005 | — | 11-ADR-001 | `tests/ui/remote-client/` | `deployment/companion/` |
| R-UI-008 | 11-O1 | 11-ADR-001 | `pipelines/ui-avalonia.yml` | `Nexus SourceCode/src/Aog.UI.Avalonia/` |

---

## 13.17 Conformance

An implementation conforms when legacy UI obligations are met, modernization requirements (metadata dashboards, remote clients, accessibility) show active verification, and shared runtime policies remain aligned with Section 11/12 decisions.

---

## Standards Context

Aligns with **ISO/IEC/IEEE 29148:2018** for UI requirement traceability and W3C accessibility guidelines (WCAG 2.1 AA) for operator-facing interfaces.
