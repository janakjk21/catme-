# Luna Task: Physical Feeding Interaction Phase 12

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

Inspect only these implementation surfaces and direct dependencies:

- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Cat/CatSleepInteraction.cs`
- `Assets/CatMe/Scripts/Cat/CatEnergy.cs`
- `Assets/CatMe/Scripts/Toys/LaserToyInteraction.cs`
- existing Call, petting, and room-camera components only where needed for input ownership or regression
- existing Phase 4, 7, 8, 9, and 10 validators
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs` only if the feeding prop cannot be composed safely at runtime
- `Assets/CatMe/Scenes/HomeRoom.unity`

Do not inspect generated Unity folders, the sibling web project, provider code, or unrelated product history.

## Objective

Add one physical, mobile-friendly feeding interaction using the existing `FeedingNook`, `Bowl`, and `FeedingApproach`:

```text
drag food packet to bowl
→ food appears in the bowl
→ cat walks to FeedingApproach
→ the feeding nook hides the face and front paws
→ subtle procedural eating motion and bowl depletion sell the action
→ cat returns to neutral and all controls unlock
```

This phase proves feeding with the current walk-only cat. It does not add Hunger, Mood, Bond, rewards, tokens, inventory, purchasing, or save-schema changes.

## Preserved foundation

Preserve:

- ModelTest acceptance and the validated local GLB
- cat hierarchy, scale, material, floor contact, skeleton, and embedded walking clip
- Phase 4 locomotion and diagnostic route
- Phase 5 Call
- Phase 6 room camera
- Phase 7 petting
- Phase 8 Sleep/Wake
- Phase 9 laser play
- Phases 10–11 Energy, automatic rest, and local saving
- existing room, baked NavMesh, feeding nook, bowl, `FeedingApproach`, and all other interaction points

Use no provider/API calls and spend no credits.

## Implementation ownership

Create one focused component:

```text
Assets/CatMe/Scripts/Cat/CatFeedingInteraction.cs
```

`CatFeedingInteraction` owns only:

- one simple temporary food-packet prop beside the existing bowl
- packet drag input and pointer ownership
- valid drop detection over the existing bowl
- one reusable food portion visual inside the bowl
- the feeding activity state
- requesting movement through `CatMotor`
- the short procedural eating and satisfied response
- activity diagnostics

`CatMotor` remains the only owner of navigation and gameplay-root movement. Do not translate the cat root directly. Do not edit imported bones, the GLB, `AxisCorrection`, or `LoadedCat`.

Compose the feeding component from `HomeRoomCatIntegration` after the motor and existing interactions are ready. Pass direct references. Do not add a singleton, event bus, general `CatBrain`, inventory service, or needs framework.

## Food packet and bowl interaction

- Reuse `Room_Blockout/Props_Blockout/FeedingNook/Bowl`.
- Add exactly one small temporary food packet next to the bowl. Keep it visually simple and clearly tappable in portrait view.
- Do not add a Feed button.
- Editor mouse and one-finger touch must both work.
- A feeding gesture begins only when the pointer starts on the packet.
- Reserve that pointer from `RoomOrbitCamera` until release.
- Move only a lightweight drag proxy or the packet presentation; do not move the room or cat.
- A release over the bowl is valid. A release elsewhere cancels cleanly.
- Invalid drops must not move the cat, create food, vibrate, or acquire an activity lock.
- Valid drops must reuse the same packet, food visual, colliders, and handlers on every cycle.

Use the established gesture ownership rule:

```text
pointer begins over UGUI → UI owns it until release
pointer begins on the food packet → feeding owns it until release
pointer begins elsewhere → existing cat/camera interaction rules apply
```

## Feeding activity

After a valid bowl drop:

1. Activate one reusable food portion inside the bowl.
2. Acquire the existing motor activity lock.
3. Move the cat to `FeedingApproach` through the existing locked-activity movement path.
4. Wait for full arrival and final authored facing.
5. Keep the cat standing. Use the nook to hide its face and front paws.
6. Apply a restrained reversible head-root or shoulder lean on `CatModelRoot` for approximately 2–3 seconds.
7. Reduce the visible food portion gradually during the action.
8. Finish with one small satisfied settle, restore the exact neutral rotation/scale, hide the empty food portion, and release the lock.

Do not create fake skeletal eating, jaw animation, or bone edits. Do not use the walking clip while the cat is stationary and eating.

Call, petting, Sleep, and laser play must reject new actions only while feeding owns the cat. The room camera remains available after the packet is released. Energy must not drain or recover because of feeding.

If Sleep or another activity already owns the motor, reject the feeding gesture cleanly. Do not queue it.

No new audio asset is required in this phase. Do not download, generate, or substitute unrelated audio.

## Validation hooks

Expose read-only diagnostics for:

- feeding state
- accepted and rejected food drops
- completed feeding cycles
- packet, food-visual, and collider instance counts
- whether feeding owns the activity lock
- arrival error at `FeedingApproach`
- maximum protected local-position drift
- neutral rotation and scale restoration errors
- maximum room-camera delta during the packet drag

Provide a narrow validation-only method that performs the same valid-drop and feeding path without simulating screen input.

Add:

```text
Assets/CatMe/Editor/Phase12BatchValidator.cs
```

The validator must run two complete feeding cycles and confirm reuse, lock release, neutral restoration, and no drift or duplicates.

## Acceptance checks

1. Unity compiles with zero new C# errors.
2. Normal HomeRoom creates exactly one packet, one food visual, and one packet collider.
3. Invalid drops cancel without food, movement, haptics, camera capture, or activity lock.
4. A valid drop creates visible food and sends the cat to `FeedingApproach` through `CatMotor`.
5. The cat arrives within `0.10 m`, finishes authored facing, and does not slide during eating.
6. The nook hides the face/front-paw limitation at the eating position.
7. The procedural eating and satisfied response remain subtle and restore the exact neutral pose.
8. The food portion visibly depletes and is hidden at completion.
9. Call, petting, Sleep, and laser are locked only for the feeding activity and restore afterward.
10. The room camera does not move during packet drag and resumes after release.
11. Energy and local save behavior remain unchanged.
12. Two complete feeding cycles produce no duplicate props, visuals, colliders, handlers, locks, or transform drift.
13. Phase 4, 7, 8, 9, and 10 validators still report `PASS`.
14. `Phase12BatchValidator` reports `PASS` and `git diff --check` passes.
15. No Hunger, Mood, Bond, rewards, economy, new animation, provider, room redesign, or final UI system is introduced.

## Validation

1. Run a focused Unity C# compile.
2. Run `Phase12BatchValidator` for two complete feeding cycles.
3. Run Phase 4, 7, 8, 9, and 10 regressions.
4. Confirm one reusable packet, food visual, and collider.
5. Confirm invalid-drop cleanup and camera pointer release.
6. Confirm final pose, activity lock, and food visual restore after each cycle.
7. Run `git diff --check`.
8. Make no provider calls and no production build.

Physical portrait drag feel and the final feeding audio remain device/polish checks.
