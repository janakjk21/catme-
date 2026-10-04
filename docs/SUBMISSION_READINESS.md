# App Store submission readiness (everything except payments)

Goal: when the owner buys the Apple Developer account, only Apple-account work remains (Sign in with Apple, signing, in-app purchases, upload). Everything here can be done now. Work in order on branch `claude/focused-faraday-eemw65`, one commit per task, never `main`. Read `AGENTS.md`, `docs/LAUNCH_AUDIT.md`, `docs/MENUS_AND_ACCOUNT.md` first. You cannot run Unity: keep C# compile-safe, add EditMode tests for logic, change the world and room only through `Editor/PlaceSceneBuilder.cs` and `Editor/HouseRoomPersonalization.cs`, and end each task with exactly what the owner must check.

**Out of scope here:** payments and the store (separate plan), Sign in with Apple and Apple account deletion (need the paid account), and anything needing provider credits.

## R1. Sprint actually works
Cause (found 2026-10-04): the Sprint button (`Garden/GardenCompanionDemo.cs`, `SprintButton`, placed by `SprintButton.Position` above the joystick) sits on the **left**, where the left thumb is already on the joystick, so it can't be held while walking. And when held, the owner soon leaves the pet >5 m behind, and the "pet far behind" rule (line ~196) cuts speed to 0.7 × 1.8 = 1.26 m/s, barely above walking (1.1).
Fix:
- **Run by pushing the stick to the edge:** full deflection (≥ 0.95) held for 0.5 s switches to running at 1.8× until the stick eases back under 0.8. Haptic tap on start. This is the main way to run.
- Move the optional **Run** button to the **right** side, just above the paw button, as hold-to-run.
- While running, skip the "pet far behind" slowdown; the pet's follow speed already scales with `SprintFactor`. Keep the slowdown for normal walking.
- Indoors: no running (as now).
- Tests: stick at the edge for 0.5 s starts running; releasing below 0.8 stops; the far-behind slowdown doesn't apply while running.

## R2. A real release build
- Add a **Release** option to `Editor/IOSDevelopmentBuild.cs` (menu "CatMe > Build iOS Release Xcode Project"): not a development build, no script debugging, no Development Console, increments the build number.
- Everything behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD` stays out of release (the room debug line in `Session/Interactions.cs` is already behind it; check the others).
- In the iOS post-process, set `ITSAppUsesNonExemptEncryption = NO` in Info.plist (only standard HTTPS is used).
- Check the usage strings say why: camera ("Take a selfie so we can make your 3D you"), photo library, photo library add (Save to Photos), notifications.

## R3. Nothing unfinished in the app
- **Hide the Store entirely** (menu row and any button) until payments ship; remove "Everything here is free while the shop is being built." (`Screens/StoreScreen.cs:34`).
- No letter-tile placeholders anywhere.
- **Wall frame art:** instead of flat colour, fill the frames with the player's own album photos when they have some, otherwise a few soft garden captures shipped with the app (generate them with the existing capture tool, `Editor/PlaceCapture.cs`, and save under `Assets/CatMe/Art/Rooms/FrameArt/`; the owner runs the capture).
- Check the menu, camera, joystick and paw buttons never show on Login, onboarding or the Making screen (launch audit 27c).

## R4. iPad
The app installs on iPad (`targetDevice: 2`), so App Review may test on one.
- Render every screen with `Editor/UiCapture.cs` at iPad sizes too (2732 × 2048 and 2388 × 1668) and fix any overlap or clipped text.
- Joystick, paw button and the sprint behaviour must sit inside the safe area on iPad.
- Owner checks one full run on an iPad or the iPad simulator.

## R5. App Review can finish the flow
- **Server (`janakjk21/catme-ai`):** `CATME_REVIEW_UIDS` (comma-separated Firebase uids) bypasses the daily caps and quotas. Tests cover it. The owner adds the review account's uid on Railway.
- Write `docs/APP_REVIEW_NOTES.md`: how to start ("Continue without signing in" or the demo account), that making a pet takes 3-8 minutes and what they see meanwhile, where account deletion is (Menu > Account > Your data), and that photos go to Tripo, Meshy and OpenAI with consent.

## R6. Privacy policy, terms and support (owner fills the bracketed facts)
- Finish `docs/legal/PRIVACY_POLICY.md` and `docs/legal/TERMS_AND_AI_POLICY.md` from the real data flows in `docs/ACCOUNTS_AND_PRIVACY.md`, `docs/PHOTO_TO_3D.md` and the server code: what's collected, why, each processor (Firebase, Tripo, Meshy, OpenAI, Crashlytics), retention, deletion, age (13+), contact. List every remaining `[...]` the owner must fill.
- In the website repo (`worntag_repo_patch/website`), add `privacy.html` and `terms.html` with the final text plus a **`support.html`** (contact email, how to delete your account, FAQ). The app already links `/privacy.html` and `/terms.html`.
- Write `docs/APP_PRIVACY_LABELS.md`: the exact App Store Connect answers. Collected and linked to the user: Photos, User ID, Purchases (later), Crash data, Product interaction. No tracking, no advertising data.

## R7. Account and data deletion check
For guest and Google accounts: Menu > Account > Delete account removes server data and local data and returns to Login; Delete my photos keeps pets. Tests with fakes. (Apple accounts come after the paid account.)

## R8. Permissions at the right moment
Camera only when taking the selfie; photo library only when picking the pet photo; notifications only after the first pet is made (never at launch). Saving to Photos asks only on first save.

## R9. Store listing ready
Write `docs/STORE_LISTING.md`:
- Name (check "WornTag" is free on the App Store), subtitle (30 characters), description, keywords (100 characters). No "Ghibli", no other brands, no promises the app doesn't keep.
- A screenshot plan from **real in-game captures**, not the login painting: iPhone 6.9" (2868 × 1320 landscape) and iPad 13" (2752 × 2064 landscape), 5 each: your pet walking with you, the garden, the house room, the reveal, the photo album.
- Age-rating questionnaire answers (likely 12+), with reasons.
- Support URL, marketing URL, copyright line.

## R10. Licence audit
Write `docs/ASSET_LICENSES.md`: every third-party folder in `Assets/`, its source (Unity Asset Store, Fab, itch.io, other) and licence. Flag anything not cleared for a commercial app. **`Assets/VRChat/HomeKit` is the main risk:** if its licence doesn't allow use outside VRChat, replace the room furniture that uses it with `LowPolyLivingRoomPack` or Asset Store furniture (through `HouseRoomPersonalization`). The owner confirms the sources.

## R11. Known bugs still open
- White wedges on the ground in front of the house (the builder's "Ground check in front of the house: 47 suspicious renderer(s)" warning in `PlaceSceneBuilder`): find and fix them.
- Missing scripts on the wildlife (Chicken, Horse, Deer) after the asset archive; restore the needed scripts from `../catme-asset-archive/`.
- Feeding: tapping **Feed** near the bowl should fill it straight away (today it only leads you to the bowl, and filling is a second step nobody discovers).

## R12. Production backend checklist (owner executes)
Write `docs/GO_LIVE.md`: the exact steps and values: merge order (API then app), Railway variables (`GENERATION_MODE=live`, `GENERATION_DOGS=1`, `PHOTO_CHECK_REQUIRED=1`, `OPENAI_API_KEY`, quotas, daily caps, `CATME_REVIEW_UIDS`, `CATME_REQUIRE_APP_CHECK` once App Check is on), publishing the Firebase rules (`firebase deploy --only firestore:rules,storage`), checking `/health`, watching logs during review, and an IPv6 check of the API host.

## After the owner buys the Apple account (not now)
Sign in with Apple and Apple account deletion, signing with the paid team, in-app purchases and paywall (from the pricing plan), TestFlight, then submission with the `asc-*` skills.
