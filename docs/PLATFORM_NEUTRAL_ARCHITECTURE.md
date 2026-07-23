# Platform-Neutral ORBAT Architecture

## Decision

The ORBAT system uses contract-first integration. WinForms remains the first
reference editor and renderer, but its C# types are not the public integration
contract.

The stable boundary is:

1. versioned JSON documents
2. normalized symbol geometry
3. GeoJSON map geometry
4. stable definition and instance identifiers
5. explicit adapter interfaces at each host boundary

## C4ISR Workbench Host Profile

`F:\Github\c4isr-workbench` is the main host system. It already provides:

- React/TypeScript and MapLibre map rendering
- an ASP.NET Core mutation and authorization boundary
- GeoJSON-backed map overlays
- WGS84 canonical coordinate storage
- stable tactical feature IDs
- extension contributions for symbol renderers, symbol templates, tactical
  graphic templates, draw tools, map layers, and import connectors
- HQ/staff/operation ownership, classification, permissions, audit, and
  realtime workflow state

The ORBAT runtime supplies definitions, composition, rendering, and drawing
tools to that host. It does not duplicate the host's overlay persistence,
authorization, audit, synchronization, or MapLibre workspace.

## Definition, Instance, and Placement

These concerns remain separate:

- A symbol definition contains normalized reusable graphics.
- A symbol instance selects a definition, modifiers, affiliation, status, and
  explicitly supplied amplifiers.
- A tactical feature places that instance at a WGS84 GeoJSON point.
- A control measure is a GeoJSON feature referencing an Operational Graphics
  template.
- An overlay document wraps a GeoJSON FeatureCollection with C4ISR ownership,
  security, style, and provenance metadata.

Normalized symbol coordinates must never be interpreted as longitude/latitude.
Map coordinates must never be written into a reusable symbol definition.

## Current Compatibility

Existing `*.orbatsymbol.json` and `*.orbatoverlay.json` files remain supported
by the WinForms application. No existing library file is rewritten by adding
the v1 contracts.

The current formats map to v1 as follows:

| Current field | v1 field |
| --- | --- |
| `Version` | source format version retained by an adapter |
| `LibraryId` | retained as legacy metadata; mapped to `definitionId` when available |
| `LibraryVersion` | `definitionRevision` |
| `PhysicalDomain` / `Domain` | `domain` |
| `SymbolRole` | `role` |
| `CompositionMode` | `composition` |
| `EquipmentFunction` / `UnitMainFunction` | `classification.function` |
| `Variant` | `classification.variant` |
| `Commands` | `scene.primitives` |
| `X`, `Y`, `Width`, `Height`, `RotationDegrees` | adapter-specific local transform |
| `Amplifiers` | `amplifierValues` |

The adapter is responsible for enum spelling and casing differences such as
`Friendly` to `friend`, `MainFunction` to `main-function`, and `SineWave` to
`sine-wave`.

## C4ISR Interop Mapping

| C4ISR Workbench field | Contract field |
| --- | --- |
| `MapOverlay.Id` | `overlay.id` |
| `MapOverlay.Name` | `overlay.name` |
| `MapOverlay.OverlayType` | `overlay.overlayType` |
| `MapOverlay.GeoJson` | serialized `overlay.featureCollection` |
| `MapOverlay.Classification` | `overlay.classification` |
| `MapOverlay.OverlayDomain` | `overlay.overlayDomain` |
| `MapOverlay.OwnerSection` | `overlay.ownerSection` |
| `HeadquartersId` and staff ownership | `overlay.ownership` |
| map style fields | `overlay.style` |
| GeoJSON `featureId` | `feature.properties.featureId` |
| GeoJSON `symbolId` | `feature.properties.symbolInstance.template.definitionId` |
| GeoJSON `rendererId` | `feature.properties.rendererId` |

During migration, a C4ISR adapter may project v1 symbol instance fields into the
existing flat GeoJSON properties expected by the current basic renderer. The
nested `symbolInstance` remains authoritative.

## Runtime Boundaries

### Contracts

Own schemas, examples, migrations, and compatibility rules. They must not
reference .NET assemblies, WinForms controls, GDI objects, database tables, or
host-specific event types.

### Core Engine

Resolves definitions, composes main/modifier/amplifier graphics, validates
documents, and produces a platform-neutral scene. A .NET implementation may be
used first, but equivalent implementations must produce the same scene.

### Renderer Adapter

Translates scene primitives to a platform surface:

- WinForms/GDI+
- SVG
- HTML Canvas
- WebGL
- native mobile or desktop graphics

### C4ISR Extension Adapter

Publishes an extension manifest aligned with the host `ExtensionRegistry`.
Initial delivery can use static JSON/assets. Executable cross-language plug-ins
may later negotiate HTTP, JSON-RPC, WebSocket, or WASM protocols.

The host remains authoritative for permissions, classification, ownership,
auditing, persistence, conflict handling, and synchronization.

## Migration Sequence

1. Keep current WinForms behavior and libraries unchanged.
2. Maintain v1 contracts and C4ISR host examples.
3. Implement legacy-to-v1 adapters and round-trip tests.
4. Move composition rules out of forms into a reference core engine.
5. Render the same v1 symbol with both GDI+ and SVG.
6. Register the SVG renderer and symbol definitions through the C4ISR extension
   adapter while retaining the current basic renderer as fallback.
7. Build Control Measure templates and draw tools on the shared GeoJSON model.

## Rules for New Work

- Do not add `System.Drawing` or WinForms types to serialized/domain contracts.
- Use normalized geometry for reusable graphics.
- Use WGS84 longitude/latitude in GeoJSON order for map features.
- Separate reusable definitions, composed instances, and map placement.
- Store only explicitly supplied amplifier values on an instance.
- Preserve stable IDs through save, load, edit, import, and synchronization.
- Attach ownership, security, and provenance at the host persistence boundary.
- Add a schema version before changing a persisted shape.
