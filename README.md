# CatMe

CatMe is a Unity mobile companion game. Players create a cat from a photo and an owner avatar, then explore a garden together. The owner is controlled by the player; the cat follows. The first play loop is walking together and throwing a ball for the cat to chase.

## Current first session

Create/select the pair → enter the garden → the cat playfully hides nearby → follow gentle clues → find the cat in a close-up greeting → walk together → throw the ball → continue free play. The opening search is short, safe, skippable, and non-punitive. Optional emotional story chapters come later.

## Start here

- [Current status](context/STATUS.md)
- [Context router](context/README.md)
- [Garden product and implementation plan](context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md)
- [Garden experience design](docs/CATME_EXPERIENCE_DESIGN_V1.md)
- [Garden implementation guide](docs/CATME_MOBILE_GAME_IMPLEMENTATION.md)
- [Current implementation handoff](docs/implementation/catme/INDEX.md)

The former indoor HomeRoom, room props, first-person room loop, laser play, and room-care systems are legacy prototype work. The files and local app data remain intact, but they do not define current development. Pre-garden briefs and research are archived under `context/archive/pre-garden/` and `docs/archive/pre-garden/`.

## Project boundaries

- This repository owns the Unity mobile client.
- `../clonecatme` remains the separate browser/generation laboratory.
- Routine iteration uses known-good local character assets; do not spend provider credits for ordinary gameplay changes.
- Never commit generated character files, Unity caches, credentials, or signing material.
- Do not distribute the current direct-provider-call prototype; see the current handoff for its production blocker.

## Unity setup

Unity `6000.6.2f1`, URP, Input System, Cinemachine, AI Navigation, and glTFast are configured.

- `Bootstrap.unity`: app startup.
- `ModelTest.unity`: isolated character asset diagnosis.
- `GardenCompanion.unity`: current gameplay prototype.
- `HomeRoom.unity`: legacy scene, retained pending a deliberate migration.
