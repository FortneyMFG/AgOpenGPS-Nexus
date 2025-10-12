# Extensibility & Plugins (Status: collecting proposals)

## Problem statement
Outline how developers extend AgOpenGPS (custom tools, integrations, UI modules) without forking core code, and what constraints exist today.

## Requirements (from contributors)
- R-EXT-000 (MUST, current-AgOpenGPS): Continue exposing shared libraries (`AgLibrary`, `AgOpenGPS.Core`) that downstream executables reference for customization.【F:SourceCode/GPS/AgOpenGPS.csproj†L32-L48】【F:SourceCode/AgIO/Source/AgIO.csproj†L23-L33】
- R-EXT-001 (MUST, current-AgOpenGPS): Keep multiple companion executables (AgIO, ModSim, GPS_Out, Keypad, AgDiag) available for extension via source modifications.【F:SourceCode/AgOpenGPS.sln†L6-L35】
- R-EXT-002 (SHOULD): Define a plugin boundary (UI, PGN handlers, analytics) that avoids shipping forked executables for every variation.
- R-EXT-003 (SHOULD): Provide guidelines or templates for third-party modules so they integrate with packaging and settings.
- R-EXT-010 (SHOULD, proposed-variable-layer): Allow plugins/modules to register new telemetry layers via dependency injection and published ID registries so they appear in dashboards without core code edits.【F:docs/SRS/options/O-BACKEND-4_LayerControllers.md†L19-L33】【F:docs/SRS/options/O-API-5_VersionedLayerSchemas.md†L32-L49】
- R-EXT-004 (COULD): Support sandboxing or capability declarations for plugins to protect critical operations.
- R-EXT-011 (SHOULD, governance): Establish contribution governance for community plugins (review queues, namespace reservation, security vetting) before enabling DI registration so unsafe modules cannot bypass safety-critical boundaries.

## Options
- O-EXT-0: Status quo — Extend by modifying source projects and rebuilding.
- O-EXT-1: Introduce a managed plugin API (MEF/AssemblyLoadContext) for UI and logic extensions.
- O-EXT-2: Expose scripting hooks (Python/Lua) for automation and custom workflows.
- O-EXT-3: Offer gRPC/webhook extension points for out-of-process services.
- O-EXT-4: Package optional modules as NuGet packages consumed by the desktop apps.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-EXT-0 | Simple, aligns with current repos | Requires full rebuilds | Fork divergence | Shared libraries |
| O-EXT-1 | Controlled extension points | Loader/security complexity | Plugin crashes impact runtime | MEF patterns |
| O-EXT-2 | Rapid prototyping | Performance + safety concerns | Scripts can break guidance | Existing automation | 
| O-EXT-3 | Language-agnostic | Requires transport layer | Network failures | PGN bridge |
| O-EXT-4 | Versioned distribution | Package management overhead | Dependency hell | NuGet ecosystem |

## Evaluation criteria
Safety, maintainability, ease for contributors, performance impact, packaging complexity.

## Current sentiment
- Developers fork today; we need a plugin surface that honors safety-critical boundaries while reducing merge burden.
- Layer metadata + ID registries are expected to become the bridge for safe third-party modules once DI hooks exist.【F:docs/SRS/options/O-BACKEND-4_LayerControllers.md†L34-L47】【F:docs/SRS/options/O-API-5_VersionedLayerSchemas.md†L32-L64】

## Open questions
- Which features are safe to expose via scripting vs. compiled plugins?
- How do we version plugin APIs alongside firmware expectations?
