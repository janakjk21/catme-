# CatMe Context Router

This folder provides small, task-specific context packets. It exists to prevent every coding task from loading the complete product history.

## Required startup

For every task, read only:

1. Repository `AGENTS.md`.
2. `context/STATUS.md`.
3. One relevant module from the table below.
4. The files directly involved in the task.

Open the full `docs/CATME_MOBILE_GAME_IMPLEMENTATION.md` only for cross-system design, milestone planning, or an unresolved requirement.

Open `docs/CATME_EXPERIENCE_DESIGN_V1.md` only when implementing or reviewing a complete player flow. Isolated tasks should continue using the smaller modules below.

## Task routing

| Task | Context to load | Typical source area |
|---|---|---|
| Cat import, GLB, rig, animation, materials | `ASSET_PIPELINE.md` | `Assets/CatMe/Scripts/Cat/`, `ModelTest` |
| Walking, navigation, state, energy, sleep, toys | `GAMEPLAY.md` | `Scripts/Cat/`, `Room/`, `Toys/` |
| Camera, touch, haptics, performance, phone build | `MOBILE.md` | `Scripts/Camera/`, `Input/`, project settings |
| API, download, manifest, caching, generated cat | `ASSET_PIPELINE.md` | `Scripts/API/`, `Scripts/Save/` |
| Tokens, Studio Cats, food, toys, shop, ownership | `ECONOMY.md` | product data, shop, inventory, UI |
| Product scope or milestone decisions | `PRODUCT.md` | relevant module only |
| Unity packages, scenes, folder ownership, validation | `UNITY_PROJECT.md` | project configuration |
| System boundaries, dependencies, ownership, new services | `ARCHITECTURE.md` | affected runtime modules |
| Credentials, uploads, downloads, privacy, storage, release | `SECURITY.md` | API, save, platform configuration |

For a task spanning two areas, load at most two modules. State why the second is needed.

## Source priority

When information conflicts, use this order:

1. Current user instruction.
2. Repository `AGENTS.md`.
3. `context/STATUS.md` and a recorded decision.
4. The relevant context module.
5. Detailed implementation reference.
6. Old conversation history.

## Keeping context current

- `STATUS.md` contains only verified current state, current milestone, blockers, and the next concrete task.
- `DECISIONS.md` records durable choices with a date and short reason.
- Topic modules describe stable contracts and boundaries.
- Replace stale entries. Do not append progress logs or debugging transcripts.
- Use `TASK_TEMPLATE.md` for a clean handoff to another agent.
- Active bounded task specifications live under `context/tasks/`; load only the task named by `STATUS.md`.
