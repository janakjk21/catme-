# Luna Task: Laser Toy Interaction Phase 9

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

Inspect only these implementation surfaces and direct dependencies:

- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Cat/CatPetReaction.cs`
- `Assets/CatMe/Scripts/Cat/CatSleepInteraction.cs`
- `Assets/CatMe/Scripts/Input/CatCallInput.cs`
- `Assets/CatMe/Scripts/Input/CatPetInput.cs`
- `Assets/CatMe/Scripts/Camera/RoomOrbitCamera.cs`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`
- existing Phase 4, 7, and 8 batch validators
- `Assets/CatMe/Scenes/HomeRoom.unity`

Do not inspect the sibling web project, generated Unity folders, the complete product specification, or unrelated systems.

## Objective

Add the first prop-led game activity using the existing toy in `ToyBasket` and the existing open play area.

The player taps the existing toy prop to begin a short laser-pointer activity. While the activity is active, the player places a glowing laser dot on a valid open-floor point. The cat follows that point through the existing `CatMotor`, reaches it, performs a restrained procedural catch response, and waits for the next target. After three successful catches, the activity ends and normal Call, petting, sleeping, and room-camera behavior resume.

Do not add energy, rewards, bond, needs, saving, audio, new animations, final UI, or economy in this phase.

## Preserved foundation

Use only the validated local cat:

```text
LocalFixtures/Cats/master-walking-cat.glb
```

Preserve:

- ModelTest acceptance
- one cat and `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`
- scale, material, floor contact, skeleton, and embedded walk clip
- Phase 4 locomotion tuning and explicit diagnostic route
- Phase 5 Call behavior
- Phase 6 camera orbit, zoom, limits, collision protection, and UI exclusion
- Phase 7 petting, purr, gesture ownership, and neutral restoration
- Phase 8 Sleep/Wake behavior, activity lock, renderer restoration, and `Zzz` cue
- the existing room, baked NavMesh, `OpenPlayZone`, `ToyBasket`, `Toy`, and `ToyApproach`

Do not modify the GLB, imported bones, room geometry, interaction-point transforms, NavMesh, camera limits, or locomotion tuning.

## Activity flow

Use this bounded flow:

```text
Idle
→ tap existing Toy prop
→ Aiming
→ place laser dot on valid OpenPlayZone floor
→ Chasing
→ Catch response
→ Aiming for next target
→ finish automatically after 3 catches
→ Idle
```

Requirements:

- The session starts only when the loaded cat is stationary, awake, and not owned by another activity.
- The existing toy prop is the start control. Do not add a Laser button.
- Use exactly three successful catches for the prototype session.
- Repeated toy taps cannot create duplicate sessions, dots, colliders, or input handlers.
- The session ends cleanly after catch three and releases every activity and pointer lock.

## Architecture

Create one focused component:

```text
Assets/CatMe/Scripts/Toys/LaserToyInteraction.cs
```

`LaserToyInteraction` owns:

- identifying the existing toy prop
- a runtime-owned, enlarged trigger used only for reliable toy tapping
- mouse and one-finger laser input
- the activity state and three-catch session
- one reusable laser-dot visual
- valid-floor targeting
- the subtle catch response
- conservative platform-gated haptic feedback
- focused diagnostics

`CatMotor` remains the sole owner of cat world movement. Add only the smallest activity-command API needed so the laser activity can keep the motor's existing activity lock while requesting each target. Do not duplicate NavMesh pathing, rotation, acceleration, animation playback, or arrival logic.

Do not add a general toy manager, `CatBrain`, `CatNeeds`, event bus, singleton, service locator, or dependency-injection framework.

## Toy activation target

- Resolve the existing `Room_Blockout/Props_Blockout/ToyBasket/Toy` object.
- Create one runtime trigger target around it large enough for reliable portrait touch without changing the visible prop.
- The trigger must identify only the toy and must not affect room physics or NavMesh movement.
- A pointer that starts on UGUI remains UI-owned.
- A pointer that starts on the stationary cat remains petting-owned when no laser session is active.
- A pointer that starts elsewhere remains camera-owned unless it starts on the explicit toy trigger.

## Laser targeting and gesture ownership

- During `Aiming`, a one-finger touch or Editor mouse press on valid open floor belongs to the laser until release.
- Reserve that pointer through the existing `RoomOrbitCamera` pointer-suppression API.
- Update the laser dot while the captured pointer remains over valid floor.
- On release, send the cat to the final valid point.
- A laser gesture must produce no meaningful camera yaw, pitch, or distance change.
- Pinch zoom remains camera-owned and must not become two laser gestures.
- Do not transfer pointer ownership midway through a gesture.
- During `Chasing` and `Catch`, room camera orbit and zoom remain available.

## Valid target rules

- Raycast against the existing architecture floor, not furniture, walls, the cat, or arbitrary colliders.
- Accept targets only inside the authored `OpenPlayZone` footprint.
- Sample the final target onto the existing NavMesh before sending the command.
- Reject unreachable, partial, off-floor, out-of-zone, and too-close accidental points without moving the cat.
- Use one reusable runtime target transform; do not create a new destination object per gesture.

## Cat movement and activity lock

- Reuse the existing speed, acceleration, rotation, stopping distance, walk playback, and final arrival handling.
- The laser activity owns the existing motor activity lock for the full session.
- Add a narrowly named activity movement entry point in `CatMotor` that can run only for the current locked activity while normal Call and Sleep requests remain rejected.
- Call, petting, and Sleep are unavailable during the laser session.
- The Phase 4 diagnostic route remains explicit and independent.
- Always release the activity lock when the session finishes, is cancelled, or the component is destroyed.

## Laser dot and catch response

- Create one small emissive-looking red dot at runtime; reuse it for every target.
- Keep it slightly above the floor to prevent z-fighting.
- The dot must not have a physics collider and must never enter the NavMesh.
- Hide the dot outside `Aiming`, `Chasing`, and `Catch` as appropriate.
- On arrival, approximate a catch with a short `0.3–0.5 s` visual response: a subtle forward lean or scale pulse on `CatModelRoot`, plus a dot pulse/disappearance.
- Keep rotation within roughly `1–3°` and scale within `1.5%`.
- Apply the temporary cat response late enough that the idle pet-reaction reset does not overwrite it.
- Never translate `CatRuntime`, `CatModelRoot`, `AxisCorrection`, `LoadedCat`, or imported bones for the catch response.
- Restore the exact cached neutral rotation and scale after every catch with no accumulated error.
- Do not add jump geometry, bone animation, particles, audio, or a new animation clip.

## Haptic placeholder

- Request at most one built-in vibration pulse per successful catch.
- Do nothing in the Editor and unsupported platforms.
- Record request count for validation.
- Physical strength and feel remain device-only checks.

## Diagnostics

Expose read-only values for:

- current laser state
- session count
- accepted and rejected target counts
- successful catch count
- haptic request count
- whether the toy pointer is captured
- whether the activity lock is active
- number of laser-dot instances
- maximum camera delta during a laser gesture
- maximum protected local-position drift
- remaining rotation and scale error after each catch

Log one concise laser validation summary with `PASS` or `FAIL`. Do not log every pointer movement.

## In scope

- `LaserToyInteraction.cs` and `.meta`
- minimal activity-command support in `CatMotor`
- minimal integration after the cat and existing interactions are ready
- one runtime toy trigger and one reusable laser dot
- three-catch session and restrained catch feedback
- focused Phase 9 batch validator
- `context/STATUS.md` update after validation

## Out of scope

- Energy, hunger, mood, bond, rewards, tokens, progression, or saving
- Final toy art, inventory, shop, economy, purchase flow, or unlocks
- Jump, pounce, face, tail, or bone animation; re-rigging or GLB changes
- New audio, particles, camera modes, CatFocus, Activity camera framing, or room redesign
- Feeding, autonomous behavior, meow action, memories, lighting, or onboarding
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, credentials, or provider spending
- Production iOS build or physical-device completion claims

## Acceptance checks

1. Unity compiles with zero new C# errors.
2. ModelTest and the explicit Phase 4 11-leg validator still report `PASS`.
3. Phase 7 petting and Phase 8 two-cycle Sleep/Wake validators still report `PASS`.
4. Normal HomeRoom loads exactly one cat and preserves Call, camera, petting, and Sleep while no laser session is active.
5. Tapping the existing toy starts exactly one laser session without adding a debug activity button.
6. The runtime toy trigger is touch-friendly and does not affect physics or navigation.
7. A laser gesture on valid open floor captures one pointer, moves one dot, and produces no meaningful camera movement.
8. UI, cat-petting, pinch, and room-camera gestures retain their established ownership outside laser targeting.
9. Invalid, unreachable, out-of-zone, and too-close targets are rejected without moving the cat.
10. Each valid target uses existing CatMotor locomotion and reaches the sampled point without path, floor-contact, or local-drift failure.
11. Each arrival produces one restrained catch response and at most one platform-gated haptic request.
12. Cat rotation and scale return exactly to neutral after every catch; protected local-position drift remains zero.
13. After three catches, the dot hides, the activity lock releases, and Call, petting, Sleep, and camera controls work normally.
14. Two laser sessions can run consecutively without duplicate dots, colliders, handlers, locks, or transform drift.
15. The focused Phase 9 validator reports `PASS`, Unity Console has zero current C# errors, and `git diff --check` passes.
16. No needs, energy, unrelated gameplay, provider, room, final UI, or economy system is introduced.

## Validation

1. Run a focused Unity C# compile.
2. Run Phase 4, Phase 7, and Phase 8 batch validators.
3. In HomeRoom, tap the toy and confirm the camera does not move from that pointer.
4. Test invalid furniture, wall, cat, out-of-zone, and too-close targets.
5. Place three valid floor targets and confirm chase, arrival, catch feedback, dot reuse, and automatic session completion.
6. Confirm Call, petting, and Sleep are unavailable only during the session and return afterward.
7. Confirm pinch and ordinary room-camera gestures remain available when they own the pointer.
8. Run a second complete three-catch session.
9. Run the focused Phase 9 batch validator and confirm `PASS`.
10. Run `git diff --check`.
11. Make no provider calls and no production build.

Physical touch targeting, vibration strength, and visual feel remain device-only checks.

## Completion report

Report:

- files changed
- toy trigger dimensions
- gesture ownership and camera-delta result
- accepted and rejected target behavior
- two-session catch and duplication results
- movement, arrival, protected-drift, and neutral-restore results
- haptic gating and count
- Call, petting, Sleep, camera, ModelTest, Phase 4, Phase 7, and Phase 8 regression results
- compile and Console result
- remaining physical-device checks
