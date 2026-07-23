from __future__ import annotations

import json
import sys
from pathlib import Path

from jsonschema import Draft202012Validator, FormatChecker
from referencing import Registry, Resource


ROOT = Path(__file__).resolve().parent
SCHEMA_ROOT = ROOT / "v1"
EXAMPLE_ROOT = SCHEMA_ROOT / "examples"

EXAMPLES = {
    "symbol-definition.example.json": "symbol-definition.schema.json",
    "symbol-definition.land-unit-main.example.json": "symbol-definition.schema.json",
    "symbol-definition.modifier-1.example.json": "symbol-definition.schema.json",
    "symbol-definition.modifier-2.example.json": "symbol-definition.schema.json",
    "symbol-definition.echelon.example.json": "symbol-definition.schema.json",
    "symbol-instance.example.json": "symbol-instance.schema.json",
    "control-measure.example.json": "control-measure.schema.json",
    "overlay-scene.example.json": "overlay-scene.schema.json",
    "extension-manifest.example.json": "extension-manifest.schema.json",
}


def load_json(path: Path) -> object:
    with path.open("r", encoding="utf-8") as stream:
        return json.load(stream)


def build_registry() -> Registry:
    registry = Registry()
    for schema_path in sorted(SCHEMA_ROOT.glob("*.schema.json")):
        schema = load_json(schema_path)
        registry = registry.with_resource(schema["$id"], Resource.from_contents(schema))
    return registry


def main() -> int:
    registry = build_registry()
    failures = 0

    for example_name, schema_name in EXAMPLES.items():
        example_path = EXAMPLE_ROOT / example_name
        schema = load_json(SCHEMA_ROOT / schema_name)
        validator = Draft202012Validator(
            schema,
            registry=registry,
            format_checker=FormatChecker(),
        )
        errors = sorted(
            validator.iter_errors(load_json(example_path)),
            key=lambda error: list(error.path),
        )
        if not errors:
            print(f"PASS {example_path.relative_to(ROOT)}")
            continue

        failures += 1
        print(f"FAIL {example_path.relative_to(ROOT)}", file=sys.stderr)
        for error in errors:
            location = "/".join(str(part) for part in error.absolute_path) or "<root>"
            print(f"  {location}: {error.message}", file=sys.stderr)

    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
