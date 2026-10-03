# Decisions

Short, lasting decisions. Replace an entry when it changes; do not append history.

- **Brand.** The product is WornTag. "CatMe" stays only in code names, the bundle id `com.catme.home` and Firebase project `catme-8a634`.
- **One flow.** Login, cat or dog, pet photo, selfie, garden; name and reveal when the pet is ready. No skipping, no email login. Spec: `docs/ONBOARDING_FLOW.md`.
- **One scene, one map.** `GardenCompanion.unity` with `Place_HomeGarden_r2.unity`. No other scenes in the build.
- **Generation on the server only.** The phone uploads photos to the WornTag API; Tripo and Meshy keys never reach the phone.
- **Sign-in.** Firebase anonymous ("Try it free") first; Apple when the paid team exists; Google later.
- **Look.** Warm bone background, worn ink text, old brass accent; Fraunces headings, DM Sans text. Shared pieces in `UiKit` and `CatMeUIStyle`.
- **Photos and consent.** The selfie comes only from the front camera, cropped to one face on the phone. Pet photos are checked on the phone (cat or dog) and on the server (OpenAI moderation and vision) before any credits are spent; refused photos are deleted. Nothing is uploaded before the player agrees on the consent screen; the consent version is raised when the policies change.

