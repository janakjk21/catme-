# CatMe Garden Experience Design

**Status:** Current product direction. Planning contract; this document does not claim every flow is implemented or validated.

## Experience promise

A player creates a recognizable cat companion and an owner avatar, then explores a welcoming garden with the cat. The owner is controlled by the player. The cat follows, notices things, and responds. The emotional reward comes from feeling that these two characters share the space.

## Visual and camera direction

- Use the approved concept image as the scale and camera reference: owner prominent in frame, cat clearly visible nearby, camera slightly elevated behind the owner.
- Keep the pair and the path ahead visible at mobile screen sizes. Avoid shrinking characters just to show the whole garden.
- Build a compact, softly stylized garden with clear routes, a few meaningful points of interest, and uncluttered interaction space. Keep the art practical for stable phone performance.
- Do not copy the reference game's characters, logos, UI, or exact environment.

## First launch: find your cat

1. **Create the pair.** Choose a cat photo and review the generated cat. Choose or take an owner selfie and review the avatar. Explain processing; provide progress, retry, and a playable fallback.
2. **Arrive together.** Begin in the garden with the owner visible and a sense that the cat was just nearby. The cat playfully darts behind a close, safe feature such as a hedge or low garden wall.
3. **Notice and search.** The owner pauses, looks around, and calls softly. The player gets a very small set of clues: leaves rustle, pawprints continue along the path, a tail peeks out, or a familiar meow comes from nearby.
4. **Guide without frustration.** A subtle directional hint helps if needed. There is no timer, fail state, danger, or permanent loss. Let players skip or enable extra visual cues.
5. **Recognize and reunite.** The cat peeks out and approaches. Briefly move to a close view so the player can recognize their generated cat. The owner kneels or reaches if supported; the cat meows, rubs, or settles close if its rig supports that motion. A simple “There you are” may be offered, but the moment should work through body language and sound alone.
6. **Start playing.** Pull back smoothly to the approved trailing camera. Teach movement with one quiet cue, then let the player walk while the cat follows. Introduce the ball after the player has had a moment to walk together.

The opening should feel like a playful first meeting and reunion, not a frightening missing-pet scenario. Keep it short and skippable.

## Core play: walk and ball

### Walk together

The lower-left movement control moves the owner. The cat follows with believable distance and timing, occasionally looking toward the owner or catching up after a turn. A few garden details can attract the cat briefly, but should not steal control or leave the companion off-screen for long.

### Ball interaction

Show a clear ball action when the player is ready. Let the owner pick up and throw or aim/release the ball. The cat notices, anticipates, chases, investigates or bats it, then returns near the owner. Give the action a readable beginning and end so the player can repeat it or continue walking.

### Small connection moments

Use sparing glances, a short meow, a sniff, a pause beside the owner, or a gentle pet response to make the pair feel connected. Avoid constant pop-ups, energy pressure, compulsory care chores, and reward spam in the first slice.

## Future emotional story mode

Story chapters come after walk-and-ball play has been proven enjoyable. Keep them optional, brief, and driven by what the characters do together rather than a wall of dialogue. A later arc could explore separation and finding one another again, using the owner's wider animation range—listening, searching, crouching, laughing, or crying—and the cat's familiar reactions.

A possible first chapter is **The New Path**: the cat notices a bird near a garden gate, looks back at the owner, and waits. The player follows. They discover a sunny nook; the owner kneels, the cat comes close, and the nook remains as a small change in the garden. The pair return to free play.

For players who choose a remembrance experience, the story may acknowledge longing for a real pet. Do not assume a pet has died, imply the digital model is the same living animal, use guilt or fear to retain players, or make loss the default marketing promise. Let the player decide whether to engage with remembrance themes. Never make the cat's disappearance permanent or a consequence of missed play.

## Audio, animation, and accessibility

- Prioritize expressive timing, orientation, approach, and pauses. Use cry/laugh or other intense human performances only when they fit an explicitly chosen story beat and the rig can deliver them naturally.
- Keep meows, purrs, footsteps, leaves, and garden ambience gentle; provide mute and volume controls.
- Pair sound clues with visible clues. Provide additional search guidance and a skip option.
- Avoid flashing cues, tiny hit targets, rapid repeated tapping, and UI covering the characters.
- Inspect every generated GLB's scale, rig, clips, and facial controls. A selfie does not guarantee a convincing expressive avatar.

## Success checks

- A new player can explain who they control and what the cat does.
- Players find the cat without confusion or distress and understand the first close-up moment.
- The camera keeps both characters at the approved scale while leaving the route readable.
- Walking feels smooth; the cat catches up without snapping or becoming lost from view.
- Players understand the ball action and see a satisfying chase/rejoin.
- Search cues, UI, sound, and movement are comfortable on physical phones.

## Scope boundary

This is the garden game design. The old indoor HomeRoom, room furniture interactions, laser game, room feeding/sleep system, and room-specific HUD are legacy prototype work, not part of this experience contract. Keep legacy implementation data intact until a migration is explicitly planned.
