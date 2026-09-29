# Luna Task: Player Call Behavior Phase 5

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/GAMEPLAY.md`
4. `context/MOBILE.md`
5. This task file

Inspect only these implementation surfaces and their direct dependencies:

- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/LocalCatAssetLoader.cs`
- `Assets/CatMe/Editor/Phase4BatchValidator.cs`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`
- `Assets/CatMe/Scenes/HomeRoom.unity`
- Existing `CallDestination`, camera, Canvas, and EventSystem objects in `HomeRoom`

Do not inspect the sibling web project, generated Unity folders, the full product specification, or unrelated game systems.

## Objective

Replace the normal-play automatic route with one player-directed action: pressing a visible mobile-friendly `Call` control makes the loaded cat walk to the existing authored `CallDestination`, stop naturally, face the room camera, and wait there.

This phase proves the first complete player-to-cat interaction. It does not add general tap-to-move, petting, needs, reactions, sound, haptics, or final interface art.

## Preserved foundation

Use only:

```text
LocalFixtures/Cats/master-walking-cat.glb
```

Preserve:

- `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`
- normalized cat height `0.35 m`
- asset anatomical forward local `+Z`
- existing imported material, skeleton, skin weights, and walk clip
- existing root-motion isolation
- existing NavMesh, room geometry, `CatSpawn`, and `CallDestination`
- Phase 4 speed, acceleration, turning, stopping distance, and velocity-based walk playback
- `ModelTest` acceptance and Phase 4 batch validation

Do not modify the GLB, rebake the room NavMesh, or call a provider/network API.

## Architecture

Keep `CatMotor` as the only owner of navigation and world movement.

Extend it with a small reusable command API such as:

```csharp
public bool TryMoveTo(Transform destination, string reason)
public bool IsMoving { get; }
public bool HasArrived { get; }
```

The exact names may differ, but the behavior must be explicit and testable.

Create one small input component under `Assets/CatMe/Scripts/Input/`:

```text
CatCallInput.cs
```

`CatCallInput` owns only:

- the single Call control binding
- resolving the existing `CallDestination`
- forwarding the request to `CatMotor`
- short input debounce
- disabling the control until the cat is loaded and the motor is ready

It must not own navigation, animation playback, cat loading, camera movement, needs, or behavior selection.

Do not create `CatBrain`, a general interaction framework, dependency injection, an event bus, or a state-machine framework.

## Normal play behavior

- `HomeRoomCatIntegration` still loads and validates exactly one cat.
- After the stationary acceptance passes, initialize `CatMotor` but do not automatically start the Phase 4 diagnostic route.
- The cat waits at `CatSpawn` with its walk clip stopped or paused.
- Display one simple, clearly tappable `Call` control in the portrait safe area.
- Mouse click must activate the same control in the Editor.
- Pressing Call requests a complete NavMesh path to the existing `CallDestination`.
- The cat turns, accelerates, walks forward, decelerates, and stops using the Phase 4 motor behavior.
- Repeated presses while already travelling to the same destination must not restart the animation, teleport the cat, or create duplicate requests.
- On arrival, stop the walk clip and rotate the cat smoothly to the authored `CallDestination` facing direction so it appears attentive to the player.
- If the cat is already at the call point, pressing Call may produce a concise diagnostic log but must not jitter or replay walking.
- If the path is invalid, keep the cat in place and log one clear error.

## Diagnostic route preservation

Retain `BeginDiagnosticRoute()` for validation, but never invoke it automatically during normal `HomeRoom` play.

Update `Phase4BatchValidator` only as needed so it explicitly starts the diagnostic route after the stationary acceptance. Phase 4 must continue to complete its 11-leg validation independently of the new Call control.

Do not duplicate Phase 4 movement code in the Call input component.

## Temporary Call control

The control is validation UI, not the final interface. Keep it deliberately small in scope:

- one `Call` button only
- minimum mobile touch target approximately `48 × 48` logical pixels
- anchored inside the portrait safe area near the bottom center
- high-contrast text and background
- no menu, HUD, status meter, icons, animation, localization system, or design-system work
- avoid covering the cat or the room's main interaction area

Prefer existing UGUI support already installed in the project. Do not add a new UI package.

## In scope

- Reusable one-destination command support in `CatMotor`
- `CatCallInput.cs` and its `.meta`
- Minimal HomeRoom integration after the cat loads
- One temporary mobile-friendly Call button
- Arrival facing at the authored `CallDestination`
- Input debounce and duplicate-request protection
- Focused Phase 5 automated or Editor validation
- Focused update to `context/STATUS.md` after successful validation

## Out of scope

- Tap-to-move, arbitrary floor destinations, joystick control, or camera gestures
- Petting, feeding, sleeping, toys, laser play, meow reactions, sound, particles, or haptics
- Energy, mood, hunger, bond, memories, persistence, or autonomous behavior
- Final UI art, navigation menus, onboarding, login, shop, tokens, payments, or analytics
- New animations, procedural bone animation, re-rigging, GLB editing, or optimization
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, credentials, or provider spending
- Room redesign, new props, lighting work, or NavMesh rebaking without a demonstrated defect

## Acceptance checks

1. Unity compiles with zero new errors.
2. `ModelTest` still reports `PASS` for the same local fixture.
3. Normal `HomeRoom` play loads exactly one cat and leaves it waiting at `CatSpawn`; the automatic diagnostic route does not start.
4. One visible Call button appears inside the portrait safe area and works with both touch-compatible UI input and an Editor mouse click.
5. The Call button remains disabled until the cat and `CatMotor` are ready.
6. One Call press sends the cat to the existing `CallDestination` through a complete NavMesh path with no teleport or sideways travel.
7. Movement retains the validated Phase 4 acceleration, rotation, deceleration, floor contact, and velocity-synchronized walk playback.
8. The cat arrives within `0.10 m`, stops its leg animation, and smoothly faces the authored destination rotation.
9. Repeated Call presses during travel or after arrival do not jitter, restart, duplicate the cat, or create duplicate navigation commands.
10. An invalid or missing destination fails clearly and leaves the cat stable.
11. The explicit Phase 4 batch validator still completes at least ten consecutive path legs and reports `PASS`.
12. Unity Console shows zero current errors after successful validation, and `git diff --check` passes.
13. No unrelated interaction, gameplay, provider, room, or final UI system is introduced.

## Validation

1. Run a focused C# compile.
2. Run the existing Phase 4 batch validator and confirm ModelTest, stationary acceptance, and the 11-leg route still report `PASS`.
3. Open `HomeRoom` in normal Play Mode and wait until the Call button enables.
4. Confirm the cat remains stationary at `CatSpawn` before input.
5. Press Call once and observe the entire trip from turning through arrival.
6. Press Call repeatedly during travel and after arrival; confirm there is no restart, jitter, teleport, or duplicate request.
7. Confirm the cat stops within `0.10 m`, stops walking animation, and faces the authored call-point direction.
8. Confirm exactly one cat remains loaded with the same material, scale, hierarchy, floor contact, and local-root stability.
9. Confirm the Console has one concise call result and no current errors.
10. Run `git diff --check`.
11. Do not run provider calls, broad unrelated tests, or a production build.

## Completion report

Report:

- files changed
- how normal play differs from the Phase 4 diagnostic mode
- how the Call input reaches `CatMotor`
- arrival distance and facing result
- duplicate-press behavior
- Phase 4 regression result
- compile and Console result
- any concrete blocker before adding petting or camera gestures

