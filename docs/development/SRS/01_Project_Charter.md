# AgOpenGPS Nexus Project Charter
*(Status: Draft for Steering Review)*

## Document Control
- **Version:** 0.4.0
- **Authors:** Nexus Program Office (Codex)
- **License:** GPLv3
- **Reviewers:** Platform Foundations Working Group, UI Working Group, Release Working Group
- **Approval Authority:** Nexus Program Steering Committee
- **Review Cycle:** Ad-hoc when scope/assumptions shift materially
- **Created:** 2025-10-20
- **Last Updated:** 2025-10-23
- **Related Specifications:** `sections/1X_Platform_Foundations/11_OS_Support.md`, `sections/1X_Platform_Foundations/12_Development_Language_Runtime.md`, `sections/1X_Platform_Foundations/13_UI_Framework_UX.md`

---

## 1. Executive Summary
AgOpenGPS Nexus rebuilds the guidance platform on a maintainable, cross-platform foundation. The charter affirms disciplined parity with AgOpenGPS v6, targeted investments in build and test automation, and governance that keeps community contributions focused on reliability before new feature expansion.

Key outcomes this charter commits to delivering:
- Unified runtimes, packaging, and UI shells for Windows and Linux environments aligned with SRS §11 and §13.
- Clear architectural seams between Core guidance logic, AgIO hardware services, and Avalonia presentation layers.
- Tooling and documentation that shorten onboarding for new contributors while preserving operator confidence earned through v6 deployments.

---

## 2. Mission & Vision
**Mission:** Deliver AgOpenGPS Nexus on a cross-platform foundation that keeps current operators productive, invites new contributors, and supports incremental innovation without disruptive rewrites.

**Vision:** Nexus becomes the canonical community-supported guidance platform that:
- Runs reliably on Windows x64, Linux x64, and Linux ARM64 hardware with consistent operator experiences.
- Preserves or intentionally rationalises core v6 capabilities through automated and field validation.
- Documents clean boundaries between Core logic, AgIO services, and UI shells so teams can extend the system with confidence.
- Reduces contribution friction through clear onboarding guides, repeatable builds, and community-supported governance.

---

## 3. Guiding Principles & Guardrails
- **Foundation first:** Modernise runtimes, architecture, and tooling before expanding feature scope.
- **Parity with intent:** Capture deviations from v6 behaviour in ADRs with mitigation plans and operator sign-off.
- **Modular, inspectable code:** Preserve the community-driven spirit with extensible, reviewable components.
- **Inclusive contribution model:** Keep workflows accessible to Windows and Linux contributors and avoid proprietary tooling barriers.
- **Deterministic validation:** Maintain automation and replay guardrails that expose regressions before operators encounter them.

---

## 4. Goals & Success Criteria
- **G1 — Maintain critical functional parity with AgOpenGPS v6.** Priority field scenarios (guidance, GNSS processing, autosteer, implement control) pass automated regression suites and targeted field shakedowns; intentional retirements are documented with operator approval.
- **G2 — Achieve cross-platform deployment.** Builds for Windows x64, Linux x64, and Linux ARM64 share a common codebase with only packaging differences; installers and packages smoke-test cleanly on fresh images.
- **G3 — Deliver a responsive, accessible Avalonia UI shell.** v6 workflows land with UX refinements, 60 FPS rendering on reference hardware, and accessibility review sign-off.
- **G4 — Establish comprehensive automated testing.** Unit and integration suites enforce coverage gates, and simulated field operations run pre-merge to protect determinism and latency budgets.
- **G5 — Clarify architecture boundaries.** Contracts separate Core, AgIO, and UI layers; ADR-backed APIs and analyzers enforce the separations.
- **G6 — Improve contributor onboarding.** Setup guides for supported OS baselines complete in under one hour and publish contribution checklists that match CI expectations.

---

## 5. Non-Goals
The foundation phase explicitly excludes:
- Cloud sync, AgShare integration, or other remote data services beyond extensible interfaces.
- Mobile-native clients (iOS/Android); support is limited to remote desktop or thin-client experiences.
- Fleet coordination, ISO certification, or regulatory deliverables in the first release.
- Large-scale firmware rewrites unless required for cross-platform parity.
- Backward compatibility with v5 or earlier data formats outside published migration tooling.

The charter also reaffirms that we are not rewriting proven algorithms without demonstrated benefit or adopting license-restricted toolchains that exclude community contributors.

---

## 6. Scope
### 6.1 In Scope
- Unified .NET LTS runtime and Avalonia UI adoption per ADR-001 decisions, with review checkpoints as new frameworks stabilise.
- Packaging pipelines for Windows installers, Linux packages, containers, and systemd definitions that support headless deployments.
- AgIO abstraction updates covering serial, UDP, and CAN integrations plus GNSS/IMU handling focused on data formats instead of vendor specifics.
- Core guidance refactoring that isolates business rules, simulation hooks, and deterministic behaviours.
- Documentation refresh for architecture, onboarding, operator workflows, and contributor governance.

### 6.2 Out of Scope
- Experimental rendering stacks beyond Avalonia and supported graphics APIs.
- Dedicated mobile UI frameworks or AR/VR interfaces.
- Net-new analytics or cloud data pipelines beyond telemetry hooks needed for diagnostics.
- Wholesale replacement of validated v6 hardware drivers without parity justification.

---

## 7. Stakeholders & Governance
| Role | Responsibilities | Named Group |
|------|------------------|-------------|
| Executive Sponsor | Approves charter, allocates resources, resolves cross-team escalations. | Nexus Program Steering Committee |
| Program Lead | Maintains roadmap, coordinates dependencies, enforces foundation-first guardrails. | Platform Foundations Working Group |
| Technical Leads | Own Core, AgIO, UI, and packaging delivery; steward ADRs. | Area Owners (Core, AgIO, UI, Release) |
| Quality & Safety | Define validation protocols, oversee field testing, track regressions. | Safety & QA Team |
| Community & Operator Advocates | Gather feedback, coordinate beta participation, manage documentation clarity. | Community Relations |

Governance cadence focuses on bi-weekly program syncs for scope and risk, plus monthly ADR/SRS alignment audits that keep documentation current.

---

## 8. Risks & Mitigations
| ID | Risk | Likelihood | Impact | Mitigation / Contingency |
|----|------|------------|--------|--------------------------|
| R1 | Linux GPU drivers underperform on Raspberry Pi/CM5 hardware. | Medium | High | Benchmark early, tune rendering fallbacks, stage ARM64 GA if required. |
| R2 | Vendor GNSS/CAN integrations lack Linux parity. | Medium | High | Prioritise protocol-level support (NMEA, UBX, etc.), expand AgIO adapters, phase rollouts by device class. |
| R3 | Volunteer capacity drops or burns out. | High | Critical | Rotate module leads, scope work into two-week increments, celebrate progress openly. |
| R4 | Scope creep undermines foundation focus. | High | High | Enforce ADR/charter guardrails, maintain enhancement backlog, reaffirm non-goals publicly. |
| R5 | Data migration introduces regressions. | Low | High | Develop migration tooling early with reversible paths; keep v6 installs supported during transition. |
| R6 | UI/UX regressions erode operator trust. | Medium | High | Maintain operator advisory group, run usability reviews. |
| R7 | CI/CD costs or complexity exceed budget. | Low | Medium | Optimise pipelines, leverage FOSS credits, prioritise essential matrix coverage. |

---

## Appendix A — Revision History
| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.4.0 | 2025-10-23 | Streamlined charter to emphasise mission, guardrails, goals, and scope; removed process-specific execution details. | Nexus Program Office (Codex) |
| 0.3.1 | 2025-10-22 | Consolidated charter with vision guardrails and baseline assumptions. | Nexus Program Office (Codex) |
| 0.3.0 | 2025-10-22 | Expanded goals, scope, and governance based on Next charter lessons learned. | Nexus Program Office (Codex+Jon Fortney) |
| 0.2.0 | 2025-10-21 | Community review update incorporating steering feedback. | Next Program Office (Markus Nuuja) |
| 0.1.0 | 2025-10-20 | Initial draft aligning with SRS foundations. | Nexus Program Office (Codex+Jon Fortney) |

*End of document.*
