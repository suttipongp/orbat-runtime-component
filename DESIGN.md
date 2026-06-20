# Design

## Product

C4ISR Workbench is a Windows desktop command-room tool for commanders, operations staff, and intelligence analysts.

## Visual Direction

The product uses a restrained command-room interface: compact panels, clear hierarchy, sharp grid alignment, and sober status colors. It should feel operational and focused, not cinematic or decorative.

## Color Tokens

Use OKLCH tokens in implementation.

```css
:root {
  --bg: oklch(18% 0.018 248);
  --surface: oklch(23% 0.018 248);
  --surface-2: oklch(28% 0.016 248);
  --line: oklch(38% 0.018 248);
  --ink: oklch(92% 0.012 245);
  --muted: oklch(74% 0.014 245);
  --subtle: oklch(60% 0.014 245);
  --accent: oklch(68% 0.095 195);
  --success: oklch(68% 0.11 155);
  --warning: oklch(78% 0.12 82);
  --danger: oklch(63% 0.15 28);
  --unknown: oklch(72% 0.06 270);
}
```

## Typography

Use a system sans-serif stack for interface text and a monospace stack for coordinates, callsigns, timestamps, and event IDs. Avoid large display typography inside the app shell. Prioritize compact labels, readable tables, and stable control sizes.

## Layout

The first screen is the working surface:

- left rail: ORBAT and workspace navigation
- center: tactical map with overlays
- right panel: intelligence, surveillance, reconnaissance, and AI assistant
- bottom strip: timeline, events, and communications status

Panels should be resizable later, but Phase 1 can use stable fixed tracks.

## Components

- App shell with command bar
- ORBAT tree
- Tactical map viewport
- Layer controls
- Status tiles
- Timeline/event rows
- AI assistant panel
- Audit/approval queue

## Interaction

Critical actions should be explicit and reversible where possible. AI-generated changes must appear as drafts until approved. Keyboard command palette behavior should mirror a power-user IDE workflow.
