# Luna Task: Mobile Room Camera Phase 6

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/MOBILE.md`
4. This task file

Inspect only these implementation surfaces and their direct dependencies:

- `Assets/CatMe/Scenes/HomeRoom.unity`
- Existing `CameraRig`, `CameraPivot`, `HomeAnchor`, `LeftAnchor`, `RightAnchor`, and `Main Camera`
- `Assets/CatMe/Editor/RoomBlockoutSetup.cs`
- `Assets/CatMe/Scripts/Input/CatCallInput.cs`
- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs` only to preserve behavior, not to redesign it
- Project Input System and EventSystem configuration only when directly needed

Do not inspect the sibling web project, generated Unity folders, the complete product specification, or unrelated game systems.

## Objective

Add the first portrait mobile room camera: one-finger drag or Editor mouse drag rotates an elevated guided camera around the existing room pivot, and two-finger pinch or Editor mouse wheel zooms within safe authored limits.

Camera gestures must coexist with the Phase 5 Call button. Pressing, holding, or releasing the Call button must never rotate or zoom the camera.

This phase implements only the normal `Room` camera mode. It does not add cat focus, petting, activity framing, free movement, or final camera polish.

## Preserved foundation

Preserve:

- the existing room geometry, baked NavMesh, lighting, props, and authored camera objects
- the existing `Main Camera` and its rendering settings
- exactly one local cat with the validated runtime hierarchy and material
- Phase 4 locomotion and explicit diagnostic route
- Phase 5 normal-play behavior and Call button
- portrait-first composition

Do not change the GLB, cat scale, movement tuning, Call destination, room layout, or NavMesh.

## Architecture

Create one component under `Assets/CatMe/Scripts/Camera/`:

```text
RoomOrbitCamera.cs
```

`RoomOrbitCamera` owns:

- room-camera gesture recognition
- target yaw, pitch, and distance
- smoothed camera transform updates
- authored orbit limits
- camera obstruction protection
- concise camera diagnostics

It must not own cat movement, Call input, petting, UI, room simulation, or gameplay state.

Use the existing `CameraPivot` as the orbit target. Reuse the existing `Main Camera`; do not create a second gameplay camera.

Do not introduce a general camera-state framework or implement `CatFocus` and `Activity` modes yet.

## Camera behavior

At startup:

- derive initial yaw, pitch, and distance from the current `Main Camera` position relative to `CameraPivot`
- keep the current HomeRoom view without a visible first-frame snap
- continue looking at `CameraPivot`

Input:

- one-finger horizontal drag rotates yaw
- a small amount of vertical drag may adjust pitch within narrow limits
- two-finger pinch changes distance and suppresses single-finger rotation for that gesture
- Editor left-mouse drag mirrors one-finger orbit
- Editor mouse wheel mirrors pinch zoom
- ignore a touch or mouse press that begins over UGUI
- once a pointer begins over UI, keep that pointer excluded until release
- clicking or dragging the Call button must not move the camera

Motion:

- apply yaw, pitch, and zoom smoothly and independently of frame rate
- release should settle without a position or rotation snap
- do not add inertia or momentum in this phase
- keep the camera upright with no roll
- continue framing the room pivot while orbiting

Start with conservative limits and tune only when the existing room requires it:

- yaw approximately `-55°` to `+55°` from the Home view
- pitch approximately `15°` to `32°` elevation
- distance approximately `4.2 m` to `7.8 m`
- field of view remains the existing `42°`

The final values may vary slightly if visual validation demonstrates a better safe range. Keep them serialized and record the chosen values in the validation summary.

## Composition and collision

- At the default Home view, the cat should remain readable and the room should feel like an elevated diorama.
- The camera must never pass below the floor.
- The camera must not enter walls or large room geometry.
- Use a small sphere cast or equivalent obstruction check between `CameraPivot` and the desired camera position.
- When obstructed, move the camera inward with a small clearance instead of clipping through geometry.
- When the obstruction clears, return smoothly toward the requested distance.
- Never move the room, cat, `CameraPivot`, or interaction points to solve camera collision.
- Near zoom must not become an extreme close-up that hides most of the room.

## Input-system boundary

Use the installed Unity Input System for direct pointer and touch state when practical. Keep UGUI button routing intact.

Do not install another input or camera package. Do not replace the Phase 5 button or redesign its EventSystem unless a focused compatibility defect is demonstrated. If an EventSystem adjustment is necessary, preserve both Editor mouse clicks and mobile touch behavior and document the exact reason.

## Diagnostics

Expose read-only runtime values needed for focused validation:

- current and target yaw
- current and target pitch
- current and target distance
- whether the current gesture began over UI
- whether collision shortened the camera distance

Provide a concise validation summary containing:

- configured yaw, pitch, and distance limits
- default derived pose error after initialization
- minimum observed camera height
- whether any wall penetration occurred
- whether Call-button input moved the camera
- result: `PASS` or `FAIL`

Do not log every frame or every drag update.

## In scope

- `RoomOrbitCamera.cs` and its `.meta`
- Minimal `HomeRoom` or focused editor-setup changes needed to attach and configure it
- One-finger/mouse orbit
- Pinch/mouse-wheel zoom
- smooth bounded motion
- UGUI gesture exclusion
- camera obstruction protection
- focused camera validation
- update to `context/STATUS.md` after successful validation

## Out of scope

- CatFocus, tap-cat focus, petting, or drag-over-cat detection
- Activity camera framing, cinematic transitions, camera shake, or photo mode
- Joystick movement, tap-to-move, arbitrary cat destinations, or changes to Call behavior
- Feeding, sleeping, toys, laser play, sound, particles, haptics, needs, bond, or saving
- Final UI, menus, onboarding, login, shop, economy, payments, or analytics
- Room redesign, final art, lighting changes, NavMesh changes, or new props
- Cat generation, provider APIs, downloads, caching, credentials, or credit spending
- Production iOS build or physical-device sign-off

## Acceptance checks

1. Unity compiles with zero new errors.
2. Normal `HomeRoom` still loads exactly one cat and keeps the Phase 5 Call behavior working.
3. The existing `Main Camera` starts from the current Home view with no visible initialization snap.
4. Editor left-mouse drag and one-finger touch input rotate the camera smoothly around `CameraPivot` within the authored yaw and pitch limits.
5. Editor mouse wheel and two-finger pinch zoom smoothly within the authored distance limits.
6. A two-finger pinch never also rotates the camera as a one-finger drag.
7. Pressing, dragging, or releasing the Call button causes zero meaningful camera movement and still calls the cat once.
8. The camera remains above the floor, upright, aimed at `CameraPivot`, and outside walls or large room geometry throughout the allowed range.
9. Reaching an input limit produces no jitter, wraparound, or snap.
10. The cat remains visible and readable at default, near, far, left, and right camera limits while waiting and while walking to `CallDestination`.
11. ModelTest remains unchanged, and the explicit Phase 4 batch validator still reports `PASS` for its 11-leg route.
12. The focused camera validation reports `PASS`, Unity Console has zero current errors, and `git diff --check` passes.
13. No petting, CatFocus, Activity camera, unrelated gameplay, provider, or final UI system is introduced.

## Validation

1. Run a focused C# compile.
2. Run the existing Phase 4 batch validator and confirm ModelTest, stationary acceptance, and the 11-leg route still report `PASS`.
3. Open `HomeRoom` in normal Play Mode and confirm the default view does not jump when the camera component initializes.
4. Exercise Editor mouse drag and wheel through near, far, left, right, minimum-pitch, and maximum-pitch limits.
5. Use Input System touch simulation or a focused automated harness to verify one-finger orbit and two-finger pinch arbitration.
6. Hold and drag across the Call button, then click it normally. Confirm the first gesture does not move the camera and the click still calls the cat exactly once.
7. Observe the cat waiting and walking from the default view and the camera limits. Confirm the cat remains readable and camera movement does not alter cat navigation.
8. Confirm no camera position enters room colliders or falls below the floor.
9. Confirm the camera validation summary reports `PASS` and the Console has no current errors.
10. Run `git diff --check`.
11. Do not run provider calls, broad unrelated tests, or a production build.

## Completion report

Report:

- files changed
- final yaw, pitch, and distance limits
- mouse, touch, pinch, and wheel behavior verified
- UI-gesture exclusion result
- collision and minimum-height result
- Call behavior regression result
- Phase 4 regression result
- compile and Console result
- any concrete blocker before CatFocus or petting

