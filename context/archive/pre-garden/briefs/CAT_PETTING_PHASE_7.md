# Luna Task: Direct Cat Petting Phase 7

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

- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/LocalCatAssetLoader.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Input/CatCallInput.cs`
- `Assets/CatMe/Scripts/Camera/RoomOrbitCamera.cs`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`
- `Assets/CatMe/Editor/Phase4BatchValidator.cs`
- `Assets/CatMe/Scenes/HomeRoom.unity`
- `Assets/CatMe/Audio/cat-purr.mp3`

Do not inspect the sibling web project, generated Unity folders, the complete product specification, or unrelated gameplay systems.

## Objective

Add direct petting as the second player-to-cat interaction. When the stationary cat is visible, a touch or Editor mouse drag that begins on the cat must be captured as a petting gesture, produce a gentle temporary body response, play the existing local purr audio, and request limited mobile vibration feedback.

A drag that begins outside the cat remains a Phase 6 room-camera gesture. A petting gesture must never rotate or zoom the camera, press the Call button, move the cat, or alter its imported rig.

This phase does not add CatFocus, bond, needs, new animation clips, final particles, or final haptic tuning.

## Preserved foundation

Use only the validated local GLB and existing audio:

```text
LocalFixtures/Cats/master-walking-cat.glb
Assets/CatMe/Audio/cat-purr.mp3
```

Preserve:

- exactly one loaded cat
- `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`
- `0.35 m` normalized height, floor contact, material, skeleton, and walk clip
- Phase 4 locomotion quality and explicit diagnostic route
- Phase 5 Call button and `CallDestination` behavior
- Phase 6 room-camera orbit, zoom, limits, collision protection, and UI exclusion
- ModelTest behavior

Do not modify the GLB, imported bones, skin weights, animation clip, cat scale, NavMesh, room layout, or camera limits.

## Architecture

Create two focused components:

```text
Assets/CatMe/Scripts/Cat/CatPetReaction.cs
Assets/CatMe/Scripts/Input/CatPetInput.cs
```

`CatPetInput` owns:

- pointer-down raycast against the runtime cat target
- capture of a petting pointer until its release
- short-stroke distance and timing thresholds
- touch and Editor mouse parity
- gesture arbitration with `RoomOrbitCamera`
- requesting a reaction, without directly moving visual transforms or playing audio

`CatPetReaction` owns:

- a runtime trigger collider fitted to the loaded cat renderer bounds
- the temporary gentle visual response
- the local purr `AudioSource`
- conservative feedback cooldowns
- diagnostic counters and restoration of the neutral pose

Keep `CatMotor` as the only owner of navigation and world movement. Do not introduce `CatBrain`, `CatNeeds`, `CatAnimationController`, an interaction framework, event bus, service locator, or dependency-injection framework.

## Pet target

The generated GLB cannot be assumed to contain a useful collider.

- Create one simple trigger collider on a runtime-owned pet target under `CatRuntime`.
- Derive its center and dimensions from the loaded renderer bounds.
- Use a capsule or box that covers the visible body without becoming an enormous screen-space target.
- Do not add colliders to imported bones or `LoadedCat`.
- The collider must not affect NavMesh movement, gravity, floor contact, or room physics.
- Recalculate it once after the cat finishes loading and normalization.
- Pet raycasts must identify only this explicit target, not arbitrary furniture or the floor.

## Gesture ownership

Use one clear rule for the lifetime of each pointer:

```text
pointer begins over UGUI → UI owns it until release
pointer begins on the stationary cat → petting owns it until release
pointer begins elsewhere → room camera may own it until release
```

Requirements:

- Add the smallest explicit suppression API needed on `RoomOrbitCamera` so `CatPetInput` can reserve one mouse or touch pointer until release.
- Give `CatPetInput` an earlier execution order than the camera input processor so the claim is established before camera movement in the same frame.
- Do not make the camera search for cat components or perform pet raycasts.
- Do not transfer ownership midway through a gesture.
- A camera drag that later crosses the cat remains a camera drag.
- A pet stroke that leaves the cat remains captured but only accumulates pet distance while the pointer ray still intersects the pet target.
- Two-finger pinch keeps ownership over the camera and must not become two simultaneous pet gestures.
- Call-button touches remain UI-owned.

## Pet stroke

- Petting is available only after the cat is loaded and while `CatMotor.IsMoving` is false.
- A short tap alone must not repeatedly trigger feedback.
- Require a small intentional drag across the cat, starting around `25–40` logical pixels of accumulated on-cat motion.
- Ignore sub-pixel jitter and accidental micro-drags.
- Limit reaction triggers to a conservative cooldown around `0.6–0.9 s`.
- Continuing to stroke may maintain the purr, but it must not trigger haptics every frame.
- Releasing the pointer ends the active stroke and lets the purr fade or stop shortly afterward.
- If the cat starts moving, cancel petting cleanly and return the visual response to neutral.

## Visual response with limited animation

The master cat has no petting clip. Use a restrained procedural response:

- apply a small smooth lean or wobble to `CatModelRoot` only while the cat is stationary
- keep rotation subtle, approximately `1–3°`; optional scale change must remain below about `1.5%`
- never translate the gameplay root, `CatModelRoot`, `AxisCorrection`, `LoadedCat`, or imported bones
- never rotate `AxisCorrection` or imported bones
- smoothly return `CatModelRoot` to its exact cached neutral local rotation and scale after the response
- do not accumulate transform error across repeated strokes
- do not run the response during walking or the Phase 4 diagnostic route

No hearts, particles, face deformation, bone animation, or new animation clips in this phase.

## Purr audio

- Use the existing `Assets/CatMe/Audio/cat-purr.mp3`; do not duplicate or download audio.
- Assign it as a serialized scene or setup reference rather than relying on a fragile filesystem or `Resources` lookup.
- Use one `AudioSource` attached to a runtime-owned cat object.
- Keep volume conservative and use partial 3D spatial blend so the purr belongs to the cat without disappearing at the far Room-camera distance.
- Start or fade in after a valid pet stroke.
- Stop or fade out shortly after the final stroke or when petting is cancelled.
- Repeated strokes reuse the same source and must not layer multiple purr instances.

## Haptic placeholder

- Request one built-in mobile vibration pulse only when a valid stroke crosses the reaction threshold and the cooldown allows it.
- Do nothing in the Editor and on unsupported platforms.
- Never vibrate continuously and never request vibration per frame.
- Keep the platform check isolated so final iOS haptic strength can be replaced later without changing pet detection.
- Record that strength and feel require physical-device validation.

## Diagnostics

Expose read-only values useful for focused validation:

- whether the current pointer is captured for petting
- valid stroke count
- rejected tap or micro-drag count
- reaction count
- haptic request count
- whether purr is playing
- maximum positional drift of protected cat transforms
- maximum rotation or scale error remaining after each response returns to neutral
- camera yaw, pitch, and distance deltas during petting

Log one concise petting validation summary with `PASS` or `FAIL`. Do not log every pointer movement or audio frame.

## In scope

- `CatPetInput.cs`, `CatPetReaction.cs`, and their `.meta` files
- A minimal pointer-suppression API in `RoomOrbitCamera`
- Runtime pet-target collider derived from renderer bounds
- Existing purr audio assignment and one AudioSource
- Subtle temporary body response
- Conservative platform-gated vibration request
- Minimal HomeRoom integration after the cat and motor are ready
- Focused petting validation
- Update to `context/STATUS.md` after successful validation

## Out of scope

- CatFocus camera, automatic zoom to the cat, Activity framing, photo mode, or camera redesign
- Bond, mood, energy, hunger, rewards, meters, progression, cooldown UI, or saving
- Feeding, sleeping, toys, laser play, meow action, autonomous behavior, or memories
- Hearts, particles, final animation, facial animation, bone animation, new clips, re-rigging, or GLB modification
- New UI buttons, final UI art, onboarding, login, shop, economy, payments, or analytics
- Room redesign, new props, lighting work, NavMesh changes, or model optimization
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, credentials, or provider spending
- Production iOS build or claiming physical haptic quality is validated

## Acceptance checks

1. Unity compiles with zero new errors.
2. ModelTest remains unchanged and the explicit Phase 4 11-leg validator still reports `PASS`.
3. Normal HomeRoom still loads exactly one cat, preserves Call behavior, and preserves the Phase 6 camera limits and default pose.
4. One runtime-owned trigger collider closely covers the normalized cat and does not affect navigation, floor contact, or room collisions.
5. Editor mouse drag and one-finger touch that begin on the stationary cat are captured as petting gestures.
6. A drag that begins outside the cat remains a camera drag even if it later crosses the cat.
7. A petting gesture produces no meaningful camera yaw, pitch, or distance movement and never activates the Call button.
8. A valid stroke requires intentional on-cat motion; taps and micro-drags do not repeatedly trigger feedback.
9. A valid stroke creates one subtle temporary response, starts or sustains one purr source, and respects the reaction/haptic cooldown.
10. The visual response returns exactly to the cached neutral rotation and scale with no accumulating position, rotation, or scale drift.
11. Petting is cancelled cleanly when the cat moves, and it cannot interfere with Call navigation or the diagnostic route.
12. The purr uses the existing local clip, does not layer duplicates, and stops or fades after petting ends.
13. The haptic path is platform-gated, rate-limited, and records requests without vibrating in the Editor.
14. The focused petting summary reports `PASS`, Unity Console has zero current errors, and `git diff --check` passes.
15. No CatFocus, needs, bond, unrelated gameplay, provider, room, or final UI system is introduced.

## Validation

1. Run a focused C# compile.
2. Run the existing Phase 4 batch validator and confirm ModelTest, stationary acceptance, the camera runtime summary, and all 11 route legs report `PASS`.
3. Open HomeRoom in normal Play Mode, wait for the cat and Call control, and confirm the pet target fits the visible cat.
4. Drag outside the cat and confirm the camera orbits; cross over the cat during that drag and confirm ownership remains with the camera.
5. Begin a drag on the stationary cat and confirm the camera does not move, the Call button does not activate, and the pet gesture remains captured until release.
6. Test a tap, micro-drag, one valid stroke, a long continuous stroke, and rapid repeated strokes.
7. Confirm one purr source is reused, the reaction is subtle, feedback is rate-limited, and all protected transforms return to neutral.
8. Press Call while idle, confirm the cat walks normally, and confirm petting is unavailable during movement.
9. After arrival, pet again and confirm interaction resumes normally.
10. Confirm the petting summary reports `PASS` and the Console has no current errors.
11. Run `git diff --check`.
12. Do not run provider calls, broad unrelated tests, or a production build.

Physical iPhone haptic strength, audio balance, and touch feel remain device-only checks and do not block the Editor implementation milestone.

## Completion report

Report:

- files changed
- cat collider type and fitted dimensions
- gesture ownership and camera-delta result
- valid, rejected, and repeated-stroke behavior
- reaction range and neutral-pose restoration result
- purr source and clip behavior
- haptic request count and platform gating
- Call, camera, ModelTest, and Phase 4 regression results
- compile and Console result
- remaining physical-device checks

