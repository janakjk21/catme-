# Asset licence audit

Status: **not cleared for commercial release**. Folder names and presence in the project are not proof of a licence. The owner must attach purchase/download receipts and the licence terms applicable at acquisition before submission. Archived assets are in `../catme-asset-archive/2026-10-04/` and may still need records if they appear in Git history or marketing captures.

| Asset folder | Use in app | Source / licence evidence | Release decision |
|---|---|---|---|
| `3D set of stylized nature - GHIBLI style` | garden dressing | owner receipt and terms needed; avoid brand reference in listing | pending |
| `AI cartoon butterflies` | animated butterflies | owner receipt and terms needed | pending |
| `ALP_Assets` | house | owner receipt and terms needed | pending |
| `Anime Trees` | foliage | owner receipt and terms needed | pending |
| `Bublisher` | dog visitor/models | owner receipt and terms needed | pending |
| `Free` | stones | source and terms needed | pending |
| `JTS_Shrine_Torii_Gates` | torii gate | owner receipt and terms needed | pending |
| `LowPolyLivingRoomPack` | room props | owner receipt and terms needed | pending; candidate replacement furniture |
| `PolyOne` | water material | owner receipt and terms needed | pending |
| `Rain Particles` | weather | owner receipt and terms needed | pending |
| `Rolling_Balls-Sci-fi_Pack` | ball | owner receipt and terms needed | pending |
| `TileableBricksWall` | wall material | source and terms needed | pending |
| `VRChat/HomeKit` | room geometry and furniture | **highest risk:** confirm use outside VRChat is allowed | **block release until cleared or replaced** |
| `Waldemarst` | Japanese-garden art | owner receipt and terms needed | pending |
| `WoodenParkBench` | benches | source and terms needed | pending |
| `ithappy` | wildlife | source and terms needed | pending |
| `Firebase`, `ExternalDependencyManager`, `Plugins` | SDK/native integrations | verify redistributable SDK versions and bundled notices | pending |
| `Resources`, `StreamingAssets` | mixed app and bundled models | audit every third-party file, model provenance and generator terms | pending |
| `CatMe` | project-authored art, audio and code | retain creator records and Stormwave audio purchase/licence | pending |

For each pack record: original store URL, publisher, acquisition date, order/licence ID, licence text/version, whether redistribution inside an iOS game and marketing screenshots is allowed, attribution requirements, and whether AI training/generation restrictions apply. For `VRChat/HomeKit`, either obtain terms that explicitly cover this game or replace its referenced room geometry/furniture through `HouseRoomPersonalization` and rebuild. Do not remove the pack until the rebuilt room and a phone build show no missing materials or meshes.
