# Luna Task: HomeRoom Cat Integration Phase 3

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/ASSET_PIPELINE.md`
4. `context/GAMEPLAY.md`
5. This task file

Inspect only these implementation surfaces and their direct dependencies:

- `Assets/CatMe/Scripts/Cat/LocalCatModelLoader.cs`
- `Assets/CatMe/Scenes/ModelTest.unity`
- `Assets/CatMe/Scenes/HomeRoom.unity`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`

Do not inspect the sibling web project, generated Unity folders, or the complete product specification.

## Objective

Place the already validated local cat into `HomeRoom` at realistic scale and prove that it renders correctly inside the room. Reuse the working glTFast loading and normalization behavior from `ModelTest` without bringing ModelTest's diagnostic ground, marker, camera, or validation presentation into gameplay.

This phase establishes one visible cat inside the room. It does not add navigation, player controls, interactions, needs, or UI.

## Source asset

Use only:

```text
LocalFixtures/Cats/mochi-tripo-walk.glb
```

Known validated values:

- Source size: `140235836` bytes
- One renderer
- One skinned mesh
- One material
- Walk clip: `preset:quadruped:walk`
- Clip duration: `2.6 s`
- Normalized cat height: `0.35 m`
- Asset forward correction: `0` degrees; the cat's anatomical forward is local `+Z`

Do not copy the GLB into `Assets/`, `StreamingAssets/`, or Git. Do not call any provider or network API.

## Architecture requirement

Do not attach the existing diagnostic `LocalCatModelLoader` directly to `HomeRoom`; it creates ModelTest-only ground, camera, marker, and logging presentation.

Extract the proven reusable loading portion into one small runtime component under `Assets/CatMe/Scripts/Cat/`. A suitable name is:

```text
LocalCatAssetLoader.cs
```

It should own only:

- resolving the known local fixture path
- asynchronous glTFast loading and instantiation
- preserving materials, textures, skeleton, and skin weights
- discovering embedded animation clips without assuming a provider name
- normalizing the cat to a serialized target height
- centering the model and aligning its rendered lower bound to local floor height
- applying a serialized asset-forward yaw correction at `AxisCorrection`
- exposing the loaded root, selected walk clip, imported `Animation`, final bounds, and success or failure state

Keep ModelTest-only camera setup, ground, forward marker, diagnostics, and 10-second acceptance logic in `LocalCatModelLoader`. Refactor it to use the shared loader if needed, but preserve its existing successful behavior.

Avoid creating a general manifest, download service, provider client, save system, or dependency framework.

## HomeRoom hierarchy

Add this runtime hierarchy without restructuring `Room_Blockout`:

```text
CatRuntime
  CatModelRoot
    AxisCorrection
      LoadedCat (created at runtime)
```

Rules:

- Keep `CatRuntime` separate from `Room_Blockout` so authored room geometry stays independent from the runtime cat.
- Use the existing `CatSpawn` transform as the starting position.
- If `CatSpawn` is missing, report a clear error. Do not silently use world origin.
- `CatModelRoot` owns normalization and floor offset.
- `AxisCorrection` owns only the validated asset correction of `0` degrees.
- `CatRuntime` may face the room camera using its own world rotation. Keep this gameplay-facing rotation separate from the asset correction.
- Do not edit bones, skin weights, mesh transforms, or imported materials.

## Initial placement

- Place `CatSpawn` on reachable open floor near the center of the room, approximately `(0, 0, -0.5)` if its current location is unsuitable.
- Keep it outside furniture and clear of walls.
- Face the cat toward the open camera side for the initial room presentation. With the current room coordinate contract, this will normally mean rotating `CatRuntime` approximately `180` degrees around `Y`, while leaving `AxisCorrection` at `0`.
- Align the cat's rendered lower bound with the room floor, within `0.01 m`.
- Keep the rendered height at `0.35 m ± 0.02 m`.
- The complete cat must be visible from the existing `HomeAnchor` camera composition.
- Do not redesign the room or replace its camera anchors.

## Temporary animation behavior

For this integration phase, loop the validated walking clip in place so materials, skinning, lighting, and animation can be observed inside the room.

- Select a clip containing `walk`, case-insensitive, with first usable clip as a warned fallback.
- Loop it using the imported legacy `Animation` component.
- Isolate root motion so the cat remains at `CatSpawn` within `0.02 m` for 10 seconds.
- Do not translate the cat through the room yet.
- Do not create a fake or procedural walking clip.
- Mark the in-place loop as temporary diagnostic behavior for Phase 3. The later locomotion phase will synchronize it with actual movement.

## Scale reference

- Keep the authored `CatScaleReference` in the scene for future editing.
- Disable its renderer during Play Mode after the real cat loads so the player sees only one cat-sized subject.
- Do not delete the reference object.

## Lighting and materials

- Preserve the imported coat material and all supplied maps.
- Do not replace the cat material or add a fur shader.
- Do not change room lighting unless a small, documented intensity adjustment is required for the cat to be readable.
- The cat must not appear magenta, untextured, or completely black.
- Do not add post-processing or final art.

## Diagnostics

Log one concise HomeRoom integration summary containing:

- resolved local file path and byte size
- load duration
- renderer, skinned-mesh, and material counts
- discovered clips and selected clip
- normalized height and floor distance
- asset-forward yaw correction
- `CatRuntime` room-facing yaw
- resolved `CatSpawn` world position

Log an explicit error for a missing fixture, missing `CatSpawn`, no renderer, no skinned mesh, no usable clip, or failed instantiation.

## In scope

- Small reusable cat-loading code under `Assets/CatMe/Scripts/Cat/`
- `ModelTest` changes required to preserve its diagnostic validation after extraction
- `HomeRoom` cat runtime hierarchy and loader configuration
- `CatSpawn` adjustment when required
- Runtime hiding of `CatScaleReference`
- Focused updates to `context/STATUS.md` after validation

## Out of scope

- NavMeshAgent, pathfinding, destinations, call behavior, or world movement
- Cat state machine, `CatMotor`, `CatBrain`, or `CatNeeds`
- Petting, feeding, sleeping, toys, laser play, meow behavior, energy, mood, hunger, or bond
- Camera gestures, Cinemachine transitions, follow camera, or camera redesign
- UI, buttons, login, shop, tokens, payments, saving, analytics, or onboarding
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, or provider credentials
- GLB optimization, compression, conversion, re-rigging, or texture generation
- Room redesign, final environment assets, or mobile performance optimization

## Acceptance checks

1. Unity compiles with zero new errors.
2. `ModelTest` still loads the same local cat and its automatic 10-second acceptance still reports `PASS`.
3. `HomeRoom` loads exactly one real cat from the named local fixture.
4. The runtime hierarchy matches `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`.
5. The real cat is `0.35 m ± 0.02 m` tall and its rendered lower bound is within `0.01 m` of the room floor.
6. The imported coat material renders under room lighting with no magenta or missing-material result.
7. The complete cat is visible from the existing `HomeAnchor` composition and is not inside furniture or a wall.
8. `AxisCorrection` remains `0` degrees, while room-facing rotation is applied only at `CatRuntime`.
9. The walking clip loops visibly for 10 seconds and the runtime root stays within `0.02 m` of `CatSpawn`.
10. `CatScaleReference` remains authored but is hidden during Play Mode after the real cat loads.
11. Unity Console reports the HomeRoom integration summary and zero errors after successful loading.
12. No navigation, interaction, UI, API, provider, economy, or room-redesign work is introduced.

## Validation

1. Run a focused C# compile.
2. Open `ModelTest`, enter Play Mode, wait for the GLB, and confirm its existing 10-second acceptance still reports `PASS`.
3. Stop Play Mode and open `HomeRoom`.
4. Enter Play Mode, wait for the GLB, and observe the cat for at least 10 seconds.
5. Confirm one cat, correct room placement, full camera visibility, coat material, scale, floor contact, forward direction, looping walk, and stationary root.
6. Confirm the Console contains the integration summary with zero errors.
7. Do not run provider calls, broad unrelated tests, or a production build.

## Completion report

Report:

- files created or changed
- whether loading logic was extracted or reused and how ModelTest was preserved
- fixture byte size and HomeRoom load duration
- renderer, skinned-mesh, and material counts
- discovered and selected animation clips
- normalized height and floor distance
- `CatSpawn` position
- asset-forward and room-facing yaw values
- result of the 10-second stationary walk check in both scenes
- Console result
- any concrete blocker before the locomotion phase
- concise proposed update for `context/STATUS.md`

Stop after every acceptance check passes or after reporting a concrete blocker. Do not begin navigation or cat gameplay.
