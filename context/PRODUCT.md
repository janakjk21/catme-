# CatMe Product Requirements

## Product promise

Turn a photograph of a player's cat into a personal 3D companion, create an owner avatar from the player's photo, and explore a garden together. The owner is the character the player controls. The cat follows and reacts.

## First playable experience

1. Create or choose the cat, then create or choose the owner avatar. Show honest progress, review, retry, and fallback states.
2. Enter the garden together. The cat briefly hides nearby as a playful first-run moment.
3. Guide the player with subtle, readable clues to find the cat. The search is short, safe, and non-punitive.
4. Reveal the cat in a close, personal greeting. The owner can kneel or reach toward it; the cat approaches, meows, or rubs close if its rig supports the motion.
5. Introduce thumb-friendly owner movement. The cat follows, checks back, and catches up.
6. Introduce one ball interaction: pick up, aim/throw, watch the cat chase, then rejoin. Return to free garden play.

## Core design rules

- The owner is the playable lead; the personalized cat is their responsive companion.
- Use the approved relative character scale and slightly elevated camera behind the owner. Keep both characters readable on a phone and leave enough view of the path.
- Keep the first garden compact, authored, and calm. Every prop should support navigation, a clue, or a shared moment.
- Make the first hide-and-find moment playful. Never imply the cat is permanently lost, punish the player, or force a timed search.
- Use animation, movement, gaze, sound, and pauses for emotion before adding dialogue or prompt-heavy UI. Do not claim a photo-created avatar has expressions or motions its rig does not support.
- Keep the journey usable without provider generation: explain failures clearly and preserve a playable fallback.
- Treat uploaded photos, likeness, generation cost, consent, deletion, and local storage as product requirements, not implementation afterthoughts.

## Prototype success criteria

- A new player understands that they control the owner and the cat follows.
- The player finds the nearby hiding cat using clear but gentle cues and gets a satisfying close-up reunion.
- Owner movement and cat following feel smooth at the agreed scale and camera on a phone.
- The player can throw a ball and understand the cat's response.
- The player can leave and return without losing the saved pair or basic state.

## Later features

After the walk-and-ball loop works, consider optional short story chapters, simple petting and bonding reactions, garden discoveries, and a shared photo capture. A deeper emotional story about separation and reunion must be optional and carefully framed for players remembering a pet. It must not suggest the digital companion replaces a pet who died or exploit grief. Food/sleep care, progression, multiple characters, social features, economy, accounts, and cloud sync require separate product decisions.

## Out of current scope

The indoor HomeRoom, room-prop interactions, laser play, room-based feeding/sleeping loop, and room HUD are legacy prototype work. They are not requirements for the garden game. Keep existing scenes and save data intact until a migration is deliberately planned.
