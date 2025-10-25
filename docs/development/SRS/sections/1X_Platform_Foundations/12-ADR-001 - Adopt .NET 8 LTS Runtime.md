# 12-ADR-001 — Adopt .NET 8 LTS Runtime Across Nexus  
*(Status: Accepted — 2025-10-25)*  

**Authors:** Nexus Team (Fortney)
**Reviewers:** Architecture Working Group, Release & Tooling Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `12_Development_Language_Runtime.md`  
**Related Options:** `12-O1_Runtime_Policy.md`  
**Upstream Sections:** §11 (OS Support)  |  §14 (Build Environment & Tooling)  
**Downstream Impacts:** UI Framework Selection (§13), Plugin Contracts (§6X)  

---

## 1) Context

Section 12 defines the runtime and language policies governing all Nexus components.  
Legacy AgOpenGPS releases mixed **.NET Framework**, **.NET 6**, and native utilities,
resulting in divergent build pipelines, inconsistent Linux support, and plugin
incompatibilities.

Sections 11 and 14 introduce new requirements for reproducible cross-platform builds
and deterministic tooling. To satisfy these, all managed components — including Core,
UI, AgIO, and CLI — must share a single long-term-support runtime.

**.NET 8 LTS** (supported through November 2026) provides the required cross-platform
JIT, SDK, and CI tooling for both Windows and Linux targets. It also enables Avalonia
and NativeAOT development without fragmenting the stack.

---

## 2) Decision

Adopt **.NET 8 LTS** as the managed runtime for all Nexus managed components until a
new LTS migration ADR supersedes this one.

- All projects **must target** `net8.0` (or `net8.0-windows`, `net8.0-linux`, etc. as
  needed for platform-specific assets).  
- CI and local environments **must use** the .NET 8 SDK pinned via `global.json`.  
- NativeAOT or trimming experiments **may** proceed if outputs remain compatible with
  .NET 8 tooling and pass §11 smoke tests.  
- Runtime upgrades follow Microsoft’s LTS cadence; evaluate previews but do not adopt
  until a successor ADR is approved.

---

## 3) Consequences

### Positive Impacts
- Unifies runtime and eliminates build drift between Windows and Linux.  
- Enables Avalonia, ASP.NET Core, and shared analyzers under one toolchain.  
- Provides plugin developers a stable baseline with long-term vendor support.

### Negative / Mitigated Impacts
- Requires migration of legacy .NET Framework projects. *Mitigation:* adapter shims and
  migration guides.  
- Slightly higher hardware/runtime requirements. *Mitigation:* document minimum specs in §11.  
- Future LTS transitions require planning. *Mitigation:* annual review and evergreen upgrade backlog.

### Follow-Up Actions
- Audit all solutions for `TargetFramework` entries =`net8.0`.  
- Update dependency allowlists and analyzers for .NET 8 compatibility.  
- Maintain temporary downgrade bundles until .NET 8 is verified on both OSes.

---

## 4) Rationale

.NET 8 LTS delivers the cross-platform stability, runtime features, and toolchain
maturity needed for Nexus. Remaining on .NET Framework or adopting non-LTS runtimes
would violate §11 and §12 requirements for OS parity, determinism, and maintainability.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|---------|----------|----------|
| Windows-only .NET Framework | Continue shipping legacy runtime for Windows builds only. | **Rejected** — fails cross-platform and modern tooling requirements. |
| Adopt .NET 7 or preview builds | Use short-term or unstable runtimes. | **Rejected** — non-LTS and upgrade churn increase maintenance cost. |
| Move to native C++/Qt stack | Rewrite outside the .NET ecosystem. | **Rejected** — prohibitively expensive and breaks existing code and plugins. |

---

## 6) Governance

- **Ownership:** Platform Foundations Working Group.  
- **Review Cadence:** Annual or when a new Microsoft LTS runtime is announced.  
- **Artifacts:** `global.json`, runtime compliance dashboard, analyzer configurations.  
- **Exit Criteria:** Superseded by a future ADR that adopts the next LTS version.

---

## 7) Risks & Mitigations

| ID | Risk | Mitigation |
|----|------|-------------|
| **ADR12-R1** | Legacy dependencies incompatible with .NET 8. | Maintain compatibility backlog and provide shim packages. |
| **ADR12-R2** | Runtime upgrades introduce regressions. | Maintain regression suites and stage upgrades in preview branches. |
| **ADR12-R3** | Contributors lack .NET 8 tooling. | Provide bootstrap scripts and enforce toolchain setup via §14 build policy. |

---

## 8) Change Log

| Date | Change | Author |
|------|---------|--------|
| 2025-10-20 | Initial adoption of .NET 8 LTS runtime. | Nexus Team (Codex) |
| 2025-10-25 | Clarified NativeAOT scope and dependency governance linkage. |  Nexus Team (Fortney) |
