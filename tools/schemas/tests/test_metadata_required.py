"""Regression tests for required metadata fields in core schemas."""
from __future__ import annotations

import json
from copy import deepcopy
from pathlib import Path
import unittest

from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError


REPO_ROOT = Path(__file__).resolve().parents[3]


def load_schema(name: str) -> Draft202012Validator:
    schema_path = REPO_ROOT / "schemas" / name
    with schema_path.open("r", encoding="utf-8") as handle:
        schema = json.load(handle)
    return Draft202012Validator(schema)


class RequiredMetadataTests(unittest.TestCase):
    def test_job_metadata_is_required(self) -> None:
        validator = load_schema("Job.v1.json")
        base_instance = {
            "id": "job:sample",
            "name": "Sample Job",
            "farmId": "farm:sample",
            "fieldIds": ["field:one"],
            "operation": "planting",
            "createdBy": "user:test",
            "createdAt": "2025-01-01T00:00:00Z",
            "lastModifiedAt": "2025-01-01T00:00:00Z",
        }

        validator.validate(base_instance)

        for field in ("createdBy", "createdAt", "lastModifiedAt"):
            missing = deepcopy(base_instance)
            missing.pop(field)
            with self.assertRaises(ValidationError):
                validator.validate(missing)

    def test_layer_metadata_is_required(self) -> None:
        validator = load_schema("Layer.v1.json")
        base_instance = {
            "id": "layer:coverage:sample",
            "kind": "coverage",
            "createdBy": "user:test",
            "createdAt": "2025-01-01T00:00:00Z",
            "lastModifiedAt": "2025-01-01T00:00:00Z",
        }

        validator.validate(base_instance)

        for field in ("createdBy", "createdAt", "lastModifiedAt"):
            missing = deepcopy(base_instance)
            missing.pop(field)
            with self.assertRaises(ValidationError):
                validator.validate(missing)

    def test_season_metadata_is_required(self) -> None:
        validator = load_schema("Season.v1.json")
        base_instance = {
            "id": "season:2025",
            "name": "2025",
            "dateRange": {"start": "2025-01-01", "end": "2025-12-31"},
            "createdBy": "user:test",
            "createdAt": "2025-01-01T00:00:00Z",
            "lastModifiedAt": "2025-01-01T00:00:00Z",
        }

        validator.validate(base_instance)

        for field in ("createdBy", "createdAt", "lastModifiedAt"):
            missing = deepcopy(base_instance)
            missing.pop(field)
            with self.assertRaises(ValidationError):
                validator.validate(missing)


if __name__ == "__main__":  # pragma: no cover - unittest entry point
    unittest.main()
