# Garden Gameplay Contract

## Player and companion roles

- The owner avatar is player-controlled.
- The cat follows the owner at a readable, comfortable distance. It catches up smoothly after turns, checks back when the owner pauses, and stays inside the useful camera view when possible.
- The garden is a compact authored play space, not an open-world promise.

## First session loop

```text
Pair setup
→ enter garden together
→ cat playfully slips behind a nearby garden feature
→ owner calls/listens; player follows subtle visual and audio clues
→ cat is found; close-up greeting and owner acknowledgment
→ walk together
→ pick up and throw one ball
→ cat notices, chases, engages, and rejoins
→ free garden play
```

The opening search is brief, readable, and impossible to fail. The cat is nearby and safe. Do not use loss, countdowns, a fail state, or repeated nagging prompts. Allow skip/accessibility cues if searching or sound cues are difficult.

## Desired companion behavior

- Follow the owner with smooth acceleration and turns; avoid snapping or hovering.
- Occasionally glance toward the owner, sniff a nearby point of interest, or trot to catch up. Keep idle reactions sparse so they do not interrupt player movement.
- Notice the ball before the throw, orient toward its path, chase after release, investigate/catch it, then return near the owner or wait beside it.
- A gentle direct interaction may invite the cat closer. Use a meow, purr, head rub, or paw gesture only when the loaded asset has a suitable motion; do not fake unsupported body animation.

## Owner animation opportunities

The owner can make the relationship legible with a small authored set: walk, stop/look, call/listen, crouch/reach for the reunion, pick up/throw a ball, and respond warmly when the cat returns. Add stronger emotional performances only in a later optional story and only when the human rig supports them convincingly.

## Mobile controls and camera

- Lower-left movement stick controls the owner.
- One clear ball action appears when useful; aiming and release should be easy to discover.
- Keep camera look and movement gestures distinct. Avoid covering the owner, cat, clues, or ball path with UI.
- Preserve the approved slightly elevated trailing view and relative character scale. Make safe-area, touch, rotation, and performance decisions on physical phones.

## Story boundary

The first hide-and-find is a warm onboarding moment, not a tragedy. A later optional story may explore separation, memory, or reunion with emotional depth. Never make the cat permanently disappear, punish absence, or assume whether a player's real pet is alive. A remembrance path must be clearly user-selected and gentle.

## Implementation ownership and limits

Keep cat navigation in `CatMotor` and animation in `CatAnimationController`; keep owner locomotion, camera, ball interaction, and UI in their existing focused modules or clearly owned garden equivalents. Discover clips, scale, axes, and supported rig controls from each GLB. The current GardenCompanion prototype includes walking characters, follow/call controls, and ball chase, but the opening hide-and-find flow and polished human reactions are not yet verified as complete.

`HomeRoom`, laser, feeding nook, room needs, and room-specific ambient behavior belong to the legacy prototype. Do not use them as the active garden gameplay specification.
