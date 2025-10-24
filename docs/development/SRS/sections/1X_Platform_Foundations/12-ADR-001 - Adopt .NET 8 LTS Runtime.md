# 12-ADR-001 — Adopt .NET 8 LTS Runtime Across Nexus

*(Status: Accepted — 2025-10-25)*

**Authors:** Platform Foundations Working Group  
**Reviewers:** Architecture Working Group, Release & Tooling Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-25  
**Related SRS:** `12_Development_Language_Runtime.md`  
**Related Options:** `12-O1_Runtime_Policy.md`

---

## 1) Context

Section 12 governs runtime and language policy for Nexus. Legacy AgOpenGPS releases
mixed .NET Framework, .NET 6, and native utilities, leading to divergent tooling,
missing Linux validation, and incompatibilities for plugins. Section 11 now requires
Windows and Linux parity, and Section 14 depends on deterministic build tooling. A
single managed runtime is needed so all components, including UI, AgIO, headless
services, and plugins, share the same contracts, analyzers, and CI lanes.

.NET 8 is the current long-term-support release from Microsoft with published support
through November 2026. It carries the required runtime, JIT, and tooling support for
Windows and Linux architectures targeted by Section 11, and it unlocks Avalonia and
NativeAOT exploration paths without fragmenting the stack.

---

## 2) Decision

Adopt **.NET 8 LTS** as the managed runtime for all Nexus managed components until the
next LTS migration plan is accepted.

- All projects must target `net8.0` (or `net8.0-windows`, `net8.0-linux`,
  `net8.0-android`, etc. when framework-specific assets are required).
- CI and local development environments must restore and build with the .NET 8 SDK
  pinned via `global.json`.
- NativeAOT experiments MAY proceed if they emit artifacts compatible with .NET 8
  tooling and pass Section 11 smoke tests.
- Runtime upgrades follow the LTS cadence: evaluate .NET 9+ previews, but do not adopt
  until a new ADR supersedes this decision.

---

## 3) Consequences

**Positive impacts**

- Single runtime reduces build drift and ensures parity across Windows and Linux.
- Avalonia, ASP.NET Core backends, and tooling share analyzers and packages without
  multi-TFM complexity.
- Plugin developers have a consistent baseline and can rely on long-term support from
  Microsoft.

**Negative/mitigated impacts**

- Requires porting remaining .NET Framework projects. Mitigated by adapter shims and
  upgrade guidance captured in Section 12.
- Older hardware may hit higher runtime requirements. Mitigated by documenting minimum
  hardware specs in Section 11 and verifying performance dashboards.
- Future LTS migrations still require planning. Mitigated by annual review cadence and
  evergreen upgrade backlog items.

**Follow-up actions**

- Audit all solution files to confirm `TargetFramework`/`TargetFrameworks` entries align
  with `net8.0`.
- Update dependency allowlists and analyzers to versions compatible with .NET 8.
- Maintain downgrade bundles until the .NET 8 rollout is verified across Windows and
  Linux agents.

---

## 4) Rationale

.NET 8 provides the LTS runway and cross-platform tooling required by Sections 11, 12,
13, and 14. Remaining on .NET Framework or adopting a non-LTS runtime would fragment
platform support and complicate Avalonia adoption. .NET 8 also provides improved JIT,
NativeAOT, and container support aligned with future Android and headless scenarios.

---

## 5) Alternatives Considered

| Option | Summary | Outcome |
|--------|---------|---------|
| Stay on .NET Framework | Maintain legacy runtime for Windows-only builds. | Rejected — fails cross-platform goals and modern tooling requirements. |
| Adopt .NET 7 or preview builds | Target non-LTS frameworks. | Rejected — short support window and upgrade churn. |
| Move to native C++/Qt stack | Rewrite runtime stack outside .NET. | Rejected — high cost and breaks existing code/assets. |

---

## 6) Governance

- **Ownership:** Platform Foundations Working Group.  
- **Review cadence:** Annual or when Microsoft announces a new LTS release.  
- **Artifacts:** `global.json`, runtime compliance dashboard, analyzer configuration.  
- **Exit criteria:** Superseded by an ADR that moves Nexus to a newer LTS runtime.

---

## 7) Risks & Mitigations

| ID | Risk | Mitigation |
|----|------|------------|
| ADR12-R1 | Legacy dependencies incompatible with .NET 8. | Maintain compatibility backlog; provide shims or replacements. |
| ADR12-R2 | Runtime upgrades introduce regressions. | Maintain regression suites; stage upgrades in preview branches. |
| ADR12-R3 | Contributors lack .NET 8 tooling. | Document bootstrap scripts; enforce via Section 14 build tooling. |

---

## 8) Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial adoption of .NET 8 LTS runtime. | Platform Foundations WG |
| 2025-10-25 | Clarified NativeAOT scope and dependency governance linkage. | Platform Foundations WG |
