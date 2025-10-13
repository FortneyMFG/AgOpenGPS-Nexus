#!/usr/bin/env python3
"""Validate Nexus configuration files against the published JSON schemas."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, Iterable, Tuple

try:
    from jsonschema import Draft202012Validator
    from jsonschema.exceptions import ValidationError
    from referencing import Registry, Resource
    from referencing.jsonschema import DRAFT202012
except ImportError as exc:  # pragma: no cover - import guard
    raise SystemExit(
        "jsonschema is required. Install with `python -m pip install jsonschema`."
    ) from exc


SCHEMA_SUFFIX = ".schema.json"
SAMPLE_SUFFIX = ".sample.json"


def load_schemas(schema_dir: Path) -> Tuple[Dict[str, dict], Registry]:
    schemas: Dict[str, dict] = {}
    registry = Registry()
    for path in sorted(schema_dir.glob(f"*{SCHEMA_SUFFIX}")):
        with path.open("r", encoding="utf-8") as handle:
            schema = json.load(handle)
        schemas[path.name] = schema
        resource = Resource.from_contents(schema, default_specification=DRAFT202012)
        uri = path.resolve().as_uri()
        registry = registry.with_resource(uri, resource)
        schema_id = schema.get("$id")
        if schema_id:
            registry = registry.with_resource(schema_id, resource)
    return schemas, registry


def validator_for(schema: dict, registry: Registry) -> Draft202012Validator:
    return Draft202012Validator(schema, registry=registry)


def infer_schema_name(json_path: Path) -> str:
    stem = json_path.stem
    if stem.endswith(".sample"):
        stem = stem[: -len(".sample")]
    return f"{stem}{SCHEMA_SUFFIX}"


def validate_file(
    schema_dir: Path,
    schemas: Dict[str, dict],
    registry: Registry,
    schema_name: str,
    json_path: Path,
) -> None:
    if schema_name not in schemas:
        raise SystemExit(f"Unknown schema '{schema_name}'. Expected one of: {', '.join(sorted(schemas))}.")

    with json_path.open("r", encoding="utf-8") as handle:
        instance = json.load(handle)

    schema_path = schema_dir / schema_name
    if not schema_path.exists():
        raise SystemExit(f"Schema file '{schema_name}' was not found next to validate.py.")

    validator = validator_for(schemas[schema_name], registry)
    validator.validate(instance)



def run_default_samples(schema_dir: Path, schemas: Dict[str, dict], registry: Registry) -> Iterable[str]:
    samples_dir = schema_dir / "samples"
    if not samples_dir.exists():
        return []

    for sample_path in sorted(samples_dir.glob(f"*{SAMPLE_SUFFIX}")):
        schema_name = infer_schema_name(sample_path)
        validate_file(schema_dir, schemas, registry, schema_name, sample_path)
        yield f"{sample_path.name} ✔ {schema_name}"



def main(argv: Iterable[str] | None = None) -> int:
    schema_dir = Path(__file__).resolve().parent
    schemas, registry = load_schemas(schema_dir)

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("json", nargs="*", type=Path, help="Config file(s) to validate.")
    parser.add_argument(
        "--schema",
        dest="schema",
        metavar="NAME",
        help="Schema filename (e.g. core.schema.json). Defaults to matching on the JSON filename prefix.",
    )
    args = parser.parse_args(list(argv) if argv is not None else None)

    try:
        if args.json:
            schema_name = args.schema
            for json_path in args.json:
                name = schema_name or infer_schema_name(json_path)
                validate_file(schema_dir, schemas, registry, name, json_path)
                print(f"{json_path} ✔ {name}")
        else:
            for message in run_default_samples(schema_dir, schemas, registry):
                print(message)
    except FileNotFoundError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1
    except ValidationError as exc:
        print("validation failed:", file=sys.stderr)
        print(exc, file=sys.stderr)
        return 1
    except json.JSONDecodeError as exc:
        print(f"invalid JSON: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":  # pragma: no cover - CLI entry point
    sys.exit(main())
