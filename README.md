# CatMe Home

CatMe Home is the mobile game client for the personalized CatMe companion.

The player uploads a cat photograph through the existing CatMe generation service, approves the generated model, and brings the resulting rigged cat into a small interactive home. The room, toys, energy, sound, haptics, navigation, and animation are built in Unity.

## Project boundary

- This repository owns the Unity mobile game.
- `../clonecatme` remains the local generation laboratory and browser preview.
- Meshy and Tripo credentials remain on the server.
- The mobile client receives a versioned cat manifest and optimized GLB.
- Routine game development uses a known-good local cat and spends no provider credits.

## Start here

For development tasks, start with [context/README.md](context/README.md) and [context/STATUS.md](context/STATUS.md). Open only the context module associated with the task.

Use [docs/CATME_MOBILE_GAME_IMPLEMENTATION.md](docs/CATME_MOBILE_GAME_IMPLEMENTATION.md) when a task needs the full product specification.

Use [docs/CATME_EXPERIENCE_DESIGN_V1.md](docs/CATME_EXPERIENCE_DESIGN_V1.md) for the approved room, camera, behavior, interaction, relationship, memory, and economy experience.

The first playable proof is:

```text
Local animated cat GLB
→ correct Unity import
→ correct scale and forward direction
→ HomeRoom scene
→ NavMesh walking
→ cat-house destination
→ hidden sleep transition
```

## Imported prototype assets

The minimum useful assets were copied from the browser prototype:

| Asset | New location | Use |
|---|---|---|
| Completed Tripo walking cat | `LocalFixtures/Cats/mochi-tripo-walk.glb` | Local model and animation test |
| Simple white room | `Assets/CatMe/Art/Environments/white-room1.glb` | Initial room blockout |
| Cat meow | `Assets/CatMe/Audio/cat-meow.mp3` | Call and voice feedback |
| Cat purr | `Assets/CatMe/Audio/cat-purr.mp3` | Petting and sleep feedback |

`LocalFixtures` is ignored by Git because the walking model is approximately 134 MB. It remains available locally for development without another Meshy or Tripo request.

## Unity setup

The project is configured for Unity `6000.6.2f1` with:

- Universal Render Pipeline
- Input System
- Cinemachine
- AI Navigation
- glTFast runtime GLB loading
- Portrait orientation
- Linear colour space
- 60 FPS runtime target
- Android and iOS application ID `com.catme.home`

Starter scenes:

- `Bootstrap.unity` — application startup and persistent services.
- `ModelTest.unity` — isolated generated-cat inspection.
- `HomeRoom.unity` — room gameplay.

Use **CatMe → Configure Project** inside Unity to regenerate the foundation after an intentional settings reset.

# catme-
