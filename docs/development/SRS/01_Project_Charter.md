# AgOpenGPS Nexus Project Charter
*(Status: Draft for Steering Review)*

## Document Control
- **Version:** 0.4.0
- **Authors:** Nexus Team
- **License:** GPLv3
- **Reviewers:** Platform Foundations Working Group, UI Working Group, Release Working Group
- **Approval Authority:** Nexus Program Steering Committee
- **Review Cycle:** Ad-hoc when scope/assumptions shift materially
- **Created:** 2025-10-20
- **Last Updated:** 2025-10-24
- **Related Specifications:** `sections/1X_Platform_Foundations/11_OS_Support.md`, `sections/1X_Platform_Foundations/12_Development_Language_Runtime.md`, `sections/1X_Platform_Foundations/13_UI_Framework_UX.md`

---

## 1. Executive Summary
AgOpenGPS Nexus rebuilds the guidance platform on a maintainable, cross-platform foundation. The charter affirms disciplined parity with AgOpenGPS v6, targeted investments in build and test automation, and governance that keeps community contributions focused on reliability before new feature expansion.

Key outcomes this charter commits to delivering:
- Unified runtimes, packaging, and UI shells for Windows and Linux environments aligned with SRS §11 and §13.
- Clear architectural seams between Core logic, AgIO hardware services, and Avalonia presentation layers.
- Tooling and documentation that shorten onboarding for new contributors while preserving operator confidence earned through v6 deployments.

---

## 2. Mission & Vision
**Mission:** The mission of AgOpenGPS Nexus is to create a modern, cross-platform foundation that keeps current operators productive while opening the door for unrestricted innovation. Nexus replaces rigid, centralized development with an open plugin ecosystem—where anyone can build, modify, or share features without bureaucracy, forks, or dependency lock-in. Our goal is to make extending AgOpenGPS as easy as publishing an Arduino library or WordPress plugin.

### **Vision**
Nexus becomes the canonical, community-driven guidance platform that:
- Operates reliably across Windows x64, Linux x64, and Linux ARM64 with a consistent, high-quality operator experience.
- Preserves and rationalizes the proven strengths of v6 through automated testing and real-world field validation.
- Establishes clear, documented boundaries between Core services, system logic, and UI shells — empowering teams to extend and experiment with confidence.
- Enables innovation without friction through clear onboarding, reproducible builds, and lightweight, community-led governance.
- Cultivates a thriving plugin ecosystem where ideas evolve openly instead of fragmenting into forks.

---

## 3. Guiding Principles & Guardrails
- **Foundation first:** Modernize runtimes, architecture, and tooling before expanding feature scope.
- **Parity with intent:** Document all deviations from v6 behavior in ADRs, including mitigation plans and operator validation.
- **Modular and inspectable:** Preserve the community-driven spirit through extensible, transparent components.
- **Inclusive contribution model:** Keep workflows accessible across Windows and Linux, avoiding reliance on proprietary tooling.
- **Deterministic validation:** Enforce automated and replayable tests that expose regressions before they reach the field.


---

## 4. Goals & Success Criteria
- **G1 — Maintain critical functional parity with AgOpenGPS v6.**  
  Priority field scenarios (guidance, GNSS processing, autosteer, implement control) must pass automated regression suites and targeted field validations. Any intentional retirements are documented in ADRs with operator approval.
- **G2 — Achieve cross-platform deployment.**  
  Builds for Windows x64, Linux x64, and Linux ARM64 share a unified codebase with only packaging differences. Installers and packages must smoke-test cleanly on fresh system images.
- **G3 — Deliver a responsive, accessible Avalonia UI shell.**  
  v6 workflows are retained with UX refinements, maintain 30 FPS rendering on reference hardware, and pass accessibility review sign-off.
- **G4 — Establish comprehensive automated testing.**  
  Unit and integration suites enforce coverage thresholds, and simulated field operations run pre-merge to preserve determinism and latency budgets.
- **G5 — Clarify architecture boundaries.**  
  Contracts cleanly separate Core, AgIO, and UI layers, with ADR-backed APIs and analyzers enforcing those separations.
- **G6 — Improve contributor onboarding.**  
  Setup guides for supported OS baselines complete in under one hour and publish contribution checklists that align with CI expectations.

---

## 5. Non-Goals (Foundation Phase Boundaries)
The foundation phase emphasizes modernization and modularity while deferring higher-level features until a stable core is proven. These areas are not primary objectives:
- **Cloud and data sync:** No built-in cloud synchronization or AgShare integration beyond defined extension points for future or third-party solutions.
- **Mobile clients:** Native Android or iOS builds are not part of the core roadmap, though contributors may explore them independently if low-impact.
- **Fleet and compliance features:** Fleet coordination, ISO certification, and other regulatory deliverables are deferred until after initial field validation.
- **Firmware compatibility:** Communication protocols may evolve, but legacy module compatibility must be retained through PGN support and compatibility layers.
- **Legacy data migration:** Backward compatibility with v5 or earlier formats is limited to published migration tooling.

Nexus is a ground-up rewrite focused on modernization, modularity, and maintainability—while preserving proven field performance wherever practical.

---

## 6. Scope
### 6.1 In Scope
- Unified .NET LTS runtime and Avalonia UI adoption per ADR-001, with review checkpoints as new frameworks mature.
- Packaging pipelines for Windows installers, Linux packages, containers, and systemd units supporting headless deployments.
- AgIO service updates covering serial, UDP, and CAN integrations, plus GNSS/IMU data handling focused on open formats rather than vendor specifics.
- Core guidance refactoring that isolates business rules, simulation hooks, and deterministic behaviors.
- Modernized AOG-Link V1 communication layer with maintained backward compatibility for legacy AOG-Link V0 modules through PGN support.
- Documentation refresh for architecture, onboarding, operator workflows, and contributor governance.

### 6.2 Out of Scope
- Alternative rendering stacks beyond Avalonia and supported graphics APIs.
- Dedicated mobile UI frameworks or AR/VR interfaces.
- Cloud analytics or data pipelines beyond telemetry hooks for diagnostics.

---

## 7. Stakeholders & Governance
AgOpenGPS Nexus is a community-driven effort built on shared ownership rather than formal hierarchy. Roles describe areas of focus, not authority.

| Role | Responsibilities | Named Group |
|------|------------------|-------------|
| Core Maintainers | Coordinate releases, review major contributions, and keep the project aligned with its charter. | Nexus Maintainers |
| Area Leads | Shepherd development within focus areas such as Core, AgIO, UI, or Packaging. | Area Contributors |
| Field Testers | Validate new builds in real-world conditions, report regressions, and confirm operator usability. | Volunteer Testers |
| Documentation & Support | Maintain guides, onboarding docs, and community FAQs. | Docs & Onboarding Team |
| Community Contributors | Propose features, submit pull requests, and share plugin or hardware ideas. | Open Community |

Governance is based on transparent discussion, consensus, and documented decisions through ADRs and SRS updates.  
Regular syncs are informal and community-led—typically via GitHub issues, Telegram, or live chats when active development spikes.

---

## 8. Risks & Mitigations
| ID | Risk | Likelihood | Impact | Mitigation / Contingency |
|----|------|------------|--------|--------------------------|
| R1 | **Linux graphics or GPU performance issues on CM5 and ARM64 hardware.** | Medium | High | Benchmark early on reference devices, offer software rendering fallback, and delay ARM64 GA until performance is acceptable. |
| R2 | **Device or GNSS integrations differ between Windows and Linux.** | Medium | High | Standardize on open formats (NMEA, UBX, CAN), expand AgIO adapters, and document per-device quirks as part of compatibility testing. |
| R3 | **Volunteer time and momentum fluctuate.** | High | Critical | Keep scope manageable, rotate ownership where possible, and recognize contributions publicly to sustain engagement. |
| R4 | **Feature creep distracts from core modernization.** | High | High | Track new ideas as plugin candidates or post-foundation enhancements, and regularly revisit non-goals to stay focused. |
| R5 | **Migration of v6 configurations or data causes regressions.** | Low | High | Build migration tooling early, include reversible imports, and maintain v6 interoperability during transition. |
| R6 | **UI or UX changes frustrate operators used to v6 workflows.** | Medium | High | Gather operator feedback before releases, document major UX differences, and preserve familiar workflows where possible. |
| R7 | **Build or CI/CD infrastructure becomes unreliable or costly.** | Low | Medium | Use open-source runners and FOSS credits, cache dependencies, and simplify matrix testing where possible. |
| R8 | **Hardware variability across farms makes validation inconsistent.** | Medium | Medium | Encourage diverse field testing, log environment details in feedback, and prioritize reproducible bug reports. |

---

## Appendix A — Revision History
| Version | Date | Changes | Author |
|---------|------|----------|--------|
| 0.4.0 | 2025-10-24 | Major rewrite for clarity and realism: simplified governance, reframed risks, modernized mission and vision to reflect community-led development. | Nexus Team (Jon Fortney) |
| 0.3.2 | 2025-10-23 | Streamlined charter to emphasize mission, guardrails, goals, and scope; removed process-specific execution details. | Nexus Team (Codex) |
| 0.3.1 | 2025-10-22 | Consolidated charter with vision guardrails and baseline assumptions. | Nexus Team (Codex) |
| 0.3.0 | 2025-10-22 | Expanded goals, scope, and governance based on Next charter lessons learned. | Nexus & Next Teams (Codex + Jon Fortney) |
| 0.2.0 | 2025-10-21 | Community review update incorporating steering feedback. | Next Team (Markus Nuuja) |
| 0.1.0 | 2025-10-20 | Initial draft aligning with SRS foundations. | Nexus Team (Codex) |

*End of document.*
