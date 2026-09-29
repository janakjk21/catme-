# CatMe: fixed room, responsive companion play

Status: implementation brief only. Created 2026-09-27. Gameplay changes below are not implemented or validated by this document.

## Objective and precedence

Keep the current landscape room and make interacting with the cat enjoyable from one stable viewpoint. The player should feel that their touch causes a visible, understandable response from the cat or toy. Improve input, locomotion, and play before further visual polishing.

Repository: `/Users/janaksapkota/Downloads/appliction new idea/catme/catme-home-mobile`.
Current phone reference: `/Users/janaksapkota/Downloads/IMG_8541.PNG`.
Earlier composition reference: `/Users/janaksapkota/Downloads/73B2ED0B-8951-41A9-9A67-9B9841CC4BE7.PNG`.

Read AGENTS.md, context/README.md, context/STATUS.md, this brief, and context/MOBILE.md plus context/GAMEPLAY.md. Both modules are needed because camera/input ownership and cat activity ownership must agree. This brief expresses the newer user direction: fixed landscape camera and existing room. It supersedes older requirements for orbit, seated first-person movement, automatic activity close-ups, and portrait presentation for this pass.

Interpretation of fixed camera: lock the angle and room focus; retain the previously requested, deliberately bounded pinch zoom. Do not interpret immersion as adding a movable player, camera shake, or cinematic camera changes.

For subsequent toy/material polish, use `CATME_TACTILE_ART_AND_PHYSICS.md`. Its latest reference shifts surfaces to warm wood, cream fabric, and woven toys with cardboard accents; keep this brief's fixed camera and room-layout constraints. Treat texture/physics improvements as targeted edits, not a room rebuild.

## Evidence and boundaries

- The current phone screenshot shows the room, cat, ball, HUD, open toy tray, and a Development Console reporting `Touch was already deallocated`. The image does not establish the cause of that error, moving paw contact, or gesture correctness.
- Current source allows orbit, seated view, cat following, and activity reframing. Ball, petting, Call, feeding, sleep, laser, and ambient behavior all have camera-related callers. Fixing only the one-finger orbit handler is insufficient.
- Current ball defaults: base motor speed 0.68 m/s with ball multiplier 1.12–1.28; notice delay 0.32–0.52 s; pursuit refresh 0.48 s; post-bat wait 0.65 s. Walk playback already follows measured movement velocity and clamps at 1.55x.
- Use existing models, materials, room layout, audio, navigation, saving, and interaction systems. `CatMotor` remains the sole owner of cat movement; the ball Rigidbody owns ball motion.
- Known-good cat has a walk clip; do not assume a run, idle, sit, pounce, or paw-swipe clip exists. Discover supported clips per loaded cat. Do not deform an unknown skeleton or accelerate a walk indefinitely to imitate running.
- Preserve uncommitted work. Before any scene edit, save a recoverable copy of HomeRoom and touched authoring assets outside Assets and Git. Do not run Build Room Phase 1, Restore Reference Home Room, or other whole-room builders for this task.
- No room redesign, new economy/needs/backend systems, generated assets, provider jobs, paid credits, commits, pushes, or uploads to TestFlight/App Store. Photo upload remains outside this pass because its development endpoint starts generation.

## 1. Lock the room view

Primary file: `Assets/CatMe/Scripts/Camera/RoomOrbitCamera.cs`.

1. Add an explicit fixed-room mode and make it the HomeRoom player default. Keep the authored room anchor and orientation from the current scene. Record its position, rotation, pivot, FOV, and initial distance before modifying behavior. Start at the current 5.0 m distance if that reproduces the screenshot's composition.
2. In this mode, disable one-finger orbit, keyboard/controller walking, seated movement/joystick, and the player-facing view-mode switch. Keep Home view as a zoom reset. Do not remove public methods that callers still depend on.
3. Make `FocusOnCat`, `FocusOnActivity`, and activity completion/reset calls preserve the fixed anchor, angle, and user-selected zoom. Only an explicit Home view press resets zoom. Route calls centrally so Call, petting, ball, laser, feed, sleep, wake, and ambient roaming cannot reframe the view.
4. Retain two-finger zoom about the room anchor. Initial distance limits: 4.3–5.0 m; constant yaw, pitch, roll, and FOV. Fingers together zoom closer and fingers apart zoom farther, preserving the user's earlier requested direction. Smooth distance over approximately 0.18 s. A held, unmoving pinch must not drift.
5. Treat these limits as initial tuning values, not validated geometry. Narrow the zoom range if it clips walls, cuts off the playable floor, hides the cat at a required interaction point, or puts a destination beneath opaque UI. Do not solve framing by changing cat scale or rebuilding the room.
6. At default zoom, retain sofa/plants left, window behind, tower/house right, open center, visible paws and toy. Validate 16:9 and the supplied iPhone's wide landscape aspect. Restrict minor framing corrections to camera distance/FOV before changing any furniture.
7. Keep both landscape orientations and safe areas. No automatic camera motion in response to cat position, throws, pet strokes, or sleep transitions.

Acceptance: with no explicit pinch or Home view press, run every activity and allow 30 seconds of ambient movement. Camera position must stay within 0.005 m and rotation within 0.05 degrees of its initial pose. With pinch, angle and pivot remain fixed; only distance changes within bounds. Disable procedural camera effects if they violate this.

## 2. Give each touch exactly one owner

Inspect camera, `Input/CatPetInput.cs`, `Input/CatCallInput.cs`, `Toys/CatBallInteraction.cs`, `Toys/LaserToyInteraction.cs`, feeding gestures, and HUD/EventSystem configuration.

Implement the following rules through the existing pointer reservation mechanism, extending it only where necessary:

| Gesture | Owner and result |
|---|---|
| Begins over UI | UI only, for its entire lifetime; releasing over the room never triggers a world action. |
| One finger over the stationary cat in normal mode | Pet input; a deliberate stroke produces pet feedback. A tap does not orbit or force a close-up. |
| One finger in active ball aiming mode | Ball preview and throw on valid release; never pet or orbit. |
| One finger in laser mode | Existing valid-floor laser aiming/release behavior; never pet or orbit. |
| Two eligible world fingers | Cancel any uncommitted pet stroke or toy aim without throwing; enter pinch. An already launched ball can continue. |
| Finger lifted after pinch | Consume remaining fingers until all lift; do not convert the remaining finger into a new pet/throw gesture. |
| Empty-floor drag in normal mode | No camera movement and no unsolicited movement command. |
| Cancellation, focus loss, menu transition, or app backgrounding | Clear all captured pointer IDs, previews, and pending releases; never dispatch an action from a stale touch. |

UI-owned touches cannot become eligible pinch touches mid-gesture. Preserve existing locks for hidden feeding/sleep states. Switch or cancel toys by calling their cleanup paths before acquiring another activity. A disabled action should show brief, relevant feedback rather than silently fail.

Reproduce `Touch was already deallocated` using development device logs and obtain the full stack. Check touch lifetime, input module duplication, and focus/cancellation handling according to that evidence. Do not assume the source from the screenshot, replace the input stack speculatively, or suppress exceptions to hide the bug. Store pointer IDs and copied position values for later frames; do not retain short-lived enhanced-touch records across callbacks or awaits.

Acceptance: repeat 20 sequences mixing UI tap → toy drag → second finger → release → pet → background/resume. Zero duplicate actions, stuck captures, unintended throws, camera turns, or touch exceptions.

## 3. Make ball play visibly faster

Primary files: `Scripts/Cat/CatMotor.cs`, `Scripts/Toys/CatBallInteraction.cs`.

Initial tuning to implement and then observe:

| Parameter | Current | First target |
|---|---|---|
| Ball chase speed multiplier | 1.12–1.28 | 1.45 (about 0.99 m/s at existing base speed) |
| Ball acceleration multiplier | 1.12 | 1.45 (about 3.19 m/s² at existing base acceleration) |
| Notice delay | 0.32–0.52 s | 0.15–0.25 s |
| Pursuit target refresh | 0.48 s | 0.20 s, and only update for meaningful target displacement |
| Watch delay after bat | 0.65 s | 0.30 s |
| Walk playback ceiling | 1.55x | Keep 1.55x initially |

Expose these as clearly named ball-specific tuning values. Set the pace once when a chase begins; do not randomize it every time the target updates. Preserve acceleration, corner slowdown, final braking, and existing navigation ownership. Keep feeding, sleep, laser, and normal roaming speeds unchanged during this ball adjustment.

Preserve Notice → Chase → Brake/approach → Bat → Watch → Chase/finish. Repath without resetting the walk clip or restarting a noticing delay. Match playback to measured horizontal velocity. If visible paw sliding remains, calibrate the speed/playback relation and reduce chase speed before increasing the clip ceiling. A brisk walk is the available gait.

Keep the existing 1.1–4.0 m/s throw range and 1.5 m/s bat impulse initially: increasing ball speed too would make it harder for the cat to catch. Make swipe direction agree with the screen-space preview: a swipe toward the back of the room must launch toward the back. Normalize drag distances against the safe-area dimensions so the same relative swipe behaves alike across phone resolutions. Preserve the current throw strength at the reference phone dimensions as the calibration baseline.

Retain one reusable ball, collision detection and interpolation, floor contact, and furniture collisions. Raycast valid floor and validate NavMesh approach points; do not let the player target furniture interiors or unreachable gaps. Recall remains available for a trapped ball and returns it to a reachable visible point only through an explicit action.

Bat only when the ball is near the cat's visible front contact area, the cat faces it, and the ball is low enough to reach. Existing root distance of 0.58 m alone is not a sufficient contact test. Derive an approximate front-contact point from the normalized model bounds and discovered forward axis; debug-draw it during tuning. Start with ball-surface/contact tolerance at 0.10–0.15 m and facing tolerance 25 degrees, then verify the actual loaded model. Do not require a paw-swipe animation that does not exist. One impulse, impact sound, and optional light haptic occur together when contact is valid. Preserve the three-bat loop and cleanup timeout.

Acceptance: compare the same 2 m unobstructed chase before/after; target at least 20% shorter release-to-first-contact time without sliding or overshoot. Record measured times. Run the existing two-throw/three-bat validator, then try five short and five strong throws toward center, sides, and a furniture boundary. No duplicate balls, remote bats, floor tunneling, permanently held locks, or unrecoverable pursuit. If an edge throw is unreachable, finish gracefully and keep Recall usable.

## 4. Make the cat respond to the player

Use existing `CatCallInput`, `CatPetInput`, `CatPetReaction`, `CatMotor`, `CatAmbientBehavior`, `CatRoomAudio`, feeding, and sleep systems. Do not add a second cat controller.

- **Call:** immediately acknowledge the button press; retain the short noticing delay; walk to an authored foreground point that is inside the NavMesh and outside the bottom-right HUD. Choose the point so the cat is approximately 25–35% of screen height at default view without rescaling it. On arrival face toward the camera's ground projection. The cat comes to the player's side of the room; the camera stays put.
- **Pet:** a deliberate stroke over the stationary visible cat starts the existing reaction and ramps the purr over approximately 0.15 s. Fade it after stroking stops. Keep haptics brief and rate-limited; no continuous vibration. Preserve existing reward cooldowns. Do not count an empty-floor swipe, UI release, or pinch as petting.
- **Ambient:** reuse existing activities and give player commands priority. Let the cat remain available for approximately 2–4 s after Call before choosing another ambient destination. Do not interrupt active petting, feed, sleep, or toy locks. Pace changes should correspond to starting, turning, approaching, or changing activity rather than random jitter.
- **Laser:** retain the existing floor-target/release interaction this pass; give clear dot feedback and reachable chase targets. Route every focus request through the fixed camera rule. Do not turn laser play into direct one-to-one cat dragging.
- **Feed and Sleep/Wake:** preserve current activity logic, timing, energy, and saves. Check that the fixed view still hides unsupported eating and sleep transitions. If needed, adjust the specific approach/hidden point minimally; do not move the whole camera to cover it.
- **Missing clips:** stationary cat quality remains limited by available animations. Use existing safe reactions/audio and document that limit. Do not freeze a mid-step pose and report it as a new idle animation, or add arbitrary mesh wobble to simulate life.

Acceptance: Call arrives visibly, faces the player, and is immediately pettable. Each valid touch has visual or audio acknowledgment; muted play remains understandable. Cancel or complete each activity and confirm another can start. Save/resume retains the same cat and existing saved state.

## 5. Keep the room readable

Primary files: `Scripts/UI/CatHomeHud.cs`, existing audio/feedback classes, and `Editor/IOSDevelopmentBuild.cs` for build presentation only.

- Keep the current layout and rounded corners. Fix obvious edge clipping, seams, and overlays only where they obstruct play. No material/lighting/prop overhaul.
- Keep Settings/Energy top-left, Home view/More top-right, and Sleep/Play/Call/Feed bottom-right. Use safe-area anchors and minimum 44 pt equivalent touch targets on device; a reference-resolution pixel count is not proof of physical size.
- Play opens Ball/Laser/Recall. Close the tray after choosing a toy and retain one clear active-tool/exit indicator. Make the selected toy and exit action identifiable. Reopen Play to use Recall if necessary. Menus must not leave transparent raycast blockers over the room after closing.
- Keep feedback near the action and brief; avoid covering the cat or adding a new permanent status panel. Rate-limit repeated sounds and haptics. Honor existing mute settings.
- Fix the touch error before hiding diagnostics. Retain a development build for debugging. When verified, export the phone evaluation build without Unity's Development Build watermark/console, while retaining useful device logs. Do not describe hiding the console as fixing its underlying error.

## 6. Execution order and evidence

1. Capture current HomeRoom camera values, screenshot, cat/ball tuning, scene backup, and Git status. Record current behavior before editing. Do not revert unrelated uncommitted changes.
2. Implement fixed camera and gesture ownership. Compile C# with Unity 6000.6.2f1; check the actual Game view in landscape, including overlay UI. Resolve the device touch exception with a stack-supported fix.
3. Implement ball pacing/contact changes. Run focused ball checks and the existing navigation route check because pursuit speed and braking changed. Keep successful checks unless subsequent changes invalidate them.
4. Adjust Call arrival/facing, pet feedback, and HUD tray behavior. Check feed, sleep, laser, and ambient callers for fixed-view regressions. Reuse existing validators where appropriate, plus observed interactions; do not create an unrelated broad test suite.
5. Export through `CatMe.Editor.IOSDevelopmentBuild.Build` and retain the offline StreamingAssets cat, both landscape orientations, and `com.catme.home`. Keep signing material and generated GLBs ignored. Verify current team/device state; update the existing phone app in place and preserve data. Ask the user only for actual account, trust, or agreement actions that require them.
6. On the physical iPhone: launch/cat loading; both rotations; fixed angle through all activities; pinch extremes; Call and five pet strokes; ten ball throws; laser; Feed; Sleep/Wake; mute; interruption; background/resume; restart/save. Observe five minutes of play for stalls, runaway logging, and increasing resource use. Record actual FPS/frame-time observations if available; do not promise a stable 60 FPS from project settings alone.
7. Update STATUS with implemented changes, exact output/install receipt, checks actually performed, and remaining defects. Update MOBILE and the camera portions of older design documents to stop future agents reinstating obsolete camera behavior. Record the fixed-view decision in DECISIONS when implemented. Keep the existing research task separate.

## Completion report format

- Implemented behavior and files changed.
- Before/after ball timing and final tuning values.
- Unity compile/runtime results and screenshot showing Game view plus HUD.
- Phone build path, install/launch results, and physical checks actually observed.
- Touch exception: reproduced stack, fix, repeat-check outcome, or explicit unresolved blocker.
- Remaining animation/rig limitations or device issues. An export, install receipt, or running process alone is not proof of a playable physical experience.

If native build work blocks progress, record the exact error and attempted fixes for a focused handoff. Do not call the milestone complete until the physical interaction checks have evidence.
