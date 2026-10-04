# Tasks for Claude

Work a Claude Code session can pick up and finish without the owner, in this order. Read `AGENTS.md`, `context/STATUS.md` and the doc named in each task first. Load the skill named in each task (`docs/DESIGN.md`, Skills to use).

**Rules for every task**
- No provider credits (Tripo, Meshy) unless the task says the owner approved it. Use the server's test models (`GENERATION_MODE` unset on a local server, or unit tests with fake providers).
- No pushes, deploys to Railway, published Firebase rules or purchases without the owner. Local commits are fine; end each with the attribution line.
- Compile after every C# change (`unity recompile` with the editor open, or batch mode). Play the change in Play Mode before calling it done.
- Update the doc the task follows in the same change, and `context/STATUS.md` at the end.

---

## Launch tasks (2026-10-04): do these first, in this order

Source of truth: `docs/LAUNCH_AUDIT.md` (item numbers below) and `docs/MENUS_AND_ACCOUNT.md` (menus, account, settings, look). Read both before starting. Work on branch `claude/focused-faraday-eemw65` in both repos (app `janakjk21/catme`, server `janakjk21/catme-ai`).

**If you cannot run Unity** (cloud session): keep each C# change small and compile-safe, add or extend EditMode tests in `Assets/CatMe/Tests` for logic, and list in your final message exactly what the owner must compile, play and check. Never hand-edit `.unity`, `.prefab` or `.asset` files; world changes go through `Editor/PlaceSceneBuilder.cs`. Server: `npm test` must pass.

**Already done and validated, do not redo:** spend limits and daily caps, selfie person prompt, pet markings, restore pets, resume creation, bad-model fallback, policy links, EditMode tests, and (code only) lookout, river mesh, mountains, full day cycle. See "Validation of ChatGPT's fixes" in the audit.

### L1. Account, sign-out and per-account storage (audit 31a, 31b; `MENUS_AND_ACCOUNT.md` section 4) - highest priority

**Code done 2026-10-04 (cloud, not compiled):** `AccountStorage` (per-uid folders, one-time legacy move, remembered last account), roster/store/profile/drafts/downloads/person model/album/session per account, two-state Account screen (`OnboardingFlow.ShowAccount`), Save my pets with Google (link; "already has pets" switch), sign-out to Login via scene reload with C1/C2, C3, C4 hold-to-delete (`ConfirmDialog.ShowWithCancel`, `HoldToConfirm`), deletion scoped to this account. Tests: `AccountStorageTests` (4). Apple stays hidden until the paid team.
- Store all local data under `Profiles/{uid}/` (`PetRoster.cs:35`, `LocalPlayerProfile.cs:34`, `DeviceBridge.HumanModelPath`, house frame cache, downloads). Migrate `Profiles/local-guest` into the first signed-in uid once.
- `RestoreAccountAsync` must restore into that account's folder only, so account B never sees account A's pets.
- Sign out: save, sign out of Firebase, clear in-memory roster, models, frame and pending job, reload to Login. Guests get confirm C1 first.
- Account screen: the two states (guest, signed in), "Save my pets with Google" linking, which account you are signed in with, the "Your data" section, confirms C1 to C4.
- Done when: EditMode tests cover per-uid paths and the guest migration; owner Play Mode: sign out returns to Login and a second account sees none of the first account's pets.

### L2. Menus and look (`MENUS_AND_ACCOUNT.md` sections 1 to 9, taste rules; audit 27, 28)

**Code done 2026-10-04 (cloud, not compiled):** sage tokens; menu is a right-side sheet (Your pets, Photo album, Account with a note, Settings; Store hidden until `GardenCompanionDemo.StoreShipped`), every screen opened from it returns to it (`CatMeAppFlow.OpenFromMenu`); pause menu never opens outside the garden; Your pets rebuilt (Add first, Walking now, Being made, dog stand-in note, "…" Rename / Remove with confirm, page dots, one chip colour, no store button); name rule `PetRoster.CleanName`; removed server pets stay removed (`hiddenServerIds`); Settings is a sectioned scrolling sheet with renamed toggles, Replay row, Account row, privacy, terms and version, and no delete buttons (they live in Account); album caption above the buttons with "N of M", back arrow, Save to Photos main, Delete as a Danger text button; Store back arrow, pill tabs, SageTint Owned chip; paw labels on Paper pills; one fixed contextual card spot ("Go inside"); login per 4b; no `Tag()`, no brand dot; main buttons and dialogs use SageDeep instead of Ink. Tests: `PetRosterRulesTests` (3).
Not done in L2: real icon family (owner supplies sprites, see `MENUS_AND_ACCOUNT.md` section 1 "Icons"), 3D render portraits on pet cards, "Couldn't be made" card state (failed jobs are not kept in the roster yet), "Retake my selfie" link, rename synced to the server (rename is on this phone only), Share in the album.
- Add the colour tokens (`Stone`, `SageDeep`, `Sage`, `SageTint`) to `CatMeUIStyle.cs`; sage is the only accent; retire brass and black main buttons on these screens.
- Menu becomes a right-side list sheet; "Back to the garden" is the only close wording; Store has one entry point.
- Never show menu, camera, joystick or paw buttons outside the garden (Login, onboarding, Making) (27c).
- Your pets: Add a pet first, walking-now state, being made / failed / stand-in states, rename and remove, page dots; same chip for cat and dog; name rules.
- Settings: grouped, renamed toggles, Account row, privacy, terms, version; scroll hint.
- Photo album: caption above buttons, previous / next, Save to Photos, captures without HUD or watermark (27e).
- Paw menu labels readable (27f); one-line contextual card above the paw button (27g).
- Remove eyebrow `Tag()` labels and the brand dot; replace the 35 code-drawn icons in `UiIcons.cs` with one licensed family (Phosphor Regular suggested; the owner provides the sprites if you cannot download).
- Login per section 4b.
- Done when: the section 11 checklist in `MENUS_AND_ACCOUNT.md` passes.

### L3. Joystick and camera (audit 31c, 31d)

**Code done 2026-10-04 (cloud, not compiled):** floating stick in the left 40% of the safe area (rests at 22% height, base 220, travel 110, thumb 88, `JoystickResponse`: dead zone 0.15, eased), iOS bottom-edge gestures deferred at runtime; tapping the pet inside the stick's zone still works; the half-speed slowdown is now 0.7 and announced ("Slowing down so Mochi can catch up."). Camera: one zone at a time (`CameraZonePicker`: priority, then smaller box, 1 m margin both ways; builder sets priorities GateReveal 2, GardenClose 1, Lookout 3, others 0, rebuild the places to apply), profiles within 2.6 to 3.8 m and FOV 48 to 53 with the pair always on the same side, `Snap()` dips to warm white for 0.25 s unless the session fade already covers it or Reduce motion is on, leaving a seat blends instead of cutting, idle drift only after 8 s and 10 degrees, yaw follow about 2 s; the steering frame stays latched while the stick is held. Tests: `CameraAndStickTests` (3).
- Floating joystick inside the safe area, travel ~110, dead zone 0.15, eased response; set `deferSystemGesturesMode` for the bottom edge; remove the silent half-speed slowdown or explain it.
- Camera: one zone at a time by priority with ~1 m hysteresis; keep distance and FOV close between profiles and never flip the screen offset side; replace `Snap()` cuts with a 0.25 s fade (off with Reduce motion); remove or slow the idle orbit; stop the steering feedback loop (slower yaw follow, or lock the steering frame while the stick is held).
- Done when: owner plays a 5-minute walk and the camera never jumps without a fade, and holding a diagonal walks straight.

### L4. Errors and resume follow-ups (audit 9, 10, 4)

**Done 2026-10-04 (app cloud, not compiled; server tests 61/61):** server `REFUSALS` gives each refusal its own line for pets and for the person (QUOTA_EXHAUSTED, JOB_IN_PROGRESS, ACCOUNT_DELETING, SESSION_REQUIRED, CONSENT_REQUIRED, DAILY_CAP_REACHED); guests are marked `guestQuota` and get topped up to the normal allowance once when the same uid signs in with Google or Apple (accounts made before this change have no mark and are not topped up). App: `CreationClient.FailedMessage` shows "We couldn't check your photo just now. Try again in a minute." for CHECK_UNAVAILABLE; offline text is neutral; the old `WornTag.Creation.Pending` key moves once (`MigrateLegacyPending`); the pending record keeps species and name (no hard-coded cat); the person model has one home (`AccountStorage.PersonDownloadPath` then `InstallPersonModel`, stand-ins dropped) on create, resume, restore and retry. Tests: server `refusals.test.js` (14), app `ErrorsAndResumeTests` (6).
- Server (`catme-ai`): give each creation refusal its own message instead of "Creation cannot start yet." (`index.js:204,274`): QUOTA_EXHAUSTED, JOB_IN_PROGRESS, ACCOUNT_DELETING, SESSION_REQUIRED.
- App: show a retry line for `errorCode == CHECK_UNAVAILABLE` ("We couldn't check your photo just now. Try again in a minute."); make the offline message neutral (no "your pet is saved" before a pet exists).
- Resume: migrate the old `WornTag.Creation.Pending` key once; never hard-code species "cat" in the fallback (store species in the pending record); one location for the person model.
- Server: a guest that links Google gets the normal pet quota.
- Done when: tests cover each message and the migration.

### L5. Garden during Making, reveal in the garden (audit 13, 30)

**Code done 2026-10-04 (cloud, not compiled):** Name is the last onboarding step (`OnboardingFlow.FlowFor`), with the first suggested name selected; after it the garden opens at once (`OnMakingInGarden`): morning, no rain (`DayDirector.EnsureDaylight`), stand-in pet, tutorial, and a Paper banner "Mochi is being made · about 3 min" that follows the server stage and hides under menus. When the pet is ready it is swapped in where the player is, with hearts, its hello and "Say hello to Mochi." (`OnRevealInGarden`); a failure shows the Try again toast. A resumed creation after relaunch also goes to the garden. Leaving the app while making schedules one local reminder in 4 min ("Mochi should be ready soon. Come and say hello."), cancelled on return (`World/LocalNotifications.cs`, `Plugins/iOS/CatMeNotifications.mm`; UserNotifications.framework is added by the development build post-process only, add it for release builds too). The Making screen remains only as a fallback. Not done: the stand-in hiding behind a bush or petal puff; the server keeps the placeholder name given at photo time (the phone's name wins locally). Tests: `MakingInGardenTests` (3).
- After the selfie and naming, open the garden in daylight with the bundled stand-in and a "[Name] is being made" banner; the tutorial runs here; swap in the real models when ready with a short reveal; local notification if backgrounded.
- Name the pet before the wait (move the Name screen ahead of Making).
- Done when: a new player reaches the garden within a minute of the selfie.

### L6. World fixes (audit 27a, 27b, 29, 16, 18)

**Code done 2026-10-04 (cloud, not compiled, places not rebuilt):** 27a: the exact object can't be seen from here, so the builder now keeps flat flowers out of the strip in front of the house, stops the path material washing out to white (base colour was above 1), and logs every untextured near-white or broken-shader renderer in that area after a rebuild ("[CatMe][Places] Suspicious: ..."). 27b: only one UI input module stays on the EventSystem (a scene-saved StandaloneInputModule beside the Input System module read the same touches; legacy and new input are both enabled) and extra EventSystems from place scenes are switched off; the camera skips empty touch slots. New sessions start at 0.12 (morning); a night played through still runs into dawn. 29: green cottage first (old house as fallback) with flower boxes and door planters, procedural mossy stone texture on the garden walls, a built hanging lantern (light at night only), fewer and more varied flowers away from the path and the camera's side, path slabs and edge stones, water tinted by time of day (`DayDirector.WaterTint` to the `_CatMeWaterTint` global in `River.shader`). 16: a timber jetty through a gap in the riverside fence, with rails and invisible stops, and a curiosity at its end that draws the pet. Audio list for the owner in `docs/ASSETS.md` ("World sounds to add"). Tests: `WorldDayTests` (2).
Owner: run CatMe > Rebuild With Last Seed, read the Console's ground check, and walk to the jetty.
- Find and fix the white wedges on the ground in front of the house (27a).
- Find the source of "Touch was already deallocated" (27b); `CompanionCamera.cs:272-275`, `World/CatPetInput.cs:36` are the touch readers.
- A new day starts at morning (~0.12), not dawn.
- Lived-in house instead of the derelict one, textured garden wall, a real lantern instead of the beige cube, varied and thinned flowers away from the camera side, path detail, river tinted by time of day (unlit shader glows at night) (29).
- River: one access point where the pet can reach the water; list the audio clips needed (river loop, footsteps by surface, birds by time of day, night insects, music day and night, dog recordings) in `docs/ASSETS.md` for the owner to supply.
- Done when: builder changes compile; the owner runs CatMe > Rebuild With Last Seed and checks.

### L7. Dogs get their own model (audit 6; task 3 below)

**Code done 2026-10-04 (cloud, not compiled):** dogs download a model like cats (`OnCreate` destination, `OnCreated` copies it to the pet's folder; restore and retry already handled every species); the garden loads the active pet's own GLB for cats and dogs (`PetModelChoice`), scaled to 0.72 m for dogs, driven by its one walk clip like the cat; the corgi appears only for a dog without a model, and switching pets reloads when the model changes. The server stays dog-off (`GENERATION_DOGS` unset): the owner tests with a stand-in dog GLB in Storage `fixtures/` and a local server. Tests: `DogModelTests` (4).

### L8. Photo tips and crash reports (audit 28, 23)

**Code done 2026-10-04 (cloud, not compiled):** pet photo tips are "Their whole body, nose to tail", "Lots of light", "Only your pet" for cats and dogs. `Onboarding/Telemetry.cs` sends Crashlytics crash reports and Analytics events through reflection, so it compiles and stays silent until the owner imports `FirebaseCrashlytics` and `FirebaseAnalytics` (13.17.0, like the other Firebase packages) and adds the Crashlytics dSYM upload run script for release builds. Events: `onboarding_step` (step), `creation_started` (species), `creation_result` (ready, a reject reason code or failed), `time_to_ready` (seconds), `first_walk_done` (skipped yes/no). No user id, name, email or photo is sent. Settings > Account and data has "Share crash reports" (on by default). Privacy draft updated (data table, processors, retention, legal basis); the App Store privacy label must list Crash Data and Product Interaction, not linked to the user, not used for tracking. Tests: `TelemetryAndTipsTests` (4).
- Pet photo tips: "Their whole body, nose to tail", "Lots of light", "Only your pet".
- Add Firebase Crashlytics and a few funnel events (onboarding step reached, creation result and reason, time to ready, first walk finished); update the privacy policy draft.

### Owner only (do not attempt)
Renew the Apple signing profile (expires 2026-10-08); paid Apple team and Sign in with Apple (audit 12); publish Firebase rules (8); Railway: lower `CATME_FREE_PETS` / `CATME_FREE_OWNERS` from 1,000, set realistic daily caps, set `OPENAI_API_KEY` and `PHOTO_CHECK_REQUIRED=1`; merge both branches to `main` (Railway deploys the server from `main`); fill and publish the privacy policy and terms (11); run Rebuild With Last Seed and commit the scene; re-save the Firebase plugin `.meta` files in the editor (26); provide audio clips and icon sprites; approve credits for one real person, one real cat and one real dog.

---

## 1. Account deletion and photo deletion (needed for the App Store)

Follows `docs/ACCOUNTS_AND_PRIVACY.md`. Skills: `ui-ugui`, `copywriting`.

- Server (`worntag-api/mobile/index.js`): `DELETE /v1/photos` (deletes `users/{uid}/photos/*`), `DELETE /v1/account` (deletes the user's Storage folder, `users/{uid}` and its subcollections, their `jobs`, then the Firebase user). Tests with the fake Firestore/Storage used in `mobile/index.test.js`.
- App: Settings gets **Delete my photos** and **Delete account** (words in `docs/COPY.md`), each behind `ConfirmDialog`; on success clear local data (`PetRoster`, drafts, downloaded models) and return to the login page.
- Done when: unit tests pass, both buttons work in Play Mode against a local server with a throwaway guest. Deploy waits for the owner.

## 2. Daily spending cap and App Check

Follows `docs/PHOTO_TO_3D.md` (Limits and safety).

- Server: `CATME_DAILY_PET_CAP` and `CATME_DAILY_OWNER_CAP` (default 10 each): a Firestore counter per UTC day, checked in the same transaction as the per-account quota; over the cap returns `DAILY_CAP_REACHED`. The app shows "We are making a lot of pets today. Try again tomorrow."
- Guests: optional `CATME_GUEST_FREE_PETS` (default 1).
- App Check stays off until the owner sets it up in Firebase; leave the switch (`CATME_REQUIRE_APP_CHECK`) as is.
- Done when: tests cover the cap and its reset at midnight UTC.

## 3. Real dog models in the app

Follows `docs/PHOTO_TO_3D.md`, `docs/GAMEPLAY_AND_WORLD.md`. Skill: `unity-cli` for play testing.

- `CatMeAppFlow.OnCreate`: give dogs a model destination like cats; `OnCreated`: copy the dog model into the pet folder.
- `GardenCompanionDemo.SetActivePet` / `ApplyCompanionModel`: load a dog's own GLB (glTFast, scaled like the cat) in place of the corgi; drive its single walk clip the way the cat's is driven; keep the corgi as the fallback.
- Server stays dog-off. Test with a stand-in dog GLB placed in Storage `fixtures/` and a local server, not Tripo.
- Done when: a dog with its own model walks and follows in Play Mode; the corgi still appears when no model exists. Turning on `GENERATION_DOGS=1` and one real dog test are for the owner.

## 4. Smaller person models

Follows `docs/PHOTO_TO_3D.md`, `docs/QUALITY.md` (budget under 25 MB).

- In `worntag-api/mobile/providers.js` (Meshy `model`): try `texture_resolution: '2k'` and a lower `target_polycount` (for example 60 000).
- Unit-test the request body. The size check on a real person needs the owner's approval (credits).

## 5. Gameplay words

**Code written 2026-10-03 (cloud session, no Unity there): needs a compile and the Play Mode check below.** Pet names come from `PetRoster.NameForText`.

Follows `docs/COPY.md`. Skill: `copywriting`.

- Put every line marked "to be put in" into the code: tutorial hints and buttons (`CatMeAppFlow.RefreshGuide`), status lines (`Session/Interactions.cs`), the "Who's that?" cat line (`Pets/CompanionBehavior.cs`), settings lines, empty states, error messages.
- Use the pet's name where `COPY.md` says `[Name]`.
- Done when: a search for each old string finds nothing, and the tutorial reads right in Play Mode.

## 6. A call pose for generated people

App side done 2026-10-03: a generated person no longer loads its large model a second time for the call; Call keeps the walking person. The server plan below is still open.

Follows `docs/GAMEPLAY_AND_WORLD.md` (Controls: Call).

- A generated person has only a walk clip; the Call action falls back to it. Option: Meshy's animation endpoint for a "wave/call" action on the existing rig (`createAnimation` in the old web pipeline, `clonecatme/server/meshy.js`). Server-side plan and unit tests only; real use needs the owner's approval (credits).

## 7. Screen fade and small polish

**Code written 2026-10-03 (cloud session): `ScreenFade` in `OnboardingFlow.cs` (0.22 s, off with Reduce motion); Back and Close moved to y 440. Needs a compile, `UiCapture` renders and Play Mode.**

Follows `docs/DESIGN.md` (Motion and feel).

- A short fade between onboarding screens, respecting `CatMe.ReduceMotion`.
- The Back button sits too close to the top edge on the photo screens (see renders); move it inside the safe area.
- Done when: rendered with `UiCapture` and played in Play Mode.

## 8. Selfie: front camera and face crop

**Code written 2026-10-03 (cloud session): needs an Xcode build and a phone test.** `CatMePhotoPicker.mm` opens the front camera ("Take a selfie") or the library ("Choose a photo instead"); Vision must find exactly one face; only a head-and-shoulders crop is saved and uploaded. Messages in `COPY.md`. Camera permission text set in Player Settings and `IOSDevelopmentBuild`.

- Server follow-up (`worntag-api/mobile/ownerPrompt.js`): the person prompt still asks to keep the photo's outfit and footwear, which a head-and-shoulders crop cannot show. Rewrite it for a face-only reference (face and hair from the photo, a simple neutral outfit) and unit-test the request body. A real check spends Meshy credits: owner approval.

## 9. QA pass

Follows `docs/QA_PASS.md`. Report only, into `context/QA_REPORT.md`. No new creations unless the owner approves one.

---

## Waiting on the owner (do not start)

- Paid Apple Developer Program, then Sign in with Apple and TestFlight (`docs/XCODE_BUILD.md`, `docs/RELEASE.md`).
- Approving real credits for: a full cat + person from the phone, a real dog, the smaller-person check.
- Publishing Firebase rules (`firebase/`), committing the garden map folder, pushing to GitHub, a GitHub repo for `worntag-api`.
- Choosing a photo-check (vision) vendor; prices and payments.
