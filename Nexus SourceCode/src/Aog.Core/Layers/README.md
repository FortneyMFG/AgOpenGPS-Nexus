# Layers module

This directory contains services and helpers that manage spatial layer storage and provenance.

## LayerEditEvent journal service

`LayerEditEventJournalService` offers an in-memory implementation of the journaling pipeline described in ADR-044. It
accepts strongly typed append requests, validates them against the `LayerEditEvent.v1` schema constraints, computes a
deterministic hash for each entry, and links entries together via `previousHash`/`nextHash` pointers. The service exposes
methods to append entries, list an ordered journal for a layer, and truncate the redo tail after undo operations. Use the
service as the persistence boundary before forwarding events to storage or telemetry pipelines.

## Controller diagnostics feed (NX-218)

`LayerControllerDiagnosticsFeed` converts `LayerControllerSnapshot` instances into health summaries consumed by dashboards
and TileStore writers. The feed normalises quality thresholds, records the time since the last sample, and produces
structured metrics so UI shells can expose controller status without inspecting raw accumulator state.

## TileStore writer (NX-219)

`LayerControllerTileStoreWriter` transforms controller snapshots into immutable `LayerControllerTile` records, wiring the
diagnostics feed so persistence layers capture health metadata alongside numeric aggregates. Tile identifiers follow the
`tile:*` convention used by ADR-009 and include per-controller sequencing to keep hold frames deterministic.
