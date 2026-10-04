# Testing and quality

## Before calling any change done

1. **Compiles.** Unity batch mode, or the generated C# projects. 0 errors.
2. **Renders.** New or changed screens rendered with `UiCapture` and compared with the design.
3. **Plays.** The changed path played in Play Mode in `GardenCompanion.unity`.
4. **Phone.** Anything touching input, camera, photos, sign-in, performance or layout is checked on a physical iPhone before the milestone is called done.
5. **Server.** `npx vitest run` in `worntag-api` passes after any server change.

EditMode tests in `Assets/CatMe/Tests` cover server-pet roster merging, mobile API error messages, generated-character scale calculations, and resuming an existing creation without another create request. Run them from Test Runner before the Play Mode checklist.

## Editor checklist (onboarding)

- [ ] Login shows; Try it free goes to Cat or dog.
- [ ] Cat and Dog each lead to the right photo screen and tips.
- [ ] Choosing a photo opens the file dialog; a valid photo goes to the selfie.
- [ ] Selfie goes to the garden with the "being made" toast.
- [ ] Test models arrive; "ready" toast; name; reveal; garden shows the new pet.
- [ ] Relaunch: login skipped, straight to the garden.
- [ ] Pause > Account > Sign out returns the login next time.

## Phone checklist

- [ ] Fits the screen in both landscape directions; nothing under the notch or home bar.
- [ ] Photo library and camera both work; permission refusal shows a clear message.
- [ ] Drag to walk, pet button, tap and hold the pet, bench and tent buttons.
- [ ] Sound, vibration and reduce motion settings work.
- [ ] Airplane mode: clear message, nothing stuck; back online resumes.
- [ ] App closed while a pet is being made: reopened, the pet still arrives.
- [ ] 15 minutes of play: no crash, phone not too hot.

## Odd cases to handle

| Case | Expected |
|---|---|
| No internet at sign-in or upload | message, Try again |
| App killed mid-creation | the same server job continues; the pet arrives later (idempotency key) |
| Photo rejected | reason shown, choose another photo, no credit used |
| Quota used up | clear message (store later) |
| Server down | message, the garden still opens with existing pets |

## Budgets

| Thing | Limit |
|---|---|
| Frame rate | 60 fps target on iPhone 12 and newer; never below 30 |
| Memory | under 1.2 GB on device |
| One generated model | 60 MB hard cap on the server; aim for under 25 MB |
| Starter models in the app | cat 19.6 MB, person walking 27 MB, person call 27 MB today (`StreamingAssets/CompanionGarden`) |
| App download | aim for under 300 MB |
| Cold start to login | under 5 seconds |

The fps, memory and app-size figures are targets; they have not been measured on a phone yet.
