# Setup and running

## Run in the Unity editor

1. Open the project in Unity 6000.6.2f1 and open `Assets/CatMe/Scenes/GardenCompanion.unity`.
2. Press Play. The login page appears. Tap **Try it free**.
3. If sign-in says accounts are still starting: macOS may have blocked Firebase's native files. Run `xattr -dr com.apple.quarantine Assets/Firebase Assets/Plugins`, then restart Unity. Firebase can be checked: run `CatMe.Editor.FirebaseProbe.Run` in batch mode.
4. To see the login again after signing in, use Account in the pause menu and sign out.

## Firebase (project `catme-8a634`)

| Piece | State |
|---|---|
| Plan | Blaze (pay as you go) |
| Authentication providers | Anonymous (Try it free) and Apple are used. Email/Password, Google and Game Center are also switched on in the console but not used by the app. |
| iOS app | `com.catme.home`; its config file is `GoogleService-Info.plist` at the project root and in `Assets/` (Unity copies it into the build) |
| Firestore | default database, region `eur3`; layout in `ARCHITECTURE.md` |
| Storage | bucket `catme-8a634.firebasestorage.app`; test models in `fixtures/cat-walking.glb` and `fixtures/human-walking.glb` |
| Rules | `firebase/firestore.rules`, `firebase/storage.rules`, `firebase/firestore.indexes.json`, listed in `firebase.json`. Written and dry-run checked, **not published yet** (publish with `firebase deploy --only firestore:rules,storage` after the owner approves) |
| Admin key for the server | Firebase console > Project settings > Service accounts > Generate new private key. Put its fields into Railway (below), then delete the downloaded file. Never commit it. |
| Unity SDK | Firebase Unity SDK 13.17.0 (Auth, Firestore, Storage) under `Assets/Firebase` |

Firebase CLI: `npx firebase-tools login`, then `npx firebase-tools projects:list` to check access.

Apple sign-in also needs, in Firebase > Authentication > Apple: your Apple Team ID, and a Sign in with Apple key (Key ID and `.p8`) so a deleted account can revoke its Apple token.

## Server (Railway service `api`)

Code: sibling folder `catme/worntag-api` (Node, Express, Firebase Admin), GitHub repository `janakjk21/catme-ai`. Railway project `worntag-api`, service `api`, address https://api-production-fbe0.up.railway.app. The service deploys from `catme-ai/main`.

First-time setup: `npm install`, then `npx @railway/cli login` and `npx @railway/cli link` to the project.

Set these as separate variables on the service. Never commit them.

| Variable | Value |
|---|---|
| `CATME_MOBILE_API_ENABLED` | `true` |
| `FIREBASE_SERVICE_ACCOUNT_JSON`, or `project_id` + `client_email` + `private_key` | Firebase Admin key for `catme-8a634` |
| `CATME_STORAGE_BUCKET` | `catme-8a634.firebasestorage.app` |
| `CATME_FREE_PETS`, `CATME_FREE_OWNERS` | free creations per account (default 2 and 1) |
| `GENERATION_MODE` | `live` for real models; leave unset for test models |
| `TRIPO_API_KEY`, `MESHY_API_KEY` | provider keys (live mode). The server logs a free key check at start: `generation: {...}` |
| `GENERATION_DOGS` | `1` to let dogs spend Tripo credits; otherwise dogs use the built-in corgi |

Deploy from `catme/worntag-api`: `npx @railway/cli up --service api`. Check: `GET /health`. Tests: `npx vitest run`.

The app finds the server through `Assets/CatMe/Resources/MobileApiConfig.asset` (`baseUrl`, HTTPS only).

## iPhone build and signing

Step by step for a phone build: `XCODE_BUILD.md`.

Build with `CatMe > Build iOS Development Xcode Project` (or `CatMe.Editor.IOSDevelopmentBuild.Build` in batch), then open the generated `.xcworkspace` in Xcode.

| Variable (set before building) | Effect |
|---|---|
| `WORNTAG_APPLE_TEAM_ID` | your 10-character Apple Team ID; turns on automatic signing |
| `WORNTAG_PAID_TEAM=1` | adds Sign in with Apple and Game Center (paid Apple Developer Program only) |
| `WORNTAG_LOCAL_HTTP=1` | allows plain HTTP, only for a local test server |

Bundle id `com.catme.home`. A free personal team can install the app and use Try it free; Apple sign-in needs the paid program, the App ID capability, and the Apple provider in Firebase.

## Never commit

Generated GLBs, `LocalFixtures/`, `.local-private/`, provider keys, Firebase service-account files, signing material, `Library/`, `Temp/`, `Logs/`, `Builds/`.
