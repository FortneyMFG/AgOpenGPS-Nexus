# Nexus Contribution Guide

This document describes the shared workflow for every Nexus contributor, AI or human.
Unless a subdirectory includes its own `AGENTS.md`, treat this file as the source of
truth and update it as the project evolves.

## 1. Working Agreements

- Keep `main` shippable; long-lived work belongs on feature branches.
- Map every change to exactly one ticket in `tasks.md` (`NX-###` or `NX-PP-###`).
  - Reference the ticket in commit messages (`NX-123: ...`) and PR bodies.
  - Need a new ticket? Coordinate in `#nexus-dev` before writing code.
- Use short-lived branches: `feat/NX-123-short-slug`.
- Respect freeze windows (contracts, sim core, plugin API). Do not merge into a frozen
  area without an approved ADR.
- Surface blockers early, keep `tasks.md` status current, and hand off cleanly.

## 2. Task Flow

1. **Select a ticket** that is ready, within your ownership band, and roughly a
   20-minute unit of focused work.
2. **Collect context**: ADRs, specs, existing implementation, crash reports, open PRs.
3. **Plan first**. Use the planning tool for any task that is not trivial (more than
   one step or five minutes). Update the plan after each step; never create a one-step
   plan.
4. **Execute**:
   - Prefer `rg`, `dotnet`, and other fast tooling.
   - Maintain existing style; default to ASCII.
   - Do not roll back or overwrite unrelated changes.
5. **Validate** (see section 4) and capture output.
6. **Document** the outcome in the PR template (section 6) and adjust `tasks.md` if the
   ticket status changes.

## 3. Ownership Bands

| Area | Owner (interim) | Notes |
| --- | --- | --- |
| `/Nexus SourceCode/proto/` | Proto Owner (TBD) | Generated from ADR-backed contracts only. |
| `/tools/schemas/` | Schema Owner (TBD) | JSON schema definitions and validators. |
| `/Nexus SourceCode/src/Aog.Abstractions/` | Proto Owner (TBD) | Generated code; never hand-edit. |
| `/Nexus SourceCode/src/Aog.Core/**` | Core Owner (TBD) | Orchestrator, sim primitives, determinism hooks. |
| `/Nexus SourceCode/src/Aog.Agio/**` | AGiO Owner (TBD) | Hardware hosts and backends. |
| `/Nexus SourceCode/src/Aog.Plugins/**` | Plugins Owner (TBD) | Plugin logic, manifests, sim providers. |
| `/Nexus SourceCode/src/Aog.UI.Avalonia/**` | UI Owner (TBD) | Desktop UI shell and controls. |

Update the table as soon as ownership changes or a new area appears.

## 4. Quality Gates

Run every relevant check and record results in the PR template.

- **Baseline for all tickets**
  - `dotnet build`
  - `dotnet test`
  - `nexus sim smoke`
- **UI / Avalonia**: ensure XAML compiles cleanly; run view model or control tests.
- **Plugins**: add or refresh integration fixtures when behavior changes.
- **Simulation and replay**: maintain determinism (seed-lock, regression vectors, repeat).
- **Mapping stack**: keep render paths ES3 safe and validate on both desktop and
  Raspberry Pi configurations.
- Temporary helper scripts are fine; remove them before merge.

If you cannot run a required check (no hardware, missing dependency), note why and call
out follow-up verification.

## 5. Coding and Tooling Guidelines

- Default to `bash -lc` (or PowerShell equivalents). Use `rg` for search, `dotnet` for
  build and test, and the `nexus` CLI for simulation flows.
- Use `apply_patch` for surgical edits; avoid it for bulk or auto-generated changes.
- Honor nullable annotations and dotnet analyzers.
- UI work should favor data bindings over imperative wiring; keep XAML theme-aligned.
- Mapping and rendering specifics:
  - Follow the layer contract (`IMapLayer`, `FrameCtx`, `Localizer`) and split CPU
    scheduling (`UpdateCpu`), GPU uploads (`UploadGpu`), and drawing (`Draw`).
  - Stick to ES3 features (no geometry shaders, wide GL lines, or vendor-only paths).
  - Manage caches deterministically; keep memory footprints suitable for Raspberry Pi
    targets (roughly 128-256 MB for textures and buffers).
- Never ship licensed imagery or assets; all raster sources must be user supplied.

## 6. PR Template (required)

```
## Task
- NX-### Title

## Summary
- What changed (1-2 bullets)

## Testing
- [ ] dotnet build
- [ ] dotnet test
- [ ] nexus sim smoke

## Rollout / Flags
- Feature flag name(s) and default state
```

Only check a box after the command runs successfully. Include log snippets or links when
investigating failures.

## 7. Documentation and Configs

- Every feature ships with usage notes: README snippet, inline docs, or config sample.
- New settings must include a schema or JSON example plus validation tests.
- Update ADR or SRS references whenever behavior changes; backfill missing links.
- Keep `tasks.md` in sync (`Status`, `Owner`, `Human QA`).

## 8. Freeze Windows

- **Contracts Freeze 1**: starts after NX-003, NX-004, NX-005 and lasts 72 hours. Covers
  proto, schema, and abstraction generation.
- **Sim Core Freeze**: starts after NX-012 and NX-013 and lasts 48 hours. Applies to
  `/Aog.Core` and shared sim primitives.
- **Plugin API Freeze**: starts after NX-031 and NX-032 and lasts 48 hours. Applies to
  plugin contracts and manifest surfaces.

Only merge critical bug fixes during a freeze, and capture ADR sign-off plus notes in the
PR and `tasks.md`.

## 9. Human in the Loop Verification

- Record the verifier in `tasks.md` (`Human QA` column) with name and date once the
  feature is exercised on real hardware or a representative sim.
- For operator-facing features (mapping overlays, guidance, sections), attach screenshots
  or sim recordings to the PR or release notes.

## 10. Quick Reference

- Issue tracker: `tasks.md` (ordered by section).
- Specs: `docs/SRS` (including `docs/SRS/sections` for ADRs).
- Plugin manifests and leases: `/Nexus SourceCode/src/Aog.Plugins`.
- Mapping UI stack: `/Nexus SourceCode/src/Aog.UI.Avalonia/Controls`, `/Views/Mapping`,
  `/Rendering`, `/Models`.
- Configuration samples: `bundles/`, `plugins/**/examples`.
- Support channel: `#nexus-dev`.

Stay focused, keep the build green, and deliver experiences that feel like a premium ag
monitor running smoothly on a Raspberry Pi.
