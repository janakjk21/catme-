# Luna Task: Cat House Sleep Interaction Phase 8

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

- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Cat/CatPetReaction.cs`
- `Assets/CatMe/Scripts/Input/CatCallInput.cs`
- `Assets/CatMe/Scripts/Input/CatPetInput.cs`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`
- `Assets/CatMe/Editor/Phase4BatchValidator.cs`
- `Assets/CatMe/Editor/Phase7BatchValidator.cs`
- `Assets/CatMe/Scenes/HomeRoom.unity`

Do not inspect the sibling web project, generated Unity folders, the full product specification, or unrelated gameplay systems.

## Objective

Add one understandable cat-house sleep interaction using the existing `SleepApproach` and `SleepInside` points.

Normal HomeRoom play must begin as it does now. A temporary mobile-friendly `Sleep` button asks the stationary cat to walk to `SleepApproach` through the existing `CatMotor`. After arrival and final facing, the cat enters the existing house through a short occluded transition, becomes hidden, and displays a restrained `Zzz` cue above the house. The button then reads `Wake`. Pressing it returns the cat to `SleepApproach`, restores its visible neutral presentation, and returns control to Call and petting.

This phase proves the limited-animation sleep illusion. It does not add energy, needs, autonomous sleep, rewards, persistence, or a sleep animation.

## Preserved foundation

Use only:

```text
LocalFixtures/Cats/master-walking-cat.glb
```

Preserve:

- exactly one loaded cat
- `CatRuntime/CatModelRoot/AxisCorrection/LoadedCat`
- the normalized height, floor contact, material, skeleton, and embedded walking clip
- Phase 4 movement tuning and explicit diagnostic route
- Phase 5 Call behavior
- Phase 6 room camera behavior and limits
- Phase 7 petting ownership, purr, reaction, and neutral-pose restoration
- the existing room, baked NavMesh, `CatHouse`, `SleepApproach`, and `SleepInside`
- ModelTest behavior

Do not modify the GLB, imported bones, skin weights, animation clip, cat scale, room geometry, interaction-point transforms, NavMesh, camera limits, or locomotion tuning.

## Interaction sequence

Use this exact first-pass state flow:

```text
AwakeIdle
→ Sleep requested
→ WalkingToHouse
→ short EnteringHouse transition
→ SleepingHidden
→ Wake requested
→ short ExitingHouse transition
→ AwakeIdle at SleepApproach
```

Requirements:

- Sleep can start only after the cat is loaded, stationary, and not already owned by Call, petting, or the diagnostic route.
- Use `CatMotor.TryMoveTo(SleepApproach, "Sleep")`; do not implement separate pathing.
- Wait for actual arrival and final facing before entering the house.
- Keep sleep manual and indefinite until `Wake` is pressed.
- Wake restores the cat at the authored `SleepApproach`, facing the room as authored.
- Do not automatically send the cat back to `CatSpawn`.
- Rapid repeated presses must not restart or duplicate a transition.

## Architecture

Create one focused component:

```text
Assets/CatMe/Scripts/Cat/CatSleepInteraction.cs
```

`CatSleepInteraction` owns:

- the sleep state flow
- the temporary Sleep/Wake control
- resolving the two existing authored sleep points
- the short entry and exit timing
- caching and restoring renderer enabled states
- the `Zzz` presentation cue
- concise sleep diagnostics

`CatMotor` remains the sole owner of the gameplay root position and navigation. Add only the smallest APIs required to:

- tell whether a command has fully arrived and finished final facing
- place the gameplay root at the authored hidden point with the NavMeshAgent safely suspended
- restore the gameplay root to a sampled reachable point and resume normal commands
- expose whether a hidden activity currently owns the cat

Do not let `CatSleepInteraction` directly translate or rotate `CatRuntime`, `CatModelRoot`, `AxisCorrection`, `LoadedCat`, or imported bones.

Do not add `CatBrain`, `CatNeeds`, a general activity framework, event bus, singleton, service locator, or dependency-injection framework.

## Hidden transition

The master asset has no sleep or crouch clip. Use the room geometry as the transition:

- After the cat reaches `SleepApproach`, stop walk playback and allow final facing to finish.
- Use a short transition of approximately `0.35–0.6 s`.
- Through a `CatMotor` API, suspend the NavMeshAgent and place the gameplay root at `SleepInside`.
- Hide the loaded cat renderers during the occluded part of entry. Cache each renderer's previous enabled state and restore those exact states on wake.
- Do not disable or destroy the complete `CatRuntime`, motor, input components, audio objects, or imported hierarchy.
- While sleeping, the cat must not be visible outside the house, must not drift, and must not play the walking clip.
- On wake, restore the root through `CatMotor` at a valid NavMesh sample around `SleepApproach`, restore renderer states, and leave the cat idle.
- The wake transition must not flash the cat at `SleepInside` or expose a one-frame teleport.

The hidden placement is an activity presentation state. It must not change the authored NavMesh or treat `SleepInside` as a walkable destination.

## Temporary control

- Add one temporary mobile-friendly `Sleep` button alongside the existing Call button.
- Keep both controls inside the existing portrait safe area and prevent overlap.
- Reposition the temporary Call button only as much as needed to fit both controls; preserve its behavior and visual style.
- The sleep button reads `Sleep` while awake, becomes non-interactable during entry, reads `Wake` while sleeping, and becomes non-interactable during exit.
- UGUI owns touches beginning over either button for their complete lifetime, preserving the Phase 6 and Phase 7 gesture rules.
- Do not add final artwork, meters, menus, panels, or a general HUD framework.

## Ownership during sleep

- While walking to the house or transitioning, the existing movement lock already prevents petting.
- While `SleepingHidden`, Call and petting must be unavailable even though the cat is not walking.
- Expose one small motor activity-lock property and use it in `CatCallInput`, `CatPetInput`, and `CatPetReaction`; do not make those components search for `CatSleepInteraction`.
- Call and petting resume immediately after a successful wake.
- Room camera orbit and zoom remain available throughout sleep and wake.
- The Phase 4 diagnostic route must remain available when explicitly invoked and must never start the sleep flow.

## Sleep cue

- Create one restrained runtime `Zzz` cue above the existing `CatHouse` only while `SleepingHidden`.
- Prefer a simple world-space or overlay text cue with no dependency on downloaded art.
- It must remain readable in portrait framing without blocking the room or intercepting input.
- A small fade or gentle vertical motion is allowed; keep it subtle.
- Destroy or disable the cue on wake and on component teardown.
- Do not add particles, hearts, dream bubbles, sleep audio, lighting changes, or camera mode changes in this phase.

## Diagnostics

Expose read-only values useful for focused validation:

- current sleep state
- sleep request count
- successful entry count
- successful wake count
- whether the hidden activity lock is active
- whether all cached renderer states were restored
- maximum protected local-position drift for `CatModelRoot`, `AxisCorrection`, and `LoadedCat`
- wake position error from sampled `SleepApproach`
- whether the `Zzz` cue is active only during sleep

Log one concise sleep validation summary with `PASS` or `FAIL`. Do not log every frame or transition tick.

## In scope

- `CatSleepInteraction.cs` and its `.meta` file
- minimal command-completion and hidden-placement APIs in `CatMotor`
- minimal activity-lock checks in Call and petting components
- one temporary Sleep/Wake UGUI control
- one simple runtime `Zzz` cue
- HomeRoom composition after the validated cat and motor are ready
- one focused Phase 8 batch validator
- update to `context/STATUS.md` after successful validation

## Out of scope

- Energy, hunger, mood, bond, needs, rewards, cooldown meters, progression, or saving
- Automatic tiredness, autonomous behavior, offline recovery, timers, or day/night behavior
- Sleep, crouch, lie-down, wake, face, or bone animation
- CatFocus, Activity camera framing, camera redesign, or lighting changes
- Feeding, toys, laser play, meow action, shop, economy, creation, or final UI
- Room redesign, new props, NavMesh rebake, model optimization, or new audio
- Meshy, Tripo, OpenAI, Supabase, uploads, downloads, credentials, or provider spending
- Production iOS build or physical-device completion claims

## Acceptance checks

1. Unity compiles with zero new errors.
2. ModelTest remains unchanged and the explicit Phase 4 11-leg validator still reports `PASS`.
3. Normal HomeRoom loads exactly one cat and preserves Call, camera, and direct petting behavior while awake.
4. One Sleep button appears beside Call without overlap and remains owned by UGUI for the full pointer gesture.
5. Pressing Sleep once sends the cat to the authored `SleepApproach` through the existing `CatMotor` with unchanged locomotion quality.
6. Entry begins only after the cat has arrived and finished final facing; rapid repeated presses cannot duplicate it.
7. `CatMotor` owns the hidden placement and safely suspends the NavMeshAgent without changing the room or baked NavMesh.
8. During sleep, the loaded cat is hidden inside the house, walk playback is stopped, root drift is zero, and exactly one `Zzz` cue is visible.
9. While sleeping, Call and petting cannot start, while room camera orbit and zoom remain available.
10. Pressing Wake restores the exact cached renderer states at a valid sample near `SleepApproach`, removes the `Zzz` cue, and restores Call and petting.
11. Wake produces no visible one-frame flash at `SleepInside`, no duplicate cat, and no accumulated local transform error.
12. A second complete Sleep/Wake cycle works without stale locks, duplicate UI, duplicate cues, or drift.
13. The explicit Phase 4 route can still run independently and never starts sleep behavior.
14. The focused Phase 8 summary reports `PASS`, Unity Console has zero current errors, and `git diff --check` passes.
15. No needs, energy, unrelated gameplay, provider, room, camera, or final UI system is introduced.

## Validation

1. Run a focused C# compile.
2. Run the existing Phase 4 batch validator and confirm ModelTest, stationary acceptance, camera initialization, and all 11 route legs report `PASS`.
3. Run the existing Phase 7 validator and confirm petting still reports `PASS` while awake.
4. Open HomeRoom in normal Play Mode and verify the Call and Sleep controls fit the portrait safe area without overlap.
5. Press Sleep, verify the cat walks to `SleepApproach`, finishes facing, and enters once.
6. During travel and transition, try rapid Sleep presses and petting; confirm they do not restart movement or trigger reactions.
7. While asleep, verify the cat is hidden, walking is stopped, one `Zzz` cue is visible, Call and petting are unavailable, and room camera gestures still work.
8. Press Wake and verify the cat appears at `SleepApproach`, idle and correctly facing, with no frame flash or transform drift.
9. Verify Call and petting work again after wake.
10. Complete a second Sleep/Wake cycle.
11. Run the focused Phase 8 batch validator and confirm its summary reports `PASS`.
12. Run `git diff --check`.
13. Do not run provider calls, broad unrelated tests, or a production build.

Physical touch layout and transition appearance remain device checks and do not block the Editor implementation milestone.

## Completion report

Report:

- files changed
- sleep state sequence and timing
- approach arrival and wake position results
- hidden-placement and NavMeshAgent handling
- renderer cache/restore and `Zzz` behavior
- control layout and interaction-lock behavior
- two-cycle drift and duplication results
- Call, petting, camera, ModelTest, Phase 4, and Phase 7 regression results
- compile and Console result
- remaining physical-device checks
