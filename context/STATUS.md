# Status

Last updated: 2026-10-03

## Now

- **Flow (in app, build 17 on the iPhone 14 Pro Max):** login -> cat or dog -> pet photo -> selfie -> making screen (wait) -> name -> reveal -> garden. See `docs/ONBOARDING_FLOW.md`.
- **Sign-in:** Try it free works in the editor. Google sign-in is built (Swift package + `Assets/Plugins/iOS/CatMeGoogleSignIn.mm`), on the phone in build 17, not yet confirmed working. Apple needs the paid Apple Developer Program.
- **Server (`worntag-api`, Railway, live):** `GENERATION_MODE=live`. Cat = Tripo only (photo, model, rig check, rig, walk). Person = Meshy (reference with the original person prompt, model, rig with walk). Dogs off (`GENERATION_DOGS` unset); the app shows the built-in corgi for dogs. Railway sets new-account quotas to 1,000 pets and 1,000 people; the owner reports 11 existing accounts were raised too. The cat prompt was shortened to 1,672 characters after Tripo rejected the 5,249-character version. Railway deployment `8dacc563-da59-4244-bd78-db83fee2e82a` is running; `/health` responds; 17 local server tests pass.
- **Garden:** loose lawn boulders removed; pond rebuilt (dark bed, river water, ring of the garden's rocks).
- **House:** supplied cat house and dog bowl placed upright in `HouseRoom_r3.prefab`. The cat leads toward a large wall frame, with shared Add a photo / Change photo / Save photo actions like Sit. The existing picker preserves photo proportions; `HousePhotos/{uid}/frame.jpg` caches the account photo. Local API additions upload private `photos/frames/` JPEGs and store Firestore `houseFrame`, with restore and failed-save retry. Deleting photos or the account removes these copies. Fresh room and saved-photo renders: `docs/implementation/house-photo-frame/`.
- **Builds:** exports on `/Volumes/Janak Projects/WornTag Builds/`; steps in `docs/XCODE_BUILD.md`. Free personal team `658662TU43`; the profile expires 2026-10-08.

## Not yet verified

- A full real cat + person made from the phone with the current Tripo cat prompt. The 2026-10-03 cat jobs before the prompt deployment failed with `TRIPO_ERROR`; no new creation after that deployment has been verified. The recent person job is ready. Failed jobs are closed and do not reduce the account quota, though provider work may have spent credits.
- Google sign-in, Try it free, the making screen, name and reveal on the phone.
- Walk animations of generated models in motion. Person model is 43.7 MB (aim under 25 MB).
- Photo-frame picker and contextual button on a physical iPhone. Account save and cross-device restore against live Firebase are unverified: the new server frame endpoints have not been deployed. The installed build 17 does not contain this feature.

## Changed 2026-10-03 in a cloud session (not compiled; no Unity there)

Branch `claude/focused-faraday-eemw65`: game words from `COPY.md` (tutorial with the pet's name, status lines, messages); selfie front camera with a one-face check and head-and-shoulders crop (`CatMePhotoPicker.mm`, `DeviceBridge`, selfie screen); onboarding screen fade and Back/Close inside the safe area; a generated person is loaded once instead of twice. Compile, Play Mode and an iPhone build are needed before any of it counts as done (`context/CLAUDE_TASKS.md` 5 to 8).

Owner push `b1633e8`: old 166 MB butterfly replaced by AI cartoon butterflies, Rain Particles pack added, place rebuilt. Follow-up (cloud, not compiled): butterflies now switch the pack Animator between flying and sitting, eight different species, rain pack wired into `PlaceWeather` with splashes and a phone particle cap, rain materials converted to URP by the existing menu, pack demo script `CharacterController.cs` removed. Needs: Convert Imported Materials To URP, Rebuild With Last Seed, Play Mode in rain.

Feeding and frame (cloud, not compiled): bigger bowl and cat house (menu CatMe > Rooms > Refurnish House Room), "Fill the bowl" with kibble and the pet eating, pet leads home when hungry and to the bowl inside, pet admires a new frame photo, frame lead only while empty, frame account check once a second.

Polish pass (cloud, not compiled): days now change (new day after a short night, on relaunch after 3 h or at night, never the same weather twice) with a day line; bigger tent and a furnished tent room with a fire (rebuild the place); leaving the tent anytime; Story linger lengthens all lines; Save to Photos in the album (needs NSPhotoLibraryAddUsageDescription, set by IOSDevelopmentBuild for development builds only); store food used at dinner; guest account text no longer mentions Apple only.

Cat and dog (cloud, not compiled): `PetSounds` gives each species its own sounds (dog: bark, excited bark, pant, sigh, crunch; generated until recordings are added, see `docs/ASSETS.md`); dogs fetch the ball back and drop it at your feet, cats pounce and leave it (the ball no longer vanishes); species timings and lines for dinner, fire, frame, ball, call and sleep; no player-facing "Your cat" left for dogs.

Photo guardrails (cloud, not compiled): selfie is camera only (no library; "Open Settings" when refused); pet photos get Apple's on-device cat/dog check (wrong animal or several refused, none warns). Server half designed in `PHOTO_TO_3D.md` (moderation + vision check before credits), not built.

Consent and policies (cloud, not compiled): consent screen before the first photo (nothing uploads without "I agree"; the app used to record consent automatically); server photo check built on `catme-ai` branch `claude/focused-faraday-eemw65` (41 tests pass, not deployed); privacy policy and terms with AI and photo policy drafted in `docs/legal/`.

## Current task

Owner request: furnish the house and add an account-saved photo frame with a cat guide and contextual action. Unity compilation reports 0 errors and warnings; 22 focused local server tests pass (frame, privacy, existing API). Play Mode checks use a fake account API and isolated temporary save, never the player's photo or a provider. Evidence in `docs/implementation/house-photo-frame/`. Live account persistence requires owner-approved deployment of the sibling server; phone validation remains pending.

## Next

Resume `context/CLAUDE_TASKS.md` task 1: Settings/account deletion controls and server routes are implemented; focused tests pass. Still test both actions with a throwaway guest against a local server. Apple deletion needs reauthorization and token revocation setup. The installed build 17 does not include these local app changes.

## Testing notes

- The Unity CLI drives the editor (`unity command ...`; package `com.unity.pipeline`). Keep the Unity window in front, or Play Mode freezes at frame 1. Turn off Console > Error Pause.
- `DeviceBridge.EditorAutoPickPet` / `EditorAutoPickHuman` (editor only) feed test photos instead of the file dialog.
- `docs/QA_PASS.md` is the brief for a play-and-report pass.
- The player's earlier local data is backed up at `~/Library/Application Support/CatMe/WornTag-backup-2026-10-03`.
- The garden map folder `Assets/CatMe/Scenes/Places/` is not in git yet (owner to decide).
