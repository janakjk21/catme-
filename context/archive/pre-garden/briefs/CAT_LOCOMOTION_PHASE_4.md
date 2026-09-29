# Luna Task: Cat Locomotion and Navigation Phase 4

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/GAMEPLAY.md`
4. This task file

Inspect only these implementation surfaces and their direct dependencies:

- `Assets/CatMe/Scripts/Cat/LocalCatAssetLoader.cs`
- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/LocalCatModelLoader.cs`
- `Assets/CatMe/Scenes/HomeRoom.unity`
- `Assets/CatMe/Scenes/ModelTest.unity`
- The existing navigation and interaction-point setup in `Assets/CatMe/Editor/RoomBlockoutSetup.cs`

Do not inspect the sibling web project, generated Unity folders, or the complete product specification.

## Objective

Make the validated local cat walk naturally between existing authored destinations in `HomeRoom`. Use the baked NavMesh for world movement and synchronize the supplied walking clip with actual velocity so the cat turns, accelerates, walks forward, slows, and stops without sliding sideways or teleporting.

This phase proves locomotion through an automatic diagnostic route. It does not add player controls, call behavior, interactions, needs, UI, or new animations.

## Preserved foundation

Use only:

```text
LocalFixtures/Cats/master-walking-cat.glb
```

Preserve these runtime contracts:

- Runtime hierarchy: `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`
- Normalized height: `0.35 m`
- Asset anatomical forward: local `+Z`
- Asset-forward correction: `0` degrees at `AxisCorrection`
- Select a walk clip by name, case-insensitive, rather than assuming a provider-specific clip name
- Imported material, textures, skeleton, and skin weights
- Imported animation remains in place locally; world movement belongs to the gameplay root
- `ModelTest` remains an isolated asset diagnostic and must keep passing

This local fixture was restored from the existing browser-prototype asset
`clonecatme/public/cat/master_walking_cat.glb` and must be revalidated in
`ModelTest`. Do not copy it into `Assets/`, `StreamingAssets/`, or Git. Do not
call any provider or network API.

## Architecture

Create one small runtime movement component under `Assets/CatMe/Scripts/Cat/`:

```text
CatMotor.cs
```

`CatMotor` owns:

- `NavMeshAgent` setup and destination requests
- world position and world-facing rotation of `CatRuntime`
- smooth acceleration and deceleration
- stopping distance and arrival detection
- walk clip play, pause, and playback-speed synchronization from actual horizontal velocity
- movement diagnostics needed by this phase

Keep responsibilities separate:

- `LocalCatAssetLoader` continues to own only GLB loading, normalization, materials, clip discovery, and local root-motion isolation.
- `HomeRoomCatIntegration` continues to own scene integration and should initialize `CatMotor` only after the cat loads successfully.
- `CatMotor` moves `CatRuntime`; it must not move `AxisCorrection`, imported bones, or `LoadedCat` directly.
- Do not introduce `CatBrain`, `CatNeeds`, `CatAnimationController`, a state-machine framework, service locator, or dependency-injection framework in this phase.

Use `NavMeshAgent.updateRotation = false` and rotate the gameplay root deliberately so the visible cat's local `+Z` aligns with horizontal travel direction. Keep vertical movement and floor placement controlled by the NavMeshAgent.

## Authored route

Use the already authored transforms. Do not create substitute coordinates:

```text
CatSpawn
CallDestination
WindowApproach
SleepApproach
FeedingApproach
ToyApproach
```

Run a temporary deterministic diagnostic route after successful loading:

```text
CatSpawn
→ CallDestination
→ WindowApproach
→ SleepApproach
→ FeedingApproach
→ ToyApproach
→ CatSpawn
```

Repeat destinations as needed until at least ten consecutive path legs have completed. Pause briefly at each destination so arrival and animation stopping are visible. This automatic route is diagnostic code for Phase 4 and must be easy to disable before later player-directed behavior.

Resolve every destination by name and report a clear error if one is missing or cannot be sampled onto the NavMesh. Do not silently fall back to world origin.

## Movement tuning

Start with cat-scale values and adjust only when visual validation requires it:

- Speed: approximately `0.55 m/s`
- Acceleration: approximately `1.8 m/s²`
- Angular speed: approximately `300 degrees/s`
- Stopping distance: approximately `0.07 m`
- Destination pause: approximately `0.6 s`

Requirements:

- Rotate substantially toward a new path direction before accelerating to full speed.
- Continue turning smoothly while moving.
- Accelerate and decelerate without a visible snap.
- Never teleport between destinations.
- Do not modify `transform.forward` with an extra 90-degree workaround. The visible cat must face its velocity using the validated local `+Z` contract.
- Keep the root-motion isolation that prevents the imported clip from fighting NavMesh movement.
- Stop or pause the walk clip while stationary. Do not leave the legs cycling during destination pauses.
- Resume the same imported walk clip when movement begins.
- Scale animation playback speed from actual horizontal movement velocity, using a conservative range such as `0.7` to `1.2` while moving.
- Do not invent an idle animation or procedurally animate bones.

## Phase 3 preservation

Before enabling the diagnostic route, retain a short stationary integration check that proves the imported walk has no local root drift and the cat remains aligned to the floor. It may be shorter than the former manual observation period if the existing Phase 3 measurement logic is reused reliably.

The final HomeRoom locomotion test must still satisfy:

- exactly one loaded cat
- `0.35 m ± 0.02 m` rendered height
- rendered lower bound within `0.01 m` of the floor while stationary
- imported coat material remains visible
- `AxisCorrection` stays at `0` degrees
- no movement is applied to imported bones or the loaded model root

Do not change the existing `ModelTest` acceptance behavior.

## Diagnostics

Log one concise locomotion validation summary after at least ten consecutive successful path legs. Include:

- completed path-leg count
- failed, partial, off-NavMesh, and stuck counts
- maximum arrival error
- minimum forward-to-velocity dot product measured while moving after the initial turn-in period
- maximum unintended local drift of `CatModelRoot`, `AxisCorrection`, or `LoadedCat`
- configured speed, acceleration, angular speed, and stopping distance
- selected walk clip
- final result: `PASS` or `FAIL`

Log an explicit error for a missing destination, missing or invalid NavMesh, rejected destination, path failure, stuck timeout, duplicate cat, missing animation, or lost floor contact.

Do not log every frame.

## In scope

- `CatMotor.cs` and its `.meta`
- Minimal integration changes required after the HomeRoom cat loads
- NavMeshAgent configuration on the existing `CatRuntime`
- Automatic diagnostic route between existing points
- Velocity-aligned rotation and walk playback synchronization
- Focused editor setup changes only if required to attach/configure the component
- Focused update to `context/STATUS.md` after validation

## Out of scope

- Tap-to-move, touch input, call button, player-selected destinations, or camera gestures
- Petting, feeding, sleeping, toys, laser play, meow behavior, reactions, energy, mood, hunger, or bond
- `CatBrain`, autonomous behavior selection, needs simulation, or save/load
- UI, buttons, onboarding, login, shop, tokens, payments, analytics, or economy
- New animations, procedural bone animation, re-rigging, GLB modification, or model optimization
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, provider credentials, or credit spending
- Room redesign, NavMesh rebake without a demonstrated defect, final art, audio, haptics, or mobile performance work

## Acceptance checks

1. Unity compiles with zero new errors.
2. `ModelTest` still loads the same local fixture and its existing automatic acceptance reports `PASS`.
3. `HomeRoom` loads exactly one cat using the existing Phase 3 hierarchy, scale, material, floor alignment, and asset-forward correction.
4. The existing baked NavMesh produces complete paths to all five authored approach points and back to `CatSpawn`.
5. The automatic route completes at least ten consecutive path legs without teleporting, becoming stuck, leaving the NavMesh, entering walls, or visibly intersecting furniture.
6. Arrival error is no greater than `0.10 m` for every completed destination.
7. After the initial turn-in period, the visible cat faces its horizontal travel direction with a forward-to-velocity dot product of at least `0.95` while moving.
8. Movement starts and stops smoothly, with no obvious one-frame position or rotation snap.
9. The walk clip plays only while moving, pauses or stops at destinations, and changes playback speed with actual velocity without severe foot sliding.
10. Imported local root motion does not fight navigation; `CatModelRoot`, `AxisCorrection`, and `LoadedCat` show no accumulating local drift beyond `0.02 m`.
11. The locomotion validation summary reports `PASS`, and Unity Console shows zero current errors after the successful run.
12. No player interaction, UI, needs, economy, provider, room-redesign, or unrelated system is introduced.

## Validation

1. Run a focused C# compile.
2. Open `ModelTest`, enter Play Mode, wait for the local GLB, and confirm the existing acceptance still reports `PASS`.
3. Stop Play Mode and open `HomeRoom`.
4. Enter Play Mode and allow the automatic route to complete at least ten consecutive path legs.
5. Observe turns from front, side, and diagonal travel directions. Confirm the cat's head and chest point along its actual movement rather than sideways.
6. Observe at least two arrivals. Confirm smooth deceleration, stopped legs during the pause, and smooth resumption.
7. Confirm the cat stays on walkable floor, avoids walls and furniture, retains its coat material, and remains the only loaded cat.
8. Confirm the Console contains the final locomotion `PASS` summary and no current errors.
9. Run `git diff --check`.
10. Do not run provider calls, broad unrelated tests, or a production build.

## Completion report

Report:

- files created or changed
- CatMotor ownership and how it is initialized
- route destinations used
- movement and animation-speed settings
- completed path-leg count and all failure counts
- maximum arrival error
- minimum forward-to-velocity alignment
- maximum local drift
- ModelTest result
- HomeRoom locomotion result
- Unity Console and focused compile result
- any concrete blocker before adding player-directed call behavior
- concise proposed update for `context/STATUS.md`

Stop after every acceptance check passes or after reporting a concrete blocker. Do not begin interactions or UI.
