# CatMe Garden Implementation Guide

**Current reference:** this guide follows the garden companion direction. The full product and phase plan is [`context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md`](../context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md); the player experience is [`CATME_EXPERIENCE_DESIGN_V1.md`](CATME_EXPERIENCE_DESIGN_V1.md).

## Product contract

The owner avatar is the player-controlled character. The owner's personalized cat follows through a compact, authored garden. The first session creates or selects both characters, begins with the cat playfully hiding nearby, guides the player to find it, reveals a close-up greeting, then teaches walking and one ball chase. Optional emotional story chapters and richer bonding behavior come later.

The approved phone framing keeps owner and cat clearly visible from a slightly elevated camera behind the owner. Keep mobile controls minimal: movement and one ball action. Search is short, safe, skippable, and never timed or punitive.

## Prototype foundation

The repository contains a separate `GardenCompanion` scene with a recorded prototype for human movement, cat roaming/follow/call, and ball throw/chase. Character GLBs are loaded from `Assets/StreamingAssets/CompanionGarden/`. The existing CatMe project also contains character import, movement, camera, input, save, and UI modules that may be reusable after focused inspection. `context/STATUS.md` records which behaviors and device builds have actually been verified.

This guide does not treat the old indoor `HomeRoom`, room props, first-person room camera, laser activity, or room-based care as product requirements. Existing code can remain for reference until a migration is approved.

## System boundaries

- Generation jobs and credentials belong behind a secure service before public distribution. Unity should receive validated, versioned character assets and cache approved assets locally.
- Owner movement, camera/input arbitration, cat following, ball interaction, animation, and save data should have clear module ownership; avoid a single scene-wide manager.
- `CatMotor` remains the owner of cat navigation unless the garden refactor establishes and documents a safer replacement. Animation code must use clips and rig features present on the loaded asset.
- Keep generated character files, credentials, caches, and signing material out of Git.

## Character creation risks

The recorded local prototype used direct Tripo cat and Meshy human calls with ignored Unity credentials. This exposes keys to anyone who can extract a build. Do not distribute it. Production requires removing those keys and direct calls, authenticated per-user jobs over HTTPS, clear photo consent/deletion behavior, failure recovery, and an asset-size/performance budget. Generation likeness, rig quality, completion time, provider permissions/cost, and human facial expressiveness remain unvalidated.

## Delivery order

1. Confirm the garden first-session layout, camera, character scale, and controls.
2. Validate pair creation and local save/reload using known-good models.
3. Polish owner movement and cat following on a phone.
4. Polish ball throw, chase, and return.
5. Add the brief find-your-cat opening and close-up reunion.
6. Consider optional story chapters only after the core loop works.

Each phase needs focused Unity validation and physical-phone review. A successful compile or Editor preview is not proof of comfortable mobile play. For current checkpoints, use `context/STATUS.md`.
