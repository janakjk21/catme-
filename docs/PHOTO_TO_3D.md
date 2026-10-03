# From photo to 3D pet

Every step from the moment the player picks a photo to the 3D pet walking in the garden. The phone only talks to the WornTag API; the API talks to Firebase and to the 3D providers.

## The steps

```
PHONE                                   API (Railway)                          PROVIDERS
1 pick photo, shrink to 1536 px JPEG
2 sign in (Firebase)  -- ID token -->   3 /session: create user, free quota
4 /consent                              record photo consent
5 /uploads            <-- signed link   15-minute write link to users/{uid}/photos/{id}.jpg
6 PUT photo straight to Storage
7 /pets (name, species, photo, key)     8 create pet + job, start worker
                                        9 checking: file exists, is a JPEG
                                       10 generating:                   -->  Tripo (cat, dog) / Meshy (person)
                                       11 save GLB to users/{uid}/models/{petId}/v1.glb
                                       12 ready, take one credit
13 poll /pets/{id} every 2 s           returns stage, and when ready a 15-minute read link
14 download GLB, check size and MD5
15 save in the pet's own folder, garden loads it
```

The selfie runs the same way through `/owner` and lands at `users/{uid}/avatar/v1.glb`. In the app the pet job starts as soon as the pet photo is chosen, and the selfie joins the same run when it is taken (`CreationRequest.ownerPhotoTask`).

## Inside "generating" (server, `worntag-api/mobile/live.js`)

| Who | Steps | Result |
|---|---|---|
| Cat or dog (all on Tripo) | photo -> Tripo image step: one clean, standing, full-body image of the same pet (prompts in `mobile/petPrompts.js`; the cat prompt is condensed to 1,672 characters to fit Tripo's 1,800-character limit while keeping its identity, coat and pose constraints) -> Tripo image to model from that image -> rig check (must be riggable, four-legged) -> four-legged rig -> walk animation | walking GLB |
| Person (Meshy) | photo -> clean full-body reference image (single image, not multi-view; the person prompt from the earlier web pipeline, kept word for word in `mobile/ownerPrompt.js`) -> image to 3D -> rig with a walking clip | walking GLB |

- After every step the provider task id and phase are saved on the job, so a server restart continues the same task and never pays twice.
- The worker checks the provider every 6 seconds and gives up after 30 minutes.
- Model downloads are accepted only from provider hosts, up to 60 MB, and must start with the GLB header.
- **Why extraction first:** two phone runs sent the raw photo straight to Tripo and were rejected at the rig check (`pet_not_riggable`): a sitting or cropped cat is not a clean four-legged pose. The Tripo image step redraws the pet standing first (about 5 credits).
- **Live since 2026-10-03.** First real run (photo straight to Tripo): cat 5.7 MB textured walking GLB; Meshy person 43.7 MB walking GLB. Both load in the garden. Meshy spent 71 credits on that first person, including one failed attempt.
- **Test mode** (no `GENERATION_MODE=live`): the worker copies `fixtures/cat-walking.glb` or `fixtures/human-walking.glb` from Storage instead. Dogs without `GENERATION_DOGS=1` get the built-in corgi.

## What the player sees

| Server stage | In the app |
|---|---|
| checking, queued, generating | the Making screen (four steps, turning dots) |
| ready | name, reveal, then the garden |
| rejected | a message saying what to fix (too dark, no cat found, more than one, could not build a walking body) and Try again |
| failed | "We couldn't make your pet this time." and Try again |

Rejections and failures do not reduce the account's creation quota. A provider may still charge for work started before the failure.

## Limits and safety

- Code defaults for new accounts are 2 pets and 1 person (`CATME_FREE_PETS`, `CATME_FREE_OWNERS`). Railway currently sets both to 1,000 for new accounts; the owner reports that all 11 existing accounts were also raised to 1,000. One pet job and one person job at a time.
- Rate limits: 60 requests a minute; 10 costly requests (uploads, creations) an hour per account.
- Photos: JPEG under 8 MB. Each creation sends an idempotency key, so a retry never creates a second job.
- Every route needs a Firebase ID token. Photos and models are private to their owner; links last 15 minutes.

## Where the code is

| Step | File |
|---|---|
| Picking and saving the photo | app `Scripts/Garden/DeviceBridge.cs` |
| Upload, create, poll, download | app `Scripts/Onboarding/CreationClient.cs`, `MobileApi.cs` |
| Saving the model per pet, loading it | app `Scripts/Garden/CatMeAppFlow.cs` (`OnCreated`), `GardenCompanionDemo.LoadCharacters` |
| Routes | server `mobile/index.js` |
| Job runner | server `mobile/worker.js` |
| Provider steps | server `mobile/live.js`, `mobile/providers.js` |
| Tests | server `mobile/*.test.js` (`npx vitest run`) |

## Not built yet

- A vision check (right animal, one subject, light, focus) before generating; today only the file type is checked.
- A real dog model per player; the dog path through Tripo is written but off by default.
