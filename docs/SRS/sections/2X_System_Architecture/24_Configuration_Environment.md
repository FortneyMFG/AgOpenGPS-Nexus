# 24 — Configuration & Environment
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 24
**Editors:** @nexus-specs, @ops-wg
**Last Updated:** 2025-10-20
**Related Sections:** 21, 22, 23, 64, 94
**Upstream Dependencies:** 11, 64
**Downstream Impacts:** 42, 91, 96

---

## 24.1 Purpose & Scope

Specify how Nexus captures environment configuration—profiles, secrets, feature flags, and per-device overrides—so deployments remain reproducible across desktops, CM5 controllers, and managed fleets.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L128】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L211】

---

## 24.2 Context

- Telemetry and replay pipelines rely on consistent configuration metadata for traceability and health reporting.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L146】
- Feature flag governance must align with release engineering workflows to coordinate staged rollouts.【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L149-L211】
- Linux Core packaging introduces secrets management requirements that differ from Windows DPAPI assumptions.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md†L78-L128】

---

## 24.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Configuration Store | INI/flat files per UI host with manual overrides. | Hard to audit and share across rigs. | Versioned JSON/TOML profiles with schema validation. | 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L96】 |
| Secrets | Stored inline or in plaintext config. | Risk of credential leakage on shared devices. | Encrypted secret stores with logical key references. | 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L94-L126】 |
| Feature Flags | Scattered toggles per executable. | No centralized governance or telemetry. | Shared flag registry consumed by UI, Core, plugins. | 【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L149-L211】 |

---

## 24.4 Definitions

| Term | Definition |
|------|------------|
| Profile | Versioned set of configuration values applied per rig or deployment mode. |
| Secret Lease | Time-bound access to encrypted credential managed by platform key store. |
| Feature Flag Registry | Authoritative list of toggles with rollout metadata and telemetry hooks. |
| Configuration Drift | Divergence between expected profile and live values detected via telemetry. |

---

## 24.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|----------------|-----------------------------|
| R-CONF-000 | MUST | Configuration Store | Use versioned JSON/TOML profiles with schema validation and provenance metadata. | Ops WG | Schema validation pipeline rejects invalid updates; history logged. 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L96】【F:docs/SRS/sections/4X_Interprocess_Communications/41-O5 - Versioned layer schemas and quality metadata.md†L32-L49】 |
| R-CONF-001 | MUST | Secrets Management | Isolate credentials in encrypted stores with runtime leases referencing logical keys. | Security review | Secrets rotation tests verify expiry and renewal. 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L94-L126】 |
| R-CONF-002 | SHOULD | Profile Switching | Support operator-selectable profiles that swap transports, plugins, feature flags without manual edits. | UI WG | UI automation verifies profile swap <5 s and updates telemetry. 【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L70-L126】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L211】 |
| R-CONF-003 | MUST | Feature Flags | Provide centralized definitions with rollout notes, defaults, telemetry adoption metrics. | Release engineering | Feature flag registry automation publishes docs + dashboards. 【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L149-L211】 |
| R-CONF-004 | SHOULD | Environment Detection | Detect hardware capabilities and apply safe defaults while allowing overrides. | Hardware WG | Detection harness covers CM5, desktop GPU, GNSS combos. 【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L12-L116】 |
| R-CONF-005 | MUST | Health Integration | Surface configuration drift and missing secrets via telemetry dashboards and CLI health checks. | Telemetry WG | Health alerts triggered within 60 s of drift. 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5 - Layer diagnostics and health monitoring.md†L1-L38】 |

### 24.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-CONF-000 | Ops governance workshop (2025-01) | Fleet reproducibility demands auditable profiles. |
| R-CONF-001 | Security review | Protects RTK credentials and OTA signing keys. |
| R-CONF-003 | Release engineering sync | Aligns rollout with telemetry-driven quality gates. |
| R-CONF-005 | Telemetry roadmap | Enables fast diagnosis of misconfiguration. |

---

## 24.6 Acceptance Criteria & Verification

- Schema validation CI must block invalid profile changes and capture history for auditing.
- Secrets rotation drills verify encrypted stores on Windows and Linux respond to lease expiry.
- Telemetry dashboards and CLI checks must detect configuration drift within defined thresholds.

### 24.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-CONF-000 | CI validation | `pipelines/config_schema_check.yml` | Reject invalid schema commits |
| R-CONF-001 | Integration test | `tests/security/secret_rotation.md` | Lease renews before expiry |
| R-CONF-002 | UI automation | `tests/ui/profile_switch.robot` | Profile swap <5 s |
| R-CONF-003 | Automation | `tools/flags/generate_registry.md` | Docs + telemetry updated |
| R-CONF-005 | Telemetry check | `tests/health/config_drift.md` | Alert <60 s |

---

## 24.7 Constraints

- Configuration formats MUST remain backward compatible across minor releases and be versioned per profile.
- Secrets integration MUST avoid storing raw credentials in source control or logs; logging only logical key names.
- Feature flag registry MUST integrate with release dashboards and change management policy (§96).

---

## 24.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-24-1 | Secrets rotation may drift between Windows DPAPI and Linux keyrings. | High | Align providers with §22 deployment playbooks and automate lease validation per ADR-028.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md†L78-L128】 | @ops-wg |
| ISSUE-24-1 | Feature flag telemetry coverage inconsistent across Core and UI. | Medium | Enforce registry automation and dashboards described in §96 until parity achieved. | @release |

---

## 24.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Profile versioning & provenance | Profiles and overrides require schema validation with audit trails for fleet reproducibility.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L96】 |
| C2 | Secrets abstraction across platforms | Configuration must reference logical keys so Windows DPAPI, Linux keyrings, and vault providers remain interchangeable.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md†L78-L128】 |
| C3 | Remote deployment readiness | Configuration artifacts support headless Core packaging and remote clients defined in §22 while surfacing drift telemetry.【F:docs/SRS/sections/2X_System_Architecture/22_Process_Model_Deployment.md†L1-L160】 |

### 24.9.1 Assumptions & Preconditions

- [A1] Schema repositories and validation tooling are version-controlled alongside deployment pipelines.
- [A2] Secrets providers expose lease status for telemetry ingestion within 60 s.
- [A3] Remote deployments share configuration metadata with telemetry systems to detect drift.

---

## 24.12 Option Evaluation

### 24.12.1 Configuration Strategies

| Strategy | Summary | Pros | Cons |
|----------|---------|------|------|
| Flat files | Single JSON/TOML file per rig. | Simple, easy diff. | Harder to scope secrets and staged rollouts. |
| Profile registry + overrides | Central registry with per-device override fragments. | Supports fleet governance, selective rollout. | Requires tooling for merge/validation. |
| Managed service | Hosted configuration service with API clients. | Central auditing, remote updates. | Adds infra dependency for offline rigs. |

### 24.12.2 Decision Summary

Adopt profile registry + overrides with schema validation and encrypted secret references, keeping flat file export for offline rigs while enabling managed fleet workflows.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L211】

---

## 24.13 Evaluation & Verification

Pilot deployments must exercise profile switching, secret rotation, and telemetry drift detection across Windows desktop, Linux CM5, and managed container stacks prior to marking the section “final”.

---

## 24.14 Implementation Policy

- Store base profiles in `config/profiles/` with semantic versioning; device overrides stored under `config/overrides/<device-id>/`.
- Secrets references use logical keys resolved by platform-specific providers (DPAPI, libsecret, Vault). Values never committed to git.
- Feature flag registry publishes Markdown + JSON artifacts for tooling and UI consumption.

---

## 24.15 Community Sentiment

Reliable configuration management is viewed as prerequisite for remote deployments and plugin governance; schema-validated profiles with managed secrets are preferred over ad-hoc INI files for parity across Windows and Linux hosts.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L146】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L211】

### 24.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-02-14 | Reformatted to SRS v2 template and added verification plan. | #0000 |

---

## 24.16 Traceability

| Requirement ID | Related Strategy / Consideration | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------------------|--------|-----------------------|--------------------------|
| R-CONF-000 | Profile registry + overrides, C1 | 21-ADR-028 | `pipelines/config_schema_check.yml` | `docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md` |
| R-CONF-001 | Secrets providers, C2 | 21-ADR-028 | `tests/security/secret_rotation.md` | `docs/SRS/sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md` |
| R-CONF-003 | Feature flag registry, C3 | 21-ADR-900 | `tools/flags/generate_registry.md` | `docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md` |
| R-CONF-005 | Telemetry integration, C3 | 21-ADR-004 | `tests/health/config_drift.md` | `docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md` |

---

## 24.17 Conformance

Conformance requires validated schemas, encrypted secret handling, and telemetry-backed drift detection across all supported deployment profiles with documented exceptions for deferred **SHOULD** requirements.

---

## Standards Context

Follows ISO/IEC/IEEE 29148:2018 configuration requirement practices and IEEE 1016:2017 guidance for documenting environment assumptions and configuration governance.
