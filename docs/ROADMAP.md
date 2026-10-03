# Roadmap

The single ordered task list. Work only on the current task (named in `context/STATUS.md`). A task is done only when every check under it passes. Add or reorder tasks here first, with the owner's agreement.

| # | Task | Follows | Done when |
|---|---|---|---|
| Current owner request | **Cat leads to an account-saved house frame** | `GAMEPLAY_AND_WORLD.md` | Cat leads toward the frame; the shared contextual button opens the picker nearby; private upload and Firestore metadata restore the photo next time. Local/fake API tests and Play Mode pass. Production deployment and phone verification are separate owner gates. |
| 1 | **Play the onboarding in the editor** | `ONBOARDING_FLOW.md` | Play Mode from Login to Garden with Try it free and the server's test models: every screen matches the design, the pet and person load in the garden, the ready toast leads to name and reveal. Fix what breaks. |
| 2 | **Delete account and photos** | `ACCOUNTS_AND_PRIVACY.md` | Settings has Delete my photos and Delete account; the server removes the user's files and records; tested with a throwaway account. |
| 3 | **Publish Firebase rules** | `SETUP.md` | Owner approves; rules deployed; app and server still work. |
| 4 | **One real cat** | `PHOTO_TO_3D.md` | Owner sets `GENERATION_MODE=live` and keys; one real cat and one real person made and walking in the garden. Credits spent recorded in STATUS. |
| 5 | **iPhone test** | `QUALITY.md` | The full flow on a physical iPhone passes the phone checklist. |
| 6 | **Apple sign-in** | `SETUP.md` | Paid Apple Developer team, App ID capability, Firebase Apple provider; sign in, sign out, sign back in on the phone. A guest can link to Apple without losing pets. |
| 7 | **Photo quality check** | `PHOTO_TO_3D.md` | Server rejects wrong animal, several subjects, too dark or blurry before spending credits; clear message in the app. |
| 8 | **Gameplay words** | `COPY.md` | All tutorial, status, settings and error text in the app matches `COPY.md`. |
| 9 | **Real dogs** | `PHOTO_TO_3D.md` | Dog path tested on several breeds; `GENERATION_DOGS=1`; dogs load and walk. |
| 10 | **Google sign-in** | `ACCOUNTS_AND_PRIVACY.md` | Native Google sign-in on iPhone through Firebase. |
| 11 | **Store and payments** | `PETS_AND_STORE.md` | Owner decides prices; App Store in-app purchases; server-side receipt check and credit wallet. |
| 12 | **TestFlight** | `RELEASE.md` | Build uploaded, testers invited, release checklist passes. |
| 13 | **App Store** | `RELEASE.md` | Listing, privacy labels, review submitted. |

Tasks 1 to 5 need no new owner decisions except the provider keys and spend for task 4.
