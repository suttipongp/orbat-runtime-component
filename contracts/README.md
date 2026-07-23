# ORBAT Platform-Neutral Contracts

This directory defines the serialized contracts shared by ORBAT editors,
renderers, overlay hosts, and plug-ins. The contracts are independent of
WinForms, `System.Drawing`, and .NET runtime types.

## Versioning

- Contract schemas live under `contracts/v1`.
- `contractVersion` identifies the contract family used by a document.
- Definition references always use `contractVersion`, `libraryId`, `libraryRevision`, `definitionId`, and `definitionRevision`.
- Additive optional fields are backward-compatible within v1.
- Breaking changes require a new versioned directory.

## Coordinate Rules

Reusable symbol graphics use a normalized Cartesian view box:

- top-left is `(0, 0)`
- bottom-right is `(1, 1)`
- angles are clockwise degrees
- stroke widths and font sizes are view-box units

Map placement and operational graphics use WGS84 GeoJSON:

- coordinates are `[longitude, latitude]`
- a placed symbol is a GeoJSON Point feature
- a control measure is a Point, LineString, or Polygon feature
- map coordinates never appear in reusable symbol definitions

## Documents

- `common.schema.json`: shared ownership, security, provenance, style, and references
- `geometry.schema.json`: normalized platform-neutral drawing primitives
- `geojson.schema.json`: WGS84 GeoJSON geometry profile
- `symbol-definition.schema.json`: reusable symbol graphics and composition metadata
- `symbol-instance.schema.json`: composed symbol without a map location
- `control-measure.schema.json`: GeoJSON operational graphic feature
- `tactical-feature.schema.json`: symbol and control-measure feature union
- `overlay-scene.schema.json`: C4ISR overlay envelope and FeatureCollection
- `extension-manifest.schema.json`: C4ISR Extension Registry contribution contract

Examples are under `contracts/v1/examples`.

## C4ISR Host Profile

The overlay and extension schemas align with `F:\Github\c4isr-workbench`:

- `MapOverlay.GeoJson` stores the serialized `featureCollection`.
- C4ISR remains authoritative for permissions, ownership, classification,
  persistence, audit, realtime events, and synchronization.
- ORBAT supplies symbol definitions, composition, renderers, tactical graphic
  templates, and map drawing tools.

## Validate

Install the Python validator once:

```powershell
& 'C:\Python313\python.exe' -m pip install -r .\contracts\requirements.txt
```

Validate every checked-in contract example:

```powershell
& 'C:\Python313\python.exe' .\contracts\validate_contracts.py
```

Python is used only as a cross-platform validation tool. Runtime consumers may
be implemented in any language with JSON Schema 2020-12 support.
