# CatMe tactile quality record

Updated: 2026-09-27. Implementation and targets are separated from measured results. The current iPhone was unavailable during this pass.

## Fixed room and shared visual direction

- Preserved the existing HomeRoom scene and room composition. No room builder, scene reconstruction, or whole-room authoring script was run.
- Warmed existing shared materials: broad walls are now softly textured/plaster-colored rather than corrugated cardboard; existing floor remains oak-grain; the rug retains its woven map with a low-sheen flax tint; fabric moves from bright orange to matte warm linen; window/daylight tints move toward cream. Beams/cardboard repair details remain accents.
- HUD base controls use cocoa and terracotta, with sage for Sleep and honey for Feed. The existing safe-area layout and rounded screen mask remain.
- The Editor camera render after import is saved at `docs/release/evidence/editor-home-room-material-preview.png`. It confirms the cream wall and oak floor tint, but the ceiling remains a dark flat band and the weave is still subtle at room distance. The camera-only render omits HUD and does not establish the physical screen experience. Cat-marking visibility at default and pinch views, lighting/shadow quality, frame pacing, and phone readability remain unverified.

## Hero ball source changes

The existing single reusable sphere remains 15 cm in diameter pending an on-device scale comparison beside the cat. Runtime material samples the existing `RoomReference_WovenRug.png` weave via `Resources/CatMeArt/WovenRug.png`; no new texture or purchased/generated model was introduced. Its color is muted yarn/flax, smoothness 0.08, and its aim preview remains while the orange flight trail is disabled.

Initial physics values now in source:

| Property | Value |
|---|---:|
| Ball mass | 0.06 kg |
| Ball friction | dynamic 0.42, static 0.52 |
| Ball bounce | 0.10 |
| Air linear/angular damping | 0.08 / 0.12 |
| Ground linear/angular damping | rug 0.42 / 0.60; wood 0.12 / 0.20 |
| Grounded planar resistance | rug 0.64 m/s²; wood 0.13 m/s²; blended over about 0.1 s |
| Cat ball pace | base 0.68 m/s × 1.45 = about 0.99 m/s |
| Notice / post-bat wait | 0.15–0.25 s / 0.30 s |
| Player throw loft | `clamp(0.10 + speed × 0.045, 0.14, 0.28)` m/s vertical component |

If `WovenOvalRug` is present with a mesh, ball setup adds a static matching mesh collider at runtime and assigns a soft-rug physics material. Spawn/recall positions ray onto the actual floor/rug surface. The scene file is not modified by that setup. Wood/rug distances, bounce heights, settle time, boundary transitions, mesh edge behavior, and collision correctness are **not measured yet**; run the isolated acceptance drops/rolls before calling the tuning complete.

## Cat contact and audio

- The cat remains NavMesh driven by `CatMotor`; the ball remains Rigidbody driven.
- A ball nudge now requires the cat to be within 0.42 m, facing within the configured dot threshold, with the ball low enough. The impulse is still a proximity proxy and has no authored paw motion. It does not satisfy a visual-paw-contact gate.
- The bundled ball toy audio directory currently contains `ball_squeak_01.wav` and `ball_squeak_02.wav`. The implementation attenuates and low-pass filters the existing sound over rug and estimates impact strength from normal closing speed.
- Distinct licensed cloth-thud, wood-knock, and paw-tap recordings are missing. The existing squeaks cannot honestly stand in for those surface recordings; add a properly licensed/recorded set before final audio acceptance.

## Animation/content blockers

The known-good `master-walking-cat.glb` is documented in the current handoff as 19,557,560 bytes with a single selected 2.6 s walk clip (`Armature|Unreal Take|baselayer`); its embedded clip name does not contain `walk`. There is no verified idle/stand, sit/stand transition, attention/head turn, rest, pounce, or paw-swipe clip. Do not label a frozen/default pose as a polished idle. Record actual per-rig clips and commercial license before any asset is accepted. See `CAT_ASSET_ACCEPTANCE.md`.

## Evidence still required

- The saved Editor camera capture is a source-review artifact only; it contains no HUD, visible ball, touch gesture, or phone pixels. The roof remains overly dark and the room-distance weave remains weak in this render, so the visual phase stays open.
- A current Editor/Game view before and after material import, at default and minimum pinch distance.
- Two three-bat sequences and ten varied throw paths on an iPhone, with contact and natural settle visible.
- Isolated 0.25 m drop and 1.0 m/s wood/rug roll measurements against `CATME_TACTILE_ART_AND_PHYSICS.md`.
- One 60-second phone capture of fixed room → pinch → Call/pet → ball over rug and wood → contact → natural settle. Repeat with sound and haptics disabled.
- Full iOS device log and reproduction for `Touch was already deallocated`; do not suppress the exception.
