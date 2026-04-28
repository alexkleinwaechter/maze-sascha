# Asset Generation

Use this guide only when the user explicitly asks for asset generation or art-direction help.

This project targets Windows, low-GPU workflows, and Godot C# projects. That means the asset strategy should be pragmatic:

- prefer simple, import-ready 2D assets first
- use placeholder art early when it unblocks gameplay
- avoid pipelines that require a custom engine fork, Blender automation, or local GPU inference by default
- keep generated outputs easy to version, reimport, and replace later

## Recommended Default Strategy

For most Godot agent workflows, use this order:

1. Solid-color or procedural placeholders drawn in code for simple shapes
2. Small 2D generated assets for icons, pickups, backgrounds, UI panels, and simple sprites
3. Hand-authored prompt packs and style notes so later generations stay consistent
4. 3D generation only when the game actually needs distinct meshes and the user accepts the extra iteration cost

## What To Generate By Default

Good default candidates:

- UI icons
- inventory items
- card art
- splash screens
- title backgrounds
- tiles and tiling textures
- simple props
- NPC portraits
- placeholder spritesheets or sprite source images

Avoid as a default path:

- full character animation pipelines with many actions
- production 3D rigging workflows
- large batches of unrelated assets without an art-direction anchor
- any workflow that assumes local model inference on Windows hardware

## Consistency First

Before generating multiple assets, define a compact art brief:

- camera/view: top-down, side view, isometric, portrait, UI flat
- style: painterly, clean vector, chunky pixel-inspired, hand-painted fantasy, hard-surface sci-fi
- palette: 3-6 dominant colors
- line treatment: no outline, soft outline, thick dark outline
- lighting: flat, soft studio, dramatic rim light, overcast
- background rule: transparent, solid chroma background, or fully composed background

If generating a set, create one anchor image first and reuse it as the style reference for later prompts where the tool supports image input.

## Godot Import Readiness

Prefer outputs that drop cleanly into a Godot project:

- PNG for most 2D assets
- square or power-of-two-ish textures when practical for reusable textures
- transparent backgrounds for icons, pickups, and isolated sprites
- one asset per file with stable names
- avoid extremely large source images unless the game really displays them at that size

Suggested folders:

```text
assets/
  ui/
  sprites/
  textures/
  backgrounds/
  concepts/
```

Keep concept/reference outputs separate from in-game assets so replacement is easy.

## Prompting Hints

### UI Icon

Use prompts like:

```text
single game UI icon of a frost bomb, centered composition, transparent background, crisp silhouette, readable at small size, cool blue palette, subtle outline, no text, no watermark
```

### Tiling Texture

Use prompts like:

```text
seamless hand-painted mossy stone floor texture, top-down, even lighting, game-ready, tileable, medium detail, no perspective, no focal object
```

### Portrait

Use prompts like:

```text
fantasy merchant portrait for dialogue UI, chest-up, three-quarter view, warm earthy palette, painterly style, clean readable silhouette, plain muted background
```

### Background

Use prompts like:

```text
2D side-scrolling forest background for a platformer, layered depth, wide composition, soft atmospheric perspective, no characters, no text, cohesive green and gold palette
```

## Placeholder-First Rule

If gameplay is blocked, do not wait for perfect art.

Use one of these first:

- flat-color rectangles or circles drawn in code
- a single generated placeholder image reused across several props
- one hero concept image plus descriptive notes for later asset passes

For agent workflows, shipping a testable scene with acceptable placeholder art is better than stalling on asset polish.

## Size Guidance

Generate with intended display size in mind.

- Tiny icon in game: bold shapes, high contrast, minimal detail
- Medium portrait: allow more texture and face detail
- Full-screen background: wider aspect ratio and stronger composition
- Texture tile: avoid a single focal subject

If the final displayed size is small, more detail usually makes the asset worse after downscaling.

## Low-GPU / Windows Guidance

- Prefer cloud or API-based generation over local diffusion setups by default
- Prefer headless or scriptable post-processing steps only if they are already in the repository
- Do not assume Photoshop, Blender, or GPU upscalers are installed
- Keep the pipeline optional and lightweight so the coding workflow still works without art tooling

## C# Workflow Reminder

Generated assets should support the C# gameplay loop, not drive it.

- wire visuals into exported `Texture2D`, `PackedScene`, `SpriteFrames`, or `AudioStream` fields from C# scripts
- keep file names stable so scenes and exported fields do not churn unnecessarily
- separate placeholder and final asset paths only when the replacement plan is clear

## When 3D Is Worth It

Only suggest 3D asset generation when all of these are true:

- the gameplay materially benefits from unique meshes
- placeholder primitives are no longer enough
- the user accepts extra cleanup and validation work
- the repository already has a sane import path for 3D assets

Otherwise, stay with 2D assets, billboards, or primitive-based placeholders.

## Agent Behavior

When asked for assets:

1. ask what category of asset is needed for gameplay or UI
2. define one short art brief
3. propose the smallest useful asset set
4. generate or scaffold references in a stable folder layout
5. avoid expanding into a full art pipeline unless the user explicitly wants that