# CatMe iOS internal build handoff

Updated: 2026-09-27

The 2026-09-27 signed app below is portrait-only. A prior portrait build was installed in place on the iPhone 14 Pro Max and loaded the real rigged cat after the runtime glTF shader fix. The latest bundle adds a cat-load retry card and is ready for an in-place update when the phone reconnects.

## Build artifacts

- Unity export: [`Builds/iOS/CatMeHomeOptimized`](../../../Builds/iOS/CatMeHomeOptimized)
- Xcode project: [`Builds/iOS/CatMeHomeOptimized/Unity-iPhone.xcodeproj`](../../../Builds/iOS/CatMeHomeOptimized/Unity-iPhone.xcodeproj)
- Signed portrait device app: [`Builds/iOS/CatMeHomeOptimized/CatMeHome.app`](../../../Builds/iOS/CatMeHomeOptimized/CatMeHome.app)
- Bundle ID: `com.catme.home`
- Unity: `6000.6.2f1`; Xcode: `27.0`; target: arm64 iOS, portrait, deployment target iOS 15.0.
- The packaged `HomeRoom`, offline `master-walking-cat.glb`, native iOS photo picker, and runtime glTF Shader Graph references were verified.

The Xcode Release development build passed for arm64. Automatic signing succeeded with team `658662TU43`, and `codesign --verify --deep --strict` passed. `UISupportedInterfaceOrientations` contains Portrait only. The development profile includes the paired iPhone and expires on 2026-09-30. `devicectl` installed the prior portrait build over the existing `com.catme.home` app; the local save remained in Documents. The startup log confirmed one skinned cat renderer, one walk clip, floor contact, and the 10-second stationary acceptance pass. A phone screenshot showed the real cat, Energy, Find cat, More, and four primary actions. The latest build adds a failure/retry card, but its install attempt returned CoreDevice error 4016 after the phone became unavailable. Full manual touch, audio, haptic, performance, and photo-generation testing remain open.

## What the app currently contains

- Portrait `HomeRoom` entry with the first-person camera, NavMesh movement, bounded look, pinch FOV zoom, Find Cat, and third-person comparison mode.
- Offline starter cat, call, feeding, ball play, petting, sleep, laser, and local save systems are present. Phone startup, autonomous window walk, and a completed ball sequence were observed in device logs.
- The iOS build is a development build so the local photo-generation panel is available. Its server address defaults to `http://127.0.0.1:3001`, which refers to the phone itself when installed. Change it to the Mac's reachable LAN address for a local test.
- The panel now calls the separate `/api/catme-generation` endpoint. The matching server module is `../clonecatme/server/catmeGeneration.js`; the browser prototype's existing Meshy route remains separate. This CatMe endpoint is mounted only in server development mode and is not a customer service.

## Tripo-only development flow

The local service normalizes the selected image in memory, strips EXIF metadata, uploads it to Tripo, then runs image-to-model → rig-check → quadruped rig → quadruped walk retarget. It validates the final GLB for a mesh, skeleton, and animation before saving it for the app. The original image is not written to the local service's disk. The app downloads the saved GLB, validates the file header and size, atomically replaces its cached cat, then reloads the room.

The server route and model list were smoke-tested locally. Server ESLint and `node --check` passed. No API key was inspected or disclosed; no provider job was started and no credits were used. A real photo run is still needed to validate Tripo account permissions, actual cost, identity likeness, quadruped rig, Unity import, and walk deformation.

Current official Tripo references checked on 2026-09-24:

- [Image to 3D model, v3](https://developers.tripo3d.ai/en/docs/generation-image-to-model/standard)
- [File upload](https://developers.tripo3d.ai/en/docs/files)
- [Riggability check](https://developers.tripo3d.ai/en/docs/animations-rig-check)
- [Auto rig](https://developers.tripo3d.ai/en/docs/animations-rig)
- [Animation retarget](https://developers.tripo3d.ai/en/docs/animations-retarget)
- [Quick Start and server-side key guidance](https://developers.tripo3d.ai/en/docs/quick-start)

The current quadruped retarget list includes `preset:quadruped:walk`; it does not list quadruped greet, eat, pounce, idle, or sleep presets. Keep using the existing restrained/procedural interactions for the prototype and plan authored cat-specific clips separately. Do not claim Tripo creates these motions.

## Owner phone test steps

1. The prior portrait app is installed on the paired iPhone. Unlock/reconnect it, then install the latest update from `Builds/iOS/CatMeHomeOptimized/CatMeHome.app` or build/run `Unity-iPhone` in Xcode with team `658662TU43`. Keep bundle ID `com.catme.home` and update in place; do not delete app data.
2. First test offline play without configuring generation: walk with the lower-left stick; drag elsewhere to look; pinch to zoom; use Find Cat; call the cat, feed it, then throw the ball and turn to follow. Try the same scene in third-person comparison mode.
3. For photo creation, start the sibling local server with a Node version meeting that project's `>=22.12.0` engine requirement and set `TRIPO_API_KEY` server-side. Keep the key out of Unity and the phone. Connect the iPhone and Mac to the same trusted private network, use the Mac's LAN URL in the panel, then submit only a photo you choose to send to Tripo. The local HTTP route is for this private development test only.
4. Record iPhone model/OS, portrait safe areas, movement/look/zoom comfort, call distance, ball tracking, feed flow, cat framing near furniture, frame pacing, audio and haptics, resume/offline behavior, photo job states, actual Tripo credits, and model validation. Keep Editor, service, and phone evidence separate.

## Still open

- Complete hands-on touch, audio, haptic, performance, and photo-flow checks on the installed app.
- First-person ball UI visual capture; the code compiles, but the new hand-ball and offscreen direction cue need an Editor preview.
- Review approach scale, furniture occlusion, safe areas, touch conflicts, and game feel on the physical phone.
- Customer-facing consent, privacy, deletion, authenticated ownership, durable job records, stable HTTPS staging, resumable downloads, and production photo service.
- App icon/splash and final customer UI. Xcode reports that the required 1024×1024 store icon is absent; no store package was requested or produced.
- One owner-selected cat-photo generation run followed by Tripo output review, actual cost logging, and Unity activation.
