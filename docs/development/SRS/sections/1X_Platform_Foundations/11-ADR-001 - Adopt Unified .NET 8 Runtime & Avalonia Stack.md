# 11-ADR-001 — Adopt Unified .NET 8 Runtime & Avalonia Stack

*(Status: Accepted — 2025-10-25)*

**Authors:** Platform Foundations Working Group  
**Reviewers:** Platform Foundations Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `11_OS_Support.md`  
**Related Option:** `11-O1_Unified_DotNet8_Avalonia.md`

---

## 1) Context

Section 11 raises the bar from “Windows-only” to “Windows **and** Linux with the
same capabilities.” Legacy AgOpenGPS builds targeted .NET Framework and
WinForms, leaving Linux experiments as bespoke scripts with limited hardware
support. Investigations compared keeping the Windows baseline, pursuing a Qt/C++
rewrite, or adopting a unified .NET 8 runtime with Avalonia so the Section 11
support matrix can stay synchronized across operating systems.

Pilot builds using .NET 8, AgIO adapters, and Avalonia confirmed that identical
projects could produce signed Windows installers and Linux packages without
forking the codebase. That outcome matches the expectations laid out in SRS
requirements R-OS-000 through R-OS-004.

---

## 2) Decision

Adopt .NET 8 with the Avalonia desktop shell as the unified runtime for all
Section 11 deliverables. Every first-party executable (Core, AgIO, desktop UI,
and managed plugins) ships from this stack so Windows and Linux artifacts share
contracts, packaging scripts, and smoke tests.

**Why this matters for Section 11.**

- Windows installers and Linux packages come from the same source projects,
  satisfying the dual-platform expectation.
- AgIO exposes the same gRPC contracts on both operating systems, enabling
  parity in serial/UDP/CAN handling.
- Performance benchmarks and support matrices can rely on identical binaries,
  enabling clean operator guidance.

---

## 3) Consequences

**Positive impacts**

- Section 11 requirements gain a direct implementation path: the same
  codebase publishes both OS deliverables.
- Tooling and CI flows stay aligned, reducing drift between Windows and Linux
  releases.
- Operators receive a unified support matrix with predictable update cadence.

**Negative/mitigated impacts**

- Teams must learn Avalonia patterns; mitigated with shared templates and
  documentation maintained under Section 13.
- Linux GPU/driver variance can affect performance; mitigated with benchmark
  tracking before every release.
- Vendor SDK gaps (e.g., CAN on Linux) require shims; mitigated through AgIO
  adapter strategy and device support tiers.

**Follow-up actions**

- Finalise the Section 11 support matrix (Windows 10/11 x64, Ubuntu LTS x64,
  Debian-based ARM64).
- Publish shared install/verification scripts that run on both OS targets.
- Keep the decision under annual review in line with .NET LTS cadence.

---

## 4) Rationale

The Section 11 decision matrix (pending publication) scores 11-O1 highest for
maintainability and roadmap alignment. Retaining the Windows-only baseline
prevents Linux parity altogether, and a Qt/C++ rewrite would delay Section 11
compliance for multiple release cycles. Staying within .NET keeps existing code
investments, aligns with contributor skill sets, and directly addresses every
Section 11 requirement.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|---------|---------|
| Windows-only WinForms | Keep .NET Framework builds and publish Linux documentation only. | Rejected — fails R-OS-001 and R-OS-002. |
| Qt/C++ desktop rewrite | Re-implement desktop apps with Qt and platform-specific plugins. | Rejected — high rewrite cost and breaks existing plugin/runtime contracts. |
| Split runtimes (.NET for Windows, native for Linux) | Publish different stacks per OS. | Rejected — doubles maintenance and violates single-support-matrix goal. |

---

## 6) Implementation & Governance

- **Ownership:** Platform Foundations Working Group.
- **Review cadence:** Annual, or whenever Microsoft announces the next .NET LTS.
- **Artifacts:** Runtime matrix, OS support tiers, Section 11 verification plan.
- **Exit criteria:** Decision stays in force until a superseding ADR changes the
  runtime baseline or Section 11 requirements evolve.

---

## 7) Risks & Mitigations

| ID | Risk | Mitigation |
|----|------|------------|
| ADR11-R1 | Linux GPU drivers regress performance targets. | Benchmark dashboards and fallback rendering profiles. |
| ADR11-R2 | Vendor SDKs unavailable on Linux. | Document support tiers, invest in SocketCAN/native shims. |
| ADR11-R3 | .NET LTS upgrades introduce breaking changes. | Maintain downgrade bundle and review plan before upgrading. |

---

## 8) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial acceptance of unified runtime. | Codex |
| 2025-10-25 | Scoped ADR explicitly to Section 11 and refreshed governance notes. | Platform Foundations WG |

