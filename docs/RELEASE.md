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
- [ ] Privacy policy URL (worntag.com) covering photos, 3D providers (Tripo, Meshy), Firebase, deletion.
- [ ] Privacy labels: Photos (linked to the user, used for app functionality), User ID, no tracking.
- [ ] Age rating questionnaire.
- [ ] Sign in with Apple offered, since another sign-in (Google) will be offered.
- [ ] In-app purchases set up and reviewed, if the store is paid at launch.
- [ ] Review notes: how to sign in (Try it free) and that models take a few minutes.

## Signing

See `SETUP.md` (iPhone build and signing). Signing keys, certificates and profiles never go in git.
