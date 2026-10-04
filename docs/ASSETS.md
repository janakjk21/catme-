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

Ghibli-style nature set, ALP assets, Anime Trees, Bublisher (Dogs Kit), AI cartoon butterflies (Mixall; textures 512 on iOS; its demo `CharacterController.cs` removed because it hides Unity's class of the same name), Rain Particles (Game Seed Assets; materials converted to URP by CatMe > Places > Convert Imported Materials To URP), JTS Shrine Torii Gates, PolyOne water, TileableBricksWall, WoodenParkBench, Waldemarst, ithappy Animals, Low-Poly Objects Pack, LowPolyLivingRoomPack, Furniture_ges1, Persian Carpet, Rolling Balls, Fristy Mobile CG, VRChat Home Kit, Free.

Before release, check each pack's licence allows use in a paid mobile app, and remove packs the map no longer uses (their demo scenes are not in the build).

## Rules for models

- Format GLB, loaded at runtime with glTFast. Every character is scaled to a shared height: person 1.70 m, cat 0.46 m (`CompanionScaleStandards`).
- A walking character needs a skin and a walk animation. The person also needs a call pose for the Call action (the bundled `human-call.glb`).
- Size: aim under 25 MB per model, 100k triangles for people, 30k faces for pets (the server asks providers for these).
- Generated models never go in git.

## Rules for screen art

- Real-feeling photos for examples; painterly garden scenes for hero and reveal art, warm cream, sage, pink and old-brass palette.
- Import as Sprite, max size 2048; keep the subject so `CoverFrame` focus can keep it in frame.

## Pet sounds to add (Resources/CatMeAudio)

`Pets/PetSounds.cs` uses these when present; until then generated stand-ins play for the dog. Short (under 2 s), mono, 22-44 kHz WAV or OGG, with a licence for a paid app.

| File | Used for |
|---|---|
| `dog-bark` | saying hello, being called, dropping the ball |
| `dog-bark-excited` | rain, leading the way, seeing a visitor or butterfly |
| `dog-pant` (loopable) | being petted, after dinner |
| `dog-sigh` | settling by the fire or in bed |
| `dog-eat` | eating |
| `cat-eat` | eating (falls back to `cat_mewfood`) |


## World sounds to add (Assets/CatMe/Audio/Resources/CatMeAudio)

Launch task L6. The owner supplies these (licensed for a paid app, mono unless noted, OGG or WAV). Names are what the code loads or will load; files already in the folder are not listed.

| File | Length | Used for | Status |
|---|---|---|---|
| `river-loop` | 20-40 s, seamless loop | the river along the walk; the builder already places a source by the bank (`PlaceSceneBuilder.River`) and `DayDirector` fades it | code ready, clip missing |
| `footstep-grass-1..4` | under 0.4 s each | owner footsteps on the lawn and verges | needs clips, then a footstep player |
| `footstep-gravel-1..4` | under 0.4 s each | owner footsteps on the path | needs clips |
| `footstep-wood-1..4` | under 0.4 s each | the jetty, the house and the tent floor | needs clips |
| `birds-morning` | 30-60 s loop | dawn to late morning (time 0 to 0.3) | needs clips |
| `birds-day` | 30-60 s loop | midday to golden hour (0.3 to 0.6) | needs clips |
| `insects-night` | 30-60 s loop | crickets after sunset (0.7 to 0.95) | needs clips |
| `music-day` | 2-3 min loop, stereo | calm music by day; the Sound setting mutes it | needs clips |
| `music-night` | 2-3 min loop, stereo | calm music at night | needs clips |
| `rain-on-canvas` | 20-40 s loop | rain heard from inside the tent | needs clips |
| `dog-bark`, `dog-bark-excited`, `dog-pant`, `dog-sigh`, `dog-eat` | see above | real dog recordings (the dog plays generated stand-ins today) | needs clips |
