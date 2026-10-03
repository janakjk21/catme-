# Accounts and privacy

How a player's identity, photos and models are handled, and what we promise. "Not built" marks what the app still has to do.

## Accounts

| Way in | How | State |
|---|---|---|
| Try it free | Firebase anonymous account on this phone | built |
| Continue with Apple | native Apple sheet -> Firebase Apple credential | built; needs the paid Apple team to work on a phone |
| Continue with Google | native Google Sign-In (iPhone) -> Firebase Google credential | built 2026-10-03; works on the free Apple team; first phone test pending |

- After any sign-in the app calls `POST /v1/session`, which creates the user record and the free quota.
- A saved sign-in is restored at launch; the login page is skipped.
- **Pause > Account:** signed-in players see "You are signed in" and **Sign out**; guests see the login page with a Close button.
- **Guest to Google or Apple:** a guest who then signs in is linked (`LinkWithCredential` in `FirebaseAccountService`), so their pets stay. If that Google or Apple account already belongs to another WornTag account, the sign-in fails with a message; merging is not built.
- A guest's pets are lost if the app is deleted. The login page says so: "Signing in saves your pet if you get a new phone."

## What we keep

| Data | Where | Why |
|---|---|---|
| Pet photo, selfie | Storage `users/{uid}/photos/` | to make the 3D copies |
| 3D models | Storage `users/{uid}/models/`, `users/{uid}/avatar/` | to load them on any phone |
| Pet name, species, status | Firestore `users/{uid}/pets/{petId}` | the pet list |
| Consent time and version | Firestore `users/{uid}` | proof the player agreed to the photo upload |
| Copies on the phone | app data folder (photos, downloaded models) | play offline |
| House frame photo | private Storage `users/{uid}/photos/frames/`, metadata `users/{uid}.houseFrame`, local `HousePhotos/{uid}/frame.jpg` | restore the same picture next time or on another phone signed into the same account; never sent to a generation provider |

Photos go only to our server and the 3D provider that makes the model (Tripo for pets, Meshy for people). Nothing is public; every file link expires after 15 minutes.

## Deleting photos and accounts

The selfie screen says: "Your photos are private. You can delete them any time in Settings." The app now has both Settings actions and uses the shared confirmation dialog. The server routes are implemented locally and covered by fake Firebase tests. The app has compiled, but Settings deletion still needs a Play Mode test with a throwaway guest and a local server before this task is done. The server changes also need an owner-approved deployment before they work against Railway.

1. **Delete my photos** (Settings): `DELETE /v1/photos` deletes `users/{uid}/photos/*` after any active creation finishes. The server clears `houseFrame`; the app deletes pet photo copies, drafts and cached house photos. Models and pets stay.
2. **Delete account** (Settings or Account): `DELETE /v1/account` locks the account, cancels jobs, deletes the user's Storage folder, Firestore records and jobs, then the Firebase user. The app clears account data and returns to Login.
3. **Apple accounts:** the server currently refuses deletion with `APPLE_REAUTH_REQUIRED` before removing anything. Fresh Apple authorization and token revocation need the paid Apple team and Firebase Apple configuration. The app must complete that flow before Apple accounts can be offered this action.

See `context/CLAUDE_TASKS.md` task 1. The older roadmap calls this task 2.

## Consent

Before the first upload the app sends `POST /v1/consent` with the consent version (`CreationClient.ConsentVersion`). The server refuses uploads without it. For the house frame, choosing a picture means saving it to this account (`house-frame-v1`); it is never sent for generation. Anonymous guest accounts can save, but another phone needs the same linked account to restore. The privacy note on the selfie screen is the player-facing wording; a full privacy policy page is needed before release (`RELEASE.md`).
