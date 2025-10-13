# Contribution Playbook for Nexus AIs & Humans

This document applies to the entire repository unless a subdirectory defines a more
specific `AGENTS.md`.

## Ground Rules

1. **Stay on ticket.** Every change maps to a single NX-### task recorded in `tasks.md`.
   Note the ticket in your commit message and PR body.
2. **Short-lived branches.** Use `feat/NX-###-slug` for feature work and rebase before
   opening a PR. Keep diffs under ~200 net LOC (tests excluded) to enable fast reviews.
3. **Contracts-first.** `/Nexus Source Code/proto`, `/Nexus Source Code/src/Aog.Abstractions`,
   and `/tools/schemas` follow a contracts-freeze protocol. When a freeze is active, do not
   merge breaking changes without an ADR and consensus from the area owner.
4. **Tests + docs required.** Ship unit/integration tests and a short usage note (README or
   comment) with every feature. If you add settings, include a validating sample config.
5. **Determinism matters.** Simulation and replay code must be seed-locked. Add regression
   vectors when practical.

## Ownership Bands

| Area | CODEOWNER | Notes |
| --- | --- | --- |
| `/Nexus Source Code/proto/` | Proto Owner | Authoritative protobuf contracts |
| `/tools/schemas/` | Schema Owner | JSON schema definitions & validators |
| `/Nexus Source Code/src/Aog.Abstractions/` | Proto Owner | Generated code only via tooling |
| `/Nexus Source Code/src/Aog.Core/**` | Core Owner | Core orchestrator & sim primitives |
| `/Nexus Source Code/src/Aog.Agio/**` | AGiO Owner | Hardware hosts & backends |
| `/Nexus Source Code/src/Aog.Plugins/**` | Plugins Owner | Feature plugins & sim providers |
| `/Nexus Source Code/src/Aog.UI.Avalonia/**` | UI Owner | Desktop UI |

Update this table once owners are formally assigned. Until then, coordinate changes via
`#nexus-dev`.

## PR Template

```
## Task
- NX-### Title

## Summary
- What changed (1–2 bullets)

## Testing
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `nexus sim smoke`

## Rollout / Flags
- Feature flag name(s) and default state
```

## Freeze Windows

- **Contracts Freeze 1:** Starts after NX-003/004/005 land; lasts 72 hours.
- **Sim Core Freeze:** Starts after NX-012/013 land; lasts 48 hours.
- **Plugin API Freeze:** Starts after NX-031/032 stubs land; lasts 48 hours.

During freezes, only bug fixes or ADR-backed changes are allowed in the frozen area.

## Automation Expectations

- CI must build Windows x64 and Linux arm64 targets.
- Headless 10-second deterministic simulation must stay green.
- Lint/style checks run on every PR; fix or explicitly justify exceptions.

## Human-in-the-loop Verification

Use the `Human QA` column in `tasks.md` to record the person/date who confirmed a feature
on hardware or a realistic sim scenario.

Stay focused, communicate early, and keep Nexus shippable at all times.
