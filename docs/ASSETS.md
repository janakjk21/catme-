# Art and models

## Where things are

| Kind | Where |
|---|---|
| WornTag screens art (example photos, hero, reveal art) | `Assets/CatMe/Resources/Onboarding/Generated/` (`WelcomeGardenHero`, `GoodCatExample`, `GoodDogExample`, `PersonPhotoExample`, `CatReveal`, `DogReveal`, `WelcomeDogHero`, `PlaceholderCat`) |
| Fonts | `Assets/CatMe/Resources/Fonts/` (Fraunces, DM Sans) |
| Starter 3D models | `Assets/StreamingAssets/CompanionGarden/` (`cat-walking.glb`, `human-walking.glb`, `human-call.glb`) |
| Garden map and props | `Scenes/Places/Place_HomeGarden_r2.unity`, `Art/Places`, `Art/Garden`, `Resources/CompanionGarden/` |
| House and tent rooms | `Resources/Rooms/HouseRoom_r3.prefab` |
| Supplied cat house and dog bowl | `Assets/CatMe/Art/Rooms/cat_house.glb`, `dog_bowl.glb`; rotated from Z-up and grounded by `Editor/HouseRoomPersonalization` |
| Built-in dog | `Resources/Companions/Dog_corgi.prefab` (Bublisher Dogs Kit) |
| Local test models (not in git) | `LocalFixtures/` |

## Third-party packs in use

Ghibli-style nature set, ALP assets, Anime Trees, Bublisher (Dogs Kit), Butterfly (Animated), JTS Shrine Torii Gates, PolyOne water, TileableBricksWall, WoodenParkBench, Waldemarst, ithappy Animals, Low-Poly Objects Pack, LowPolyLivingRoomPack, Furniture_ges1, Persian Carpet, Rolling Balls, Fristy Mobile CG, VRChat Home Kit, Free.

Before release, check each pack's licence allows use in a paid mobile app, and remove packs the map no longer uses (their demo scenes are not in the build).

## Rules for models

- Format GLB, loaded at runtime with glTFast. Every character is scaled to a shared height: person 1.70 m, cat 0.46 m (`CompanionScaleStandards`).
- A walking character needs a skin and a walk animation. The person also needs a call pose for the Call action (the bundled `human-call.glb`).
- Size: aim under 25 MB per model, 100k triangles for people, 30k faces for pets (the server asks providers for these).
- Generated models never go in git.

## Rules for screen art

- Real-feeling photos for examples; painterly garden scenes for hero and reveal art, warm cream, sage, pink and old-brass palette.
- Import as Sprite, max size 2048; keep the subject so `CoverFrame` focus can keep it in frame.
