# WornTag — launch audit, 2026-10-04

Pre-launch review of the iPhone app (`catme-home-mobile`) and the API (`worntag-api`), excluding payments, tokens and the store. Every finding below was checked against the code at these commits, not taken from the docs:

- App: branch `claude/focused-faraday-eemw65` @ `ab058da` (app `main` is still `549ca18`)
- API: `main` @ `ea6a105` (live on Railway), plus branch `claude/focused-faraday-eemw65` @ `459bb4a`

## Update — 2026-10-04, after pulling `d3d7742` (app) and `459bb4a` (API)

| Check | Result |
|---|---|
| App compiles (all cloud commits + consent screen + local cat-size change) | **Pass, 0 compiler errors.** Headless Unity 6000.6.2f1 batch run exited successfully; `Assembly-CSharp.dll` rebuilt with the new code. |
| API tests on `claude/focused-faraday-eemw65` | **Pass, 41/41** (includes the polling-limit fix and the photo check). |
| Play Mode, phone build, merge to `main` | **Not done.** The editor closed during this session, so nothing was played. |
| Consent screen and legal drafts (`d3d7742`) | Built; closes most of item 11, but see the two new bugs in item 11 below. |
| Unity build log | New item 26: 12 Firebase plugin `.meta` files use an outdated importer format. |

The local app checkout is now at `d3d7742` with the cat-size change still uncommitted on top (`DeviceBridge.cs`, `GardenCompanionDemo.cs`).

## Validation of ChatGPT's fixes — 2026-10-04

15 commits, **none pushed**: API `6f56ccc..f0d045a` (4) on `claude/focused-faraday-eemw65`, app `bb25cd5..16703f6` (11) on the same branch name.

**Checks run:** API `npm test` **47/47 pass**. Unity 6000.6.2f1 headless EditMode run: **compiles, 7/7 tests pass**. Nothing was played in Play Mode or on a phone; no provider credits used.

| Item | Commit | Verdict | What's left |
|---|---|---|---|
| 2 Spend limits | `6f56ccc` | **Done, correct.** Guest quota 1 (existing guests downgraded on next session); global daily caps checked inside the same transaction as the account quota, after the duplicate-request check, so retries don't double-count; returns 429 `DAILY_CAP_REACHED`. | (a) A guest who links Google keeps the 1-pet guest quota; nothing raises it to the normal quota. (b) Caps default to 10 pets/day **for all players combined**; set real numbers on Railway. (c) Railway still sets 1,000 for signed-in accounts; owner must lower `CATME_FREE_PETS`/`OWNERS`. (d) App Check still off. |
| 7 Person prompt | `94ff9fe` | **Done in code.** Face, hair, glasses from the selfie; invented plain outfit; 2K textures, 60k polygons; tests pin it. | One real person (Meshy credits) to confirm likeness and size under 25 MB. |
| 10 Photo-check outage | `fe1f0f1` | **Half done.** The server now stores `errorCode` (e.g. `CHECK_UNAVAILABLE`) on the failed pet. | The app has the field (`MobileApi.cs:13`) but **never reads it**; players still see "We couldn't make your pet this time." Map the code to "We couldn't check your photo just now. Try again in a minute." |
| 14 Markings | `f0d045a` | **Done.** Tail, paws/socks, face mask, chest named; 1795 of 1800 characters. | No room left in the prompt for future additions. |
| 3 Restore pets | `bb25cd5` | **Done, with a privacy problem.** Reads the session's pets and owner and downloads missing models. | **Writes into the shared `Profiles/local-guest` folder**, so signing into account B on a phone that had A merges B's pets into A's list (item 31a). Restored pets have no portrait photo. Pets deleted on another phone are never removed. Downloads run one after another before the garden opens, with no progress shown. |
| 4 Resume creation | `c79d932` | **Done.** Resumes the same server job; never uploads or creates again; also picks up a job the server shows in progress. Deletion now clears the right key. | Key renamed to `CatMe.Creation.Pending.v1` with no migration from the old name. The fallback when the server doesn't list the pet hard-codes species `"cat"`, so a dog resumes as a cat. The person model is saved to `Profiles/{uid}/downloads/owner.glb` here but to `DeviceBridge.HumanModelPath` by restore: two locations for one model. |
| 5 Bad model file | `04be2ba` | **Done, correct.** Pet and person load separately; a bad file is deleted, the bundled model loads, and Try again re-downloads. | Play Mode check with a corrupt file. |
| 9 Error messages | `aca36e6` | **Done, with a regression.** Offline, 429 and 5xx now have their own messages. | The app now prefers the server's message, and the server sends **"Creation cannot start yet."** for out of free pets, already making a pet, account deleting and no session (`index.js:204,274`). Before, out-of-pets said so. Either give each code its own server message or map codes in the app. The offline text ("Your pet is saved on this phone") also shows at sign-in and upload, where no pet exists yet. |
| 11 Policy links | `640dbed` | **Links fixed** (`/privacy.html`, `/terms.html` return 200) and added to Account and Settings. | Those pages are still the **pre-launch website notices**; the app policy drafts in `docs/legal/` (placeholders still unfilled) aren't published. |
| 22 Tests | `4ca6cd6` | **Done.** 7 EditMode tests pass headless. | Add tests for the cross-account case once item 31a is fixed. |
| 15 Lookout | `3877224`, `16703f6` | **Code only.** | Not in the game until the map is rebuilt (see below). |
| 16 River | `edbb298` | **Code only.** Continuous mesh with UVs along its length, so flow follows the bends. | Its sound loads `Resources/CatMeAudio/river-loop`, which **doesn't exist**, so the river is still silent. No access point for the pet. Not rebuilt. |
| 17 Mountains | `f355e7b` | **Code only.** | Not rebuilt; needs a look in the editor. |
| 18 Day cycle | `a3132d3` | **Done in code.** Full loop dawn → midday → night → dawn, morning light for misty days, moonlit night. | A new day starts at **dawn (time 0), which is dim**; start at morning (≈0.12) so first impressions are bright. The night sky is built by the builder, so it also needs the rebuild. |

**The world items (15–18) are not in the game yet.** All changes are in `Editor/PlaceSceneBuilder.cs`, which generates the map; `Scenes/Places/Place_HomeGarden_r2.unity` was last changed on 2026-10-03 23:53, before these commits. Run **CatMe > Rebuild With Last Seed** in the editor, check it, and commit the regenerated scene and assets.

**Not addressed yet:** 1 (play, phone, merge), 6 (dogs), 8 (Firebase rules), 12 (Apple), 13 (garden during Making), 19–21, 23–30, 31 (sign-out, account, joystick, camera), and the new menu and account spec in `docs/MENUS_AND_ACCOUNT.md`.

## Verification rerun — 2026-10-04

The requested pull, app compile, API tests and audit update have been repeated against the current local checkouts. This verifies the branch code; the remaining launch findings below are still open.

| Check | Result |
|---|---|
| App pull | **Pass.** `git pull --ff-only catme claude/focused-faraday-eemw65`: already up to date at `d3d7742`. |
| API pull | **Pass.** `git pull --ff-only origin claude/focused-faraday-eemw65`: already up to date at `459bb4a`. |
| App batch compile/import validation | **Pass.** Unity `6000.6.2f1`, `-batchmode -nographics -quit`, process exit **0**; log ends `Exiting batchmode successfully now!`. No C# compiler errors or compiler warnings were found. |
| API tests | **Pass, 41/41 tests across 5 files.** `npm test` exited **0**; no live provider requests were used. |
| Firebase metadata | **Still open.** The fresh Unity log names **31 distinct plugin metadata files** with `PluginImporter` version 1 warnings. The earlier count of 12 was incomplete; see item 26. |
| Local app edits | **Preserved.** `DeviceBridge.cs`, `GardenCompanionDemo.cs`, `Gravel path.mat`, and the untracked `PetSounds.cs.meta` / `CampFire.cs.meta` remain as they were before this rerun. API worktree is clean. |
| Play Mode, physical iPhone, merge and production deployment | **Not performed in this rerun.** Compile and API test results do not close those release checks. |

Fresh Unity log: `/private/tmp/catme-launch-audit-2026-10-04-compile.log` (temporary local evidence).

**How to use this list.** Work top to bottom; P0 blocks TestFlight, P1 blocks the App Store, P2 is quality. Each item names the files, the fix and a "done when" check. Rules that still apply: read `AGENTS.md`, `context/STATUS.md` and `context/CLAUDE_TASKS.md` first; compile after every C# change; no provider credits, Railway deploys, Firebase rule publishing or pushes to `main` without the owner.

---

## P0 — blocks TestFlight

### 1. Most of the latest work has never compiled, and none of it is merged
- App `main` is the original snapshot. The 7 commits on `claude/focused-faraday-eemw65` (selfie camera, photo checks, dog sounds, feeding, day changes, butterflies, rain, tent) were written in cloud sessions with no Unity; `context/STATUS.md` marks each "not compiled".
- API photo check (`459bb4a`, OpenAI moderation + vision) is on a branch only; the live API does not run it.
- The owner's local app checkout is 5 commits behind that branch and holds an uncommitted change: cat height 0.46 → 0.65 m (`Garden/DeviceBridge.cs` `CompanionScaleStandards`, plus clip-rate and collision-radius scaling in `GardenCompanionDemo.cs` around line 434). Carry it onto the up-to-date branch.
- **2026-10-04: compiling is done (0 errors).** Still open: the editor menu steps, Play Mode, the phone build and the merge.
- **Fix:** open the branch in Unity, run the editor menu steps STATUS lists (Convert Imported Materials To URP, Rebuild With Last Seed, Refurnish House Room), play the full flow, then an Xcode build on the phone.
- **Done when:** 0 compile errors and warnings; Login → Garden plays in Play Mode with Try it free; the phone build runs the same; branch merged to `main`.

### 2. Anyone can run up an unlimited Tripo/Meshy bill
- Guests ("Try it free", Firebase anonymous) get the same allowance as signed-in players. STATUS says Railway sets new-account quotas to **1,000 pets and 1,000 people**.
- Every limit is per uid (`mobile/index.js` `userLimiter`, `costlyLimiter`); a new anonymous account starts fresh. App Check is off (`CATME_REQUIRE_APP_CHECK`).
- There is no global daily cap: the app already shows `DAILY_CAP_REACHED` (`MobileApi.cs:114`) but the server never sends it (`CLAUDE_TASKS.md` task 2 not built).
- A real pet now costs about 10 (image) + 30 (model) + rig + walk Tripo credits; a person adds Meshy.
- **Fix:** (a) restore real quotas (`CATME_FREE_PETS=2`, `CATME_FREE_OWNERS=1`) and add `CATME_GUEST_FREE_PETS=1`, checked using `req.mobileProvider === 'anonymous'`; (b) build the daily cap (Firestore counter per UTC day in the same transaction as the quota); (c) turn on App Check with App Attest in the app and `CATME_REQUIRE_APP_CHECK=true`.
- **Done when:** tests cover the guest quota, the daily cap and its midnight-UTC reset; a scripted loop of new anonymous accounts is refused after the cap.

### 3. Signing in does not bring pets back on a new phone
- The login page promises "Signing in saves your pet if you get a new phone." But `CatMeAppFlow.cs:93` calls `api.OpenSessionAsync()` and discards the response; `/v1/session` already returns `pets` and `owner`. `PetRoster` is local-only (`Pets/PetRoster.cs`).
- Result: reinstall or a new phone + Google sign-in → no pets, back to "Cat or dog", and a second paid creation.
- **Fix:** parse the session response; for each server pet missing locally, add it to `PetRoster` and download its model with `GET /v1/pets/:id` (signed `modelUrl`); the same for the owner avatar.
- **Done when:** in Play Mode against a local server, delete the app's data folder, sign in again, and the pet and person return and walk.

### 4. Leaving during "Making" restarts onboarding and pays twice
- `CreationClient` only resumes a pending job if the player re-picks the same photo path (`LoadPending(request.petPhotoPath)`, `CreationClient.cs:47,166-168`). On relaunch `HasOwnPet` is false, so the app goes to "Cat or dog"; the running server job is orphaned and a new one is started.
- Separate bug: account and photo deletion clear `"CatMe.Creation.Pending.v1"` (`CatMeAppFlow.cs:334,351`), but the real key is `"WornTag.Creation.Pending"` (`CreationClient.cs:29`), so a stale pending job survives deletion.
- **Fix:** on launch, if a pending `petId` exists (or the session shows a pet with `status` not `ready`/`failed`/`rejected`), go straight to the Making screen and resume polling. Use one constant for the key.
- **Done when:** in Play Mode, quit during Making, relaunch, and the same job finishes with no second `POST /v1/pets`.

### 5. One bad model file breaks the whole garden, every launch
- `GardenCompanionDemo` wraps all of setup in one `try`; any exception from `LoadCharacter` (truncated download, unsupported GLB) ends with `SetStatus("Garden setup failed: …")` and an empty scene (around lines 153-166). The bad file stays, so it fails again on every launch.
- **Fix:** load each character in its own try/catch; on failure fall back to the bundled cat or person, delete the bad file, show "We couldn't load [Name]. Try again", and re-download from the signed URL.
- **Done when:** with a 10-byte fake GLB in the pet slot, the garden opens with the bundled cat and the retry re-downloads.

### 6. Dogs: every dog is the same corgi, and turning dogs on would waste credits
- The app never downloads a dog's model: `petModelDestination = species == Species.Dog ? null : …` (`CatMeAppFlow.cs:128`); the garden always shows `Resources/Companions/Dog_corgi` (`GardenCompanionDemo.cs:100-111`).
- The server would still pay Tripo for a dog when `GENERATION_DOGS=1` (`worker.js:122`). The pipeline itself works: a sitting Chihuahua photo produced a standing image, a model, and a rig check of "riggable quadruped" on 2026-10-03.
- **Fix:** do `CLAUDE_TASKS.md` task 3 (download, load, scale and drive the dog's own GLB; keep the corgi as fallback). Until then, keep `GENERATION_DOGS` unset and tell dog owners plainly that they get a stand-in.
- **Done when:** a dog with its own model walks and follows in Play Mode; the corgi still appears when no model exists.

### 7. Person generation is still set up for a full-body photo
- The selfie is now a head-and-shoulders crop, but `worntag-api/mobile/ownerPrompt.js:4-37` asks for "the same outfit and footwear", forbids changing clothing, and wants the whole body "to the soles of both feet" from the photo.
- Person models are 43.7 MB (budget under 25 MB): `providers.js:98` sends `texture_resolution: '4k'`, `target_polycount: 100_000`.
- **Fix:** rewrite the prompt for a face-only reference (face, hair, skin tone and glasses from the photo; a simple neutral casual outfit and shoes invented); set `'2k'` and about `60_000`; unit-test the request body. One real check needs owner-approved Meshy credits.
- **Done when:** tests pin the new prompt clauses; one real person is under 25 MB and still recognisable.

### 8. Firebase rules have never been published
- `firebase/firestore.rules` and `firebase/storage.rules` are good (owner-only reads, no client writes to photos or models), but `ROADMAP.md` task 3 says they are not deployed; the live project may still run default rules.
- **Fix:** test with the Firebase emulator (owner can read own tree, cannot read another uid, cannot write `pets` or `photos`), then the owner publishes.
- **Done when:** emulator tests pass and the Firebase console shows the published rules.

### 9. Error messages blame the wrong thing
- `MobileApi.cs:118-124` maps every status other than 400/401/403/409 to "Account service is unavailable. Try again shortly." That covers 429 (rate limit), 5xx, and offline (status 0). Players saw this when status polling hit the old hourly limit (fixed in `ea6a105`).
- **Fix:** 0 → "You're offline. Check your connection."; 429 → "Too many tries. Wait a minute."; 5xx → "Something went wrong on our side. Try again."; read `error.code` first for all known codes.
- **Done when:** a unit test covers each mapping.

### 10. Photo check: deploy it and handle its answers in the app
- `459bb4a` passes all 41 API tests (2026-10-04). After merging it to `catme-ai/main`: set `OPENAI_API_KEY` and `PHOTO_CHECK_REQUIRED=1` on Railway.
- `CreationClient.RejectMessage` (`CreationClient.cs:121`) already handles `unsafe`. But `CHECK_UNAVAILABLE` (checker outage) closes the job as `failed`, which the app shows as the generic "We couldn't make your pet this time." even though the photo was kept for a retry.
- **Fix:** expose the failure code on `GET /v1/pets/:id` and show "We couldn't check your photo just now. Try again in a minute." with Try again reusing the same photo.
- **Done when:** a fake-network outage test shows that line and Try again succeeds without a new upload.

---

## P1 — blocks the App Store

### 11. Legal and privacy: mostly built in `d3d7742`, two bugs left
- **Done in `d3d7742`:** a consent screen before the first photo names Tripo, Meshy and OpenAI, says photos stay private and can be deleted, states 13+, and links to the privacy policy and terms; uploads are refused without it (consent version `2026-10-04`, which the server accepts). Drafts are in `docs/legal/PRIVACY_POLICY.md` and `docs/legal/TERMS_AND_AI_POLICY.md`.
- **Bug: both links are dead.** `CreationClient.cs:30` opens `https://worntag.com/privacy` and `https://worntag.com/terms`; both return **404** today. The live pages are `/privacy.html` and `/terms.html`, and those are still the pre-launch website notices, not the app policy.
- **Bug: the drafts are not publishable yet.** About 12 placeholders remain, for example `[legal name of the owner or company]`, `[postal address]`, `[privacy@worntag.com]`, `[legal entity of Tripo3D]`, `[legal entity of Meshy]`, `[date of publishing]`, and a retention line still undecided (`[or: automatically after 30 days…]`).
- **Fix:** fill in the placeholders (owner, then a legal review); publish both pages on worntag.com at `/privacy` and `/terms` (add the routes to the website's `server.mjs` allow-list), or point the app at the `.html` paths; also link them from Login and Settings, not only the consent screen.
- **Done when:** `curl -I https://worntag.com/privacy` and `/terms` return 200 with the app policy, and the links open from consent, Login and Settings.

### 12. Apple requirements
- Sign in with Apple must be offered because Google is (`RELEASE.md`). It needs the paid Apple Developer Program.
- Account deletion is refused for Apple accounts (`APPLE_REAUTH_REQUIRED`, `index.js:300`); Apple requires in-app deletion for every account type, so build the reauthorisation and token revocation flow.
- The free signing profile expires **2026-10-08** (`STATUS.md`).
- **Done when:** sign in, sign out and delete account all work for an Apple account on the phone.

### 13. The "Making" wait is long, and the app is blocked for all of it
- The Making screen has no exit and the garden opens only when the pet and person have both arrived (`ONBOARDING_FLOW.md`). Real runs take minutes (Tripo image ~40 s, model ~2 min, then rig and walk; Meshy person longer); the client waits up to 30 min (`CreationClient.cs:69`). There are no notifications.
- **Recommended (owner decision, conflicts with the current doc):** after the selfie, open the garden with the bundled stand-in pet and a "[Name] is being made" banner, swap the real models in when ready (the toast design exists), and send a local notification if the app is in the background.
- **Done when:** a player can walk during Making, and a backgrounded app gets "[Name] is ready".

### 14. Identity details get lost in the pet image
- In tests the image step dropped Nala's dark tail (ChatGPT image model) or her white paws (`banana2`). `petPrompts.js` IDENTITY lists "coat colours and every marking" but never names the tail or paws.
- **Fix:** add "tail colour and tip, paw and sock colours, face mask, chest colour" to the identity clause (stay under 1800 characters; the test pins the limit).
- **Done when:** three test photos keep tail and paw colours (owner-approved credits).

---

## P2 — world, gameplay and feel

### 15. The trail ends in an empty clearing with invisible walls
- `Editor/PlaceSceneBuilder.cs` (around lines 214-224): "the end stays an open clearing, closed by invisible walls". There is nothing to walk toward, no reward, no reason to turn back.
- **Fix:** a destination: a lookout over the river and hills with a bench, a small jetty or a picnic spot; a keepsake and a photo spot there; a pet line ("It's nice here." / "Let's head home.") and a soft turn-back.
- **Done when:** the walk has a clear end point a player wants to reach, built through `PlaceSceneBuilder` (one map, per `GAMEPLAY_AND_WORLD.md`).

### 16. The river is flat boxes, and it is silent
- `River()` (`PlaceSceneBuilder.cs:576-588`) is a chain of 4 m boxes 5 cm thick, each turned to follow the path; on bends the boxes overlap or gap.
- `Shaders/River.shader` scrolls along one fixed world direction (`_FlowDirection` default `(0,1)`), so on bends the ripples run sideways.
- The river is fenced off (`BankBlocker`), so a dog, which is "drawn to water", can never reach it.
- There is no water sound in `Resources/CatMeAudio`.
- **Fix:** one spline mesh with UVs along its length and flow following the UV; bank foam, reeds and stones; a looping river sound along the bank; one access point (stepping stones or a jetty) where the pet can drink or paddle.
- **Done when:** no seams on bends, ripples flow downstream everywhere, and the river is audible from the path.

### 17. The mountains are placeholder shapes
- "Far hills" are Unity primitive spheres stretched and given a flat colour (`Hills(string…)`, `PlaceSceneBuilder.cs:1365-1377`).
- **Fix:** low-poly mountain meshes or a painted backdrop layer matching the Ghibli garden, tinted by time of day and faded by fog.
- **Done when:** the skyline reads as distant mountains at all times of day.

### 18. Day and night cover only half a day
- `World/DayDirector.cs`: the cycle is afternoon → sunset → blue hour (`CycleSeconds = 1080`), then about 2.5 min of "night" and a screen-dim jump back to afternoon (`NightHoldSeconds`, line 112). There is no morning, sunrise or midday, though a `Morning` preset exists (`PlaceLighting.cs:50-52`).
- A "MistyMorning" day says "A misty start to the day." in afternoon light.
- Night is the sunset skybox at 10% exposure: no stars, no moon.
- **Fix:** a full loop (dawn → morning → midday → golden hour → sunset → night → dawn) with a real sunrise instead of the fade; a night sky with stars and a moon; path and house lights at night; misty days start in the morning.
- **Done when:** one uninterrupted cycle plays in Play Mode with no screen fade.

### 19. Sound is thin
- `Resources/CatMeAudio` holds about 9 clips: day ambience, morning ambience, wind, light rain, purr, two meows, two ball squeaks. There is no music, no footsteps, no river or water, no birds by time of day, and no night insects.
- Dog sounds are generated stand-ins until recordings exist (`Pets/PetSounds.cs`, `docs/ASSETS.md`).
- **Fix:** calm music for day and night (Sound setting toggles it); footsteps on grass, gravel and wood; river; birds changing with time of day; night crickets; rain on the tent; real dog recordings; more cat variety.
- **Done when:** each listed sound plays in the right place in Play Mode.

### 20. Little reason to come back tomorrow
- Curiosity keepsakes are collected (`Session/CatCuriosity.cs`, saved in `SessionSave`), but I found no screen that shows the collection.
- There is no daily moment or progression. The house rules forbid punishing timers and meters, and that should stay.
- **Fix:** a keepsake shelf in the house; a greeting from the pet on return ("You're back!"); new curiosity spots and small seasonal touches over days; album prompts at golden hour.
- **Done when:** a returning player sees something new and has somewhere to see what they've collected.

### 21. A generated person has only a walk clip
- No idle, wave or call gesture for generated people (`CLAUDE_TASKS.md` task 6). Check how the person looks when standing still.
- **Fix:** Meshy animation endpoint for idle and wave on the existing rig (server plan + tests; credits need the owner).

---

## P2 — release engineering

### 22. No automated tests for the app
- `Assets/CatMe/Tests/` contains only `.gitkeep`. The editor has manual smoke tools (`GardenCompanionSmokeCheck`, `GardenJourneyPlayCheck`, `PlayDriver`) but nothing runs on its own.
- **Fix:** EditMode tests for `PetRoster`, the `CreationClient` state machine with a fake API (resume, reject, fail, timeout), error-message mapping, `CompanionScaleStandards`; one PlayMode smoke test from Login to Garden.

### 23. No crash reports or funnel data
- No Crashlytics, analytics or notifications anywhere in `Assets/CatMe` or `Assets/Plugins`.
- **Fix:** Firebase Crashlytics (Firebase is already in the app), plus a few events: onboarding step reached, creation result and reason, time to ready, first walk finished.

### 24. Performance has never been measured on a phone
- `docs/QUALITY.md` budgets (60 fps on iPhone 12+, under 1.2 GB memory, under 300 MB download, cold start under 5 s) are marked "not measured". Heavy suspects: about 226 flower props plus dressing in the walled garden, rain particles, the foliage shader, 4K person textures.
- **Fix:** measure on iPhone 12-class and 14 Pro Max with the Unity Profiler; record the numbers in `QUALITY.md`; fix what misses.

### 25. Repository hygiene
- Make `main` the real branch in both repos once item 1 is done.
- Delete the stray `master` branch on the API remote: `git push origin --delete master`.
- Decide the final bundle id before the App Store record (`RELEASE.md`; changing it later means a new Firebase iOS app).

### 26. Firebase plugin import settings use an outdated format
- The fresh 2026-10-04 Unity verification log reports **31 distinct files** such as `Assets/Firebase/Plugins/Firebase.App.dll.meta` and `Assets/Firebase/Plugins/iOS/Firebase.Auth.dll.meta`: "contains a PluginImporter object at version 1, below the supported minimum (2). Open and re-save the file to upgrade." The earlier count of 12 was incomplete. Unity 6.6 reads them with a fallback; if the iOS/editor platform flags are read wrong, the iOS build can pick up the wrong Firebase DLLs.
- **Fix:** in the editor, select `Assets/Firebase/Plugins` and reimport (or upgrade the Firebase Unity SDK, which ships current meta files); confirm each DLL's platform checkboxes (iOS DLLs: iOS only; the others: Editor and Standalone) and commit the re-saved `.meta` files.
- **Done when:** the warning is gone from a fresh batch build log and the iOS build signs in to Firebase.

---

## P1 — what players see: screen-by-screen review (iPhone screenshots, 2026-10-04)

Reviewed from 16 screenshots of an iPhone development build. That build is **older than the code**: its Account text ("Sign in with Apple to keep your pets safe") has since changed in `CatMeAppFlow.cs:298`. Re-check each item on a fresh build; most are still in the code.

### 27. Bugs visible on screen
| # | Screen | What's wrong | Fix |
|---|---|---|---|
| a | Garden by the house | **Bright white wedges on the ground** in front of the house, on both sides of the dark path: unlit or untextured geometry, maybe path stones with a missing material or z-fighting. The most visible bug in the game. | Find the objects in `PlaceSceneBuilder` (path stones and slabs near the house entry) and give them the path material; check for overlapping planes. |
| b | Every garden screen | **Red "Touch was already deallocated" errors**, repeated, in the Development Console. An input error firing all the time while you touch the screen. Touch is read in `CompanionCamera.cs:272-275` and `World/CatPetInput.cs:36`; the exact source still needs confirming. | Reproduce in the editor with the device simulator, find the throwing call, and stop reading a touch after it ends. |
| c | Login and the "Making" screen | **The Pause menu opens over onboarding.** Your pets, Store, Photo album and Account can all be reached from the login page and from the wait screen. | Hide the pause and camera buttons until the garden is ready; `SetGameplayControlsVisible` exists (`CatMeAppFlow.cs:498`) but something re-shows them, or the pause button lives outside that group. |
| d | Account | **The title says "You are signed in" to a guest**, under body text saying they are a guest (`OnboardingFlow.cs:410` sets the title unconditionally). The guest's only button is **Sign out**, which deletes a guest's pets for good, with no warning. There is no way to link Google here, and no Delete account. | Two states. Guest: "You're playing as a guest", buttons **Save my pets with Google** (link) and Apple, then a quiet "Sign out and lose these pets" behind a confirm. Signed in: show the provider ("Signed in with Google"), Sign out, Delete account. |
| e | Photo album | **The caption text runs behind the buttons** ("…e threshol…" between Delete photo and Back to garden). There's no next or previous, no Save to Photos, and the saved picture includes the "Development Build" watermark. | Move the caption above the buttons; add left and right arrows or swipe with a "2 of 5" count; add Save; take captures with the HUD and watermark hidden. |
| f | Pet button menu | **"Roam", "Call", "Ball" are white text with no background** over the bright scene; barely readable. | Put the labels inside the round buttons, or on a dark pill. |
| g | "Go into the house" | The action card sits **on top of the player's head** and wraps to two lines ("Go into the / house"). | Anchor contextual cards at a fixed spot above the pet button, one line ("Go inside"). |

### 28. Cards and screens that need a redesign
- **Your pets.**
  - It doesn't show which pet is walking now; every card says "Walk with…". Mark the active one ("Walking now") and disable its button.
  - The 5th card is cut off with no sign you can scroll. Add a scroll hint or smaller cards, and put **Add a pet** as the first card.
  - Pets still being made, or failed, look like normal pets. Show "Being made…" with progress, or "Try again".
  - There's no rename and no remove; add both, behind a long-press or a "…" menu.
  - The card uses the uploaded photo, and **a dog card shows the real dog while the garden shows a corgi** (item 6). Until dogs are real, say so on the card: "Stand-in until [Name] is ready".
  - "You — Shared by every pet" has a generic icon. Show the person's own portrait, and let it open "Retake my selfie".
  - Names aren't checked: one pet is called "Gegsgs". Trim, limit to 20 characters, and block empty names. Keyboard mashing can't be stopped, but the suggested names should be the default.
- **Pause menu.**
  - Five tiles in a 2×3 grid leave one hole, and Settings sits alone. Use a single list, or a 2×2 grid plus Settings in the corner.
  - Store appears twice (pause menu and Your pets). Keep one entry point.
- **Pet store (excluded from this audit by request).** The letter placeholders ("B", "F", "M") read as unfinished. If the store ships at launch, it needs real item art; if not, hide it.
- **Login.**
  - The art (golden sunlight, cherry blossom, torii, a tabby, a girl in a skirt) **promises a different game** from the dusky garden, derelict wooden house and dark path the player lands in. Either repaint the login art from a real in-game capture, or bring the garden closer to it (item 18: daytime, blossom).
  - "Continue with Apple" is shown, but Apple sign-in doesn't work until the paid team exists. Hide it until then.
  - The Apple and Google icons are generic glyphs. Apple and Google both require their official button styles.
  - "Try it free, no sign-in" looks like a label, not a button. Make it a clear tappable link.
- **Cat or dog.** The cat is an illustration and the dog is a photo; use one style for both.
- **Show us your cat.**
  - The tip says "We can see their **whole face**", but the 3D step needs the **whole body**, ideally standing side-on (the image step now redraws pose, but legs and tail it can't see are invented). Change the tips to: "Their whole body, nose to tail", "Lots of light", "Only your pet".
  - The progress dots change colour and size between screens (1st dot black and short, 2nd gold and long). Use one style.
- **Settings.**
  - "Story notes" and "Story linger" are jargon; rename to "Show pet's thoughts" and "Keep text on screen longer".
  - Delete my photos, Delete account, the Privacy policy, Terms and the app version aren't visible. If they're below the fold, nothing shows the list scrolls.

### 29. What the world looks like in play
- **The house looks abandoned and spooky.** Broken windows, dark rotten planks, and a flat grey untextured garden wall in front. It clashes with "your pet, in a garden, with you". Use a warm, lived-in cottage: lit windows, a painted door, flower boxes, a textured stone or hedge wall.
- **The river glows bright cyan at dusk.** The river shader is unlit, so it doesn't darken with the day and looks neon at night (garden screenshots 1 and 2). Tint it from `DayDirector` time of day, or make it lit.
- **A beige cube on a grey post** beside the path (screenshots 1 and 3) reads as a placeholder lamp. Replace it with a real lantern prop that lights at dusk.
- **Flowers.** Three or four flower meshes are repeated hundreds of times, the flat ground flowers float and look like stickers, and tall flowers fill the foreground and hide the cat by the house. Vary species and scale, thin out the path edges, and keep tall plants away from the camera's side of the path.
- **The path** is one flat brown texture with no edging, stones, puddles or wear.
- **The whole garden is dusk or night in every screenshot.** That's the half-day cycle (item 18) showing up: players almost never see the bright garden from the login art.
- **The cat is still hard to find.** It's grey on a brown path in dim light, and hidden in flowers at the house. Item 1's size change (0.46 → 0.65 m) helps; also consider a soft rim light or a small name tag when it's off-screen or behind plants.

### 31. Root causes: sign-out, account, joystick, camera (found 2026-10-04, not fixed)

**a. Sign out does nothing visible, and can't really switch people**
- The button calls only Firebase `auth.SignOut()` and closes the screen: `OnboardingFlow.cs:412` → `CatMeAppFlow.cs:301` → `FirebaseAccountService.cs:126-131`. There's no return to Login, no scene reload, and the garden, pets and models stay loaded. The player lands back in the same garden as if nothing happened.
- **Delete account** does the full reset: it deletes data, signs out and reloads the scene `GardenCompanion` (`CatMeAppFlow.cs` around lines 420-430). Sign out has none of that.
- **Every account shares one local folder:** pets in `Profiles/local-guest/pets-v1.json` (`PetRoster.cs:35`), and the profile in `LocalPlayerProfile.cs:34`. Even a "real" sign-out would show the next person the previous person's pets, pet photos and house frame picture. That's a privacy bug as well as a flow bug.
- For a **guest**, sign-out is irreversible: the anonymous account can never be signed back into, so their server pets are orphaned. No warning is shown.
- **Fix:** keep local data per uid (`Profiles/{uid}/…`, migrating `local-guest` into the first signed-in uid). Sign out = save, sign out, clear in-memory roster and models, reload to Login. A guest gets a confirm ("Your pets will be lost unless you save them with Google first") with **Save with Google** as the main button.

**b. The account section isn't finished**
- The title "You are signed in" is shown to guests (`OnboardingFlow.cs:410`).
- There's no "Save with Google" / link button for a guest. Linking exists in `FirebaseAccountService` but isn't offered here.
- It never says *which* account you're signed in with (Google email or Apple).
- Delete account is a small red text link instead of a proper destructive button with a confirm. Apple accounts are refused server-side (item 12).
- **Fix:** see item 27d for the two-state screen.

**c. The joystick is too low and hard to use**
- **Placed outside the safe area:** `CreateMoveJoystick` (`GardenCompanionDemo.cs:922-960`) anchors a 244-point disc at (172, 170) from the bottom-left of the whole canvas (reference 1920×1080, match height), parented straight to the canvas, not to the safe area. On an iPhone in landscape it sits in the home-indicator area.
- **iOS keeps the home swipe:** `ProjectSettings.asset:96` has `deferSystemGesturesMode: 0`, so swipes starting near the bottom edge go to iOS first and some joystick drags are lost.
- **Fixed, not floating:** it only reacts if the thumb lands on the disc (`alphaHitTestMinimumThreshold`); a miss drags the camera instead. Mobile games normally spawn the stick wherever the thumb touches the left third of the screen.
- **Twitchy:** 78-point travel with a straight-line response and a tiny dead zone (0.08), so small thumb movements jump between stop and full walk. The owner also drops to half speed with no feedback whenever the pet is more than 5 m away (`GardenCompanionDemo.cs:182`).
- **Fix:** a floating stick in the left 40% of the safe area, resting at about 22% from the bottom; defer system gestures on the bottom edge; travel about 110 points with an ease-in curve and a 0.15 dead zone; remove the silent slowdown or show the pet calling.

**d. The camera switches feel random and sudden**
Four separate things move the camera on their own:
1. **Hard-edged zones that stack by entry order.** `SessionDirector.UpdateZones` (lines 190-202) pushes a profile the moment the owner crosses a box edge and pops it on the way out, with no hysteresis. Walking along an edge flips back and forth. Overlapping zones (GardenClose, GateReveal, BeyondGate, Riverside) win by whichever was entered last, not by priority. The profiles differ a lot (`CompanionCamera.cs:23-30`): distance 2.6 → 5.5 m, FOV 48 → 56, and screen offset from −0.3 to +0.2, so the framing slides from one side to the other.
2. **Instant cuts with no fade:** `Snap()` jumps the camera on place swaps (`SessionDirector.cs:271`), entering a room (`Interactions.cs:580`), placing the pair at a door (`Interactions.cs:318,374` via `PlacePair` → `GardenCompanionDemo.cs:748`) and leaving the bench or tent (`ExitFirstPerson`, `CompanionCamera.cs:74`).
3. **It orbits by itself:** after 2.5 s standing still, the camera drifts 26° to a side angle (`FollowYaw`, `CompanionCamera.cs:260-265`). Players read this as random.
4. **Steering feedback loop:** the stick is camera-relative (`CameraRelative`, `GardenCompanionDemo.cs:178`) while the camera swings to face the walking direction within 0.8 s (`FollowYaw`, line 256). Holding a diagonal makes the camera turn, which changes "diagonal", so the player walks in a curve. This is also part of why the joystick feels hard.
- **Fix:** one zone at a time chosen by priority, with a 1 m hysteresis margin; keep distance and FOV close between profiles and never flip the screen offset side; replace every `Snap()` with a short fade (0.25 s, off with Reduce motion), or a 0.6 s blend where no teleport happens; remove the idle orbit, or make it far slower and only after 8 s; slow the yaw follow (about 2 s) and freeze it while the stick is held at a constant angle, or lock the steering frame at the moment the stick is first pressed.

### 30. The first-time flow, recommended
Today's flow keeps the player on a blank wait screen for minutes, shows menus that don't belong, and lands them in the garden at dusk. The recommended order:

1. **Login.** Google; Apple only once it works; "Try it free" as a clear link. No pause button.
2. **Cat or dog.**
3. **Consent** (first time only, from `d3d7742`), with working Privacy and Terms links (item 11).
4. **Pet photo**, with whole-body tips; the on-device check.
5. **Selfie.**
6. **Name the pet now**, while the server starts. Today naming comes after the wait; moving it first means the wait screen can already say "Mochi is being made".
7. **Into the garden straight away**, in daylight, with the bundled stand-in pet and a small banner "Mochi is being made · 3 min". The tutorial runs here: walk → call → ball (item 13).
8. **Reveal in the garden** when the model is ready: the stand-in walks behind a bush or a puff of petals, and the real pet comes out. Hearts and a meow, then "Say hello to Mochi". Send a notification if the app is in the background.
9. **Account nudge after the first walk**, for guests only: "Save Mochi with Google so you never lose them".

Done when: a new player reaches the garden in under a minute after the selfie, never sees Pause or Store before the garden, and the reveal happens in the garden.

---

## Not checked in this audit

- I did not play the game or look at it running; the world findings come from the code and the builder, and visual quality should be confirmed in Play Mode with `PlaceCapture`.
- I did not run a phone build, measure performance, or test Google sign-in.
- Payments, tokens and the store are excluded by request.
