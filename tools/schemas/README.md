# Nexus JSON Schemas

Wave 1 captures the configuration contracts for the Nexus Core, AGiO host, UI, and
simulation catalog as JSON Schema Draft 2020-12 documents. These schemas describe the
shape of the configuration files referenced throughout the SRS (Core orchestration,
AGiO transports/backends, Avalonia UI layout, and simulation providers/routes/options).

## Layout

- `*.schema.json` — Authoritative schemas for each configuration surface (core, AGiO, UI, simulation, plugin manifests, job metadata, season organizers, session documents).
- `samples/*.sample.json` — Example configuration files used for smoke validation.
- `validate.py` — Helper that validates configuration files against the schemas.

## Usage

Validate every bundled sample:

```bash
python tools/schemas/validate.py
```

Validate a specific configuration file against a schema:

```bash
python tools/schemas/validate.py --schema core.schema.json path/to/core.json
```

Job metadata (`job.json`) and session payloads (`sessions/<id>.json`) are validated with
`job.schema.json` and `job-session.schema.json`, reflecting the lifecycle updates captured
in ADR-030 and ADR-041.

The validator relies on the [`jsonschema`](https://pypi.org/project/jsonschema/) Python
package:

```bash
python -m pip install jsonschema
```

Schema references use relative URIs, so the helper automatically resolves cross-schema
links when run from the repository root.
