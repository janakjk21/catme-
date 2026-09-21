# Architecture Contract

## System boundary

CatMe has three separate layers:

```text
Generation server → product service and asset storage → Unity mobile client
```

- Generation server owns photo preparation, Meshy, Tripo, provider polling, validation, and mobile asset production.
- Product service owns cat identity, version manifests, approved assets, ownership, and signed downloads.
- Unity owns presentation, cached runtime assets, room simulation, input, navigation, animation, audio, haptics, and local play state.

Do not move provider logic or credentials into Unity.

## Unity dependency direction

```text
UI and Input
    ↓ commands
Gameplay services: CatBrain, Toys, Room
    ↓
CatMotor, CatAnimationController, CatNeeds
    ↓
CatLoader and Unity engine adapters

API → manifest/download → CatAssetCache → CatLoader
Save → stable data models, never scene objects
```

Lower layers must not import UI behavior. Cat systems communicate through small commands, state, or events instead of searching arbitrary scene objects.

## Module ownership

- `Core`: startup, shared lifecycle, app state, and composition.
- `API`: server transport and response mapping.
- `Save`: persistence, cache metadata, and migrations.
- `Cat`: loading, movement, animation, needs, and behavior.
- `Room`: room bounds, navigation surface, and interaction points.
- `Toys`: toy rules and active play sessions.
- `Input`: gestures and input-mode arbitration.
- `Camera`: camera states and constraints.
- `UI`: display and player commands; no provider orchestration.

## Runtime data contracts

- Treat the server cat manifest as versioned input.
- Convert external payloads into internal models at the API boundary.
- Keep scene references serialized or injected from composition code.
- Load generated cats through one `CatLoader` path.
- Apply scale, axis correction, and floor offset at a dedicated model root.
- Read clip names from the manifest or loaded asset instead of embedding provider names in gameplay.

## Change rules

- Extend an existing responsibility before adding a new manager or singleton.
- New cross-system behavior needs a clear owner and dependency direction.
- Avoid global mutable state and repeated scene-wide object searches.
- Keep provider and Unity models independent so either implementation can change.
- Record durable changes to these boundaries in `DECISIONS.md`.

## Architecture review triggers

Load this module when adding a service, changing data ownership, introducing networking, connecting generation, changing persistence, or making two gameplay systems depend on each other. Ordinary isolated component changes do not require a full architecture review.
