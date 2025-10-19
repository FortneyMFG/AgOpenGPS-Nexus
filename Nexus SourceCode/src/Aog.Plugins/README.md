# In-Tree Plugins (Reference Implementation)

This directory contains the source for the **official Nexus plugins** that currently build in-tree. Each plugin will ultimately migrate to the zip-packaged model described in `docs/plugins/architecture.md`, but the code here remains as reference implementations and test fixtures while the packaging pipeline is finalized.

---

## Directory Layout

| Folder | Purpose |
| --- | --- |
| `*/` | Plugin-specific domain logic (AutoSteer, Sections, Mapping, etc.). |
| `Compatibility/` | Shared compatibility evaluators used by the Plugin Manager dashboard. |
| `PluginManifest*.cs` | Manifest models and validators shared between the host, tooling, and the packer. |
| `PluginLeaseManager.cs` | Runtime lease coordinator used by both in-tree and zip plugins. |

The `bin/` and `obj/` folders are generated build artifacts.

---

## Migrating a Plugin to Zip Packaging

1. **Create a dedicated project** in `plugins/<id>/<Plugin>.csproj` that references the Nexus SDK NuGets.
2. **Extract reusable components** from the in-tree implementation into the new project (reuse as shared library if multiple plugins depend on them).
3. **Author `manifest.json`** describing the plugin capabilities and entry points.
4. **Run `PackPlugin`** to produce `<id>-<version>.zip`.
5. **Install and validate** via the Plugin Manager panel.
6. Once validated, **remove or mark obsolete** the corresponding in-tree folder.

Until a plugin has fully transitioned, the in-tree version remains the canonical implementation used by regression tests.

---

## Coding Guidelines

- Follow the contracts documented in `docs/plugins/official/<plugin>.md`.
- Keep pure business logic decoupled from UI and transport services; this makes migration to zip packages straightforward.
- Write deterministic unit tests in `tests/Aog.Plugins.Tests/**` to ensure behavior parity after migration.
- Avoid `static` state; everything should be scoped through dependency injection to ease unloading in the zip world.

---

## Shared Utilities

- **`PluginManifestLoader`** — Used by tooling and runtime to parse and validate manifests.
- **`PluginCapabilityLease` & `PluginLeaseManager`** — Provide the capability lease mechanism enforced by the core host.
- **`PluginPermissionGate`** — Applies permission policy decisions (e.g., blocking network access when not granted).

Future work will move these shared pieces into the `Nexus.Sdk.*` packages to decouple plugin repositories from the main tree.

---

## Documentation

Each official plugin has a dedicated reference card in `docs/plugins/official/`. The cards call out the key classes housed here and link to relevant tests and manifests.

