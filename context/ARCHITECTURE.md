# Garden Companion Architecture Contract

## System boundary

CatMe has three separate layers:

```text
Character generation service → product/asset service → Unity mobile client
```

- A secure generation service owns photo processing, provider calls, job polling, validation, and optimized character output.
- A product/asset service owns per-player character versions, ownership, and approved downloads when online accounts are introduced.
- Unity owns presentation, local approved-asset cache, garden world, owner controls, cat navigation/animation, ball play, audio, haptics, and local play state.

The current direct provider calls are local prototype code only; the credentials can be extracted from a built app and must not ship.

## Unity dependency direction

```text
UI and input
    ↓ player commands
Garden gameplay: owner movement, cat follow, ball interaction
    ↓ state/navigation requests
Character movement, animation, and world adapters

API → validated character files → local cache → runtime character loader
Save → stable data models, never scene objects
```

Keep presentation/input above gameplay services. Lower layers must not import UI behavior. Character systems communicate through small commands, state, or events instead of searching arbitrary scene objects.

## Module ownership

- `Core`: startup, shared lifecycle, and composition.
- `API`: service transport and response mapping.
- `Save`: local persistence and cache metadata.
- `Cat`: cat loading, movement, animation, and behavior.
- `Owner`: owner avatar movement and animation (or the current focused equivalents).
- `Garden`: boundaries, navigation surface, points of interest, and search clues.
- `Toys`: ball rules and active play sessions.
- `Input`: touch gestures and input-mode arbitration.
- `Camera`: framing and constraints for the owner-cat pair.
- `UI`: player information and commands; no provider orchestration.

## Runtime contracts

- Convert external payloads into internal models at the API boundary.
- Load each runtime character through a clear loader path; keep imported model correction separate from gameplay transforms.
- Discover scale, axes, skeleton, and available clips from each generated asset. Do not assume photo-generated models share consistent authoring.
- Keep owner movement and cat navigation independently owned; the cat can follow/interact without taking control away from the player.
- Keep story beats data-driven where useful and skippable; do not build story progression into movement or ball-play ownership.

## Change rules

- Extend an existing responsibility before adding a manager or singleton.
- New cross-system behavior needs a clear owner and dependency direction.
- Avoid global mutable state and repeated scene-wide searches.
- Keep provider and Unity models independent so either implementation can change.
- Record durable product/architecture decisions in `DECISIONS.md`.
