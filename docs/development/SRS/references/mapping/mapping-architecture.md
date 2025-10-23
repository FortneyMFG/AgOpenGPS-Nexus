# Nexus mapping architecture

This note captures how the mapping stack is split between the deterministic core runtime and the rendering-focused mapping plugin. Use it as the hand-off contract when new layers, tooling, or visualization experiences are introduced.

## Core runtime responsibilities

Core owns the authoritative, replayable representation of spatial data. Anything that affects persistence, determinism, or capability discovery lives here.

- **Layer registry and provenance** — Core exposes an append-only registry of layers via `ILayerRegistry`. The registry records every layer, its metadata (`LayerDescriptor`), provenance hash, and journal state so deterministic replays and exports remain stable.
- **Layer journals and commands** — Mutations flow through `ILayerCommands` and append serialized `LayerEditBatch` entries. Core ensures edits are validated, ordered, and persisted; plugins only author commands.
- **Canonical schemas and CRS services** — Core defines layer schema names (boundary, headland, coverage, raster, etc.), enforces metadata contracts, and provides CRS helpers through `ICrsService` so every plugin receives identical projections.
- **Tile generation and storage** — The TileStore writer lives in Core. Offline rasters and vector pyramids are published through `ITileCatalog`, allowing plugins to discover MBTiles/GeoPackage artifacts without knowing where they are stored on disk.
- **Normalization and import** — All ingestion flows (legacy v6, shapefiles, text-based prescriptions) terminate in Core, producing domain-specific layers before they are surfaced through the registry.

Core never draws pixels. Its output is a deterministic description of what layers exist, how they change, and where their data resides.

## Mapping plugin responsibilities

The mapping plugin is the owner of experience. It consumes the contracts above and turns them into a real-time scene.

- **Rendering pipeline** — `MapView`, `MapScene`, and the `IMapLayer` implementations render the scene. The plugin schedules CPU/GPU work, uploads buffers, and ensures the frame loop stays responsive.
- **Layer composition** — The plugin reads `ILayerRegistry` to seed built-in layers (grid, vehicle, HUD) and to subscribe to future ones. Default draw order and visibility hints come from the registry and user overrides via `IStyleProfileStore`.
- **User tools and style** — All interaction (drawing tools, layer toggles, opacity sliders) and styling (symbolizers, themes) live in the plugin. When a user makes a change, the plugin writes it back through `ILayerCommands` or `IStyleProfileStore`.
- **Tile consumption** — Online XYZ/TMS sources, runtime caches, and the MBTiles readers that consume `ITileCatalog` outputs live entirely in the plugin. Core may emit MBTiles; the plugin decides how and when to draw them.
- **Coordinate transforms** — Rendering and UI logic uses `ICrsService` for projections without depending on Core assemblies.

If a second map plugin is built later, these responsibilities ensure Core remains untouched while the experience can evolve independently.

## Minimal contracts

The boundary is expressed through a small set of interfaces in `Aog.Abstractions/Contracts/Mapping`:

- `ILayerRegistry` — Enumerates layers (`LayerDescriptor`) and opens per-layer change streams (`ILayerStream<LayerChange>`).
- `ILayerCommands` — Allows plugins to create, delete, and edit layers by publishing `LayerEditBatch` payloads.
- `ITileCatalog` — Resolves `TileSource` entries when Core has produced offline tiles.
- `ICrsService` — Converts between WGS84, project CRS, and ENU coordinates for render-time math.
- `IStyleProfileStore` — Persists user overrides (visibility, z-order, opacity) so every UI honors the same choices.

`IMapHostServices` aggregates these contracts together with pose, field context, storage, and configuration services so plugins receive a single injection point.

## Plugin bootstrap flow

1. `MappingPlugin.CreateMapView` constructs `MapView`, `MapScene`, and the default layer set (grid, vehicle glyph, debug HUD).
2. `ApplyLayerMetadata` reads `ILayerRegistry` for canonical hints (visibility, Z-index) and merges user overrides from `IStyleProfileStore` before the scene is attached.
3. Pose samples stream in via `PoseStream`, feeding the vehicle layer.
4. Future layers can be added by reacting to `ILayerRegistry.Watch` (e.g., when Core registers boundaries, prescriptions, or rasters) and instantiating new `IMapLayer` implementations inside the plugin.

This flow keeps Core authoritative for data and ordering, while letting the plugin own how the scene is rendered and personalized.

## Adding new layers

To add a new visualization layer:

1. Implement a Core layer controller that journals edits and exposes metadata through `ILayerRegistry`.
2. Extend the plugin with a new `IMapLayer` that understands the layer type, optionally tailing `ILayerRegistry.Watch` for live edits.
3. Register style defaults in Core so every UI starts from the same baseline, then honor `IStyleProfileStore` overrides in the plugin.
4. If Core emits tiles, publish them through `ITileCatalog` so the plugin can discover cached rasters instead of re-downloading them.

Document new behaviors in ADR-029 or follow-up design notes so the split between Core truth and plugin experience stays clear.
