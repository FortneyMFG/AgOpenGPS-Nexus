# Vision & Non-goals (Status: collecting proposals)

## Vision
- Deliver a modernized AgOpenGPS experience that keeps offline field work resilient while enabling collaborative planning and telemetry.
- Preserve the community-driven spirit: modular, inspectable, and friendly to tinkering.
- Scale from hobby farms to commercial operations through configurable modules instead of forks.

## Strategic objectives
- Reduce friction deploying to mixed Windows/Linux fleets.
- Unlock richer guidance and automation through consistent data models and APIs.
- Improve UX clarity for operators and integrators with multi-monitor and remote touch layouts.

## Non-goals
- Rewriting proven algorithms without demonstrated benefit.
- Supporting proprietary, license-restricted toolchains that exclude community contributors.
- Guaranteeing certification for regulated markets in the first iteration.

## Success measures
- Community consensus on each critical architecture slice captured in ADRs.
- Reference implementations for headless + remote UI scenarios validated in field tests.
- Contributor onboarding reduced to <1 hour setup on supported OS baselines.

## Baseline field assumptions
- GNSS accuracy: Sub-5 cm RTK guidance accuracy for auto-steer workloads, with fallbacks documented for WAAS/EGNOS grade receivers.
- Compute: Quad-core 2.0 GHz CPU (x86_64 or ARM64), 8 GB RAM, and GPU supporting OpenGL 3.3 with 2 GB VRAM to sustain 60 FPS rendering and replay diagnostics.
- Latency envelope: Control loops expect <100 ms end-to-end latency; monitoring dashboards tolerate up to 500 ms while buffering offline.

## Related ADRs

- [ADR-001 — Adopt .NET 8 C# Stack](sections/1X_Platform_Foundations/11-ADR-001 - Adopt .NET 8 C stack for Nexus runtime.md)
- [ADR-004 — Composite Simulation](sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md)
- [ADR-028 — Stack Boundaries](sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md)
- [ADR-032 — Presets and Layout Linking](sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md)
