# CatMe garden character asset acceptance

**Status:** partial source inventory. License, animation quality, provider cost, and physical-phone performance remain open.

## Routine development asset

Use known-good local cat and owner assets for routine gameplay changes. Keep them out of Git when generated. Diagnose each GLB in `ModelTest` before use in `GardenCompanion`.

The cat fixture currently recorded in project notes has one skinned mesh and one 2.6-second walking clip. Its prior isolated-loader checks reported approximately 0.35 m height and floor contact. Recheck current import settings and compatibility against the actual garden scene before treating those values as release evidence.

## Required acceptance

1. Record clips, skeleton, axes, normalized height, renderer/material/texture counts, triangle count, import memory, and phone load time for each character.
2. Verify walk, stopping, owner movement, cat following, ball notice/chase/rejoin, and any used close-up greeting on device.
3. Map facial/body controls only where the rig actually provides them; do not promise expressive selfies without a convincing rig.
4. Verify commercial mobile license and attribution for character, animation, texture, and sound assets.
5. Measure download size, memory, and frame time on the minimum supported phone. Preserve likeness, skinning, and required clips during optimization.
