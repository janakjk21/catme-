# How WornTag is built

WornTag (internal code name CatMe) is a landscape iPhone game in Unity 6000.6.2f1. A player makes a 3D copy of their cat or dog and of themselves from photos, then walks with them in a garden.

## The three parts

```
iPhone app (Unity)  --Firebase ID token-->  WornTag API (Railway, Node)  -->  Tripo (pets), Meshy (people)
        |                                          |
        +------ Firebase Auth ---------------------+------ Firestore + Cloud Storage (project catme-8a634)
```

| Part | Where | Job |
|---|---|---|
| App | this repo, `Assets/CatMe/` | screens, garden, photo picking, sign-in, download and load models |
| API | sibling folder `catme/worntag-api`, Railway service `api` at https://api-production-fbe0.up.railway.app | sessions, private uploads, creation jobs, the generation worker, signed model links |
| Firebase | project `catme-8a634` | sign-in (anonymous, Apple), Firestore records, Storage files |

Provider keys (Tripo, Meshy) exist only on the server.

## The app

**One scene, one map.** `Assets/CatMe/Scenes/GardenCompanion.unity` is the app. It loads the map `Scenes/Places/Place_HomeGarden_r2.unity` additively. The house and tent interiors come from `Resources/Rooms/HouseRoom_r3.prefab`. Nothing else is in the build.

| Folder | What is in it |
|---|---|
| `Scripts/Garden` | `GardenCompanionDemo` (starts everything, loads characters, HUD), `CatMeAppFlow` (screens and wiring), `DeviceBridge` (photo picker, Apple, Firebase account), camera, voice, UI style |
| `Scripts/Screens` | `OnboardingFlow`, pets, store, dialogs and toasts, `UiKit`, `UiIcons` |
| `Scripts/Onboarding` | `FirebaseAccountService`, `MobileIdentityService`, `MobileApi`, `CreationClient`, local profile, tutorial |
| `Scripts/Pets` | the pet list (`PetRoster`), cat and dog behaviour, photo checks, store catalogue |
| `Scripts/Session` | the walk: places, weather, lighting, interactions, the pet leading the player |
| `Scripts/World` | day cycle, butterflies, visiting dog, petting, haptics, sounds |
| `Editor` | iOS build, garden scene setup, render tools (`UiCapture`), Firebase probe |

**Models.** Bundled starter models live in `Assets/StreamingAssets/CompanionGarden/`. Downloaded models are saved per pet under the app's data folder and replace the starters.

## The server

Routes (all need a Firebase ID token except `/health`): `POST /v1/session`, `POST /v1/consent`, `POST /v1/uploads` (returns a signed upload link), `POST /v1/pets`, `GET /v1/pets/:id`, `POST /v1/owner`, `GET /v1/owner`, `GET /v1/house/frame`, `PUT /v1/house/frame`, `DELETE /v1/photos`, `DELETE /v1/account`.

House frame: `HousePhotoFrame` uses `IHouseFrameApi` through the authenticated server. Frame uploads use kind `frame` and private `users/{uid}/photos/frames/` paths; Firestore `users/{uid}.houseFrame` stores the current path. The server checks ownership, JPEG type, bytes and size before accepting the pointer. `HousePhotos/{uid}/frame.jpg` is an account-scoped cache; `.unsynced` preserves failed uploads for a nearby Save photo action. Remote restore cannot overwrite a newly selected local photo. No generation job or credit is involved.

A creation job runs: checking (the file is a real JPEG) -> queued -> generating -> ready. Pets: Tripo image to model, rig check, four-legged rig, walk. People: Meshy reference image, model, rig with walking clip. Without `GENERATION_MODE=live` the worker copies test models from Storage `fixtures/`.

Data:

| Firestore | Storage |
|---|---|
| `users/{uid}` (quota, active jobs) | `users/{uid}/photos/...` (private uploads) |
| `users/{uid}/pets/{petId}` (status, stage, model path) | `users/{uid}/models/{petId}/v1.glb` |
| `users/{uid}/owner/main` | `users/{uid}/avatar/v1.glb` |
| `jobs/{jobId}` (stage, provider task, done) | `fixtures/` (test models) |

Rules files: `firebase/firestore.rules`, `firebase/storage.rules` (written, not yet published).
