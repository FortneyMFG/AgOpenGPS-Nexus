# AgOpenGPS Nexus Project Charter
*(Status: Draft for Steering Review)*

## Document Control
- **Version:** 0.3.1
- **Authors:** Nexus Program Office (Codex)
- **License:** GPLv3
- **Reviewers:** Platform Foundations Working Group, UI Working Group, Release Working Group
- **Approval Authority:** Nexus Program Steering Committee
- **Review Cycle:** Ad-hoc when scope/assumptions shift materially
- **Created:** 2025-10-20
- **Last Updated:** 2025-10-22
- **Related Specifications:**
  - `sections/1X_Platform_Foundations/11_OS_Support.md`
  - `sections/1X_Platform_Foundations/12_Development_Language_Runtime.md`
  - `sections/1X_Platform_Foundations/13_UI_Framework_UX.md`
- **Related Options & ADRs:**
  - `sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md`
  - `sections/1X_Platform_Foundations/11-ADR-001 - Adopt Unified .NET 8 Runtime & Avalonia Stack.md`
  - `sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md`
  - `sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md`
  - `sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md`
  - `sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md`

---

## 1. Executive Summary
AgOpenGPS Nexus rebuilds the platform foundations so operators can rely on familiar guidance workflows while the community advances to a modern, cross-platform stack. The charter emphasises disciplined parity with AgOpenGPS v6, investment in build/test automation, and governance that keeps contributions focused on stability before feature expansion.

Key outcomes this charter commits to delivering:
- Unified runtimes, packaging, and UI shells for Windows and Linux environments aligned with SRS §11 and §13.
- A modular architecture that cleanly separates Core guidance logic, AgIO hardware services, and Avalonia presentation layers.
- Tooling, documentation, and migration paths that shorten onboarding for new contributors while preserving field-proven confidence for current operators.

---

## 2. Mission & Vision Statement
**Mission:** Deliver AgOpenGPS Nexus on a maintainable, cross-platform foundation that keeps existing operators productive, invites new contributors, and supports incremental innovation without forcing disruptive rewrites in the future.

**Vision:** Within the first major release, Nexus becomes the canonical community-supported guidance platform that:
- Runs reliably on Windows x64, Linux x64, and Linux ARM64 hardware with consistent operator experiences.
- Preserves or intentionally rationalises core v6 capabilities (guidance accuracy, autosteer behaviour, implement management) validated through automated and field testing.
- Documents clean boundaries between Core logic, AgIO services, and UI shells so teams can extend the system with confidence.
- Reduces contribution friction by providing clear onboarding guides, repeatable builds, and community-supported governance.

This vision extends the guiding principles captured during the initial visioning cycle:
- Deliver a modernized AgOpenGPS experience that keeps offline field work resilient while enabling collaborative planning and telemetry.
- Preserve the community-driven spirit: modular, inspectable, and friendly to tinkering.
- Scale from hobby farms to commercial operations through configurable modules instead of forks.

### Strategic Objectives
- Reduce friction deploying to mixed Windows/Linux fleets.
- Unlock richer guidance and automation through consistent data models and APIs.
- Improve UX clarity for operators and integrators with multi-monitor and remote touch layouts.

---

## 3. Drivers & Timing (Why Now)
### 3.1 Current Challenges
- Windows-only deployment and legacy WinForms shells constrain hardware choices and long-term maintainability.
- Monolithic project structure increases coupling, slows testing, and blocks headless or remote scenarios.
- Contributor ramp-up remains slow due to fragmented documentation and inconsistent tooling.
- Hardware abstraction layers depend on Windows-centric drivers, complicating parity for Linux deployments.

### 3.2 Opportunities
- Cross-platform .NET (LTS) releases and Avalonia UI have matured enough for production scenarios without custom forks.
- The community maintains consensus that modernisation is required before layering new functionality.
- Field-tested v6 installations provide a stable behavioural baseline and user feedback loop.
- Expanded Linux usage (Raspberry Pi, rugged tablets) offers cost and deployment benefits once parity is achieved.

### 3.3 Foundation-First Guardrails
- Focus on runtime, architecture, and tooling upgrades before adding user-visible features.
- Capture deviations from v6 behaviour in ADRs with mitigation plans before adoption.

---

## 4. Goals & Success Criteria
| ID | Goal | Success Criteria | Priority | SRS Trace |
|----|------|------------------|----------|-----------|
| G1 | Maintain critical functional parity with AgOpenGPS v6. | 100% of priority field scenarios (guidance, GNSS processing, autosteer, implement control) pass automated regression suites and targeted field shakedowns; any intentional retirements documented with operator sign-off. | P0 (Critical) | §11.5, §11.9 |
| G2 | Achieve cross-platform deployment. | Validated builds for Windows x64, Linux x64, and Linux ARM64 share >98% code with only platform-specific packaging adjustments; installer/packaging smoke tests pass on fresh images. | P0 (Critical) | §11.5 |
| G3 | Deliver a responsive, accessible Avalonia UI shell. | Feature-complete parity with v6 WinForms workflows, 60 FPS rendering on reference hardware, accessibility review completed. | P0 (Critical) | §13.5 |
| G4 | Establish comprehensive automated testing. | Unit test coverage ≥80% on Core/AgIO, integration tests cover all critical IO paths, simulated field ops run in CI pre-merge. | P0 (Critical) | §11.6 |
| G5 | Clarify architecture boundaries. | Documented contracts separating Core, AgIO, and UI; ADR-backed APIs published; enforcement via build analyzers. | P0 (Critical) | §11.7 |
| G6 | Enable headless and remote operation. | Core + AgIO run without local UI; remote client latency ≤50 ms under benchmark conditions. | P1 (High) | §11.5, §13.5 |
| G7 | Improve contributor onboarding. | Setup guides validated on Windows and Linux complete in <1 hour; contribution checklist published. | P2 (Medium) | §11 intro |
| G8 | Validate community readiness. | ≥20 operator evaluations across ≥3 regions report readiness for production rollout before GA. | P1 (High) | §13.3 |

---

### Success Measures
- Community consensus on each critical architecture slice captured in ADRs.
- Reference implementations for headless + remote UI scenarios validated in field tests.
- Contributor onboarding reduced to <1 hour setup on supported OS baselines.

---

## 5. Non-Goals (Out of Scope for Foundation Phase)
The foundation phase explicitly excludes:
- Cloud sync, AgShare integration, or other remote data services beyond establishing extensible interfaces.
- Mobile-native clients (iOS/Android); support is limited to remote desktop or thin-client experiences.
- Fleet coordination, ISO certification, or regulatory compliance deliverables.
- Large-scale refactors of existing hardware firmware unless required for cross-platform parity.
- Backward compatibility with v5 or earlier data formats outside published migration tooling.

The vision guardrails also reaffirm that we are not:
- Rewriting proven algorithms without demonstrated benefit.
- Supporting proprietary, license-restricted toolchains that exclude community contributors.
- Guaranteeing certification for regulated markets in the first iteration.

Feature candidates deferred from this phase must be captured in the enhancement backlog with clear dependencies on the new foundation.

---

## 6. Scope Definition
### 6.1 In Scope
- Unified .NET LTS runtime and Avalonia UI adoption per ADR-001 and ADR-003, with guardrails to revisit newer frameworks as they stabilise.
- Packaging pipelines for Windows installers, Linux packages, containers, and systemd service definitions supporting headless deployments.
- AgIO abstraction updates covering serial, UDP, and CAN bus integrations, plus GNSS/IMU handling focused on data formats rather than vendor-specific implementations.
- Core guidance logic refactoring to isolate business rules, simulation hooks, and deterministic behaviours.
- Documentation refresh for architecture, onboarding, operator workflows, and contributor governance.

### 6.2 Out of Scope
- Experimental rendering stacks beyond Avalonia + supported graphics APIs.
- Dedicated mobile UI frameworks or AR/VR interfaces.
- Net-new data pipelines (cloud sync, analytics) beyond stubbed interfaces and telemetry hooks required for diagnostics.
- Wholesale rewrites of validated v6 hardware drivers without parity justification.

---

## 7. Key Deliverables
| Deliverable | Description | Acceptance Criteria |
|-------------|-------------|---------------------|
| D1 — Nexus Core Library | Cross-platform business logic assembly encapsulating kinematics, GNSS, and field management. | ≥80% unit test coverage, deterministic simulation runs recorded. |
| D2 — AgIO Services | Hardware abstraction services for Windows/Linux deployments. | Parity validation for prioritized GNSS receivers and autosteer controllers; restartable service model documented. |
| D3 — Avalonia UI Shell | Cross-platform desktop UI replicating v6 workflows with accessibility updates. | Operator advisory group sign-off; 60 FPS benchmark on reference hardware. |
| D4 — Packaging Matrix | Installers, Linux packages, containers/AppImages with automated smoke tests. | Successful deployment on fresh OS images; CI artifacts signed and archived. |
| D5 — Automated Test Suites | Unit, integration, and simulated field tests integrated into CI. | All critical scenarios gated pre-merge with published runbooks. |
| D6 — Documentation Set | Architecture docs, operator manuals, contributor onboarding. | Published in docs portal with accessibility review; change log maintained. |
| D7 — Migration Toolkit | Tools and guides for v6 → Nexus data migration. | Field boundaries, vehicle configs, and settings transfer verified with rollback instructions. |

---

## 8. Stakeholders & Governance
| Role | Responsibilities | Named Group |
|------|------------------|-------------|
| Executive Sponsor | Approves charter, allocates resources, resolves cross-team escalations. | Nexus Program Steering Committee |
| Program Lead | Maintains roadmap, coordinates dependencies, enforces foundation-first guardrails. | Platform Foundations Working Group |
| Technical Leads | Own Core, AgIO, UI, and packaging delivery; steward ADRs. | Area Owners (Core, AgIO, UI, Release) |
| Quality & Safety | Define validation protocols, oversee field testing, track regressions. | Safety & QA Team |
| Community & Operator Advocates | Gather feedback, coordinate beta participation, manage documentation clarity. | Community Relations |

**Governance Cadence**
- Bi-weekly program sync reviewing milestone burndown, risks, and scope adjustments.
- Monthly ADR/SRS alignment audit ensuring documentation matches implementation.
- Freeze window reviews per contribution guide, capturing approved exceptions and mitigation steps.

---

## 9. Milestones & High-Level Timeline
| Milestone | Target Window | Description | Key Dependencies |
|-----------|---------------|-------------|------------------|


---

## 10. Dependencies & Assumptions
### 10.1 Technical Dependencies
- Stable .NET LTS runtime with long-term support through planned GA release.
- Avalonia UI releases with production-ready support for Windows and Linux desktops.
- gRPC-based inter-process communication between Core, AgIO, and UI layers.
- CI infrastructure (GitHub Actions or equivalent) capable of parallel OS builds and hardware-in-the-loop hooks.
- Hardware benches including representative GNSS receivers, IMUs, autosteer controllers, and display units for validation.

### 10.2 Community Dependencies
- Sustained contributor participation across Core, UI, and hardware abstraction areas.
- Field testers willing to exercise pre-release builds and share diagnostic data.
- Documentation reviewers ensuring accessibility and localisation needs are met.

### 10.3 Key Assumptions
- AgOpenGPS v6 remains the behavioural reference until Nexus GA.
- No disruptive hardware API changes from key vendors during foundation development.
- Windows 10/11 and supported Linux distributions maintain API stability required by Avalonia and device drivers.
- Community endorses the foundation-first roadmap and defers feature requests until guardrails lift.

### 10.4 Baseline Field Assumptions
- **GNSS Accuracy:** Sub-5 cm RTK guidance accuracy for auto-steer workloads, with fallbacks documented for WAAS/EGNOS-grade receivers.
- **Compute:** Quad-core 2.0 GHz CPU (x86_64 or ARM64), 8 GB RAM, and GPU supporting OpenGL 3.3 with 2 GB VRAM to sustain 60 FPS rendering and replay diagnostics.
- **Latency Envelope:** Control loops expect <100 ms end-to-end latency; monitoring dashboards tolerate up to 500 ms while buffering offline.

---

## 11. Risks & Mitigations
| ID | Risk | Likelihood | Impact | Mitigation / Contingency |
|----|------|------------|--------|--------------------------|
| R1 | Linux GPU drivers underperform on Raspberry Pi/CM5 hardware. | Medium | High | Benchmark early, tune rendering fallbacks, stage ARM64 GA if required. |
| R2 | Vendor GNSS/CAN integrations lack Linux parity. | Medium | High | Prioritize protocol-level support (NMEA, UBX, etc.), expand AgIO adapters, phase rollouts by device class. |
| R3 | Volunteer capacity drops or burns out. | High | Critical | Rotate module leads, scope work into two-week increments, celebrate progress openly. |
| R4 | Scope creep undermines foundation focus. | High | High | Enforce ADR/charter guardrails, maintain enhancement backlog, reaffirm non-goals publicly. |
| R5 | Data migration introduces regressions. | Low | High | Develop migration tooling early with reversible paths; keep v6 installs supported during transition. |
| R6 | UI/UX regressions erode operator trust. | Medium | High | Maintain operator advisory group, run usability reviews |
| R7 | CI/CD costs or complexity exceed budget. | Low | Medium | Optimise pipelines, leverage FOSS credits, prioritise essential matrix coverage. |
| R8 | Legal/licensing issues surface in dependencies. | Very Low | High | Audit third-party components, maintain MIT/GPL compatibility, secure legal consultation when needed. |

---

## 12. Resource & Budget Considerations
- **Human Resources:** Core development, AgIO integration, UI/UX design, QA/field testing, documentation, and DevOps roles allocated per milestone with rotating leads.
- **Infrastructure:** Budget for CI runners, hardware benches (GNSS receivers, IMUs, autosteer controllers), and accessibility testing devices.
- **Funding Model:** Combination of community contributions, sponsorships, and in-kind hardware loans tracked via Steering Committee reporting.

---

## 13. Quality Assurance & Testing Strategy
- Baseline checks: `dotnet build`, `dotnet test`, and `nexus sim smoke` executed on every merge request.
- Automated unit and integration suites enforced via CI with coverage gates per Goal G4.
- Simulated field operations run nightly with regression tracking for guidance accuracy and latency.
- Hardware-in-the-loop testing scheduled before major releases covering GNSS receivers, autosteer controllers, and IO peripherals.
- Issue intake process captures telemetry packages and reproduction steps to accelerate triage.

---

## 14. Communication & Reporting Plan
- **Weekly:** Async status updates summarising milestone progress, blockers, and upcoming decisions.
- **Monthly:** Stakeholder reviews covering performance metrics, packaging readiness, and operator feedback.
- **Quarterly:** Community calls presenting roadmap updates, risk posture, and requests for beta participation.
- **Documentation Updates:** Published via release notes, developer mailing lists, and docs site changelog.
- **Freeze Notifications:** Broadcast at least one week prior, with exception handling documented per contribution guide.

---

## 15. Migration & Adoption Plan
- Publish migration guides, walkthrough videos, and troubleshooting FAQs for v6 → Nexus transitions.
- Provide dual-write or export tooling allowing operators to test Nexus while retaining v6-compatible datasets.
- Offer support channels (forum, discussions) dedicated to migration feedback and issue tracking.
- Document rollback procedures for operators needing to return to v6 during pilot phases.

---

## 16. Approval & Next Steps
1. Secure sign-off from Executive Sponsor and Working Group leads.
2. Publish charter within the SRS repository and link from onboarding materials.
3. Align `tasks.md` backlog items with charter milestones and assign accountable owners.
4. Initiate milestone tracking with quarterly retrospectives and SRS/ADR synchronisation checkpoints.

---

## Appendix A — Revision History
| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.3.1 | 2025-10-22 | Consolidated charter with vision guardrails and baseline assumptions. | Nexus Program Office (Codex) |
| 0.1.0 | 2025-10-20 | Initial draft aligning with SRS foundations. | Nexus Program Office (Codex+Jon Fortney) |
| 0.2.0 | 2025-10-21 | Community review update incorporating steering feedback. | Next Program Office (Markus Nuuja) |
| 0.3.0 | 2025-10-22 | Expanded goals, scope, and governance based on Next charter lessons learned. | Nexus Program Office (Codex+Jon Fortney) |

*End of document.*
