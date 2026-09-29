# Natural Cat Movement

## Goal

Make the cat feel purposeful and responsive while exploring and playing in the existing HomeRoom. Keep the single-player room prototype and existing cat, navigation, camera, needs, toy, save, and interaction systems.

## Read first

- `AGENTS.md`
- `context/README.md`
- `context/STATUS.md`
- `context/GAMEPLAY.md`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs` and its direct callers for Call, ambient behavior, laser, ball, feeding, and sleep

Use the known-good local cat. Do not generate another cat or spend Meshy/Tripo credits. Do not add accounts, rooms, purchases, or an alternate movement controller.

## Current behavior

`CatMotor` owns NavMesh movement and varies movement speed with walk playback. It adds occasional pacing pauses, but most changes are random. The active cat may only provide a walk clip, so faster movement can look like an over-cranked walk. Inspect the current clip and the actual cat rig before choosing animation or rigging changes.

## Changes

### Activity-aware pace

Keep movement inside `CatMotor`, with bounded profiles for:

- Exploring: relaxed walking, gentle acceleration, occasional purposeful pauses, and unhurried arrivals.
- Call: brief reaction delay, eager approach, and gradual braking near the player.
- Feeding: direct movement to the feeding point and slower final positioning.
- Ball: notice and watch the moving ball, accelerate into pursuit, brake near it, bat it, then follow its new direction or settle.
- Laser: short pursuit bursts with occasional hesitation near the target.
- Sleep: calm, steady movement to the existing sleep approach.

Speed changes should have a visible cause. Avoid frequent unrelated pace changes or sudden full stops during ordinary walking.

### Movement and turning

- Smoothly change desired speed and acceleration.
- Start braking based on remaining distance and current speed.
- Slow before tight turns; use the upcoming NavMesh direction to guide turning where practical.
- Preserve complete NavMesh paths around the newly furnished room.
- Prevent sideways sliding, heading snaps, and oscillation near destinations.
- Keep sharp stops occasional and specific to play.
- Preserve the deterministic diagnostic route.

### Animation

- Match walk playback to actual travel and the cat's visible stride.
- Review the minimum playback rate during slow movement and braking; avoid foot sliding.
- Do not restart the walk clip for every pace change.
- Keep imported root motion isolated from the navigation root.
- Discover usable clips per loaded cat. Blend to an idle clip only when one exists.
- If only a walk clip exists, keep a stable stop and document the limitation. Do not fake missing run, pounce, or idle motions with mesh deformation.

### Ball play

Make the existing reusable ball interaction read as **notice → pursue → brake → bat → observe → pursue again or finish**.

- Pursue the ball using its current position and velocity; update the target at a bounded rate.
- A short, clamped motion prediction is allowed when it improves interception.
- Validate predicted destinations against the NavMesh and reachable path.
- Only bat when the cat is close enough. Let ball physics determine its next roll.
- Handle a stationary or unreachable ball, wall collisions, interrupted play, and low energy.
- Keep the floor mouse toy connected to this existing reusable ball interaction.
- On completion or interruption, release movement/activity locks and leave Call, petting, laser, feeding, sleep, and camera input usable.

### Attention and packages

Inspect the cat skeleton before attempting head or neck tracking. Add a small, smoothly blended look toward a current target only when the relevant bones are identified reliably. Make it optional for assets without those bones.

Start with installed Unity navigation and animation tools. Do not install packages just to vary speed. Consider Animator blend trees or Animancer only if multiple compatible clips are available. Consider Animation Rigging only after confirming rig compatibility and a concrete need. Do not buy or download assets as part of this task.

## Acceptance

- Exploring, Call, feeding, laser, ball, and sleep movement have visibly appropriate pacing.
- Acceleration, tight turns, and arrivals feel smoother without path failures.
- Walk playback follows travel without obvious new foot sliding or root drift.
- Several ball throws show coherent pursuit, braking, and batting; the cat does not get stuck around furniture.
- Existing Call, petting, laser, feeding, sleep, camera, energy, and local save behavior still work after completion and interruption.
- Focused Unity C# compile passes, and the actual HomeRoom Game view has been checked. Reopen `HomeRoom` if the editor retained a stale in-memory scene after an external file update.
- Record physical iPhone behavior separately when no device check was performed.

Update `context/STATUS.md` with implementation, verification, and animation limitations. Keep the work local; do not commit or push.
