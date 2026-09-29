# CatMe final experience: staged implementation plan

**Status:** owner direction captured; planning only. No game source, scene, or assets were changed to create this plan.  
**Repository:** `/Users/janaksapkota/Downloads/appliction new idea/catme/catme-home-mobile`  
**Working rule:** complete one phase, verify it on the iPhone, update `context/STATUS.md`, and review before starting the next phase.

## Product direction

CatMe is a personal companion game set in a garden. The owner avatar is the playable character; the cat made from the player's photo stays nearby and follows. The player can walk with the cat and start a simple ball game where the cat chases the thrown ball. Story moments and richer bonding interactions are future additions, not requirements for the first playable slice.

The user's written direction is authoritative. Use the approved garden gameplay concept for character scale and camera composition; it is a visual guide, not production art.

## Current garden direction

- **Pair and camera:** show the owner and cat together at the relative scale and slightly elevated, behind-the-owner camera angle in the approved garden gameplay concept. Keep both readable on a phone while leaving enough path visible to guide movement.
- **Garden play:** control the owner with a simple mobile movement control. The cat follows at a comfortable distance, catches up when needed, and stays visible. Keep the garden contained and authored rather than implying a large open world.
- **Ball v1:** one clear ball action. The player picks up/aims and throws; the cat notices, chases, and returns or comes back near the owner. Keep other toys out of this first play loop.
- **Personal characters:** plan for a cat photo-to-animated-cat flow and an owner photo-to-animated-avatar flow. Treat image-to-3D quality, rigging, generation cost, privacy, and phone performance as validation risks; the concept images are not evidence that either pipeline is production-ready.

## Suggested implementation phases for the garden direction

These are planning boundaries, not authorization to begin implementation. Confirm technical feasibility and the garden visual target before buying assets or spending provider credits.

### Phase 0 — Confirm the garden slice

Agree on the approved owner/cat scale and trailing camera, garden size, first-run path, and simple touch layout. Review the current GardenCompanion prototype and identify which assets and systems can be reused. Set mobile performance targets before adding scenery.

### Phase 1 — Create and save the pair

Let the player choose a cat photo, review the generated animated cat, then create or choose an owner avatar from a selfie and review that result. Explain processing and provide progress, retry, and fallback states. Save approved character files locally so the pair can be restored. Validate quality, privacy, provider cost, generation time, and mobile file size before treating either pipeline as launch-ready. For the first session, the cat then playfully hides close by; guide the player with subtle clues to a brief close-up greeting before introducing movement. Keep this safe, short, skippable, and non-punitive.

### Phase 2 — Walk together

Make the owner controllable with a thumb-friendly movement control; make the cat follow, check back, and catch up smoothly. Preserve the agreed scale and camera. Keep ambient reactions rare enough that movement remains legible and responsive.

### Phase 3 — Ball play

Give the owner a clear ball pickup/throw interaction. The cat should notice the aim, chase the ball after release, interact with it, and rejoin the owner. Keep the input and reaction readable without adding other toys or menus.

### Phase 4 — Basic bonding and polish

Add only a few high-value reactions such as gentle petting, a nearby meow/purr, and a close-up pair screenshot. Review whether simple food/sleep care belongs in this garden version before designing its props or energy system. Check controls, safe areas, sound, performance, and recovery on a physical phone.

### Phase 5 — Optional story chapter

Only after walk and ball play feel good, prototype one short, optional garden discovery. Check that the cat's attention and movement communicate the next step, that the owner participates in the moment, and that the player returns naturally to free play.

### Phase 6 — Release readiness

Remove development controls and hard-coded credentials; keep provider calls behind a secure service before public distribution. Verify consent and deletion behavior for photos, local saves, loading/offline fallback, accessibility, performance, and the complete player journey on the target phone.

## Future story and bonding features, outside the first playable slice

- **Optional short story mode:** give the pair a reason to explore the garden through brief, self-paced chapters. A chapter can invite the player with a cat noticing a sound or place, let the player guide the owner and cat there, offer one shared interaction, then reward the moment with a small lasting garden detail. Return to free walk and ball play afterward; avoid long cutscenes, mandatory daily tasks, and dialogue-heavy scenes.
- **Example chapter — The New Path:** the cat hears a bird near a garden gate, looks back toward the owner, and waits. The player follows; the owner crouches, the cat comes close, and together they discover a sunny nook. The nook remains in the garden, and the cat may visit it later during free play.
- **Bonding in the spaces between objectives:** occasional eye contact, the cat checking that the owner is nearby, a short trot to catch up, sniffing or watching a garden detail, and settling near the owner when movement stops. These are small readable behaviors, not constant prompts.
- **Direct affection:** tapping near the cat can make the owner acknowledge it and the cat approach, rub, or meow. Use restrained purr/meow, footsteps, and grass/path ambience with mute controls. Avoid requiring precise touch gestures for basic play.
- **Shared discoveries and keepsakes:** a butterfly, rustling leaves, or a sunny patch can briefly attract the cat. A player may capture a close-up pair photo as a keepsake. Naming the owner and cat can personalize occasional optional greetings; emotional lines such as “I miss you, Mommy” remain a later idea and should not be frequent or guilt-inducing.

These features should be assessed after the garden walk and ball loop are enjoyable. A first story chapter should be short, optional, and end by returning control to the player. Do not add chapter rewards, progression systems, or a memory gallery until separately reviewed.

## Other later ideas, deliberately outside the first pass

- Cat messages/personality lines that use the owner's name, such as “I miss you, Mommy.”
- Multiple companion pairs, progression/economy, multiplayer/social features, cloud accounts/sync, or an in-game memory gallery.
- **Themed self-avatar creator:** later, consider letting a user choose a visual style for their avatar. Treat each style as a different interpretation of the same person. Reassess photo privacy, generation cost, and rigging/animation fit before planning implementation.

## Execution and change boundaries

- Implement only the active phase. Do not bundle later-phase features into it.
- Preserve the current `com.catme.home` identity and app data when installing updates.
- Keep `CatMotor` as the cat navigation owner and preserve existing save/import boundaries.
- Keep garden changes isolated to `GardenCompanion`; do not migrate or remove the legacy scene until separately reviewed.
- After each changed phase: focused Unity compile for C# changes, targeted acceptance checks, physical-phone review, and a concise `context/STATUS.md` update. A successful build alone is not phone validation.
- Do not buy props, spend generation credits, publish, or make a store release without explicit confirmation for that specific action.

## Relationship to older plans

This is the active product plan. Archived pre-garden docs are engineering history only and do not override these phases.
