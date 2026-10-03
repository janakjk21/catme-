# Release

Nothing here is done yet. Roadmap tasks 12 and 13.

## Needed before TestFlight

- [ ] Paid Apple Developer Program team; Team ID set (`WORNTAG_APPLE_TEAM_ID`).
- [ ] Final bundle id decided (today `com.catme.home`; changing it later means a new Firebase iOS app).
- [ ] App record in App Store Connect.
- [ ] Build number raised for every upload.
- [ ] Release build (not development) archived and uploaded from Xcode.
- [ ] Server live mode on, spend cap agreed, Firebase rules published.
- [ ] Account deletion built (`ACCOUNTS_AND_PRIVACY.md`); Apple requires it.

## Needed before the App Store

- [ ] Name WornTag, subtitle, description, keywords, screenshots (landscape iPhone), app icon.
- [ ] Privacy policy and terms published at worntag.com/privacy and worntag.com/terms (drafts in `docs/legal/`, need the bracketed details and a legal review).
- [ ] Privacy labels: Photos (linked to the user, used for app functionality), User ID, no tracking.
- [ ] Age rating questionnaire.
- [ ] Sign in with Apple offered, since another sign-in (Google) will be offered.
- [ ] In-app purchases set up and reviewed, if the store is paid at launch.
- [ ] Review notes: how to sign in (Try it free) and that models take a few minutes.

## Signing

See `SETUP.md` (iPhone build and signing). Signing keys, certificates and profiles never go in git.

## App Store privacy answers (draft, from `docs/legal/PRIVACY_POLICY.md`)

| Data type | Collected | Linked to the user | Used for tracking | Purpose |
|---|---|---|---|---|
| Photos or videos (pet photo, selfie crop, frame photo) | yes | yes | no | App functionality |
| User ID (Firebase account ID) | yes | yes | no | App functionality |
| Other user content (pet names) | yes | yes | no | App functionality |
| Crash data | only if Crashlytics is added | no | no | App functionality |

No tracking, no advertising data, no data sold. Review notes: start with "Try it free"; making a pet takes a few minutes; the selfie uses the front camera only.

