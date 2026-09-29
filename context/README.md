# CatMe Context Router

Use this index to load only the active context for a task. Current product direction is the garden companion game described in `context/PRODUCT.md`, `context/GAMEPLAY.md`, and `context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md`.

## Required startup

1. Read repository `AGENTS.md` and `context/STATUS.md`.
2. Load one relevant context module below.
3. Inspect only task-related scenes/scripts/assets and direct dependencies.

Use `docs/CATME_EXPERIENCE_DESIGN_V1.md` when working on a full player journey. Use `docs/CATME_MOBILE_GAME_IMPLEMENTATION.md` for a cross-system implementation question. Ordinary isolated changes should use the smaller modules.

## Task routing

| Task | Context | Typical source area |
|---|---|---|
| Cat or owner GLB, rig, clips, materials | `ASSET_PIPELINE.md` | `Assets/CatMe/Scripts/Cat/`, character loaders, `ModelTest` |
| Owner movement, cat follow, ball, interactions | `GAMEPLAY.md` | garden gameplay, `Scripts/Cat/`, `GardenCompanion`, `Toys/` |
| Camera, touch, haptics, phone build | `MOBILE.md` | `Scripts/Camera/`, `Input/`, project settings |
| API, character download, cache, save | `ASSET_PIPELINE.md` | `Scripts/API/`, `Scripts/Save/` |
| Product scope or phase decisions | `PRODUCT.md` | `context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md` |
| Unity packages, scenes, validation | `UNITY_PROJECT.md` | project configuration and `GardenCompanion` |
| System boundaries or new services | `ARCHITECTURE.md` | affected runtime modules |
| Credentials, photos, storage, privacy, release | `SECURITY.md` | API, save, platform configuration |

For cross-system changes, load at most two modules and explain why both apply.

## Source priority

1. Current user instruction.
2. Repository `AGENTS.md`.
3. `context/STATUS.md` and current decisions.
4. The active plan and relevant context module.
5. Implementation evidence.
6. Archived pre-garden material, for technical history only.

## Keeping context current

`STATUS.md` is a concise handoff, not a diary. `DECISIONS.md` contains only current durable decisions. Replace stale facts instead of appending history. The only active bounded product plan is `context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md`; older room-era briefs are archived and must not route new work.
