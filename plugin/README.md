# ORBAT Runtime Static Plugin v1.0.0

This source builds a deterministic, platform-neutral static plugin for C4ISR Workbench. It contains no WinForms, GDI, System.Drawing, database, authentication, authorization, map placement, or C4ISR operational state.

## Ownership boundary

ORBAT owns symbol semantics, definitions, composition, legacy migration, and deterministic SVG rendering. C4ISR owns users, permissions, classification enforcement, operational placement, overlay lifecycle, map state, and persistence.

## Build and verify

```powershell
npm run plugin:build
```

The command converts the tracked legacy `.orbatsymbol.json` and `.orbatoverlay.json` files read-only, validates all JSON with Draft 2020-12 schemas, writes inspectable golden SVG fixtures, runs golden hash tests, creates checksums, and verifies the complete package.

Output: `dist/c4isr-orbat-plugin/`

Use `npm run plugin:golden` only after visually reviewing an intentional renderer change.

## Host integration

1. Read and validate `manifest.json`.
2. confine every manifest/package path to the plugin root.
3. verify `checksums.json` before loading assets.
4. import `web/orbat-renderer.js` as an ES module.
5. load definitions by the complete reference tuple: `contractVersion`, `libraryId`, `libraryRevision`, `definitionId`, `definitionRevision`.
6. expose only `symbolTemplates`; modifier, echelon, mobility, amplifier, equipment-function, and variant definitions are composition components.
7. call `renderSymbol(instance, { size, definitions })` and honor `status: degraded` and `warnings`.

Legacy import is read-only. Unknown fields are retained in `extensions.legacy` or `migration-report.json`; source files are never rewritten.
