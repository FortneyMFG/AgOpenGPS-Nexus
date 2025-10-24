# 11 — Operating System Support
*(Status: drafting)*

**Section ID:** 11  
**Version:** 0.1.0  
**Editors:** @owner, @reviewer  
**Last Updated:** 2025-10-21  
**Related Sections:** 12 — Development Language & Runtime, 14 — Build Environment & Tooling
**Related Decisions:** `11-ADR-001 — Establish Windows & Linux Support Baseline`, `12-ADR-001 — Adopt .NET 8 LTS Runtime`, `13-ADR-001 — Use Avalonia for the Nexus Desktop UI Shell`
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 5X — Hardware I/O Device Layer, 9X — Frontends & Ops

---

## 11.1 Purpose & Scope

Define operating system (OS) coverage for Nexus.  
This section sets expectations for where the application must/should run at the OS level and how hardware I/O behaves consistently on each platform.

> **Example:**  
> This section defines requirements and design options for cross-OS runtime support.  
> It covers installation targets, OS-level compatibility, and I/O parity expectations.

---

## 11.2 Context

- Depends on .NET 8 runtime and the selected cross-platform UI framework (see §12, `12-ADR-001`, and `13-ADR-001`).
- Interacts with AgIO for hardware interfaces (serial, UDP, CAN) (see §5X).  
- Out of scope: service modes (e.g., headless), packaging specifics, mobile companions.

---

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
|------|------------|
| AgIO | Hardware I/O layer providing serial, UDP, and CAN communication across OSes. |
| Platform Tier | Level of official support (e.g., Primary, Secondary). |
| Cross-Platform Runtime | Shared runtime/toolkit used to build Windows and Linux desktop apps. |

---

> **Requirement Grammar (RFC-2119):**  
> - **MUST / MUST NOT** = mandatory; test must exist.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.  
>
> **Clarity Checklist:** Prefer measurable forms and single-behavior statements.

## 11.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|----------------|----------------------------|
| R-OS-000 | MUST | Compatibility | Provide a fully functional **Windows** desktop build for operators. | Legacy baseline | Installer launches; UI renders; smoke tests pass. |
| R-OS-001 | MUST | Hardware I/O | Support **Serial, UDP, and CAN** via AgIO consistently across supported OSes. | I/O parity goal | OS-specific loopback/device tests pass. |
| R-OS-002 | SHOULD | Portability | Provide an equivalent **Linux** desktop build (x86-64, ARM64) with feature parity to Windows. | Cross-OS target | App launches; UI parity checklist passes. |
| R-OS-003 | SHOULD | Deployment | Distribute signed installers/packages per platform. | Release hygiene | Signature/notarization verified. |
| R-OS-004 | SHOULD | Performance | Publish minimum hardware guidance (e.g., render target such as 60 FPS on reference scene). | Operator guidance | Benchmark doc meets stated targets. |
| R-OS-005 | MAY | Mobility | Allow future desktop builds to interoperate with mobile/tablet clients using shared contracts. | Interop note | Prototype handshake tests compile/run. |

> **Normative:** Each requirement must be objectively testable and traceable.

### 11.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-OS-000 | Legacy Windows usage | Preserve baseline operability. |
| R-OS-001 | Cross-OS I/O need | Ensure device behavior is consistent regardless of OS. |
| R-OS-002 | Cross-platform goal | Extend reach without forking features. |
| R-OS-004 | Operator clarity | Prevent underpowered hardware surprises. |

---

## 11.6 Acceptance Criteria & Verification

Describe how compliance with the requirements is validated.

- Windows installer launch and basic UI smoke test completes without errors.  
- Linux desktop build launches and completes parity checklist.  
- AgIO serial/UDP/CAN tests succeed on each supported OS.  
- Performance benchmark meets published minimums.

### 11.6.1 Requirement-to-Verification Map

| Req ID   | Verification Type | Artifact / Location | Pass/Fail Threshold |
|----------|-------------------|---------------------|---------------------|
| R-OS-000 | Installation test | `[to be added]` | App installs and launches. |
| R-OS-001 | I/O tests | `[to be added]` | All I/O channels pass loopback/device tests. |
| R-OS-002 | UI parity checklist | `[to be added]` | All items ✓. |
| R-OS-004 | Benchmark | `[to be added]` | Meets or exceeds stated FPS/latency targets. |

---

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

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-11-1 | Divergent driver support between Windows and Linux. | Medium | Keep I/O behind AgIO; document supported devices. | [TBD] |
| ISSUE-11-1 | Define which Linux distributions are in scope. | Medium | Publish support matrix in §14. | [TBD] |

---

## 11.9 Design Considerations

| ID | Consideration | Description |
|----|---------------|-------------|
| C1 | Windows baseline | Windows desktop remains the mandatory baseline for operators. |
| C2 | Linux parity | Linux desktop SHOULD provide equivalent features using the same codebase. |
| C3 | Unified I/O | Serial/UDP/CAN are OS-agnostic through AgIO. |
| C4 | Packaging | Use platform-native installers/packages; avoid custom loaders. |
| C5 | Performance guidance | Publish minimum hardware targets per release. |

### 11.9.1 Assumptions & Preconditions

- [A1] Cross-platform UI renders consistently on Windows and Linux.  
- [A2] Required device classes are available on each platform (or documented alternatives exist).  
- [A3] Build tooling produces native installers/packages per OS.

---

## 11.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| **11-O1** | Proposed | Unified runtime | Single .NET 8/Avalonia stack producing Windows and Linux builds from one solution. | 11-O1_Unified_DotNet8_Avalonia.md |

> **Informative:** Options are explored alternatives, not binding requirements.

---

## 11.11 Comparison Matrix

| Attribute / Criteria | 11-O1 |
|----------------------|-------|
| Core Approach | Shared runtime for Windows & Linux desktop apps |
| Implementation Effort | Medium |
| Maintainability | High |
| Performance Potential | High |
| Extensibility | High |
| Risk Level | Medium |

---

## 11.12 Decision Matrix

*(Reserved — to be completed if/when an option is selected.)*

### 11.12.1 Weighting Method

| Criterion | Rationale for Inclusion | Weight |
|----------|-------------------------|--------|
| Implementation Complexity | Effort to build/ship per OS | 0.25 |
| Performance / Quality Impact | Impact on operator experience | 0.25 |
| Maintainability | Long-term sustainability | 0.20 |
| Extensibility / Roadmap Fit | Future desktop growth | 0.20 |
| Ecosystem Alignment | Runtime/tooling maturity | 0.10 |
| **Total** |  | **1.0** |

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

### 11.15.1 Section Change Log

| Date       | Summary                     | PR / Issue |
|------------|-----------------------------|------------|
| 2025-10-21 | Initial draft (OS-only)     | #0000      |

---

## 11.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-OS-000 | 11-O1 | — | `[to be added]` | `[to be added]` |
| R-OS-001 | 11-O1 | — | `[to be added]` | `[to be added]` |
| R-OS-002 | 11-O1 | — | `[to be added]` | `[to be added]` |

---

## 11.17 Conformance

An implementation **conforms** to §11 when:  
1) All **MUST** requirements (R-OS-000, R-OS-001) are satisfied and verified;  
2) All **SHOULD** requirements have evidence or a documented waiver;  
3) No **MUST NOT** constraint (none defined here) is violated.

---

## Standards Context

Aligns with **ISO/IEC/IEEE 29148:2018** and **IEEE 1016:2017**.

