# Luna Task: Cat Model Phase 2 Validation

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/UNITY_PROJECT.md`
4. `context/ASSET_PIPELINE.md`
5. This task file

Do not read the complete product specification, inspect the sibling web project, or scan Unity-generated folders.

## Objective

Validate the existing locally generated cat in `Assets/CatMe/Scenes/ModelTest.unity` before it enters gameplay. Load one GLB with glTFast, preserve its supplied materials and rig, normalize its scale and floor placement, play its real embedded walking animation in place, and identify its actual forward axis.

This phase diagnoses and standardizes the asset. It does not add the cat to `HomeRoom` or make it navigate.

## Source asset

Use only:

```text
LocalFixtures/Cats/mochi-tripo-walk.glb
```

- Resolve the file from the project root at runtime, for example from `Application.dataPath/../LocalFixtures/Cats/mochi-tripo-walk.glb`.
- Do not copy the approximately 134 MB file into `Assets/`, `StreamingAssets/`, or Git.
- Do not edit, convert, compress, export, or overwrite the GLB.
- Do not call Meshy, Tripo, OpenAI, Supabase, or any network service.
- If the file is missing, stop cleanly and log the exact expected path.

## Required scene structure

Keep the diagnostic scene simple and use this authored structure:

```text
ModelTestRuntime
  Ground
  CatModelRoot
    AxisCorrection
      LoadedCat (created at runtime)
  ForwardMarker
  CameraRig
    Main Camera
  Lighting
```

`CatModelRoot` owns scale and floor alignment. `AxisCorrection` owns only the yaw correction required to make the cat face Unity world `+Z`. Never apply normalization transforms directly to bones or skinned meshes.

## Loader implementation

Create the smallest clear runtime loader under `Assets/CatMe/Scripts/Cat/`.

- Use the installed glTFast package.
- Load asynchronously without blocking the Unity main thread.
- Instantiate exactly one copy under `AxisCorrection`.
- Keep all geometry, skeletons, skin weights, materials, textures, and embedded animation clips supplied by the GLB.
- Expose a serialized target cat height of `0.35 m`.
- Expose a serialized `forwardYawDegrees` correction on `AxisCorrection`.
- Give loading, success, and failure states clear Console messages. A debug-only status label is optional and must remain inside `ModelTest`.
- Avoid adding a general asset system, manifest format, download cache, prefab pipeline, or abstraction intended for future providers.

## Bounds, scale, and floor alignment

After the model has instantiated and renderer bounds are valid:

1. Find every active `Renderer`, including `SkinnedMeshRenderer`.
2. Calculate the combined world-space bounds of the loaded cat.
3. Record the raw bounds and height.
4. Uniformly scale `CatModelRoot` so the cat is `0.35 m` tall.
5. Recalculate the bounds after scaling.
6. Translate the normalization wrapper so the cat is centered at world `X = 0`, `Z = 0` and its lowest bound rests at world `Y = 0`.
7. Record the applied scale and floor offset.

Do not use hand-entered offsets when the renderer bounds can determine them.

## Material validation

- Preserve the provider's base colour, normal, metallic, roughness or smoothness data where present.
- Do not replace the supplied materials with a generic Unity material.
- Do not add a custom fur shader, fur geometry, post-processing, or texture-generation step.
- Confirm the model is not magenta and its coat colour is visible under neutral lighting.
- Log the renderer and material counts so missing material data is obvious.

## Animation validation

- Discover and log every embedded animation clip name and duration. Do not assume a provider clip name.
- Select the first clip whose name contains `walk`, case-insensitive.
- If no clip contains `walk`, use the first usable embedded clip and emit a clear warning naming it.
- Loop the selected clip for at least 10 seconds in Play Mode.
- Keep the cat stationary at the scene origin while the legs and body animate.
- Disable or isolate root motion if the clip moves the root.
- Do not create a procedural walk, replace the clip, or add room movement.

## Forward-axis validation

- `ForwardMarker` must visibly point along Unity world `+Z`.
- Use `forwardYawDegrees` only on `AxisCorrection` to align the cat's anatomical forward direction with world `+Z`.
- Inspect the head, chest, paws, and walking motion in Play Mode and set the correction from observed behavior rather than assuming the source axis.
- Record the final yaw correction in the completion report and `context/STATUS.md`.
- Do not rotate the room, camera coordinate system, skeleton, or individual meshes to compensate.

## Diagnostic presentation

- Use a neutral ground plane and simple soft studio lighting compatible with URP.
- Use a perspective camera that keeps the entire `0.35 m` cat visible throughout the walk cycle.
- Frame the camera from a slight front three-quarter angle so forward direction and leg motion are readable.
- Keep the background plain. This is an asset test, not final room art.

## Required diagnostics

Log one concise validation summary containing:

- resolved local file path and byte size
- load duration
- renderer and skinned-mesh counts
- material count
- discovered animation clip names and durations
- selected walking clip
- raw model bounds and height
- normalized bounds and height
- applied uniform scale
- applied floor offset
- final forward yaw correction

Log an explicit error if the model has no renderers, no skinned mesh, or no usable animation clip.

## In scope

- `Assets/CatMe/Scenes/ModelTest.unity`
- Small focused scripts under `Assets/CatMe/Scripts/Cat/`
- Simple diagnostic materials, marker, lighting, or editor setup helper under `Assets/CatMe/`
- Documentation updates to `context/STATUS.md` after validation

## Out of scope

- Any change to `HomeRoom` or `Bootstrap`
- NavMesh, pathfinding, call behavior, or world-space cat movement
- Idle, sleep, petting, feeding, meow, jump, toy, needs, energy, or bond systems
- Swipe, pinch, orbit, follow, or gameplay camera controls
- UI, login, payments, shop, tokens, save data, analytics, or onboarding
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, or provider credentials
- GLB optimization, compression, conversion, re-rigging, or material replacement
- Final art or mobile performance work

## Acceptance checks

1. Unity compiles with zero new errors.
2. `ModelTest` loads exactly one cat from the named local fixture without copying it into `Assets/`.
3. The complete cat is visible, its supplied coat material renders, and no material is magenta.
4. Diagnostics confirm at least one `SkinnedMeshRenderer` and at least one usable embedded animation clip.
5. The selected walking clip loops visibly for at least 10 seconds.
6. During that 10-second check, the normalized cat root remains stationary within `0.02 m` of its starting horizontal position.
7. Normalized cat height is `0.35 m ± 0.02 m`, and its lowest rendered bound is within `0.01 m` of the ground.
8. The cat's anatomical forward direction and walk face world `+Z`, matching `ForwardMarker`.
9. The full cat remains inside the camera frame through the complete walk cycle.
10. A missing or invalid local fixture produces a clear failure message rather than a silent or stuck load.
11. No room, gameplay, API, provider, UI, or economy work is introduced.

## Validation

- Let Unity import and compile.
- Open only `ModelTest`.
- Enter Play Mode and wait for the local GLB to load.
- Observe the complete walking cycle for at least 10 seconds.
- Confirm stationary root position, scale, floor contact, materials, camera framing, and forward direction.
- Inspect the Console for the diagnostic summary and confirm zero errors after a successful load.
- Do not run provider calls, broad unrelated tests, or a production build.

## Completion report

Report:

- files created or changed
- resolved GLB byte size and load duration
- renderer, skinned-mesh, and material counts
- all discovered clips with durations
- selected walking clip
- raw and normalized bounds
- applied scale and floor offset
- final forward yaw correction
- result of the 10-second stationary walk check
- Console result
- any asset-specific risk that blocks later `HomeRoom` integration
- concise proposed update for `context/STATUS.md`

Stop after every acceptance check passes or after reporting a concrete blocker. Do not begin the next phase.
