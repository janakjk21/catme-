# CatMe Home working rules

## Scope

This repository is the Unity mobile game client. The existing browser generation pipeline remains in the sibling `clonecatme` repository.

- `Assets/CatMe/` contains authored game code and assets.
- `LocalFixtures/` contains large local-only generated models and is not committed.
- `docs/` contains the product and implementation contract.
- `Library/`, `Temp/`, `Logs/`, and other Unity-generated folders are never source.

## Workflow

1. Read `context/README.md` and `context/STATUS.md`, then load only the context module named for the task. Do not read the full implementation reference unless the routing table says it is needed.
2. Use the known-good local cat for normal gameplay work. Do not spend Meshy or Tripo credits for routine tests.
3. Keep provider credentials and provider-specific task handling on the server.
4. Validate gameplay on a physical mobile device before calling a mobile milestone complete.
5. Keep scenes small and systems modular. Prefer `ModelTest` for asset diagnosis and `HomeRoom` for gameplay.
6. Run a focused Unity compile after changing C# files. Run broader checks only when the touched surface requires them.
7. Update `context/STATUS.md` after completing a milestone or changing the next task. Record durable decisions in `context/DECISIONS.md`.
8. Never commit generated cat GLBs, Unity caches, credentials, or signing material.

## Context discipline

- Treat `context/STATUS.md` as the handoff, not as a development diary.
- Keep each context module short and replace stale facts instead of appending repeated history.
- Inspect only the current task's scripts, scenes, prefabs, settings, and their direct dependencies.
- Do not scan `Library/`, `Temp/`, `Logs/`, `LocalFixtures/`, generated models, or the sibling web repository unless the task explicitly requires them.
- Use `docs/CATME_MOBILE_GAME_IMPLEMENTATION.md` as the detailed product reference. Do not load it for ordinary isolated implementation tasks.
