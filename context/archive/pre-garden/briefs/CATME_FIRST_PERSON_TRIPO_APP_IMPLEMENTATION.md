# CatMe: first-person polish, Tripo cat creation, and testable mobile app

Status: implementation handoff, 2026-09-24. Execute the phases in order, saving progress after each one. The owner will test the app later; engineering must still verify the build on a physical phone before calling it ready.

## Goal and product bar

Make a portrait mobile experience in which the player feels present in the same room as a recognizable cat. The player can walk, look, zoom, call the cat toward their current position, pour food, and throw a ball while turning to follow the chase. The result should be pleasant to look at, readable with sound off, and usable with one hand for its main actions. A selected cat photo can become a saved, rigged, playable cat through **Tripo AI as the only generation provider**. The current localhost server is a development tool, not the app's permanent service.

Preserve the working third-person mode as a development comparison until phone and owner testing is done. Preserve the known-good local cat fixture. Do not generate cats to test routine gameplay changes. Do not commit generated GLBs, API keys, certificates, device identifiers, or Unity caches. This task changes the old Meshy-to-Tripo design by explicit owner direction.

## Start here

1. Read repository `AGENTS.md`, `context/README.md`, `context/STATUS.md`, `context/DECISIONS.md`, `context/PRODUCT.md`, `context/MOBILE.md`, `context/GAMEPLAY.md`, `context/ASSET_PIPELINE.md`, and this file. Open `docs/research/catme/FIRST_PERSON_CAMERA_PROTOTYPE.md`, `docs/research/catme/EXPERIENCE.md`, and the relevant parts of `docs/CATME_MOBILE_GAME_IMPLEMENTATION.md` for the cross-system design. Read only directly affected scripts and scenes after that.
2. Inspect `HomeRoom`, the first-person camera, cat loader and animation controller, call, feeding, ball, petting, sleep, photo picker, generation panel, local save, and iOS build script. Record a short verified baseline: what runs in the Editor, what is only code, what has been exercised on a phone, and what is missing.
3. Inspect the generation implementation in sibling `../clonecatme/server/tripo.js`, `server/catGeneration.js`, and their direct imports. Reuse bounded transfer, task polling, and error handling where useful. Treat the old Meshy route and Unity localhost URL as legacy. Respect that sibling's unrelated game and web flows; isolate CatMe changes.
4. Recheck current official Tripo documentation before implementation. On 2026-09-24, the current developer docs document v3 `POST /generation/image-to-model`, `POST /animations/rig-check`, `POST /animations/rig`, and `POST /animations/retarget`. Use `v3.1-20260211` for the image-model request and quadruped rig model `v2.5-20260210`; retarget only `preset:quadruped:walk`. The code now uses these v3 routes in the isolated development-only CatMe server module. The API payload is linted and the local route smoke-tested, but no Tripo account call has verified its access or credits. Do not mix v2 and v3 payloads. Do not assume a cat idle, eat, pounce, greet, or sleep preset exists. Pin costs and likeness claims only after one owner-selected photo passes CatMe import and animation review.
5. Create `docs/implementation/catme/INDEX.md` with a phase checklist, evidence links, decisions, blockers, and exact next action. Update it and `context/STATUS.md` at each phase boundary. Use `context/DECISIONS.md` only for durable product or architecture decisions.

## Phase 1 — Define and capture the target experience

Build one vertical slice specification before adding new systems. Write a 60–90 second player journey: room opens with cat visible; player walks near it; calls; cat visibly notices, approaches, and greets; player walks to the bowl and pours food; player picks up or presents the ball, throws, watches the cat react, tracks its chase, and sees a clean end to play. State what the player sees, hears, touches, and understands at each beat, including with sound muted.

Capture consistent portrait screenshots or clips of the current room at three positions and three moments: cat approaching, feeding, and ball chase. Record visual defects: scale, clipping, occlusion, conflicting art styles, harsh lighting, weak contact shadows, unclear controls, empty floor, objects covering the cat, and cat material/animation problems. Make a small reference board from the existing research and licensed or owned references; do not copy another game's art. Set an appearance target for the room, cat, props, lighting, and UI before changing them. Keep the cat's face and motion as the visual priority. Simplify or hide flowers and decoration only where comparison images show they obstruct the cat or actions.

**Gate:** A short design note and baseline images identify the exact moments and defects to fix. The same cat, room, and lighting can be compared after each pass.

## Phase 2 — Make walking, looking, and touching comfortable

Keep first-person as the initial view. Tune eye height, turn sensitivity, limited pitch, gentle acceleration and stop, furniture clearance, near-cat stopping distance, field-of-view zoom, and recenter speed on a portrait phone. Never force the camera to shake, snap toward the cat, or rotate during an interaction. Reserve distinct touch ownership for movement, look, pinch, petting, action buttons, food placement, and toy throw. Give each gesture clear start, cancel, and finish behavior. Protect the cat's face and toy from buttons and the movement zone. Keep the third-person switch in development builds for comparison.

Test from the room entrance, beside the sofa, at the bowl, and beside the cat. Fix camera clipping, furniture hiding the cat, awkward near-plane scale, lost cat framing, and zoom limits at those points. `Find cat` should turn the view smoothly and stop at a useful framing, without moving the player. Provide a visible, quiet direction hint when the cat moves off screen; let the player choose whether to turn. Ensure controls remain understandable without audio or haptics.

**Gate:** Editor checks cover the four positions and input ownership. On a physical portrait phone, a new tester can walk to the bowl, locate the cat, call, and track a ball chase without gesture collisions or discomfort. Record device, frame pacing, and observed issues. If phone access is blocked, carry that verification into Phase 7 and continue the independent interaction and art work.

## Phase 3 — Complete the three first-person interactions

### Call and greeting

Use the player's current position and yaw to choose a reachable NavMesh target ahead of the camera. Show an immediate cat acknowledgment before travel: a glance, ear/head turn if the rig supports it, or a small full-body orientation change. Make the approach readable and keep the cat's face clear at arrival. End with a short greeting or settle state, then release control naturally. If the rig has only walk, use restrained orientation, timing, sound, and a believable pause; list the missing clip instead of claiming it is animated.

### Pour food

Let the player physically approach the bowl. Present a food container or hand cue in the lower frame, align it to the bowl, and show visible food entering the bowl. Confirm successful placement with motion and a simple visual cue. The cat notices, walks to a safe feeding point, and appears to eat through an authored close viewpoint, subtle body/head motion if supported, bowl motion or sound, and an explicit end state. Preserve player camera control. Prevent pouring through furniture, from across the room, or while another action owns the input.

### Throw and chase

Make the ball clearly visible in the player's hand or lower frame. Use a deliberate hold/drag/release gesture with a simple landing indicator or arc hint; allow cancellation. Throw from the player's current position toward a valid floor area, using room collision and bounded force. The cat should notice, orient, wait briefly, then chase with believable pace changes, reach the ball, bat/catch/miss with a readable reaction, and recover. Limit repetition; introduce small, purposeful variation. The cat must not snap to the ball, pass through furniture, or disappear without direction feedback. Reuse the current ball system where it works; change its timing and presentation as needed.

Inspect petting and sleep from the close view so they do not unexpectedly seize the camera, misread touch, or clip through props. Implement only the fixes necessary for a complete first-session loop. Save the player's cat and progress across app restart.

**Gate:** The known-good local cat completes call, food, and ball flows from at least two player positions each, including interruption, cancellation, repeat use, and sound-off checks. Capture before/after video. Run focused Unity compile and relevant interaction validators after each C# change.

## Phase 4 — Polish the room and cat presentation

Unify the authored room around a warm, believable interior. Correct furniture scale, material roughness, floor texture repetition, lighting temperature, contact shadows, window exposure, and prop placement. Keep a navigable central play area and clear silhouettes around the bowl, ball, cat bed, and cat approach. At close range, inspect the cat's fur material, face, eyes, paws, foot contact, yaw correction, movement speed, and shadow. Prioritize coat recognition and readable expression over expensive surface detail. Avoid mixing obviously low-poly furniture with a realistic cat unless the transition is intentional and visually coherent.

Replace temporary debug-looking controls with a small, consistent portrait UI: visible primary action, contextual prompts, restrained typography, clear loading/error states, safe-area layout, and accessible tap sizes. Use short tactile/audio accents and visual confirmation; do not cover the cat with persistent meters or store controls. Compare screenshots and video at the same phone resolution and positions used in Phase 1.

**Gate:** No obvious geometry intersection, missing texture, blown-out light, hidden cat face, unexplained action state, or placeholder debug UI appears in the vertical slice. The room holds up both when the cat is near and across the floor. Record any deliberate art compromises.

## Phase 5 — Replace the mixed provider path with Tripo only

Keep one private CatMe generation service with a stable HTTPS staging URL, durable job records, private asset storage, and Tripo credentials in server environment variables. “One API” means one **model provider**; the app still needs a small backend so its Tripo key and paid calls cannot be extracted from the mobile binary. Do not depend on `127.0.0.1` on a user's phone. Do not put a provider key, temporary upload credential, or provider task ID in Unity player settings or logs.

Implement the server flow as distinct, resumable states:

```text
photo selected → local preview and consent → upload to CatMe service
→ Tripo image-to-model → preview/quality check
→ Tripo prereg check → quadruped rig → quadruped walk retarget
→ validate and package GLB → durable manifest → Unity download/cache/activate
```

The server owns retries, polling, timeouts, rate limits, budget limits, idempotency, and cleanup. Reuse an existing completed model or task when the same job resumes; a status retry must never buy another generation. Give a stable job ID per user attempt. Record Tripo model/task versions and consumed credits server-side. Download Tripo's output promptly because provider result URLs can expire; serve app downloads from CatMe-controlled storage. Keep the original output and a separate optimized mobile GLB. Verify file type, size, checksum, bounds, texture, materials, skin, joints, clip, scale, forward axis, floor offset, animation mapping, and Unity import before publication. Reject a static or broken rig as playable and keep the previous cat active.

Run a **small measured benchmark** before committing the generation settings. Test one permitted representative cat photo through the cheapest plausible current image model and one quality candidate. Compare face/coat recognition, side and rear shape, riggability, walk deformation, package size, load time, and actual credit cost. Then choose and pin one setting. Use existing fixture and recorded provider responses for all other tests. A real Tripo call is appropriate only for this bounded integration check; record each paid call and result. If credentials or credits are unavailable, finish the code and fixture-backed tests and state the exact external blocker.

The current official schema lists a quadruped walk preset for rig v2.5; extra cat actions are an animation production task. Check whether CatMe's generated skeleton can accept authored quadruped clips or retargeted motions. Make a short clip plan for idle/breathing, notice/head turn, sit, feed, pounce/bat, and settle, with rig compatibility verified in Unity. Use carefully authored procedural motion only where it does not distort the cat. Do not represent a generic walking clip as those actions.

**Gate:** One photo can travel through Tripo-only generation, rigging, walk, validation, storage, manifest, download, and in-room activation; a failed or rejected model leaves the previous cat playable. Log measured quality, latency, and credits. The Unity binary contains no provider secret.

## Phase 6 — Make photo creation usable in the app

Replace the development `CatGenerationPanel` flow with a customer-facing creation screen. The player can first play with the starter cat. From the creation screen they can choose a system photo, crop or reject it, see a plain explanation of upload/processing and retention, name the cat, start creation, leave the screen, resume later, inspect a preview, accept the playable cat, retry a failed attempt, or delete a photo/model. Show honest states: uploading, creating model, rigging, preparing for play, ready, and failed. Never claim a cat is ready on the basis of an image preview alone.

Use a small versioned API contract, for example `POST /cats`, `GET /cats/{id}/jobs/{jobId}`, `GET /cats/{id}/versions`, `GET /cats/{id}/versions/{version}/manifest`, and delete endpoints. Decide authentication or anonymous ownership before exposing private photos: an anonymous install token may be enough for the first private prototype, but it must be unguessable and safely restorable or the limits must be shown. Sign or authorize downloads and make ownership checks on every endpoint. Include manifest version, content URLs, SHA-256, byte size, axes, scale, floor offset, clips, and validation flags. Use atomic cache writes, integrity checks, rollback to the last playable cat, and clear behavior when offline. Test app termination mid-upload, mid-job, mid-download, and mid-activation.

**Gate:** The flow passes fixture-backed integration checks; a real phone can select a photo, upload over HTTPS, resume after closing the app, download and activate a valid cat, restart offline with that cat, recover from a failed job, and delete its private data. Carry any unavailable phone check into Phase 7. Do not claim backup across devices until it works.

## Phase 7 — Package a testable mobile app

Prepare iOS first with the existing Unity project and `HomeRoom` entry scene. Remove development server URL input from player builds, configure staging endpoint through build configuration, package the starter fixture legally and efficiently, set icon/splash/name, check portrait safe areas, native photo permission text, HTTPS behavior, persistent cache paths, crash logging, and build/version identifiers. Export, sign, install, launch, and use the full vertical slice on a physical iPhone. Check first launch, second launch, offline starter play, photo flow, generated cat swap, memory use, sustained frame pacing, load time, audio, thermal behavior, and background/resume. Treat Editor compile, Xcode build, signed install, app launch, and real play as separate results.

Produce an internal installable artifact and a concise test guide with build number, device/OS, server environment, known limits, and reproduction steps. Once iOS passes, produce an Android internal build and repeat touch, permissions, downloads, performance, and resume checks on an Android phone. Prepare App Store/Play Store release items only after the internal app and privacy behavior are real; do not describe an internal build as published.

**Gate:** The owner can open an internal app build later and complete starter play and photo-to-cat creation on a phone. If signing, an account, device trust, or staging hosting blocks the install, report the exact step and artifact that passed; continue all independent work.

## Required evidence and handoff

- Keep `docs/implementation/catme/INDEX.md` current. Each phase entry includes done/open/blocked, exact files changed, verification performed, evidence artifact, and next action.
- Save same-angle portrait before/after clips or screenshots, one phone capture of the full first-session loop, and one sanitized generation trace with provider task states, actual credits, model validation, and app activation. Keep private photos and GLBs out of Git.
- Use focused compile and validators during development, then a full device smoke pass and build checks at packaging. Record physical device versus Editor evidence clearly.
- Reconcile obsolete Meshy-to-Tripo text in context and implementation reference once the new contract is implemented. Preserve historical research and test comparisons.
- At the end, report what the owner can test now, how to install it, what actually works, remaining limitations, observed performance and provider cost, and the next product decision. Do not stop after a plan, an Editor screenshot, or an API task response.

## Current verified context and official sources

- Current Unity prototype: `docs/research/catme/FIRST_PERSON_CAMERA_PROTOTYPE.md`. Walking and dynamic call were verified in the Editor; first-person food, moving ball tracking, and phone use were not.
- Current Unity creation UI: `Assets/CatMe/Scripts/UI/CatGenerationPanel.cs`. It remains development-only and defaults to localhost; it now uses `/api/catme-generation`. On phone, configure the Mac's reachable LAN URL.
- The local development route is `../clonecatme/server/catmeGeneration.js`; it uploads the normalized photo directly to Tripo and polls model → rig-check → quadruped rig → walk retarget. It is not the production authenticated HTTPS service. No provider call was made.
- Current local fixture: `LocalFixtures/Cats/master-walking-cat.glb`, with one walk clip. Reuse it for ordinary gameplay work.
- [Tripo v3 image-to-model](https://developers.tripo3d.ai/en/docs/generation-image-to-model/standard)
- [Tripo v3 file upload](https://developers.tripo3d.ai/en/docs/files)
- [Tripo rig-check](https://developers.tripo3d.ai/en/docs/animations-rig-check)
- [Tripo quadruped rig](https://developers.tripo3d.ai/en/docs/animations-rig)
- [Tripo animation retarget](https://developers.tripo3d.ai/en/docs/animations-retarget)
- [Tripo Quick Start and server-side key guidance](https://developers.tripo3d.ai/en/docs/quick-start)

Provider capabilities and pricing can change. Verify these sources at execution time, and label every unavailable capability as an open production requirement rather than implying that the API supplies it.
