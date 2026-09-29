# CatMe Garden implementation handoff

**Current product direction:** owner-controlled garden companion. The personalized cat follows the owner; the first play loop is walking together and throwing one ball. The first-run story briefly guides the player to find the cat, reveals a close-up reunion, then teaches walking and ball play.

- Product and staged plan: [`context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md`](../../../context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md)
- Player journey: [`docs/CATME_EXPERIENCE_DESIGN_V1.md`](../../CATME_EXPERIENCE_DESIGN_V1.md)
- Current status and device evidence: [`context/STATUS.md`](../../../context/STATUS.md)
- Current device evidence and constraints: [`context/STATUS.md`](../../../context/STATUS.md)

## Existing prototype snapshot

`Assets/CatMe/Scenes/GardenCompanion.unity` is a standalone scene. The recorded prototype includes owner movement, cat roam/follow/call behavior, and ball throw/chase. It loads walking character GLBs from `Assets/StreamingAssets/CompanionGarden/`. The walk/ball foundation has Editor smoke evidence; the full onboarding, hidden-cat opening, photo creation flow, and physical-phone gameplay still need deliberate validation.

The existing `HomeRoom` and room-centered gameplay are legacy implementation only. Keep its files and player data intact, but do not extend it as the product direction or use old task briefs as active work. Old plans, research, and build notes are kept under `docs/archive/pre-garden/` or `context/archive/pre-garden/` for engineering history only.

## Important production blocker

The local GardenCompanion prototype was recorded as calling Tripo and Meshy directly with ignored local Unity credentials. Such credentials are extractable from an app build. Do not distribute this prototype. Before any public build, remove bundled keys/direct provider calls and move provider processing behind an authenticated HTTPS service. Do not run paid photo-generation jobs without a separately approved budget.

## Technical gates

- Use known-good local character fixtures for routine gameplay work; do not spend provider credits for iteration.
- Discover each GLB's scale, axes, skeleton, and available clips instead of assuming a common rig.
- Track model download size and runtime frame performance on target phones. The recorded three-character demo assets totaled about 71 MB before iOS packaging.
- After C# edits, run focused Unity compilation. Before calling a mobile phase complete, verify touch, safe areas, camera framing, performance, save/reload, sound, and the relevant story/gameplay flow on a physical phone.
