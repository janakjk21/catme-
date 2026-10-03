# WornTag working rules

This repository is the Unity iPhone app. The server is the sibling folder `catme/worntag-api`.

## Before any work

1. Read `context/STATUS.md`, `docs/ROADMAP.md` and `context/CLAUDE_TASKS.md` (the agent task list; work only on the current task), then the doc for the task (index: `docs/README.md`): `docs/ONBOARDING_FLOW.md` (screens and flow), `docs/DESIGN.md` (look, layout, words, skills to use), `docs/GAMEPLAY_AND_WORLD.md` (map, characters, controls, world), `docs/PHOTO_TO_3D.md` (photo to 3D pipeline), `docs/ARCHITECTURE.md` (where code lives), `docs/SETUP.md` (running, server, signing).
2. Build only what `docs/ONBOARDING_FLOW.md` says. If a request changes the flow, update that doc first, then the code.

## Rules

1. One flow, one scene (`GardenCompanion.unity`), one map (`Place_HomeGarden_r2.unity`). Do not add scenes, panels or parallel flows.
2. Generation happens only on the server. The phone never holds or uses Tripo or Meshy keys.
3. Do not spend provider credits without the owner's go-ahead. Test with the server's test models first.
4. After changing C#, compile (Unity batch mode or the generated projects). Run the flow in Play Mode before calling it done; test on a physical iPhone before calling a mobile milestone done.
5. No commits, pushes, deploys, purchases or published Firebase rules without the owner's instruction.
6. Never commit generated models, `LocalFixtures/`, `.local-private/`, credentials, service-account files, signing material or Unity caches.

## Keeping docs useful

- `context/STATUS.md` is a short handoff: now, not verified, next. Replace it; do not append a diary.
- `context/DECISIONS.md` holds lasting decisions; replace an entry when it changes.
- Remove code and docs that a change makes obsolete in the same change.
