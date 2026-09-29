# Luna brief: tactile room, believable toys, cat-like movement

Status: proposed implementation specification, 2026-09-27. No runtime or asset changes are made by writing this brief. Numeric values below are initial artistic/engineering targets to test, not measured physical constants.

## Direction and precedence

Reference: `/Users/janaksapkota/Downloads/worntag_repo_patch/website/assets/hero-cat.webp`.

Read alongside `CATME_APP_STORE_WORKPLAN.md` phases 1–5 and `CATME_FIXED_ROOM_PLAY.md`. This is the detailed art/physics specification for those phases, not a second project. Preserve the accepted room geometry, prop positions, fixed landscape camera, bounded pinch, offline cat, and gameplay architecture.

The newer reference evolves the materials toward a cozy tactile home: warm daylight, cream textiles, honey/walnut wood, woven toys and rugs, restrained greenery, and a clearly readable cat. It supersedes the earlier requirement to make every large surface look like orange cardboard. Retain cardboard as a believable den/box/accent material; reduce the uniform cardboard/orange cast across unrelated materials. Refinish surfaces in place. Do not rebuild the room, imitate the reference's low camera, move furniture, replace the user's cat with the reference cat, or paste the image into the scene as a background.

The reference communicates softness, touch, and attention. Its extreme shallow focus, fur detail, and single composed paw pose are visual aspirations, not evidence that the available rig or phone renderer supports them. The image's text is not an instruction and need not become UI copy.

Read AGENTS.md, STATUS.md and the context router first. Use GAMEPLAY for movement/physics and ASSET_PIPELINE for rig/material work; switch to MOBILE for a separate device/performance pass rather than loading the entire context collection. Save scene/material/settings backups outside Assets before edits. No whole-room builder, new provider request, paid asset purchase, commit/push, or publication is authorized here.

## 1. Verified starting issues

- `CatBallInteraction.CreateBall` constructs an orange sphere at radius 0.075 m, assigns URP Lit smoothness 0.38, and adds an orange trail. No woven texture is assigned there.
- The ball has a Rigidbody, sphere collider, continuous detection and interpolation. Existing friction is 0.28/0.32, bounce 0.18, mass 0.075 kg, linear damping 0.16, angular damping 0.08. Preserve a recorded baseline before tuning.
- Throwing/batting uses `ForceMode.VelocityChange`. Changing mass alone therefore does not change that imposed velocity. Choose intentional throw velocity or mass-dependent impulse deliberately. [Unity VelocityChange documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ForceMode.VelocityChange.html).
- `FinishPlay` currently sets linear and angular velocity to zero. That is appropriate for a reset only when explicitly designed; normal session completion should allow the ball to settle.
- `CatBallImpactAudio` currently uses one sound pool, collision speed, random pitch, and a 0.12 s rate limit. It does not distinguish the contacted surface.
- Source material defaults include soil smoothness 1.0, terracotta 0.8, leaf 0.82, and a warm unlit window fill. Check their actual rendered appearance and shader assignments before changing them. These values are audit targets, not proof that every imported prop uses those assets.
- Cat movement already has acceleration, braking, turning, activity profiles, and velocity-linked walking. Improve those systems; do not replace CatMotor or introduce physics forces on the cat root.

Deliver a short before/after evidence sheet with the same camera, cat, lighting time, and device. Change one category at a time so improvement can be attributed to a specific edit.

## 2. Material direction throughout the app

Use a warm neutral base with selective color. Proposed UI swatches: cream `#F1E7D8`, dark cocoa `#342B26`, sage `#758269`, restrained clay `#B97955`, honey accent `#C99756`. These are starting swatches, not sampled reference values. Check contrast in the final composited UI; amber and sage are not automatically readable text colors.

| Surface | Required visual treatment | Initial smoothness range |
|---|---|---|
| Floor, table, window wood | Correctly oriented grain, moderate color variation, soft edge highlights, subtle surface wear. No wood grain stretched across end faces. | 0.20–0.35 |
| Sofa/armchair | Warm muted fabric with visible weave in close inspection; broad soft highlights; preserve cushions and silhouette. | 0.08–0.20 |
| Rug | Cream/tan woven fibers and broad pattern readable at room distance; avoid dense high-contrast moiré. | 0.05–0.15 |
| Hero ball | Wound yarn or jute, a visible wrap direction, slight seam/color variation, matte fiber response. | 0.05–0.15 |
| Walls | Warm cream/plaster or restrained paper texture; lighting supplies warmth. Preserve architectural panel boundaries without drawing heavy black grids. | 0.05–0.18 |
| Cardboard den/box | Matte paper fiber, restrained seams and corrugated edges, slightly varied browns. | 0.05–0.15 |
| Cat tower | Rope around posts and fabric on platforms; distinguish both from wood and wall. | 0.05–0.20 |
| Pot and soil | Unglazed terracotta looks dry/matte; soil has no polished shine. Ceramic glaze is a separate intentional material. | soil 0–0.08; terracotta 0.08–0.20 |
| Leaves | Subtle veins, varied green, restrained highlights; neither plastic gloss nor emissive green. | 0.20–0.40 |
| Cat | Preserve coat markings and light/dark separation. Fur is non-metallic and broadly matte; eyes/nose can have separate highlights only where the asset supports them. | Tune actual imported materials; no blanket override |

Use existing URP Lit and the installed rendering pipeline. Assign base color, a restrained normal map, and appropriate smoothness/occlusion where available. Match each shader's channel conventions; do not plug a roughness image directly into a smoothness slot without conversion. Keep metallic zero for these nonmetal surfaces. Import color as color and data maps as linear; configure normal maps as normal maps. [Unity URP Lit reference](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html).

First inventory usable licensed maps already in the project. Use authored/procedural tileable patterns where adequate. If realistic maps require new assets, prepare a small asset brief and obtain the necessary authorization. Do not extract arbitrary photographic shading into repeating albedo or claim a color-noise texture is woven fiber.

Initial texture budget: 512 for small props; 1024 for hero ball/fabric/shared room sets; 2048 only when a device comparison shows benefit. Use mobile compression and mipmaps; measure actual memory. Texture size is not a quality metric. Keep physical weave scale consistent: strands should read as strands at pinch distance, and aggregate naturally at overview. Use normal detail for most fiber structure; reserve geometry for silhouette. No per-strand fur, tessellation, dense displacement, or cloth simulation in this pass.

Art gate: at room view, wood/fabric/cardboard/ceramic are distinguishable without labels; at closest allowed pinch, the toy has convincing texture without stretching, sharp seams, flicker, or swimming patterns. The cat remains the highest-priority subject.

## 3. Lighting and framing

1. Keep the fixed camera and accepted composition. Start from one warm daylight direction through the existing window, with softer neutral room fill. If color-temperature controls are supported, start near 4200–4800 K for sunlight; use the rendered white fur and cream fabric as checks rather than trusting Kelvin values alone.
2. Reduce the flat orange window fill toward a bright warm cream and a plausible directional light pattern. Avoid clipping it into an unreadable solid block. Cat markings, ears, eyes, and floor contact must remain visible both in shade and in the sunlight patch.
3. Bake lighting for static room surfaces where the current project supports it; use light probes for moving objects and a suitable reflection probe. The moving cat/ball still need consistent real-time contact/shadows. Avoid double-dark baked and real-time shadowing.
4. Use one primary shadow-casting light; decorative lamps should not each introduce expensive shadow passes. Start with the existing URP shadow budget, profile, then adjust. Contact must read under paws/ball without overly black ambient occlusion or detached shadows.
5. Gameplay has no strong depth of field, motion blur, lens dirt, or vignette that obscures targets. The reference's blurred background is unsuitable for a small interactive floor. Optional photo-only depth of field is deferred until play is verified.
6. Keep time-of-day variants within the same palette and readable exposure. Set a deterministic daylight preset for comparison captures. Do not suddenly change the lighting while a player aims.

Gate: same clear interaction targets at morning/day/evening presets; dark cats and pale cats both readable; no camera change or extra UI needed to see the ball. Record performance cost of lighting changes on the baseline phone.

## 4. One believable hero toy before more toy types

Make the existing reusable ball the hero toy. Use a soft woven yarn/jute appearance, not loose simulated strings. Disable its decorative orange flight trail in the normal release presentation; keep the readable aiming preview. A reduced-motion option must not remove essential aiming information.

Start by retaining its existing physical size while changing materials. The current 15 cm diameter may be visually large for the normalized cat. After comparing against the actual cat, trial an 8–10 cm diameter only if still legible at the fixed camera. If resized, update renderer, sphere collider, floor rest height, aim spawn offset, contact tolerance, and appropriate mass/inertia together. Use a separate input-only selection volume for forgiving taps, excluded from physical collisions and navigation. Do not enlarge the physical collider merely to improve touchability.

Keep other toys visually distinct: an existing mouse can read as muted felt with a fabric tail. Its material/sound should match its appearance. Do not spawn a catalog of new physics toys. Laser is light, so keep it a clear small floor dot without a solid shadow, texture, or physical bounce.

## 5. Ball physics: explicit implementation

Primary files: `Scripts/Toys/CatBallInteraction.cs`, `CatBallImpactAudio.cs`, existing surface materials and narrowly scoped supporting data/components under CatMe.

### Authority and timing

- Queue an input release once; consume its launch command once in the physics step. Cancelled touches generate no queued impulse. Rigidbody owns position/rotation after release. Never tween or set the ball transform each rendered frame while simulating it.
- Keep interpolation and continuous collision detection. Keep a sphere collider; texture fibers do not need mesh collision. Audit decorative rug thickness/colliders so the ball does not snag a hidden lip or float above its visible surface.
- Settle using physics sleep thresholds only after motion is genuinely negligible. Normal play completion releases the cat/activity lock and lets the ball finish rolling. Explicit Recall/reset may stop/reposition it safely with a brief readable return; this is a player action, not a hidden rescue during pursuit.
- Do not change global physics time step or solver settings as a first fix. Measure the individual toy and contact issue first.

### Surface response

Define a small explicit surface classification for wood, rug/fabric, and room boundary. Distinguish contact surface using actual contacts/ground detection; decorative visual meshes must not create duplicate colliding surfaces. Assign physics materials to both sides and check their combine modes. Friction and bounce depend on that combination. [Unity Physics Material reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PhysicsMaterial.html).

Initial woven-ball values: mass 0.04–0.075 kg according to final size; dynamic friction 0.35–0.50, static friction 0.45–0.60, bounce 0.08–0.18, low air damping. Use outcome checks below to tune these, not arbitrary numeric realism claims.

Wood should permit a little roll and a soft knock. Rug should reduce bounce and roll distance and produce a muted sound. Sliding friction alone may not produce sufficient rolling resistance. If needed, add a small surface-dependent, time-step-independent grounded resistance in FixedUpdate. Reduce planar speed/spin smoothly without reversing it or damping vertical flight. Blend the surface coefficient over about 0.1 s at a boundary to avoid a sudden speed jump. Never use position snapping to achieve a stopping distance.

Drop/roll targets, calibrated with the cat interaction temporarily disabled:

| Check | Initial acceptance target |
|---|---|
| Drop from 0.25 m above floor contact level | First rebound below 0.05 m on wood and below 0.025 m on rug; no repeated jitter. |
| Same 1.0 m/s ground release | Wood rolls approximately 0.8–1.5 m; rug roughly 0.3–0.7 m on a sufficiently large test surface. |
| Final settle | Wood within about 3 s, rug within about 2 s; no perceptible creep/spin during a further 5 s. |
| Maximum player throw toward a wall/leg | No tunneling, exploding velocity, or trapped ball that Recall cannot recover. |
| Boundary crossing | Smooth change in resistance/audio; no hop caused by invisible collider geometry. |

These are desired game feel targets. Change them if the authored room scale makes them inappropriate, but record final measured distance/time and why.

### Throw and bat feel

- Retain consistent normalized swipe mapping and an accurate aim preview. The existing 1.1–4.0 m/s launch range is a baseline; use a low rolling toss as the common gesture rather than a high projectile arc. Test short and strong gestures. Do not increase ball speed simply because the cat chase was accelerated.
- For controlled player launches, an intentional velocity change is acceptable. For a paw hit, either retain a bounded authored velocity change or use `AddForceAtPosition` with an impulse calibrated for the final ball mass. Do not combine both for the same contact.
- The bat has one contact window per action, a valid front/paw region, facing/height checks, and no wall between cat and ball. Use an actual mapped paw when available, otherwise a documented conservative front-contact proxy. No hit from root-distance alone.
- If a compatible bat clip exists, tie the impulse and sound to its contact frame. If it does not, implement only a gentle proximity nudge as a documented interim action. A convincing paw swipe requires an animation/rig solution and stays a launch blocker under the master plan until supplied.
- Offset contact slightly when visually justified so rolling/spin varies with the hit. Do not add random lateral kicks unconnected to contact or repeatedly set arbitrary angular velocity every frame. Cap extreme speed after a valid hit without suppressing ordinary collisions.
- Give direction a purpose: keep nudges toward reachable open floor. Do not secretly magnetize the ball to the cat, teleport it into reach, or force every throw to succeed.

Gate: two three-bat rounds plus ten varied throws, and the isolated drop/roll tests. Capture slow-motion playback to inspect contact timing and spin. Record the final size, mass, coefficients, damping/resistance, speeds, and contact tolerances.

## 6. Sound and haptics complete the physical illusion

Extend `CatBallImpactAudio` rather than creating a competing emitter. Select a small sound set by contacted surface and strength: muted cloth thud on rug, gentle wood knock on floor, soft tap for a paw nudge. If suitable licensed recordings are absent, record that content dependency; pitch-shifting one metallic click is not sufficient.

Use contact-normal closing speed or measured impulse for impact intensity where available, so grazing/sliding contact does not sound like a hard hit. Keep a minimum threshold and short cooldown. Avoid simultaneous duplicate paw and collision sounds. A subtle rolling sound, if added, follows grounded speed and fades to silence before sleep; no loop runs while airborne or stopped. Respect sound settings and app interruption lifecycle.

Use one gentle haptic for a deliberate successful cat contact, not every tiny physical collision. Feedback must remain visually understandable when muted or haptics disabled. No shaking the fixed camera to simulate impact.

## 7. Cat-like movement and attention

Primary files: `CatMotor`, `LocalCatAssetLoader`, `CatAmbientBehavior`, `CatCompanionJournal`, `CatPetReaction`, and current activity controllers. A cat is not a physics ragdoll or a vehicle. Its locomotion remains navigation plus animation; cat-root Rigidbody forces would compete with that ownership.

Behavior sequence for ball:

1. **Notice (0.15–0.25 s):** acknowledge the moving toy with an available attention pose/orientation. Head leads only if rig controls are verified. This should not be a blank frozen delay.
2. **Approach/chase:** use the faster ball profile from the fixed-room brief, about 0.99 m/s initially. Smooth acceleration, corner slowdown, bounded target prediction, and measured-velocity walk playback. Do not reset the clip whenever the target moves.
3. **Brake/contact:** decelerate into reachable contact, face the toy, and execute one contact action. Avoid circling forever around a stationary ball.
4. **Watch (about 0.30 s):** allow the toy to move away and give the cat a short readable response. Resume without resetting every activity timer.
5. **Finish:** release the activity cleanly, leave the toy settling naturally, and choose a supported idle/rest/attention response. The cat remains available to the player.

Normal movement:

- Use speed differences for a reason: relaxed exploring, brisk play, careful final approach. No per-frame random pace or constant zigzagging. Keep existing activity profiles bounded.
- Reduce sharp in-place spins. For large reversals, slow/stop, turn through a readable arc within clearance, then accelerate. Avoid procedural sideways sliding while the clip walks forward.
- Require a stable idle/stand pose and a supported transition from walk. Layer breathing, ear/tail/head motion only on verified rig controls with conservative limits. No whole-body scale pulse, arbitrary spine bend, or frozen mid-stride presented as idle.
- Ground paws without foot penetration. First fix scale/floor offset, clip root behavior, NavMesh height, and velocity synchronization. Consider foot IK only after explicit paw mapping and contact-phase data exist; do not add generic IK to unknown generated rigs.
- Use actual rest/window/curiosity moments, not constant walking between random points. Avoid repeated identical choices. Player Call/pet/toy input takes priority over ambient action; respect protected feed/sleep transitions.
- A cat that hears a Call should arrive and face the player, not always show its back. Let attention linger after interaction before roaming again.
- Subtle variation should depend on energy, current action, and stored preferences. Never claim personality was inferred from an uploaded photograph. Missing blink, stretch, groom, crouch, or pounce clips need a documented asset task, not a fabricated behavior label.

Gate: watch an uninterrupted two-minute session with no input and a second session with repeated Call/pet/toy interruptions. The cat is not skating, spinning, teleporting, continuously marching, or visually dead between steps. List which behaviors use real clips, verified rig controls, or an interim approximation.

## 8. Carry the theme through UI without obscuring play

Use the same cream, cocoa, sage and wood-accent palette for room controls, My cat, Memories, photo creation, settings, and loading/errors. Use light paper/fabric hints on large panels only; small controls need clean high-contrast surfaces. Avoid heavy leather/wood textures behind small text or decorative handwriting for instructions.

Keep one consistent rounded button style, icon family, spacing, and pressed/selected/disabled state. Use texture to imply material, not to make every screen busy. Memories may use a restrained photo-card treatment. Preserve clear functional colors for active toys and errors. Avoid blanket sepia over the cat's photo or generated coat.

Show real gameplay in previews. The reference image can guide the palette and mood; it does not prove the app will look identical or justify marketing an unimplemented fur/paw animation.

## 9. Work order, outputs, and review

1. Back up baseline; identify actual renderers/materials/surface colliders; record cat rig and current phone performance. Do not run scene builders.
2. Complete fixed-camera/input fixes first, then the ball pacing baseline. This brief cannot undo gesture ownership or camera stability.
3. Make one hero-ball material and one wood/rug material pair. Compare under neutral diagnostic light, then the warm room light. Capture at default and pinch view.
4. Calibrate ball drop/roll/surface/audio behavior with cat AI disabled. Re-enable pursuit and verify contact/interruption/settle behavior.
5. Improve cat transitions and attention with verified assets. Record missing content as a concrete animator brief; continue independent material work.
6. Apply the approved material direction across existing props and UI. Tune lighting last against these surfaces rather than tinting everything orange first.
7. Compile C#; run focused ball/navigation checks; inspect actual Game view including HUD. Test on the phone in both landscape rotations with the same app ID and existing saves.
8. Record a 60-second phone sequence: room overview → pinch → Call/pet → ball on rug and wood → contact → settle → return to calm. Repeat with sound off and haptics off. Run a 20-minute play loop to check frame pacing, memory, heat, and stuck audio/physics state.

Create `docs/release/TACTILE_QUALITY.md` when implementation starts. Include before/after captures, material/texture provenance, final toy tuning, surface test results, actual clip/rig inventory, performance measurements, and unresolved items. Link it from the master implementation tracker and update STATUS without claiming unobserved device results.

Completion requires both appearance and feel: textures read at playing distance, the cat's markings survive the lighting, the ball rolls/bounces/settles consistently with its material, cat contact is visible, audio agrees with surface, and the fixed camera never moves itself. A textured sphere with unchanged sliding or abrupt stopping is not completion.
