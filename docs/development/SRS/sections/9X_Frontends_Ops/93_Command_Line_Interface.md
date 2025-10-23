# 93 — Command Line Interface
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 93  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 11 — OS Support, 94 — Extensibility & Packaging Updates, 95 — Security & Permissions  
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** Tooling automation, plugin governance, field operations scripts

---

## 93.1 Purpose & Scope

Define the unified Nexus command-line interface (`nx`) that operators, integrators, and automation pipelines use to manage Core services, plugins, and deployments. The CLI must consolidate legacy scripts, respect manifest governance, and operate across Windows and Linux targets with consistent UX, scripting affordances, and security posture.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L19-L98】

---

## 93.2 Context

- Legacy workflows rely on bespoke batch/PowerShell scripts per plugin or deployment scenario.  
- Plugin governance (ADR-031) and capability discovery (ADR-018) expose metadata that the CLI must consume.  
- Remote Core orchestration demands secure transport negotiation and offline-friendly operations for farm deployments.【F:docs/development/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L19-L66】

---

## 93.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Tooling | Multiple scripts per plugin/platform. | Fragmentation and inconsistent UX. | Single `nx` binary with plugin discovery. | Legacy deployment scripts |
| Remote Control | Manual socket wiring per environment. | brittle security and configuration. | Negotiated transports with named pipes, UDS, TLS fallback. | Remote orchestration notes |
| Automation | CLI output scraped by ad-hoc parsers. | Breaks when text changes. | Structured JSON/NDJSON output with schema versioning. | Automation backlog |

---

## 93.4 Definitions

| Term | Definition |
|------|-------------|
| `nx` Host | Unified CLI executable that loads plugin verbs and core commands. |
| CLI Adapter | Plugin-provided assembly implementing `Nexus.Plugin.Cli.Abstractions` to register verbs. |
| Capability Registry | Manifest-driven index enumerating plugin features for discovery and permission gating. |
| Offline Mode | Execution context where CLI manipulates configuration without a running Core instance. |

---

> **Requirement Grammar (RFC-2119):**  
> - **MUST / MUST NOT** = mandatory; verification required.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.

## 93.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-CLI-000 | MUST | Tooling Consolidation | Ship single `nx` host binary discovering plugin verbs at startup. | Tooling consolidation WG | Installer + smoke tests |
| R-CLI-001 | MUST | Offline Readiness | Support filesystem-driven commands when Core is offline (install/remove, config edit, profile export/import). | Field deployment scripts | Offline regression suite |
| R-CLI-002 | MUST | Transport Negotiation | Connect to running Core via named pipes (Windows), Unix domain sockets (Linux), with TLS TCP fallback (`--endpoint`). | Remote orchestration plan | Transport negotiation tests |
| R-CLI-003 | MUST | Plugin Integration | Load `Nexus.Plugin.Cli.Abstractions` adapters or query plugin CLI reflection services without duplicating host logic. | ADR-018 capability discovery【F:docs/development/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L19-L66】 | Plugin CLI integration harness |
| R-CLI-004 | SHOULD | UX & Parsing | Use `System.CommandLine` and `Spectre.Console` for parsing, help, and TTY rendering, falling back to plain text for non-TTY contexts. | DevEx backlog | CLI UX regression |
| R-CLI-005 | MUST | Structured Output | Provide `human`, `--json`, and `--ndjson` output with stable DTO schemas for automation. | Automation backlog | Schema compatibility tests |
| R-CLI-006 | SHOULD | Config Discovery | Resolve configuration in priority order (`~/.nexus/config.yml`, repo `.nexus/`, environment variables). | 12-factor alignment | Configuration discovery tests |
| R-CLI-007 | SHOULD | Auth Handling | Read remote auth tokens from `~/.nexus/credentials` and emit remediation hints on permission errors. | Security posture plan | Authentication regression |
| R-CLI-008 | MUST | Version Negotiation | Enforce semantic version compatibility between CLI, Core APIs, and plugin verb contracts with upgrade guidance. | Release governance | Version negotiation tests |

### 93.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-CLI-000 | Community CLI summit | Reduce fragmentation and onboarding friction. |
| R-CLI-001 | Field deployment backlog | Allow maintenance without Core uptime. |
| R-CLI-002 | Remote orchestration pilots | Provide secure, deterministic connectivity. |
| R-CLI-005 | Automation teams | Guarantee machine-readable output for pipelines. |
| R-CLI-008 | Release governance | Prevent incompatible plugin/CLI pairings in production. |

---

## 93.6 Acceptance Criteria & Verification

The CLI must pass cross-platform smoke tests, structured output validation, and security auditing. Offline behaviors and plugin verb discovery are verified through automated integration suites and manual field simulations.

### 93.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-CLI-000 | Installer smoke | `tests/cli/HostBootstrap.feature` | `nx --help` lists core and plugin verbs |
| R-CLI-001 | Offline regression | `tests/cli/OfflineProfiles.feature` | All offline commands succeed without Core running |
| R-CLI-002 | Transport integration | `tests/cli/TransportNegotiation.cs` | Named pipe, UDS, and TLS fallback connect successfully |
| R-CLI-005 | Schema validation | `tests/cli/OutputSchemas.snap` | JSON/NDJSON match versioned schema |
| R-CLI-008 | Version contract tests | `tests/cli/VersionHandshake.feature` | Incompatible versions yield actionable upgrade guidance |

---

## 93.7 Constraints

- CLI distribution must align with OS packaging policies defined in §11 and §94.  
- Security-sensitive operations (token storage, plugin verb permissions) defer to §95 without embedding credentials in plain text.  
- CLI must operate in restricted environments (air-gapped, offline) without network dependencies beyond configured endpoints.

---

## 93.8 Interfaces & Dependencies

- Consumes capability registry and manifest governance from §94.  
- Invokes Core transports defined in §4X Interprocess Communications.  
- Surfaces verification artifacts and telemetry consumed by §96 Quality Engineering.

---

## 93.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Cross-platform packaging | Ship `nx` via MSI, deb/rpm, and zip/tarball bundles with consistent entry points. |
| C2 | Plugin verb discovery | Support both reflection-based discovery and adapter assemblies to accommodate plugins with varying dependency footprints. |
| C3 | Automation ergonomics | Provide progress spinners, status codes, and quiet mode to align with CI usage. |
| C4 | Offline UX | Offer explicit messaging when Core is offline and highlight commands that can proceed. |
| C5 | Security posture | Integrate with credential stores and propagate least-privilege guidance from §95. |

### 93.9.1 Assumptions & Preconditions

- [A1] Plugin authors publish CLI adapters alongside manifest updates.  
- [A2] Capability registry is accessible to the CLI (local cache or remote query).  
- [A3] Operators provision credentials following §95 policies before attempting remote verbs.

---

## 93.10 Option Overview

No alternative CLI proposals are under review; focus remains on maturing `nx` per considerations C1–C5.

---

## 93.11 Comparison Matrix

| Attribute / Criteria | Legacy Scripts | Unified `nx` CLI |
|----------------------|----------------|------------------|
| Maintainability | Low — per-plugin maintenance. | High — centralized release. |
| Automation Support | Low — ad-hoc parsing. | High — structured output + schemas. |
| Security | Medium — inconsistent token handling. | High — consistent credential storage guidance. |
| Cross-platform | Low — Windows focus. | High — Windows & Linux parity. |
| Extensibility | Low — manual verb wiring. | High — manifest-driven discovery. |

---

## 93.12 Decision Matrix

> **Informative:** Weighted scoring deferred until automation teams complete pilot rollouts; initial implementation follows considerations C1–C5.

---

## 93.13 Evaluation & Verification

- Execute CLI integration tests against Windows and Linux CI runners.  
- Validate credential flows with §95 policies, including token rotation and least-privilege scopes.  
- Confirm plugin verb discovery during deterministic replay runs to ensure automation remains stable.

**Acceptance Criteria**

- `nx --help` enumerates required verbs per capability registry snapshot.  
- Structured output matches schema snapshots across releases.  
- Remote orchestration completes using named pipes, UDS, and TLS fallback routes.

---

## 93.14 Implementation Policy

- Store CLI configuration in `~/.nexus/config.yml` with environment overrides for CI pipelines.  
- Persist credentials in `~/.nexus/credentials` encrypted per OS recommendations (DPAPI, libsecret).  
- Distribute CLI update notifications via plugin catalog UI (§91) and release notes in §96.

---

## 93.15 Community Sentiment

- Contributors welcome a single CLI but require plugin authors to publish verbs concurrently.  
- Automation teams prioritize deterministic JSON output and exit codes for pipeline integration.  
- Operators request guided messaging when commands require elevated permissions or remote connectivity.【F:docs/development/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L31-L68】

### 93.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted to new SRS template; formalized CLI requirements and considerations. | #0000 |

---

## 93.16 Traceability

| Requirement ID | Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|----------------|--------|-----------------------|--------------------------|
| R-CLI-000 | C1, C2 | — | `tests/cli/HostBootstrap.feature` | `src/cli/Nexus.Cli.Host` |
| R-CLI-001 | C4 | — | `tests/cli/OfflineProfiles.feature` | CLI offline module |
| R-CLI-003 | C2 | 94-ADR-018 | `tests/cli/PluginDiscovery.cs` | Plugin adapter examples |
| R-CLI-005 | C3 | — | `tests/cli/OutputSchemas.snap` | CLI serialization layer |
| R-CLI-008 | C5 | — | `tests/cli/VersionHandshake.feature` | Version negotiation service |

---

## 93.17 Conformance

The CLI conforms when **MUST** requirements pass verification, structured outputs align with schema snapshots, and plugin verb discovery succeeds using governed manifests.

---

## Standards Context

Aligns with POSIX CLI conventions and OWASP CLI security guidelines, while adopting 12-factor application principles for configuration management.
- R-CLI-009 (SHOULD, diagnostics): Offer diagnostics verbs (`nx core status`,
  `nx diag dump`, `nx events tail`) that gather logs, manifests, and bus events
  for support workflows, including streaming output where appropriate.
- R-CLI-010 (SHOULD, packaging): Build and distribute the CLI as a .NET 8 global
  tool and as self-contained single-file binaries for Windows (x64/arm64) and
  Linux (x64/arm64, including CM5/Pi) with optional macOS arm64 support when
  available.

## Options
- O-CLI-0: Unified `nx` host with plugin-discovered verbs, offline filesystem
  workflows, and Core-connected live mode (recommended design in this brief).
- O-CLI-1: Per-plugin CLIs maintained independently, each re-implementing
  transport discovery and manifest wiring.
- O-CLI-2: Script-only tooling with no supported CLI host.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
| --- | --- | --- | --- | --- |
| O-CLI-0 | Unified UX, shared transport logic, consistent telemetry | Requires plugin adapters or reflection contracts | Host must guard against plugin incompatibilities | Existing manifests, ADR-031 governance, System.CommandLine patterns |
| O-CLI-1 | Plugin teams ship at their own cadence | Fragmented UX, duplicated plumbing, harder compatibility enforcement | Divergent JSON schemas and auth models | Legacy single-purpose scripts |
| O-CLI-2 | Zero new tooling investment | Operators lack supported automation path | Unsupported scripts block rollout of new governance | None |

## Evaluation criteria
Cross-platform reach, plugin onboarding effort, observability coverage,
compatibility governance, and automation friendliness.

## Current sentiment
- Packaging and manifest governance from Section 16 position us to load plugin
  adapters safely without bypassing capability checks.
- Transport negotiation should reuse the endpoint locator logic already required
  for Core ↔ UI processes (Sections 03 & 07) while surfacing CLI-specific
  telemetry.
- A single host strengthens docs, support playbooks, and training materials
  compared to proliferating per-plugin executables.

## Upcoming ADR coverage
- **ADR-054 Nexus CLI host** will formalize the unified host, transport
  negotiation, plugin adapter expectations, and packaging plan to satisfy
  R-CLI-000 through R-CLI-010. (TBD)

## Open questions
- Which initial verbs and DTOs constitute the minimal viable CLI surface for the
  first pilot release?
- How will plugin reflection services authenticate/authorize remote CLI
  invocations when Core hosts additional security policies?
