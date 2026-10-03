# Tasks for Claude

Work a Claude Code session can pick up and finish without the owner, in this order. Read `AGENTS.md`, `context/STATUS.md` and the doc named in each task first. Load the skill named in each task (`docs/DESIGN.md`, Skills to use).

**Rules for every task**
- No provider credits (Tripo, Meshy) unless the task says the owner approved it. Use the server's test models (`GENERATION_MODE` unset on a local server, or unit tests with fake providers).
- No pushes, deploys to Railway, published Firebase rules or purchases without the owner. Local commits are fine; end each with the attribution line.
- Compile after every C# change (`unity recompile` with the editor open, or batch mode). Play the change in Play Mode before calling it done.
- Update the doc the task follows in the same change, and `context/STATUS.md` at the end.

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
