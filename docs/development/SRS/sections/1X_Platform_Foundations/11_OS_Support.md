# 11 — Operating System Support
*(Status: drafting)*

**Section ID:** 11  
**Version:** 0.1.0  
**Authors:** Nexus Team (Fortney)  
**Last Updated:** 2025-10-24  
**Related Sections:** 12 — Development Language & Runtime, 14 — Build Environment & Tooling  
**Related Decisions:** `11-ADR-001 — Establish Windows & Linux Support Baseline`, `12-ADR-001 — Adopt .NET 10 LTS Runtime`, `13-ADR-001 — Adopt Avalonia 12 LTS for the Nexus Desktop UI Shell`  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 5X — Hardware I/O Device Layer, 9X — Frontends & Ops

---

## 11.1 Purpose & Scope

Define operating system (OS) coverage for Nexus.  
This section establishes expectations for where the application **must** or **should** run at the OS level, and how hardware I/O behaves consistently across supported platforms.

It also outlines the boundaries between the Core (backend/runtime) and UI (frontend) layers with respect to OS dependencies.

> **Purpose:**  
> To ensure consistent deployment, testing, and support targets across desktop, embedded, and mobile environments without constraining future portability.

---

## 11.2 Context

- Depends on the unified runtime defined in §12 (Development Language & Runtime) and the chosen cross-platform UI framework in §13.  
- Interacts with AgIO and related hardware I/O subsystems for serial, UDP, and CAN communication (see §5X).  
- OS-level considerations directly influence build tooling (§14) and deployment packaging (§15).  
- Out of scope: service/headless modes, containerization, mobile companion specifics, or packaging formats.


## 11.3 Legacy Comparison

Describe how legacy or prior implementations handled this capability.

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Deployment | Windows-only desktop installer. | No multi-OS distribution. | Add Linux desktop builds with similar install UX. | [Link] |
| Hardware I/O | Win32 serial/UDP and vendor CAN SDKs. | OS-specific code paths. | OS-agnostic I/O via AgIO abstraction. | [Link] |
| UI | Windows Forms. | Platform-locked UI code. | Cross-platform UI stack (see §12). | [Link] |

> **Informative:** Background only; does not impose requirements.

---

## 11.4 Definitions

| Term | Definition |
|------|-------------|
| **AgIO** | Core hardware I/O subsystem responsible for serial, UDP, and CAN communication. Provides a consistent abstraction layer across supported operating systems. |
| **Platform Tier** | Classification describing the level of official support (e.g., *Primary* for fully tested field targets, *Secondary* for companion or development use). |
| **Cross-Platform Runtime** | The managed runtime and supporting framework stack (e.g., .NET + UI toolkit) enabling builds for multiple desktop or embedded OS targets. |
| **Full Stack Support** | Deployment mode where both Core and UI run on the same device with direct hardware access. |
| **Companion UI Support** | Deployment mode where only the UI runs locally and connects to a remote Core over the network. |

---

> **Requirement Grammar (per [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119)):**  
> - **MUST / MUST NOT** — Mandatory; verifiable through test or inspection.  
> - **SHOULD / SHOULD NOT** — Strong recommendation; exceptions must be justified and documented.  
> - **MAY** — Optional; permitted when enabling conditions are clearly defined.  
>
> **Clarity Checklist:**  
> - Express each requirement as a single, testable behavior.  
> - Avoid compound statements.  
> - Use measurable or observable outcomes wherever possible.


## 11.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|----------------|-----------------------------|
| **R-OS-000** | MUST | Compatibility | Provide at least one **desktop-class operating system** build capable of running both Core and UI processes (Full-Stack mode). | Legacy baseline | Installer or package launches; UI renders; smoke tests pass. |
| **R-OS-001** | MUST | Hardware I/O | Support **Serial, UDP, and CAN** communication through AgIO consistently across all supported OS environments. | I/O parity goal | OS-specific loopback/device tests succeed. |
| **R-OS-002** | SHOULD | Portability | Demonstrate cross-platform parity by supporting at least one **additional OS** beyond the primary target. | Cross-OS goal | App launches; UI parity checklist passes. |
| **R-OS-003** | SHOULD | Deployment | Provide **signed or verifiable installation packages** for each supported OS tier. | Release hygiene | Signature / checksum verified in CI. |
| **R-OS-004** | SHOULD | Performance | Publish **minimum hardware guidance** and verify representative performance targets (e.g., frame-rate or latency benchmarks). | Operator guidance | Benchmark documentation meets stated thresholds. |
| **R-OS-005** | MAY | Mobility | Enable **interoperation with companion or mobile clients** via shared network contracts. | Interop note | Prototype handshake and discovery tests succeed. |

> **Normative:**  
> Each requirement must be objectively testable and traceable to at least one verification artifact (test, checklist, or benchmark).  
> Specific OS selections will be defined in *ADR 11-001 — Target OS Prioritization*.

---

### 11.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|---------|---------|-----------|
| **R-OS-000** | Legacy multi-platform intent | Ensure at least one desktop environment can host both Core and UI for field use. |
| **R-OS-001** | Cross-OS I/O need | Guarantee identical device behavior regardless of OS through AgIO abstraction. |
| **R-OS-002** | Portability goal | Validate framework independence and prevent vendor lock-in. |
| **R-OS-003** | Release policy | Standardize distribution and improve traceability and security. |
| **R-OS-004** | Operator clarity | Avoid under-powered hardware by publishing measurable guidance. |
| **R-OS-005** | Ecosystem extensibility | Enable future companion or remote-display implementations without redesign. |

---

## 11.6 Acceptance Criteria & Verification

Defines how compliance with § 11.5 requirements will be validated and documented.

**Verification Overview**
- At least one desktop OS build installs and completes baseline UI smoke test with no errors.  
- Cross-platform build(s) launch and pass parity checklist against reference environment.  
- AgIO serial/UDP/CAN loopback tests succeed on each supported OS tier.  
- Performance benchmarks meet or exceed published minimums on reference hardware.

### 11.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass / Fail Criteria |
|---------|------------------|---------------------|----------------------|
| **R-OS-000** | Installation & smoke test | *[to be linked: build/verify/install_desktop.md]* | Application installs, launches, and renders UI. |
| **R-OS-001** | I/O functional test suite | *[to be linked: tests/agio/io_matrix.csv]* | All serial/UDP/CAN channels pass loopback or hardware device checks. |
| **R-OS-002** | UI parity checklist | *[to be linked: tests/ui/ui_parity_checklist.md]* | All checklist items complete (✓) with no major regressions. |
| **R-OS-004** | Performance benchmark | *[to be linked: tests/perf/benchmarks.md]* | Meets or exceeds defined FPS / latency targets on reference scene. |

> **Informative:**  
> Verification artifacts may evolve into automated CI tasks as OS support decisions are finalized in ADR 11-001.



## 11.7 Constraints

- Must use the cross-platform runtime defined in §12.  
- Must avoid OS-specific forks in I/O logic; AgIO is the single abstraction.  
- Must respect platform code-signing/notarization requirements.

### 11.7.1 Non-Functional Requirement Classes

- **Performance:** render/frame targets, I/O latency.  
- **Reliability & Availability:** startup success, error handling for missing drivers.  
- **Security:** signed artifacts, trusted transports.  
- **Portability:** Windows x64; Linux x86-64 and ARM64.  
- **Maintainability:** minimize OS-conditionals outside AgIO.

---

## 11.8 Risks & Open Issues

| ID | Type | Description | Impact | Mitigation / Status | Owner |
|----|------|-------------|--------|---------------------|-------|
| **RISK-11-1** | Risk | Divergent driver and hardware interface support between Windows, Linux, and ARM variants. | Medium | Keep all hardware access abstracted behind AgIO; define reference devices and regression test set. | [TBD] |
| **RISK-11-2** | Risk | Inconsistent packaging or signing requirements across OS ecosystems. | Low | Centralize build and signing through unified CI pipelines; document per-platform release process. | [TBD] |
| **ISSUE-11-1** | Open Issue | Define which Linux distributions (e.g., Debian, Raspberry Pi OS, Ubuntu) are in official scope. | Medium | Capture decisions in ADR 11-001 and publish support matrix in §14 (Build Tooling). | [TBD] |
| **ISSUE-11-2** | Open Issue | Determine whether macOS and mobile OSes qualify as “secondary” or “companion-only” tiers. | Low | Track under ADR 11-001 and update SRS 11 once finalized. | [TBD] |

> **Informative:**  
> Risks represent uncertainties affecting feasibility or consistency of OS support.  
> Open issues track pending decisions to be resolved via ADR 11-001 (Target OS Prioritization) and §14 (Build Tooling).

---

## 11.9 Design Considerations

| ID | Consideration | Description |
|----|---------------|-------------|
| **C1** | Primary desktop baseline | At least one desktop-class OS serves as the baseline environment for field operators and validation. Specific OS selections will be defined in ADR 11-001. |
| **C2** | Cross-platform parity | Additional OS builds SHOULD provide equivalent functionality using the same codebase and shared runtime. |
| **C3** | Unified I/O | Serial, UDP, and CAN interfaces MUST remain OS-agnostic through the AgIO abstraction layer. |
| **C4** | Native packaging | Prefer platform-native installers or package formats; avoid custom loaders or scripts when a standard mechanism exists. |
| **C5** | Performance guidance | Publish minimum hardware targets and benchmark expectations for each officially supported platform tier. |

### 11.9.1 Assumptions & Preconditions

- **[A1]** The cross-platform UI framework renders consistently across all supported desktop OSes.  
- **[A2]** Required device classes (serial adapters, CAN interfaces, network sockets) are available or documented per OS.  
- **[A3]** Build tooling can produce native installers or packages per supported OS without manual intervention.  
- **[A4]** The AgIO layer abstracts any OS-specific driver or API differences.  


---
## 11.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|------------|--------|---------------|-------------|--------------------|
| **11-O1** | Accepted | Unified runtime | Single **.NET 10 + Avalonia 12 LTS** stack producing both Windows and Linux builds from one solution. Establishes the baseline “full stack” target set. | ADR 11-001 — Target OS Prioritization |
| **11-O2** | Proposed | Mobile / Embedded Expansion | Extend full-stack capability to **Android** devices with sufficient hardware (e.g., rugged tablets) using the same codebase and minimal runtime differences. | *Future ADR* |
| **11-O3** | Proposed | Companion Extension | Provide a **companion-only iOS build** capable of remote UI connection to Core over local network. | *Future ADR* |
| **11-O4** | Proposed | macOS support | Add **macOS** as an optional desktop environment for analysis, playback, or bench testing; not required for field use. | *Future ADR* |
| **11-O5** | Proposed | Raspberry Pi OS refinement | Define Raspberry Pi OS as a **named ARM64 tier** under Linux, ensuring prebuilt images and driver validation. | *Future ADR* |

> **Informative:**  
> Options represent possible implementation or platform strategies under evaluation.  
> Only options accepted via an ADR become normative requirements.

---

## 11.11 Comparison Matrix

| Attribute / Criteria | 11-O1 | 11-O2 | 11-O3 | 11-O4 | 11-O5 |
|----------------------|-------|-------|-------|-------|-------|
| Core Approach | Shared .NET 10 / Avalonia 12 LTS runtime for Windows + Linux | Extend full-stack to Android | Add iOS companion app | Add macOS desktop parity build | Define Raspberry Pi OS under Linux tier |
| Implementation Effort | Medium | High | Medium | Medium | Low |
| Maintainability | High | Medium | High | Medium | High |
| Performance Potential | High | Medium | Medium | High | Medium |
| Extensibility | High | High | High | Medium | Medium |
| Risk Level | Medium | High | Medium | Medium | Low |

> **Informative:**  
> Comparative ratings are qualitative and may be revisited as prototypes mature or new ADRs are issued.

---

## 11.12 Decision Matrix

*(Reserved — to be completed upon selection of an approved option via ADR 11-001.)*

### 11.12.1 Weighting Method

| Criterion | Rationale for Inclusion | Weight |
|------------|------------------------|--------|
| Implementation Complexity | Represents total development and testing effort per OS. | 0.25 |
| Performance / Quality Impact | Influence on responsiveness, reliability, and operator experience. | 0.25 |
| Maintainability | Expected cost of long-term updates, bug fixes, and dependency management. | 0.20 |
| Extensibility / Roadmap Fit | How well the option supports future desktop / mobile expansion. | 0.20 |
| Ecosystem Alignment | Compatibility with existing runtime, libraries, and community support. | 0.10 |
| **Total** |  | **1.00** |


### 11.12.2 Scoring Scale

*(Reserved)*

### 11.12.3 Scoring Evidence

*(Reserved)*

### 11.12.4 Weighted Scoring Table

*(Reserved)*

### 11.12.5 Decision Summary

*(Reserved)*

---

## 11.13 Evaluation & Verification

*(Reserved — to be completed alongside packaging & benchmark details.)*

---

## 11.14 Implementation Policy

*(Reserved)*

---

## 11.15 Community Sentiment

*(Reserved — optional; may remain blank until there is feedback.)*

## 11.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| **R-OS-000** | 11-O1 | ADR 11-001 — Target OS Prioritization | *[to be linked]* | *[to be linked]* |
| **R-OS-001** | 11-O1 | ADR 11-001 — Target OS Prioritization | *[to be linked]* | *[to be linked]* |
| **R-OS-002** | 11-O1–O5 | *Future ADRs (mobile, macOS, Pi)* | *[to be linked]* | *[to be linked]* |

> **Informative:**  
> Traceability connects each normative requirement to the design options, decisions (ADRs), verification artifacts, and implementation modules that realize it.

---

## 11.17 Conformance

An implementation **conforms** to §11 when:  
1. All **MUST** requirements (e.g., R-OS-000, R-OS-001) are satisfied and verified.  
2. All **SHOULD** requirements have corresponding verification evidence or a documented waiver approved by the project authority.  
3. No **MUST NOT** constraint (none defined in this section) is violated.  
4. Verification artifacts referenced in §11.6 demonstrate successful execution against defined criteria.

> **Note:** Conformance applies to each officially supported OS tier as defined by ADR 11-001.

---

## 11.18 Standards Context

This section aligns with the following standards for software requirements and architecture documentation:

- **ISO/IEC/IEEE 29148:2018** — *Systems and Software Engineering — Life Cycle Processes — Requirements Engineering.*
- **IEEE 1016:2017** — *Standard for Information Technology — System Design Descriptions (SDD).*

> **Informative:** Inclusion ensures §11 remains compatible with recognized industry frameworks for requirements traceability and architectural decisions.


---

## 11.19 Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft (OS-only) | Nexus Team (Fortney) |  |


