# Offline-First Updates (Status: collecting proposals)

## Problem statement
Define how operators obtain, install, and roll back software updates when field connectivity ranges from spotty to nonexistent.

## Requirements (from contributors)
- R-UPD-000 (MUST, current-AgOpenGPS): Preserve zip-based distribution where users download releases and run the included executables offline.【F:README.md†L28-L33】
- R-UPD-001 (MUST, current-AgOpenGPS): Keep `dotnet publish` workflows that produce a consolidated output folder for manual deployment.【F:README.md†L35-L41】
- R-UPD-002 (SHOULD): Provide rollback guidance so rigs can revert to a known-good build without re-imaging machines.
- R-UPD-003 (SHOULD): Allow staged updates (AgOpenGPS vs. AgIO vs. controllers) without breaking compatibility.
- R-UPD-004 (SHOULD, proposed-LinuxCore): Provide Debian packages, Docker images, and AppImage builds for the Core/frontends with documented rollback (keep prior version) while preserving zip releases.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L20】
- R-UPD-005 (COULD): Add delta packages or background downloaders that respect limited connectivity.

## Options
- O-UPD-0: Status quo — Manual zip download/unpack per release.
- O-UPD-1: Self-updating launcher that pulls signed packages when online.
- O-UPD-2: Offline installer bundle (MSIX/Setup) with repair/rollback.
- O-UPD-3: Package manager integration (Winget/Chocolatey) for automated upgrades.
- O-UPD-4: Field-update kit (USB stick) with scripted upgrade/rollback steps.
- O-UPD-5: Linux package repositories + container images for Core/UI.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L20】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-UPD-0 | Simple, proven | Manual, error-prone | Inconsistent rigs | Current release docs |
| O-UPD-1 | Hands-free | Needs connectivity | Auto-update failures in field | Launcher prototypes |
| O-UPD-2 | Guided install | Heavier packaging | Installer corruption risks | dotnet publish output |
| O-UPD-3 | Integrates with tooling | Requires admin/network access | Repo management overhead | Release metadata |
| O-UPD-4 | Works fully offline | Logistics of distributing media | Lost media delays updates | USB deployment scripts |
| O-UPD-5 | Integrates with Linux tooling, supports rollback via package manager | Requires signing infrastructure + repo hosting | Divergent update paths vs. Windows | Linux Core packaging plan |

## Evaluation criteria
Offline usability, rollback capability, operator effort, package integrity, compatibility with existing workflows.

## Current sentiment
- Manual zips work but need a clearer rollback plan and optional automation for well-connected fleets.
- Linux packaging must ship with clear rollback/dual-boot instructions before inviting operators to pilot the Core service.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L23】

## Open questions
- How do we validate updates before rollout when rigs stay offline for months?
- What metadata should accompany each release (checksums, firmware versions)?
