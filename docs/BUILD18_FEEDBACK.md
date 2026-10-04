# Build 18 phone feedback (2026-10-04)

Owner's notes from playing build 18 on the iPhone 14 Pro Max, turned into tasks. Work them in order on branch `claude/focused-faraday-eemw65`. Read `AGENTS.md`, `docs/LAUNCH_AUDIT.md` and `docs/MENUS_AND_ACCOUNT.md` first. You cannot run Unity: keep changes compile-safe, add EditMode tests for logic, and end each task with exactly what the owner must check on the phone. World and room layout changes go through the builder scripts (`Editor/PlaceSceneBuilder.cs`, the house room refurnish script), never by hand-editing `.unity` / `.prefab` files.

## F1. Sound plays nowhere (blocker)
Owner heard **no sound at all** on the phone: no meows, no ambience, no UI.
Investigate in this order and fix what is wrong:
1. **iOS audio session.** Unity's default category on iOS obeys the ring/silent switch, so a phone on silent plays nothing. Add a tiny native call at startup (in `Assets/Plugins/iOS/`) that sets `AVAudioSessionCategoryPlayback` with `AVAudioSessionCategoryOptionMixWithOthers`, and activates the session; re-activate on resume from background.
2. An `AudioListener` exists and is enabled in `GardenCompanion` after login, after the place loads and inside the room and tent (exactly one at a time).
3. `CatMe.SoundEnabled` defaults to 1 and the Settings toggle writes the same key; nothing sets `AudioListener.volume` or `AudioListener.pause` and forgets to undo it (menus pause time with `Time.timeScale = 0`; check that pausing doesn't also pause audio permanently).
4. The new pet-audio code (`Pets/PetSounds.cs`, `Editor/PetAudioImport.cs`) finds the clips in `Resources/CatMeAudio/Cat` and `/Dog` on device (log the loaded set sizes once at startup in development builds).
- Done when: with the phone on silent and on ring, the garden ambience, a tap meow and UI taps are audible.
- **Done in code (2026-10-04, not compiled):** `Plugins/iOS/CatMeAudioSession.mm` + `World/AudioSession.cs` set Playback + MixWithOthers at startup and on every resume, keep exactly one enabled `AudioListener` on the main camera, and undo any `AudioListener.pause`/zero volume. Nothing else pauses audio (menus only set `Time.timeScale`, which does not stop sound). `CatMe.SoundEnabled` defaults to 1 everywhere and Settings writes the same key. `PetSounds` now loads the `Cat/` and `Dog/` sets (dev builds log the set sizes: `[WornTag][Audio] pet sound sets: ...`) through `PetVoicePicker` (shuffle bag, cooldowns, budget, pitch, PET_AUDIO.md); `Editor/PetAudioImport.cs` sets the import options. A tap now chirps; the reveal is forced. Tests: `PetVoiceTests` (4).

## F2. No haptics
`World/Haptics.cs` calls `CatMeHapticImpact` in `Plugins/iOS/CatMeHaptics.mm` from 14 places, but the owner felt nothing.
Check: the generator is created and `prepare`d on the main thread; the right feedback style is used (soft / light / medium); `CatMe.Haptics` defaults to 1 and the Settings toggle uses that key; calls aren't wrapped in a condition that is false on device. Add one test vibration on the Settings toggle being switched on.
- Done when: tapping the pet, pressing buttons and the call give a light tap on the phone.
- **Done in code (2026-10-04, not compiled):** the likely cause was in `CatMeHaptics.mm`: a new generator per call, released right after `impactOccurred`, and calls not forced onto the main thread. Now one generator per style is kept, used on the main thread and re-prepared; generators are warmed at startup. `CatMe.Haptics` defaults to 1 and is the Settings key; switching it on gives one test tap; links now tap too. Owner: iPhone Settings > Sounds & Haptics > System Haptics must be on, and Low Power Mode turns haptics off. Tests: `HapticsTests` (2).

## F3. Dog pipeline: make it work end to end, like the cat
Dogs still don't come out right. Trace one dog creation through every hop and make each one match the cat path:
1. App: species `dog` sent on `POST /v1/pets`; dog photo tips; dog consent copy.
2. Server (`janakjk21/catme-ai`): `GENERATION_DOGS=1` path (owner sets it on Railway); dog prompt (derived from the cat prompt; image step on `chat_image_2`); Tripo image-to-model; rig check must return `quadruped`; rig; walk retarget; download; stored at `users/{uid}/models/{petId}/v1.glb`.
3. App: dog model downloaded to the account folder, loaded with glTFast, scaled by its own dog height (small / medium / large), walk clip driven like the cat's, dog behaviour (`CompanionBehavior` dog values), dog sounds; corgi only as the fallback.
- Use the fake-provider tests on the server and a stand-in dog GLB for the app. A real dog run needs owner-approved credits.
- Done when: server tests cover the dog path, and the owner makes one real dog that walks in the garden.
- **Done in code (2026-10-04; app not compiled, server 67/67):** every hop traced. Server: dogs use the dog prompt (cat prompt with species words swapped) on `chat_image_2`, Tripo model, rig check must say `quadruped` (anything else is a rejection, no credit), rig, walk, saved at `users/{uid}/models/{petId}/v1.glb`; with dogs off a live config never pays for a dog; an optional `fixtures/dog-walking.glb` in Storage gives dogs a real stand-in in stub mode (else the corgi). `mobile/dog.test.js` (6) runs the whole live dog path with a fake Tripo. App: species `dog` was already sent; the name screen asks dogs Small / Medium / Large (`PetProfile.size`, `PetSize`): shown height 0.5 / 0.72 / 0.9 m and bark pitch 1.3 / 1.12 / 1.0; dog placeholders no longer show a cat picture; downloading and loading the dog's own GLB was done in L7, corgi as fallback. Tests: `DogSizeTests` (2).

## F4. Double person (known bug)
Two person models render on top of each other. Cause: `ReloadCharactersFromPhoneStorage` (`Garden/GardenCompanionDemo.cs`) can run several times in parallel (called from `CatMeAppFlow.cs` lines ~156, ~219, ~232, ~553 and `GardenCompanionDemo.cs:90`); each finishes and adds a `WalkingHuman`. Fix: one reload at a time with a single queued follow-up; `LoadCharacters` clears existing person and pet children before adding. EditMode test with a fake loader.
- **Done in code (2026-10-04, not compiled):** `ReloadGate` lets one reload run at a time (the first load counts) and folds any requests during it into one follow-up; `LoadCharacters` starts by detaching and destroying the old person and pet. Callers are unchanged. Tests: `ReloadGateTests` (2).

## F5. Camera: user controls it, the game stops moving it
The camera "moves so much" and feels random. Replace the automatic behaviour with player control, like a third-person game:
- **Look:** one finger dragging on the right side of the screen (anywhere not on a button) orbits the camera around the player: yaw free, pitch clamped (about 5° to 55°).
- **Zoom:** two-finger pinch zooms between about 1.8 m and 6 m; remember the zoom.
- **Joystick** stays on the left and walks relative to the camera.
- **Remove** the idle side-orbit, the automatic yaw-follow while walking, and per-zone distance and screen-offset changes. Zones may only clamp the allowed zoom range (for example tighter under trees and inside), never move the camera on their own.
- Keep collision: the camera pulls in smoothly when a wall or tree is between it and the player.
- Inside rooms and the tent, the same controls with a tighter zoom range.
- Done when: walking a full loop, the camera only moves when the player drags or pinches (plus collision pull-in).
- **Done in code (2026-10-04, not compiled):** `CompanionCamera` third-person mode is now an orbit the player owns: one finger on the right half (off buttons) turns it (yaw free, pitch 5 to 55 degrees), two fingers pinch zoom 1.8 to 6 m (remembered in `CatMe.Camera.Zoom`; mouse wheel in the Editor). Removed: idle side-orbit and dolly, yaw-follow while walking, look-ahead, pet-widening, per-zone distance, pitch and screen offset. Zones and rooms only clamp the zoom range (`CameraProfile.zoomMin/zoomMax`: tent 3.2 m max, woodland 4.5, rooms 1.3 to 2.6, petting 2.4). Collision pull-in kept. The joystick walks relative to the camera. Seated first person is unchanged. Tests: `PlayerCameraTests` (3).

## F6. Pet button bigger and placed like the joystick
The paw (pet actions) button is too small. Make it **much bigger** (about the joystick's size) and place it as the mirror of the joystick: bottom right, the same distance from the edges inside the safe area. Its Call / Roam / Ball buttons fan out above it, each with a readable label (`MENUS_AND_ACCOUNT.md` section 9).
- **Done in code (2026-10-04, not compiled):** paw 200 (stick base 220) with a 104 icon, kept by `SafeMirror` at the stick's resting spot mirrored (200 in from the safe edge, 22% up the safe height); the fan (120 buttons on Paper with Paper label pills) opens on an arc above-left of it with a fourth slot ready for Go home; the contextual card moved up to 480 so it clears the paw. Tests: `PawLayoutTests` (3).

## F7. Fence off the river end
Build a continuous fence (the same fence style already used along the bank) along the whole river side and across the end of the trail, with colliders, so the player can't walk toward the river at all for now. No gaps where the path meets the end clearing. In `PlaceSceneBuilder`; owner runs Rebuild With Last Seed.

## F8. The house room
Keep the existing contextual buttons as they are. Changes:
1. **Pet bed / cat house bigger.** Scale the pet's bed and house up clearly (about 1.6×) so they read from the camera.
2. **Go to bed.** When the player walks near the bed, the contextual button offers **Bed time**; tapping it (or tapping the bed itself) makes the pet walk over, step in, curl up and sleep: lights dim slightly, a soft sleeping sound (cat: quiet purr; dog: slow breathing / content sigh stand-in) fades in and loops quietly, and a line like "Mochi is fast asleep." Tapping the pet or walking away wakes it.
3. **Plants stand upright.** The potted plants (monstera, bonsai and the others in the room) are lying on their side: a model axis problem on import (GLB +Y up vs a -90° X rotation). Fix the rotation in the refurnish script so they stand vertical, grounded in their pots.
4. **A livelier room.** Warmer wall colour or a soft wallpaper texture, a rug, a lamp with a warm light on at night, a shelf with small objects, cushions. Use what's already in the project first (`LowPolyLivingRoomPack`, `VRChat/HomeKit`), and check `../catme-asset-archive/` for furniture packs archived during asset optimisation (`Furniture_ges1`, `sofa.glb`, `lamp.glb`); move back only the files actually placed.
5. **Frames on the walls.** The owner has downloaded picture frames; they will be placed in `Assets/CatMe/Art/Rooms/Frames/` (owner action). Hang 3 to 5 of them on the walls at eye height, not overlapping the main photo frame, with a few of the garden photos or neutral art in them.
6. **Filling the bowl animation.** When the player fills the bowl, the person should crouch and pour. The owner downloads a suitable animation (for example from Mixamo: "Kneeling" / "Picking Up" / "Pouring") into `Assets/CatMe/Art/Animations/`; retarget it to the bundled person as a Humanoid clip and play it at the bowl, then return to idle. Generated people only have a walk clip, so for them skip the animation and keep today's behaviour.
- Done when: the owner rebuilds the room and checks each item in Play Mode and on the phone.

## F9. "Go home" in the pet action menu
Add a **Go home** action to the paw menu next to Ball, Roam and Call (same size and style, label inside or under it). Tapping it: the pet says its home line, both walk to the house door (the existing lead-home behaviour in `Session/CompanionLeader`), and the house opens; if the player is already near the house, enter directly. Inside the house, the same slot becomes **Go outside**. A fade covers any teleport (no camera cut).

## F10. Sprint button
A **Sprint** button near the joystick (above it, smaller than the joystick, inside the safe area). Hold to walk about 1.8× faster (or tap to toggle; pick hold and document it). The pet speeds up to keep pace; footstep pace and walk-clip speed follow so feet don't slide. Hidden inside rooms and the tent. Haptic tap on start.

## F11. Pinch in to a close-up
Extend F5's pinch zoom: pinching in past the normal minimum moves to a **close-up** (about 0.9 m) that frames the player's face and the pet together; at the closest step the camera eases its look target from the body to head height. Pinch out returns to the normal range. Turn depth of field on only in close-up (off with Reduce motion). Clamp so the camera never enters the player or the pet; collision still wins.

## Owner actions for this list
- Check the iPhone's ring/silent switch while testing sound, and report whether ambience plays with it on ring.
- Put the downloaded frames in `Assets/CatMe/Art/Rooms/Frames/` and the bowl-filling animation in `Assets/CatMe/Art/Animations/`.
- Set `GENERATION_DOGS=1` on Railway when ready to test a real dog, and approve the credits.
- After the agent finishes: Rebuild With Last Seed, Refurnish House Room, commit the scene and room, then a new phone build.
