# 11 — OS Support

> **In plain terms:** We promise operators that Windows installs keep working while
> we grow a Linux-friendly service build. Think of it as keeping today’s cab PCs
> happy, adding a headless Linux box for fleets, and laying the groundwork so a
> phone or tablet can join later without rewriting everything.

*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 11  
**Editors:** Platform Foundations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 12 — Development Language & Runtime, 14 — Build Environment & Tooling  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 5X — Hardware IO Device Layer, 9X — Frontends & Ops

---

## 11.1 Purpose & Scope

Define the operating system (OS) coverage required for Nexus Core, AgIO backends, plugins, and desktop user interfaces.
This section clarifies which platforms must remain first-class, how emerging Linux deployments interact with the Windows legacy base, and what expectations exist for headless, kiosk, and remote UI workloads.

---

## 11.2 Context

- Legacy AgOpenGPS installers and tooling target Windows 10/11 on x64 hardware.
- Contributors are piloting Linux (Ubuntu/Debian) deployments for headless Core + remote UI flows.
- AgIO abstracts hardware access but currently depends on Windows-first device APIs; Linux alternatives require parity validation.
- Multi-monitor cabs, kiosk installs, and potential Android/iOS companions impose usability and packaging constraints.

### Questions operators ask

- **“Can I keep using the Windows installer I already know?”** Yes—the Windows build remains first-class and is validated on every release.
- **“What happens if I want to run Nexus on a Raspberry Pi or CM5?”** The Linux headless build provides systemd units and packaging so you can drop it onto those devices.
- **“How do I know if my graphics card is fast enough?”** Section 11.5 documents the 60 FPS benchmark and points to the reference hardware list.

### Scenario: Launching a new mixed fleet

1. A dealer installs the familiar Windows desktop app in the cab so operators recognize the workflow.
2. The same farm adds a Linux CM5 running the headless Core, using systemd packaging to keep it running without manual babysitting.
3. Later, the team tests a tablet-based companion UI; the dual-first requirements ensure Avalonia keeps layouts consistent across every screen.

---

## 11.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Deployment | Windows WinExe installers for Core, UI, and AgIO utilities. | Linux deployments ad-hoc; no consistent packaging. | Ship Linux systemd units, container images, and Pi/CM5 bundles. | Historical AgOpenGPS releases |
| Hardware Access | Direct Win32 serial, HID, and vendor CAN SDK integrations. | Tight coupling to Windows drivers; limited SocketCAN coverage. | Encapsulate device access in AgIO backends with Linux parity. | AgIO codebase |
| UI Shells | WinForms primary UI with emerging WPF shell. | Dual maintenance burden; touch parity limited. | Adopt cross-platform UI stack that reuses shared contracts. | Nexus UI discussions |

> **Informative:** Captures historical context and modernization drivers.

---

## 11.4 Definitions

| Term | Definition |
|------|-------------|
| AgIO | Nexus hardware abstraction host process that surfaces GNSS, CAN, and IO services.
| Headless Core | Service-oriented deployment of Nexus Core without a local UI shell.
| Kiosk Mode | Locked-down UI configuration for cabs with limited input and multi-monitor requirements.
| Companion Remote | Mobile or remote desktop client consuming Nexus APIs over gRPC or gRPC-Web.

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.

## 11.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|----------|-----------------|-----------------------------|
| R-OS-000 | MUST | Compatibility | Maintain shipping Windows desktop runtimes for Core UI executables. | Legacy operator fleet | Windows build lane + installer smoke tests |
| R-OS-001 | MUST | Hardware IO | Keep AgIO Windows Forms host viable for serial, UDP, and CAN management. | AgIO contributor feedback | Automated regression suite for device dialogs |
| R-OS-002 | SHOULD | Deployment | Preserve Windows-based flows relied on by external controllers (e.g., SK21). | Partner integrations | Beta installers validated against partner rigs |
| R-OS-003 | SHOULD | UX | Continue multi-monitor aware window placement to keep dashboards visible. | Community UX notes | UI smoke test with multi-monitor layouts |
| R-OS-004 | SHOULD | Portability | Package a Linux headless “AOG Core” service with systemd unit and dependencies. | Linux Core pilots | Linux CI lane + field smoke checklist |
| R-OS-005 | COULD | Deployment | Offer container images/AppImage bundles for advanced users. | Power user backlog | Container build pipeline with basic run verification |
| R-OS-006 | SHOULD | Performance | Document baseline hardware capable of sustaining 60 FPS rendering. | Hardware survey | Hardware validation bench capturing FPS |
| R-OS-007 | SHOULD | Mobility | Plan for Android/iOS targets that reuse Avalonia UI with minimal conditional code. | Mobile WG notes | Companion app prototypes hitting UI parity checklist |
| R-OS-008 | COULD | Mobility | Map USB-OTG serial/Bluetooth SPP/BLE integrations to shared AgIO abstractions. | Mobile WG notes | Android pilot verifying IO parity |

> **Why it matters:** These requirements guarantee a familiar Windows download for
> current farms, introduce a reliable Linux service build for fleets, and keep us
> honest about documenting hardware expectations so newcomers know if their gear
> is powerful enough.

### 11.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-OS-000 | Legacy Windows releases | Preserve operator trust and upgrade path. |
| R-OS-004 | Linux Core pilot plan | Enable headless rigs and remote client flows. |
| R-OS-006 | Cross-platform pilots | Ensure hardware targets are realistic and published. |
| R-OS-007 | ADR-003 Avalonia UI | Align desktop and mobile client investments. |

---

## 11.6 Acceptance Criteria & Verification

- Windows and Linux CI lanes publish build artifacts on every PR with smoke validation.
- Multi-monitor UI regression tests confirm window placement helpers function across OSes.
- Linux headless packages pass systemd enable/start/stop cycle tests and telemetry replay benchmarks.

### 11.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-OS-000 | CI integration | `pipelines/windows-build.yml` | Installer boots and launches UI |
| R-OS-004 | Manual checklist | `qa/checklists/linux-core.md` | All tasks ✓ |
| R-OS-006 | Benchmark | `benchmarks/platform/fps.md` | ≥ 60 FPS sustained in reference scene |
| R-OS-007 | Prototype demo | `demos/mobile-companion/README.md` | Feature parity scenarios complete |

---

## 11.7 Constraints

- Must remain compliant with vendor driver EULAs and redistributable terms.
- Maintain parity between Windows and Linux AgIO backends for critical device classes (serial GNSS, CAN bus).
- Ensure build pipelines handle code signing and package notarization where applicable.

### 11.7.1 Non-Functional Requirement Classes

- **Performance:** Rendering FPS, IO latency, startup time.
- **Reliability:** Service restarts, driver reconnect behavior, offline recovery.
- **Security:** Signed artifacts, trusted transport encryption between components.
- **Portability:** Windows x64, Linux x86_64, Linux ARM64 baselines.
- **Maintainability:** Avoid OS-specific forks; keep abstractions within AgIO layer.

---

## 11.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-11-1 | Linux packaging lacks maintainers. | Medium | Pair with Release WG; document build scripts. | @platform-wg |
| RISK-11-2 | Hardware vendor SDKs missing Linux support. | High | Isolate via gRPC shims; pursue vendor contact. | @agio |
| ISSUE-11-1 | Define supported Linux distros and kernels. | Medium | Draft support matrix in Section 14. | @release |
| ISSUE-11-2 | Certify ARM64 GPU performance for Avalonia UI. | Medium | Track via 12-O1 validation plan. | @ui |

---

## 11.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Windows-first baseline | Preserve installer workflows and UI expectations for existing operators. |
| C2 | Linux headless core | Deliver service packaging, systemd integration, and remote client compatibility. |
| C3 | Dual-first strategy | Balance Windows + Linux parity without fragmenting development tooling. |
| C4 | Remote/companion clients | Support gRPC/Web transports for Android/iOS or remote desktops. |
| C5 | Hardware abstraction | Keep IO stacks behind AgIO to avoid OS-specific forks in Core/UI. |
| C6 | Packaging ergonomics | Provide containers/AppImage bundles for advanced deployments. |
| C7 | Performance baselines | Document GPU/CPU requirements to guard against underpowered hardware. |

### 11.9.1 Assumptions & Preconditions

- [A1] Reference hardware lists remain updated per release cycle.
- [A2] AgIO abstraction contracts remain stable across OS backends.
- [A3] Contributors can provision Windows and Linux CI lanes for validation.

---

## 11.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| **11-O1** | Proposed | Cross-platform runtime | Unified Windows/Linux deployment via .NET 8 + Avalonia stack, leveraging shared AgIO backends. | [11-O1_Unified_DotNet8_Avalonia.md](11-O1_Unified_DotNet8_Avalonia.md) |

> **Informative:** Conceptual alternatives from legacy notes now live in §11.9 Design Considerations.

---

## 11.11 Comparison Matrix

| Attribute / Criteria | 11-O1 — Unified .NET 8 Stack | Legacy Windows-only Baseline |
|----------------------|------------------------------|------------------------------|
| Core Approach | Shared runtime + UI across Windows/Linux | Windows-exclusive binaries |
| Implementation Effort | Medium — requires Linux packaging + CI | Low — keep current pipelines |
| Maintainability | High — one codebase, shared contracts | Low — diverging forks for Linux |
| Performance | High — GPU-tuned Avalonia + AgIO parity | Medium — proven on Windows only |
| Extensibility | High — supports remote clients + plugins | Low — Linux/mobile off roadmap |
| Risk Level | Medium — new tooling, Linux drivers | Medium — stagnates modernization |

---

## 11.12 Decision Matrix

### 11.12.1 Weighting Method

| Criterion | Rationale for Inclusion | Weight |
|-----------|------------------------|--------|
| Implementation Complexity | Balances engineering cost vs. modernization benefit. | 0.25 |
| Performance / Quality Impact | Ensures guidance UI and IO remain responsive. | 0.25 |
| Maintainability | Avoids OS-specific forks and duplicated tooling. | 0.20 |
| Extensibility / Roadmap Fit | Enables remote clients and Linux growth. | 0.20 |
| Ecosystem Alignment | Evaluates community familiarity and library support. | 0.10 |
| **Total** |  | **1.0** |

### 11.12.2 Scoring Scale

| Score | Meaning | Qualitative Description |
|-------|---------|-------------------------|
| 1 | Very Poor | Fundamentally unsuited; major blockers. |
| 2 | Poor | Feasible but with unacceptable trade-offs. |
| 3 | Adequate | Meets minimal expectations with caveats. |
| 4 | Good | Performs well and aligns with design goals. |
| 5 | Excellent | Ideal fit; strong performance and maintainability. |

### 11.12.3 Scoring Evidence

| Criterion | 11-O1 Justification | Legacy Baseline Justification |
|-----------|---------------------|------------------------------|
| Implementation Complexity | Requires Linux packaging + CI extensions, but reuse existing C# code. | Minimal change; stays with Win-only installers. |
| Performance / Quality Impact | Avalonia pilots demonstrate 60 FPS on Windows + Linux ARM64. | Proven on Windows; no Linux/mobile support. |
| Maintainability | Single runtime, shared contracts, fewer forks. | Divergent code paths for Linux experiments. |
| Extensibility / Roadmap Fit | Unlocks remote clients and mobile companions. | Linux/mobile efforts stay ad-hoc. |
| Ecosystem Alignment | Leverages .NET 8 LTS, Avalonia ecosystem, existing skills. | Locked to Win32 stack; limited community growth. |

### 11.12.4 Weighted Scoring Table

| Criterion | Weight | 11-O1 | Legacy Baseline |
|-----------|--------|-------|-----------------|
| Implementation Complexity | 0.25 | 3.5 | 4.5 |
| Performance / Quality Impact | 0.25 | 4.5 | 3 |
| Maintainability | 0.20 | 4.5 | 2 |
| Extensibility / Roadmap Fit | 0.20 | 4.5 | 1.5 |
| Ecosystem Alignment | 0.10 | 4 | 2 |
| **Weighted Total** | **1.0** | **4.15** | **2.85** |

### 11.12.5 Decision Summary

**Selected Option:** 11-O1 — Unified .NET 8 Stack  
**Rationale:** Highest weighted score with strong alignment to modernization roadmap and cross-platform goals.  
**Formal Record:** [11-ADR-001 - Adopt Unified .NET 8 Runtime & Avalonia Stack.md](11-ADR-001%20-%20Adopt%20Unified%20.NET%208%20Runtime%20&%20Avalonia%20Stack.md)

> **Verification:** Platform Foundations WG reviewed scoring on 2025-10-20.

---

## 11.13 Evaluation & Verification

- Benchmark Linux ARM64 and Windows x64 builds against 60 FPS rendering requirement.
- Execute AgIO device matrix (serial, CAN, GPS) across OS backends.
- Validate remote client latency over Wi-Fi/Ethernet using shared gRPC contracts.

---

## 11.14 Implementation Policy

- Maintain shared deployment manifests for Windows installers and Linux packages.
- Version AgIO backends alongside shared contracts to guarantee compatibility.
- Document OS support tiers (Supported, Preview, Experimental) per release.

---

## 11.15 Community Sentiment

- Community favors dual-first Windows + Linux strategy while keeping Windows operators productive.
- Contributors expect Linux Core packaging plus Avalonia UI to unlock Pi/CM5 deployments.
- Mobile pilot interest remains, contingent on cross-platform stack maturity.

### 11.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Initial rebaseline using standardized SRS template. | #0000 |

---

## 11.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-OS-000 | 11-O1 | 11-ADR-001 | Windows installer CI | `SourceCode/GPS/` projects |
| R-OS-004 | 11-O1 | 11-ADR-001 | Linux Core checklist | `deployment/linux-core/` |
| R-OS-007 | 11-O1 | 11-ADR-001 | Mobile prototype demo | `ui/mobile-companion/` |

---

## 11.17 Conformance

An implementation conforms to Section 11 when:
1. All **MUST** requirements (R-OS-000, R-OS-001) are satisfied and validated.
2. **SHOULD** requirements have verification evidence or documented waivers.
3. No **MUST NOT** constraints are violated during deployment or runtime.

---

## Standards Context

This section aligns with **ISO/IEC/IEEE 29148:2018** requirements management guidance and maps to Nexus deployment governance for cross-platform support.
