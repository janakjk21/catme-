# Current Handoff

Last updated: 2026-09-29

## Active game direction

CatMe is a garden companion game. The player creates a cat and an owner avatar, then controls the owner while the cat follows. The approved camera sits slightly above and behind the owner and keeps the pair visible at the agreed relative scale.

**First session:** create/select the pair → enter the garden → the cat playfully hides close by → follow gentle clues → find the cat in a close-up greeting → walk together → throw one ball for the cat to chase → continue free play. The search is short, safe, skippable, and never timed or punitive. Optional emotionally deep story chapters are later features. See `context/tasks/CATME_FINAL_PRODUCT_IMPLEMENTATION_PLAN.md` and `docs/CATME_EXPERIENCE_DESIGN_V1.md`.

## Verified prototype foundation

- `Assets/CatMe/Scenes/GardenCompanion.unity` exists separately from the legacy HomeRoom scene. The recorded Garden prototype includes human movement, cat roam/follow/call, and ball throw/chase. Its three character GLBs total about 71 MB before iOS packaging.
- A Unity batch Play Mode smoke check loaded the three models and expected clips and exercised movement, follow, call, and ball actions. This is Editor evidence, not proof of phone comfort.
- Signed iOS build 8 was installed and launch returned on the paired iPhone 14 Pro Max on 2026-09-28. The rendered app screen was not visually inspected; touch, camera, safe areas, performance, and the complete opening sequence remain unverified on device. Android Build Support is not installed in the recorded Unity editor.
- The cat-hide onboarding, close-up reunion, polished owner emotions, and complete first-run creation journey are design requirements, not verified implementation.

## Production blocker

The recorded local prototype calls Tripo and Meshy directly using an ignored Unity credential file. Keys inside an app can be extracted. Do not distribute this build. Before public use, remove bundled credentials and direct provider calls; move jobs behind an authenticated HTTPS service and define photo consent, retention/deletion, cost, and failure recovery. Do not trigger paid generation during routine gameplay work.

## Next work

Use known-good local character assets to prototype the cat-hide → clue → close-up reunion opening in `GardenCompanion`, then validate the full first session on the physical iPhone. Keep the legacy HomeRoom scene and existing app data intact while the garden flow is reviewed. After any C# changes, run focused Unity compilation; only call a mobile phase complete after the corresponding physical-device checks.

## Legacy implementation boundary

HomeRoom, room furniture, first-person room movement, laser, room feeding/sleep, and the room HUD are retained implementation history only. Do not use them as active design requirements or extend them for the garden direction.
