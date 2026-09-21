# Current Handoff

Last updated: 2026-09-21

## Current objective

Build the smallest convincing mobile CatMe prototype one bounded phase at a time. The room blockout and isolated local cat validation are complete; the next bounded phase can place this validated cat into the room.

## Verified foundation

- Unity `6000.6.2f1` project opens and compiles.
- URP, Input System, Cinemachine, AI Navigation, glTFast, and UGUI are installed.
- Portrait orientation, Linear colour space, 60 FPS runtime target, and application ID `com.catme.home` are configured.
- Scenes exist: `Bootstrap`, `ModelTest`, and `HomeRoom`.
- Local fixture exists at `LocalFixtures/Cats/mochi-tripo-walk.glb` and is intentionally ignored by Git.
- The simple room and cat meow/purr audio are under `Assets/CatMe/`.
- Only Mac and WebGL editor support are currently installed. iOS Build Support and Xcode can be installed before physical iPhone testing.

## Completed milestone

Room Phase 1: complete.

Verified:

- `HomeRoom` contains the required `Room_Blockout` hierarchy, scaled architecture, zones, props, interaction points, camera anchors, lighting, and scale reference.
- The room is approximately `6.5 m × 5.0 m × 2.8 m` with a clear central play area.
- Feeding and sleep geometry support the planned occlusion transitions.
- A baked NavMesh reaches `CallDestination`, `WindowApproach`, `SleepApproach`, `FeedingApproach`, and `ToyApproach`.
- Unity Console showed zero errors during the final build and validation.

## Completed milestone

Cat Model Phase 2: isolated local GLB validation.

Verified in Unity Play Mode:

- `ModelTest` loaded exactly one local fixture from `LocalFixtures/Cats/mochi-tripo-walk.glb` through glTFast.
- The runtime hierarchy contained `ModelTestRuntime/CatModelRoot/AxisCorrection/LoadedCat`, plus `Ground`, `ForwardMarker`, `CameraRig`, and `Lighting`.
- The supplied cat rendered with its imported material and coat; no material errors were logged.
- Diagnostics reported one renderer, one skinned mesh, one material, and the embedded `preset:quadruped:walk` clip at `2.6 s`.
- The cat normalized to `0.35 m`, rested on the floor, and matched the world `+Z` marker with `0` degrees of yaw correction.
- The automatic 10-second walk acceptance logged `PASS`, with `maxRootDrift=0 m`, `height=0.35 m`, and `floorDistance=0 m`.
- Unity Console showed zero errors after the corrected compile and successful runtime load.

## Current milestone

HomeRoom Cat Integration Phase 3.

Active task:

```text
context/tasks/HOME_ROOM_CAT_PHASE_3.md
```

The task extracts the proven reusable local loading behavior, places exactly one validated cat at `CatSpawn` inside `HomeRoom`, and verifies room scale, floor contact, materials, camera visibility, and a stationary in-place walk. It must preserve the completed `ModelTest` acceptance and stop before navigation or interaction work.

## Boundaries

- No Meshy, Tripo, OpenAI, Supabase, login, payment, or remote-download work during this milestone.
- Do not spend provider credits.
- Do not start navigation, interactions, UI, or the creation funnel during Phase 3.
- Do not commit the 134 MB local fixture.

## Known risks

- The provider GLB is approximately 134 MB and is too large for production mobile delivery.
- Animation clip name, model scale, floor offset, and forward axis must be discovered from the asset rather than assumed.
- Physical iPhone behavior remains unverified until iOS tooling is installed.
- Room composition has been checked in the Unity editor Game view but not yet on a physical portrait device.
