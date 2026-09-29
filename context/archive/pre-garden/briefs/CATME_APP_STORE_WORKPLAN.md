# CatMe: complete product and App Store workplan for Luna

Prepared 2026-09-27. Status: planning document, not an implementation or release approval. Recommended product choices below are proposals unless already established by the user. Values are initial tuning targets, not measured results.

## 1. The product to deliver

A small, warm, landscape home containing a cat the player cares about. The player calls, touches, plays with, feeds, and rests with the cat. The cat responds consistently, sometimes initiates contact, develops observable preferences, and leaves the player with a small memory worth returning to. A free Studio Cat lets anyone experience the game immediately. The full intended product lets a player create a recognizable companion from a cat photograph, including an older photograph, without asking whether the pet is alive or deceased.

The current room composition is accepted. Preserve the sofa, plants, window, open floor, rug, house, and tower. Improve coherent presentation and interaction within it. Do not restart the room or introduce a moving human avatar.

The fixed camera/touch/ball specification is `context/tasks/CATME_FIXED_ROOM_PLAY.md`. Implement it first. That document's fixed landscape angle and bounded pinch supersede old portrait, orbit, first-person, seated, and automatic activity-camera requirements.

The newer material/physics reference is `/Users/janaksapkota/Downloads/worntag_repo_patch/website/assets/hero-cat.webp`. Follow `context/tasks/CATME_TACTILE_ART_AND_PHYSICS.md` during phases 1–5 for woven toys, wood/fabric surface response, natural cat contact and movement, warm lighting, and app-wide styling. Its warm natural materials supersede the uniform orange/cardboard treatment while retaining the current room layout and fixed camera. Do not duplicate these tasks or run whole-room builders.

Player experience is the priority order: trustworthy touch → believable cat → satisfying play → gentle return loop → reliable personalization → reliable purchases → release presentation.

## 2. Execution contract for Luna

Repository: `/Users/janaksapkota/Downloads/appliction new idea/catme/catme-home-mobile`.

At each session:

1. Read AGENTS.md, context/README.md, STATUS.md, this plan's current phase, and only the relevant routed context modules. Read the smaller fixed-room brief during phase 1. Read the sibling generation repository only during its API/generation phase.
2. Inspect Git state, open Unity state, and actual relevant code. The repository has extensive uncommitted work; do not clean, reset, replace, or rebuild the whole scene. Save a recoverable scene/material/settings copy outside Assets before any scene-authoring edit.
3. Work on one bounded phase. Extend existing systems rather than creating a second navigation, touch, save, or cat controller. `CatMotor` owns cat navigation; the ball Rigidbody owns ball physics. Asset loading preserves axes, scale, skin, materials, and root-motion isolation.
4. Compile after C# changes. Run the checks appropriate to the changed behavior. Record Editor, build, installation, and physical-device results separately. Do not invent visual observations from a successful command.
5. Maintain `docs/release/IMPLEMENTATION_STATUS.md` with phase states: not started, in progress, blocked, implemented, device verified. Each row names changed files, evidence paths, remaining defect, and next action. Create it when implementation begins. Keep STATUS.md short and current; record accepted durable decisions in DECISIONS.md.
6. Keep source local until the user requests commit/push. Asset purchase, paid provider runs, service deployment, TestFlight upload, and App Store submission require the applicable explicit authorization. Prepare the concrete changes, cost estimate, and evidence before requesting such actions. A planning request does not authorize them.
7. If blocked on a rig, provider, account, or native build, record the exact unmet requirement and attempted fixes. Continue independent approved work. Do not hide the problem or call the phase complete.

Current verified foundation: the app was built, installed, and launched on an iPhone; this does not establish complete gameplay validation. Current source includes activity pacing, ball/laser, Call/petting, feed, energy/sleep, local saves, memories/photos, an ambient selector, and a trait/discovery journal. Those are systems to improve, not a reason to reimplement everything. Journal traits currently have neutral defaults; a full learned personality and bond loop is not verified. The standard saved state currently records energy/state/time, so new persistent fields need explicit migration.

## 3. What to keep, change, add, and remove

| Decision | Exact direction |
|---|---|
| Keep | One room, one active cat, warm tactile home aesthetic with cardboard accents, fixed landscape framing, offline core, existing controls and assets where functional. |
| Change | Cat pose transitions, touch feedback, ball pace/contact, animation timing, Call arrival/facing, HUD consistency, error recovery, player onboarding. |
| Add | A real idle and usable reaction set; cat naming; skippable learning in the room; small preferences/discoveries; a compact bond presentation; useful memories; dependable settings; production photo/job recovery; launch operations. |
| Remove from release | Developer URLs, provider/job IDs, raw trait numbers, debug text/console, development watermark, alternate camera modes, empty/coming-soon pages, duplicate controls, unexplained disabled buttons. |
| Defer | More rooms, simultaneous cats, AR, voice conversations, multiplayer, public social feeds, clothing inventories, competitive scores, daily streaks, ads, subscriptions, paid currency packs. |

Do not add more buttons to create an impression of completeness. Every visible action must have loading, success, cancellation, and failure behavior where applicable.

## 4. The first session and return session

Target a first enjoyable interaction within 30 seconds after the room is ready. Loading targets are specified separately; do not disguise loading as onboarding.

1. Launch into the room with the bundled free cat. A brief loading state says what is happening and offers recovery on failure. No login, notification prompt, or purchase blocks entry.
2. Offer a name with a default suggestion and Skip. Naming can be edited later; validate length and characters for UI layout, never send it to analytics.
3. The cat visibly notices the player using an actually supported reaction. Show one hint: “Tap Call.” After arrival, replace it with “Stroke your cat.” After a valid stroke, offer “Try the ball.” Let users dismiss hints and persist that choice.
4. The first throw should travel into reachable open floor. The cat chases, contacts the ball, and responds. After that, remove tutorial coaching. Do not force sleep, purchases, or generation to finish onboarding.
5. Offer a memory/photo after a successful moment; preserve the moment if the player declines. A discrete My cat/More entry exposes photo creation later.
6. End a session naturally: the player can leave anytime. Save accepted actions before suspension. Never require a daily checklist or a sleep timer to finish playing.

On return, load the same cat/name/settings, restore a safe stable state, and apply capped offline recovery once. Select a contextual greeting, rest, or window moment using current state. Show at most one unobtrusive new discovery/memory. No guilt messages, accumulated unpaid chores, or compulsory pop-up carousel.

## 5. Ordered implementation phases

### Phase 0 — Establish the baseline and release definition

Deliver `docs/release/BASELINE.md`, an evidence folder, and the implementation tracker. Record the current camera pose and screenshot, navigation anchors, cat dimensions/clip inventory, input failure stack, asset licenses, build identity, and performance on the current phone. Capture a 60-second sequence: idle → Call → pet → ball → sleep/wake.

Mark every item observed, source-inspected, or unverified. The screenshot's `Touch was already deallocated` is a real visible defect, but its cause requires a stack. The room reconstruction in the preceding install pass means the current scene must be backed up and reviewed rather than assumed identical to every earlier screenshot.

Choose an initial device support matrix from actual hardware availability. Proposed scope: iPhone landscape first; do not enable iPad-specific distribution without checking its layout and input. OS floor must satisfy current store requirements and the installed Unity version; choose a higher floor if the declared hardware/performance budget requires it.

Gate: recoverable scene baseline, reproducible build, defect list, and no ambiguity about which app/scene is being tested.

### Phase 1 — Fixed view, reliable input, faster ball

Execute `CATME_FIXED_ROOM_PLAY.md` fully. Its initial values include ball chase around 0.99 m/s, notice 0.15–0.25 s, post-bat pause 0.30 s, fixed angle, and bounded pinch. Measure the before/after chase instead of declaring the values inherently natural.

Files: `Scripts/Camera/RoomOrbitCamera.cs`, `Scripts/Cat/CatMotor.cs`, `Scripts/Input/`, `Scripts/Toys/`, `Scripts/UI/CatHomeHud.cs`, and the direct camera callers in existing activities.

Gate: fixed camera through every activity; no stale touch exceptions; no shared gesture actions; two repeated three-bat loops plus ten varied throws; visible contact and no stuck locks on phone. Keep Call, feed, sleep, and other movement speeds stable while tuning ball.

### Phase 2 — Make the cat believable while stationary and moving

This is the largest experience gap. A better HUD will not compensate for a cat repeatedly stopping in a walking pose.

1. Audit both the free cat and currently supported generated-cat rig. Write `docs/release/CAT_ASSET_ACCEPTANCE.md`: source/license, triangles, textures, skeleton, clips, root behavior, axes, normalized size, memory, and phone screenshots. Do not call an asset commercially usable without its license.
2. Define the minimum launch motion set: stationary idle/breathing, walk, smooth start/stop, a supported attention/pet reaction, and a rest representation. For the full-quality free cat, prefer sit/stand and a short paw interaction as well. Treat a run clip as optional; cap brisk walking to a believable range until one exists.
3. Blend the walk into a real stationary pose over an initial 0.15–0.25 s. Synchronize walk rate to measured horizontal velocity. Root translation comes only from CatMotor. Retarget only to a verified compatible quadruped rig. Do not assume humanoid retargeting works.
4. Add head/ear/tail attention only where those rig controls are identified and bounded. Never deform arbitrary bones or bounce/scale the whole mesh to pretend it is breathing. If a generated rig cannot support these controls, it must still meet the core idle/walk/reaction quality gate through a verified compatible asset path.
5. Keep turns believable: turn before accelerating into sharp reversals, slow at corners, stop at destination without skate-like sliding. Use the existing path logic; do not add Rigidbody forces to the cat root.
6. For paid photo cats, test the same quality standard on several different coats/body shapes. Do not ship a visibly broken personal cat beside a polished free cat.

If required clips do not exist, Luna must produce a precise animator/asset brief with rig mapping, required clips, looping/root rules, license requirements, and cost options. Continue other phases, but mark this gate blocked. No new asset purchase or generation is authorized by this plan. Do not promise scripting alone can supply convincing missing animation.

Gate: 60 seconds of idle and a varied ten-path sequence look deliberate, grounded, and free of snapping/obvious foot slide on phone. Supported cat variants pass the same basic checks. Owner views the recording before this phase is marked visually accepted.

### Phase 3 — Finish the core interaction loop

Interaction rules:

| Action | Player experience and behavior | Completion check |
|---|---|---|
| Call | Immediate UI acknowledgment; short cat reaction; approach foreground; face player; remain available for petting. | Five calls from distinct destinations; no camera change or furniture blockage. |
| Pet | Body hitbox follows the visible cat; stroke produces a supported reaction, gentle purr, brief haptic. | Five valid strokes register; taps/UI/pinch do not; muted feedback is legible. |
| Ball | Clear aim preview; release direction matches swipe; fast pursuit; contact impulse; one reusable toy. | Short/strong/edge throws, Recall, cancellation, and mode switch recover. |
| Laser | Dot only on reachable floor; cat notices and pursues after input; clear catch feedback. | No through-wall targets or exact finger-to-cat dragging; exit always works. |
| Feed | Bowl visibly changes; cat reaches its point; fixed-view occlusion covers unsupported eating motion. | Repeated Feed never stacks routines or creates endless resources/rewards. |
| Sleep/Wake | Clear entry, rest, recovery, and wake feedback; button reflects actual state. | User can leave/reopen while resting; locks and visibility recover correctly. |

Treat one play round as a short satisfying arc, initially about 15–30 seconds rather than a long automated chase. The player can throw again, change toy, or stop. End with calm availability, not an intrusive result screen. Keep the existing three-bat structure as the default until observed play suggests a change.

Low energy should invite quieter interaction, not make the entire game unusable. Petting and observing remain available; basic food and rest never require payment. If an activity is temporarily unavailable, give a concise reason and an available next action.

Add a small preference only when it affects behavior: for example, stronger interest in ball versus laser, longer settling near the window, or a quieter response while resting. Do not infer real temperament from the photo. Use neutral defaults and cautious in-game learning; preferences do not excuse missed input.

Gate: a new player completes Call → pet → ball → feed → sleep/wake without spoken guidance, and can interrupt/recover every interruptible state. Haptics and sound have independent toggles.

### Phase 4 — Relationship, memories, and reasons to return

Extend `CatCompanionJournal`, existing ambient selection, `CatPhotoCapture`, and save data. Do not create a second trait/journal system.

- Present the cat's name and one relationship phrase, such as New companion, Familiar, or Bonded. Keep raw trait numbers and reward arithmetic out of the main room UI.
- Bond increases through completed meaningful interactions; never decreases because of absence. Start with one qualifying reward per activity per 60 seconds and a daily cap, represented in tunable data. Review this against player testing; no fast-tap farming.
- Add three observable discoveries for v1: first voluntary approach, a preferred toy, and a window/rest habit. Store unlocks by stable cat identity. Do not remove an earned discovery when a rolling list fills.
- Give milestone feedback once, unobtrusively. Use already-supported behavior or a memory as the reward; do not promise an animation the rig lacks.
- Keep Energy as the primary explicit need. Represent comfort/mood/hunger gently through behavior and a compact cat detail view if implemented; do not add three more permanent meters or urgent alerts. Existing economy aspirations do not require implementing every meter for launch.
- Capture player photos and occasional genuine in-session candid moments without HUD. Use thumbnails, view/share/delete, bounded storage, and failure handling. Suggested initial cap: 50 memories with a visible management path; do not silently delete favorites. Never fabricate moments supposedly simulated while the app was closed.
- Save an observed favorite and select occasional greeting/ambient choices on return. No daily streak loss, red-dot chores, notification pressure, or paid affection.

Gate: repeated actions cannot farm unlimited progress; restart/update preserves identity and discoveries; photo failures do not block play. After a return visit, the player can point to something personal and explain why it occurred.

### Phase 5 — App presentation and accessibility

Keep current art direction and composition. Build a small shared UI style definition before polishing individual screens: one font family, three text sizes, consistent icon family, warm surfaces, high-contrast text, shared corner radius, 8-unit spacing rhythm, and pressed/selected/disabled/loading states. Use actual device readability to tune those values.

Keep the four main actions. Close the toy tray after selection, show the active tool and one exit, and keep Recall accessible. More contains My cat, Memories, and relevant secondary features. Settings contains sound, haptics, reduced motion, tutorial replay, privacy/support, restore purchases if present, and account/data controls if applicable. Home view resets zoom only. No hidden gesture is the sole route to an essential action.

Use approximately 0.12–0.20 s UI transitions, no blocking decorative delays. Respect reduced motion, iPhone safe areas, a minimum 44 pt equivalent touch area, increased-text layout, readable contrast, and icon plus text labels. All gameplay remains understandable without sound and without color alone. Provide native/accessibility labels and a workable focus order for menus; test VoiceOver and document the actual accessibility scope of the 3D game. Do not claim full accessibility from large buttons alone.

Art cleanup checklist: consistent warm exposure without washing out cat markings, paws contacting floor, no floating props/z-fighting, clear foreground silhouette, no harsh black corner mask, legible rug/toy separation, and minimal sharp shadow flicker. Avoid redrawing the room to chase photorealism.

Audio checklist: quiet ambient bed, distinct soft ball impacts, nonrepeating meows, purr fade in/out, no stacked loops, interruption recovery, and appropriate mute/background behavior. Use licensed assets and maintain an attribution/license inventory.

Gate: no debug/developer controls in the release flow; every visible screen has empty/error/loading states; all controls remain legible in both landscape rotations on the smallest supported phone. Screenshots show the actual shipping renderer and HUD.

### Phase 6 — Save integrity, lifecycle, and performance

Files: `Scripts/Save/CatLocalSave.cs`, `CatSaveData.cs`, `CatCompanionJournal.cs`, cat loader, photo cache, and build settings/direct dependencies.

- Version and migrate saves instead of resetting on any schema mismatch. Write atomically, keep a last-good recovery copy, handle corrupt data and storage failure. Persist accepted rewards and ownership changes, not only app quit.
- Introduce a stable product cat ID. The current journal uses an asset-content hash; re-optimizing/re-downloading the same cat must not erase its relationship. Migrate the old hash identity to the stable ID once with evidence.
- Separate permanent cat metadata/ownership, game state, and re-downloadable cache. Keep custom-cat access through reinstall/restore when selling it. Offline ordinary play works after the cat has downloaded; first launch has the bundled free cat.
- Clamp offline elapsed-time calculations and recovery; handle time-zone/clock changes without large gains/losses. Resume a safe stable activity instead of continuing a stale touch or mid-throw transaction.
- Confirm cold launch, loading cancellation/retry, memory warning, low disk, background during every activity, calls/audio interruptions, photo picker return, and upgrade from the installed save.
- Proposed performance budgets to measure: first room interactive within 5 s on the baseline supported phone; touch acknowledgment within 100 ms; target 60 FPS with a deliberate stable 30 FPS fallback on lower hardware; no unbounded memory/cache growth during a 20-minute loop. Record median and 95th percentile frame times, loading and memory measurements. Tune support claims to evidence.
- Profile render scale, shadows, transparency, texture sizes, animation, model import and garbage allocation before optimizing. Do not globally lower visual quality or upgrade all packages speculatively. Maintain a 10–25 MB target for downloaded cat packages where quality permits, and measure peak import memory separately.

Gate: twenty repeated background/resume sequences and ten cold launches without data loss/crash; successful upgrade migration and forced-corrupt-save recovery; offline core works. Performance report names hardware, OS, build, duration, and actual measurements.

### Phase 7 — Photo-to-cat as a production feature

This phase is necessary to launch with the original photo-companion promise. A local URL field and successful photo normalization tests are not a finished service. Continue using the known-good fixture for gameplay work.

Read ASSET_PIPELINE, SECURITY, and the isolated sibling CatMe generation route when working on this phase. Keep Tripo as the existing selected provider unless the owner changes it. Do not touch unrelated browser/person generation.

Player journey: My cat → Create from photo → single-photo picker → crop/preview and consent → explicit price/creation confirmation if paid → upload → truthful job stages → model review → approved rigging/validation → playable preview → adopt/name → return to same room. Default generation is optional; the free cat remains usable while waiting. Never claim a percentage or finish time that the service cannot support.

1. Create a production API contract and mocked fixtures before connecting live spending. Use CatMe HTTPS endpoints, authenticated ownership, private object storage, expiring downloads, and server credentials only. Release app contains no localhost/LAN URL editor or broad HTTP exception.
2. Preserve the intended preview-before-rigging decision. The current dev route's direct generation flow is not the production contract. Specify which stage costs money, where the user accepts, and how rejection/failure is handled before enabling real requests.
3. Persist jobs durably with idempotency keys and one authoritative job ID. Support queued, uploading, generating, awaiting preview approval, rigging, validating, ready, failed, and cancellation where supported. Resume polling after interruption; retry status/download safely; never blindly retry paid task creation.
4. Preprocess safely: file byte/dimension/type limits, HEIC/JPEG/PNG handling, orientation, metadata stripping, malformed images, picker cancellation, weak network, and expired authorization. Explain retake guidance without promising an exact replica.
5. Validate result manifests, sizes/checksums, parse bounds, textures, rig, clips, pose quality, and ownership. Stage the new cat and activate atomically only after it loads. Preserve the last-good cat on failure. Never substitute the free cat silently for a paid custom result.
6. Treat user photos privately; specify retention and deletion for source images, generated assets, provider copies, and backups. Keep personal names/images/URLs out of telemetry. Use minimum photo access through the system picker. Allow choosing a different photo without needing full-library permission.
7. Produce an owner-approved failure/refund/retry policy and cost model: failed attempts, preview rejection, rigging, storage, download, support, and platform fees. Do not promise unlimited regenerations. Technical duplicate requests never consume another entitlement.
8. Before paid generation, use budget-limited, explicitly authorized real fixtures to measure resemblance, rig success, latency, rejected outputs, and cost per accepted playable cat. A proposed pilot is 10–20 diverse cat photos with permission; report counts and uncertainty, not an invented success rate.

Gate: a real authorized photo completes the whole path on a physical phone, survives app termination/network interruption, loads a validated cat, and preserves it across return. Failed/duplicate jobs and deletion are exercised. If quality or economics fail, keep this feature out of public release and remove its store promise; owner must explicitly choose a narrower Studio Cat release rather than Luna silently redefining the product.

### Phase 8 — Identity, purchase, and recovery

Recommended path: allow free offline play without sign-in; offer Sign in with Apple when durable personal-cat ownership/recovery is needed. Preserve anonymous progress when linking. If identity is unavailable, free play remains available. Account creation introduces deletion and recovery work; do not add login simply to gather emails.

Use StoreKit for the initial cross-storefront digital purchase implementation. Maintain existing policy: one-time Studio Cats, optional earned soft tokens only if they create actual useful unlocks, no subscriptions/ads/paid token packs, free basic care, no paid bond. Prefer launching with very few finished offers over an empty catalog.

Studio Cat purchases are permanent entitlement products. Photo creation needs a separately documented product/entitlement design: distinguish a creation service consumption from durable ownership of the resulting cat. Do not assume consumable purchases automatically restore like non-consumables; the service must recover job/result ownership. The owner selects actual prices after measured generation costs and store setup.

Implement localized StoreKit price display, purchase pending/cancel/failure, verified transactions, server-side ownership for remote assets, idempotent grants, restore/sync, redownload, interrupted checkout, refunds/revocations, and support receipts. Never authorize ownership from a PlayerPrefs boolean or client callback alone. Keep the active cat playable offline according to a documented cache/entitlement policy; avoid erasing a user's companion on a transient validation error.

Gate: sandbox purchase/replay/restore/reinstall/failure cases pass with no double grants or charges triggered by retries. No production purchase tests or product publication without authorization. Unfinished commerce is hidden as a complete feature, not left as a broken button.

### Phase 9 — Observe actual use before launch

Create `docs/release/PLAYTEST_PROTOCOL.md` and `METRICS.md`. Begin with five fresh testers for interaction clarity, then an owner-approved 20–30-person beta across intended devices. These are proposed diagnostic samples, not proof of market demand. Do not recruit or contact people without authorization.

Ask testers to name/call/pet/play without coaching, return the next day if they choose, and explain what felt alive, confusing, repetitive, or worth returning for. Record missed gestures and abandonment locations. Proposed usability gate: at least four of five complete the core loop unassisted. Iterate if they do not; do not add rewards to cover unusable controls.

Track only useful first-party events if analytics is approved: room ready, onboarding step complete, interaction start/end/outcome, session end, return, memory saved, generation stage/outcome, purchase outcome, crash. Common fields: app version, event version, coarse device class, duration/outcome, and deduplication ID. No photo, cat name, raw touches, signed URL, or provider secret. Define consent, retention, deletion and SDK behavior before sending data.

Define denominators: activation = new room-ready players completing Call + pet + one play round; next-day return = eligible new-player cohort returning the following local day; creation conversion = accepted playable cats / confirmed creation attempts. Server/store records are purchase and job truth. Separate crash-free sessions from crash-free users. Use observed rates with counts; do not impose an invented universal retention benchmark.

Gate: material usability defects are resolved and beta evidence supports the stated product promise. If players understand it but do not want to return, revisit the cat/reaction/meaningful-moment loop before spending on more content or acquisition.

### Phase 10 — App Store package and release operations

Create `docs/release/APP_STORE_CHECKLIST.md`, `REVIEW_NOTES.md`, `ASSET_LICENSES.md`, and `OPERATIONS.md` as concrete deliverables. Recheck Apple's live requirements on submission day.

Current official checks (verified 2026-09-27): Apple lists Xcode 26+/iOS 26 SDK requirements, an iOS 13+ deployment target requirement, and updated age-rating questions. SDK version and deployment target are different. Audit the prior native build's iOS 11 target warning across Unity-generated targets. [Apple submission requirements](https://developer.apple.com/news/upcoming-requirements/).

Review needs a complete app, accurate metadata, accessible services, and access to account-dependent functionality. Plan standard in-app purchasing for digital content. [App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/).

Disclose actual data collection, including third-party behavior; audit privacy manifests, required-reason API declarations, and listed SDK/signature obligations against the actual archive. [App privacy details](https://developer.apple.com/app-store/app-privacy-details/), [SDK requirements](https://developer.apple.com/support/third-party-SDK-requirements/).

If accounts exist, provide in-app initiation of account deletion, including automatically created service accounts. [Apple account deletion guidance](https://developer.apple.com/support/offering-account-deletion-in-your-app/). Complete the current questionnaire honestly instead of choosing an age rating based on the cat theme. [Age-rating setup](https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating).

Concrete work:

- Retain `com.catme.home`, archive a non-development distribution build, confirm version/build numbers, signing and provisioning, symbols, and supported devices. A Personal Team phone build is not evidence of a paid Developer Program distribution setup.
- Verify developer enrollment/account agreements, tax/banking only if paid products require them, legal publisher name, territories, and support contact. Owner completes private account access and agreements; never request device passcodes or credentials in chat.
- Prepare icon, real device screenshots, concise description, subtitle/keywords, app preview if useful, working privacy/support pages, and review instructions. Do not use the photoreal reference mockup as if it were gameplay. Only claim photo creation, animations, accessibility, or offline features that ship and were tested.
- Inventory all art/audio/fonts/plugins/generated asset rights, including redistribution in a mobile app. Clear missing licenses before publication. Record provider terms applicable to stored photos/models and commercial use.
- Review photo consent/retention, privacy wording, permission strings, encryption/export questions, age rating, and any target-audience obligations against actual features. No public community/UGC feed in v1; do not add moderation infrastructure for a feature that does not exist.
- Provide an approved review account/demo path if necessary and an authorized bounded route for reviewers to exercise creation without paying personally. Keep the review service live and never add reviewer-only behavior that conceals the real app.
- Prepare internal TestFlight, then an external beta if chosen, and submission only after user authorization. Resolve archive validation and review findings explicitly. Preserve existing installs and saves when moving between build channels where supported.
- Before launch prepare server health/error visibility, job-spend caps, a creation kill switch that leaves offline play working, support procedure, refund/retry handling, incident ownership, storage backup/restore, and service cost alerts. Do not remotely change paid ownership based on an unreliable flag.
- Use an owner-controlled manual initial release after approval. For later updates, consider staged rollout options supported by App Store Connect; do not claim a live binary can be instantly rolled back. Keep a tested fix build/process and stop new generation when its service fails.

Gate: exact release archive, storefront metadata, privacy/rights records, review access, operations runbook, and device evidence are ready together. Submission/public release remains an owner action, not an automatic consequence of finishing code.

## 6. Blocking launch criteria

Do not mark ready while any of these remain:

- Reproducible crash, deallocated-touch error, save loss, duplicate grant/paid job, or inability to restore paid ownership.
- Cat missing, visibly broken rig/materials, walk-pose freezing presented as finished idle, or an unreachable core interaction.
- Uncommanded camera motion, UI covering required targets, gestures triggering multiple actions, or an unexitable activity.
- Private photo exposure, embedded secrets, unauthenticated paid job creation, development endpoint required for customers, or indefinite job/payment state.
- Unsupported claims in screenshots/description, unknown asset rights, missing deletion/recovery flow for shipped accounts, or missing store-required metadata.
- No physical evidence for the declared device range and full customer flow.

Minor cosmetic issues can be explicitly deferred when they do not impair readability, trust, accessibility, or the cat's appearance. Approval by Apple is not proof of customer enjoyment; beta evidence and launch metrics remain separate.

## 7. Scope gates and external decisions

Full intended launch = polished core + private reliable photo companion + reliable ownership/recovery + compliant release operations. Studio-only launch is an owner-selected alternative if generation quality/cost is not ready; it must have honest revised marketing. Do not automatically remove the defining photo promise to mark completion.

Owner decisions are required only where work actually depends on them: minimum target hardware/test devices; animation asset/artist budget; real provider-test budget; production service host/spend/retention; purchase products/prices; free-versus-full launch if generation fails; external tester recruitment; publication. Prepare a concrete recommendation and its costs/evidence when each decision becomes necessary. Continue the independent local phases without repeatedly asking about the entire project.

This plan authorizes no new spending, service deployment, communication to others, or public distribution. It defines the work to make those decisions reviewable.

## 8. Session handoff template

At every stopping point record:

1. Phase and specific result achieved.
2. Files changed and any scene/settings changes.
3. Evidence: compile, observed runtime, performance, phone recording, build/install; explicitly mark missing evidence.
4. Exact unresolved defect or decision, not “needs polish.”
5. Next bounded task and its acceptance check.

Do not repeatedly re-read the whole research programme. Consult its evidence when choosing market/price assumptions, and do not replace missing customer evidence with confidence in this implementation plan.
