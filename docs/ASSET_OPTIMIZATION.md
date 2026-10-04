# Asset optimisation plan

Measured on 2026-10-04 at commit `f91638f` (branch `claude/focused-faraday-eemw65`). Re-measure before and after every step with:

```sh
python3 tools/asset_deps.py        # total vs used MB per top-level Assets folder; writes tools/asset_used.txt
```

The script follows Unity GUID references from the two build scenes (`GardenCompanion.unity`, `Places/Place_HomeGarden_r2.unity`), every `Resources` folder and `StreamingAssets`. **It cannot see files loaded by path in code**, so the "loaded by path" list below must be kept even when the script says 0 MB.

## Where the size is

| | Size |
|---|---|
| `Assets/` | 2.7 GB, of which **~950 MB is referenced by the game** |
| Git history (`.git` pack) | 2.86 GB, no Git LFS |
| `Library/` (local cache, not in git) | 5.9 GB |
| `Builds/` (local Xcode exports, not in git) | 13 GB |

### Per pack: total vs used

| Folder | Total MB | Used MB | Status |
|---|---:|---:|---|
| `JTS_Shrine_Torii_Gates` | 494 | 47 | Partly used; builder loads `Prefabs/` by path. Strip the unused textures and prefabs. |
| `ALP_Assets` (country house) | 334 | 294 | Used; oversized textures. |
| `Persian_Carpet_URP` | 328 | 0 | **Unused**, no code references. |
| `Firebase` | 264 | (code) | Needed. Desktop-only binaries (`Plugins/x86_64`, 183 MB) never ship to iPhone. |
| `PolyOne` (Water URP) | 155 | 0 | Builder references only `Water URP/Materials/M_Shader Water.mat`; a 66 MB `T_Skybox.exr`, a 39 MB `Soft Hills.fbx` and hill textures are unused. |
| `Fristy Mobile CG` | 152 | 0 | **Unused**, no code references (includes 50 MB `.psd`/`.png` rock maps). |
| `Waldemarst` (Japanese garden) | 144 | 123 | Used; a 53 MB rock normal map. |
| `3D set of stylized nature - GHIBLI style` | 143 | 57 | Partly used. |
| `Bublisher` | 108 | 107 | Used. |
| `VRChat` | 107 | 89 | Used (HomeKit textures). |
| `CatMe` | 87 | 82 | Our code and art. |
| `StreamingAssets` | 74 | 74 | Bundled cat and person models. |
| `TileableBricksWall` | 71 | 0 | Builder references one material (`Materials/Bricks_01_mat.mat`). |
| `Furniture_ges1` | 56 | 0 | **Unused**, no code references. |
| `Rolling_Balls-Sci-fi_Pack` | 40 | 34 | Used (the ball). One ball does not need 34 MB. |
| `Plugins` | 32 | (code) | Native iOS code; keep. |
| loose `.glb` in `Assets/` root | 46 | 15 | `sofa.glb` 17.8, `lamp.glb` 7.2, `sofa_free.glb` 3.8, `rhyzome_plant.glb` 1.8, `gray_cat_toy.glb` 0.3, `free_empty_room.glb` are unused; `monstera…`, `bonsaitree.glb`, `carpet_free.glb` are used. |
| `AI cartoon butterflies` | 17 | 2 | Partly used (also loaded by code). |
| `Anime Trees` | 16 | 15 | Used. |
| `ithappy` | 13 | 3.5 | Partly used. |
| `Free` | 8 | 0 | Builder references `Free/Stones`. |
| `Low-Poly_Objects_Pack` | 5 | 0 | **Unused**, no code references. |
| `LowPolyLivingRoomPack` | 2 | 0 | Builder and `GardenEnvironmentAuthoring` load prefabs by path. |
| `Rain Particles` | 1 | 0 | Builder and `PlaceWeather` load it by path; it shows 0 only because the map hasn't been rebuilt since it was wired in. |

### Loaded by path (keep, even at 0 MB)
Grep before removing anything: `rg -n "Assets/" Assets/CatMe/Editor Assets/CatMe/Scripts`. As of this commit:
`Assets/JTS_Shrine_Torii_Gates/Prefabs/`, `Assets/PolyOne/Water URP/Materials/M_Shader Water.mat`, `Assets/TileableBricksWall/Materials/Bricks_01_mat.mat`, `Assets/LowPolyLivingRoomPack/Prefabs/`, `Assets/Free/Stones`, `Assets/Rain Particles/` (prefab and materials), `Assets/ALP_Assets/country house01/Prefabs/`, and the `Resources.Load` paths under every `Resources` folder. Keep each of these **plus everything it references** (run the script with these paths added as seeds).

### The 12 biggest textures the game uses
All 142 textures the game uses are capped at 2048 px and **none has an iPhone override**.

| MB | File |
|---:|---|
| 53 | `Waldemarst/FreeJapaneseGarden/Textures/Misc/T_FJG_Rock_001_Normal.png` |
| 31 | `ALP_Assets/country house01/Textures/OldHouseMapWood01.png` |
| 28 | `ALP_Assets/country house01/Textures/OldHouseMapWood02_N.png` |
| 28 | `JTS_Shrine_Torii_Gates/Textures/tex_torii_wood_kuroki_01.psd` |
| 27 | `ALP_Assets/country house01/Textures/OldHouseMapIndoorWall01_N.png` |
| 25 | `ALP_Assets/country house01/Textures/OldHouseMapWood02.png` |
| 24 | `ALP_Assets/country house01/Textures/OldHouseMapIndoorWall01.png` |
| 21 | `Waldemarst/…/T_FJG_Rock_Dark_001_Albedo.png` |
| 18 | `JTS_Shrine_Torii_Gates/Textures/tex_torii_wood_kuroki_01_nrm.psd` |
| 17 | `VRChat/HomeKit/Textures/VRC_Wood_basecolor.png` |
| 17 | `ALP_Assets/country house01/Textures/DoorsMap01.png` |
| 16 | `VRChat/HomeKit/Textures/VRC_Metal_basecolor.png` |

Bundled models: `StreamingAssets/CompanionGarden/human-walking.glb` 25.7 MB and `human-call.glb` 25.9 MB (the same person twice, different clips), the cat 19.6 MB.

---

## Plan

Work on a branch. Do the steps in order, re-run `tools/asset_deps.py` after each, and record before/after numbers in the table at the end. **Never hard-delete:** move removed packs to `../catme-asset-archive/<date>/` (outside the Unity project and outside git) with a `MANIFEST.md` listing what moved and why, so anything can be restored by moving it back (this follows `AGENTS.md`: confirm references, then archive).

### 1. Archive packs the game never uses (~560 MB)
`Persian_Carpet_URP`, `Fristy Mobile CG`, `Furniture_ges1`, `Low-Poly_Objects_Pack`, and the unused root `.glb` files (`sofa.glb`, `lamp.glb`, `sofa_free.glb`, `rhyzome_plant.glb`, `gray_cat_toy.glb`, `free_empty_room.glb`).
- Before moving each: `rg -n "<folder or file name>" Assets/ ProjectSettings/` must find nothing outside the pack itself.
- Move the folder **and** its `.meta` together.

### 2. Strip partly used packs down to what is referenced (~600 MB more)
For `JTS_Shrine_Torii_Gates`, `PolyOne`, `TileableBricksWall`, `3D set of stylized nature - GHIBLI style`, `AI cartoon butterflies`, `ithappy`, `Free`:
- Seed the script with the build scenes, `Resources`, `StreamingAssets` **and** the loaded-by-path list; archive every file in the pack not in the result.
- Replace source `.psd` files that are used with flattened `.png` at the size actually needed (1024 or 2048). Keep the `.psd` in the archive.
- PolyOne: keep `M_Shader Water.mat` and only what it references; archive `T_Skybox.exr` (66 MB), `Soft Hills.fbx` (39 MB) and the Soft Hills textures.

### 3. Texture import settings for iPhone (biggest runtime win)
Add an editor script `Assets/CatMe/Editor/TextureBudget.cs` (menu **CatMe > Optimise > Apply Texture Budget**) that sets an **iPhone override** on every texture, so the rule lives in code rather than in 142 hand edits:

| Kind | iPhone max size | Format |
|---|---:|---|
| Hero surfaces seen up close (house walls, path, torii) | 1024 | ASTC 6x6 |
| Normal maps | 1024 | ASTC 6x6, Normal map type |
| Rocks, trees, foliage atlases | 1024 | ASTC 6x6 (8x8 for distant) |
| Small props, distant scenery, mountains | 512 | ASTC 8x8 |
| UI sprites and icons | as authored | ASTC 4x4, no mipmaps |
| Skyboxes | 2048 (cubemap) | ASTC 6x6 |

Also: Read/Write off, mipmaps on for world textures and off for UI, streaming mipmaps on (Quality settings: texture streaming budget ~256 MB).

### 4. Model import settings
Same editor script, for every `.fbx`/`.obj` the game uses: Read/Write off (unless the code reads the mesh), mesh compression Medium, Optimize Mesh on, no imported cameras or lights, blend shapes off unless used, animation compression Optimal, and strip unused animation clips. Log any single mesh over 50k triangles (the house, trees) for a decision.

### 5. Bundled character models (StreamingAssets, 74 MB)
- **One person file, not two.** Merge `human-walking.glb` and `human-call.glb` into one GLB with both clips (walk, call, sit). The code already looks clips up by name (`Call_Gesture`, `Chair_Sit_Idle_F`).
- Compress the GLBs: `gltfpack -cc -tc` (meshopt geometry plus KTX2 textures) or `gltf-transform optimize --texture-size 1024`. glTFast reads meshopt and KTX2 when the `com.unity.cloud.ktx` and `com.unity.meshopt.decompress` packages are installed. **Install them first** and test that the compressed files load.
- Target: person under 10 MB, cat under 8 MB. Apply the same to downloaded models on the server later (the person budget is 25 MB).

### 6. Audio
Short sound effects: Decompress On Load, ADPCM, mono. Ambience and music: Streaming, Vorbis quality 0.5, stereo only if it matters. Keep sample rate at 44.1 kHz or lower.

### 7. Build and package settings
- Managed stripping level High; IL2CPP; strip engine code on. Check `Packages/manifest.json` for packages the game doesn't use and remove them (confirm with a code search first).
- Firebase: keep only App, Auth, Firestore and Storage (and Crashlytics once added); remove modules no code calls.
- After a build, read the **Build Report** in `Editor.log` (search "Build Report") and list the top 20 assets in the build.

### 8. Stop the repo growing
- Add Git LFS for `*.psd *.png *.jpg *.tga *.exr *.fbx *.glb *.wav *.mp3 *.ogg` (`git lfs track`, commit `.gitattributes`). Applies to new commits only.
- Shrinking the existing 2.86 GB history needs a history rewrite (`git filter-repo`). **That is an owner decision; do not do it.**
- Remove the leftover `tmp_pack_*` and `tmp_obj_*` garbage with `git gc --prune=now` (local only, safe).

### 9. Verify
- `tools/asset_deps.py` before and after; Play Mode in `GardenCompanion.unity` after **CatMe > Rebuild With Last Seed**: house, torii, river, rain, bench, ball, butterflies all present, no magenta (missing-shader) materials, no missing-script warnings.
- iPhone build: app size from App Store Connect / Xcode archive, cold start, memory in Xcode.

### Results (fill in)

| Measure | Before | After |
|---|---:|---:|
| `Assets/` total | 2.7 GB | |
| Referenced by the game | 952 MB | |
| iPhone build (`.ipa`) | not measured | |
| Peak memory on iPhone | not measured | |

### Execution log (2026-10-04)

| Step | Assets before MB | Reachable before MB | Assets after MB | Reachable after MB | Notes |
|---|---:|---:|---:|---:|---|
| 1 | 2705.9 | 952.2 | 2135.1 | 952.2 | Unused packs and loose GLBs archived with `.meta` files. |
| 2 | 2135.1 | 1031.5 | 1375.7 | 985.2 | Path-loaded seeds added; 155 unreachable files archived, 12 used PSDs flattened to PNG with GUIDs retained. Reachable MB fell because the flattened PNGs are smaller. |
| 3 | 1375.7 | 985.2 | 1375.7 | 985.2 | Texture budget editor menu added; import overrides and runtime size await running it in Unity. |
| 4 | 1375.7 | 985.2 | 1375.7 | 985.2 | Model budget editor menu added; import overrides await Unity. Clip stripping deferred until animated models are inspected in Play Mode. |
