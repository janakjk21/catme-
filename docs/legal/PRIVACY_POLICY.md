# WornTag Privacy Policy

> **DRAFT for review.** Written against the app and server code on 2026-10-04. Review the provider terms, deployment settings and legal wording before publishing at https://worntag.com/privacy. Update `CreationClient.ConsentVersion` in the app whenever this page changes in a way players must agree to again.

**Last updated:** [date of publishing]

WornTag ("we", "us") is a game where you walk in a garden with a 3D pet made from a photo of your pet, and a 3D you made from your selfie. This policy explains what we collect, why, who helps us, how long we keep it and how you can delete it.

**Who we are.** WornTag is operated by Janakjk Sapkota, 2 Gorse Rise, NG3 1BS, United Kingdom. Contact: privacy@worntag.com. We are the controller of your personal data under the UK GDPR.

## The short version

- You choose to give us a photo of your pet and a selfie. We use them only to make your 3D characters and check the photos are safe.
- Your photos and characters are private to your account. Nobody else in the game can see them.
- You can delete your photos, or your whole account, any time in **Settings**.
- No ads. No tracking across other apps. We never sell your data.
- The app records a small set of onboarding events on your phone. The optional Firebase crash-reporting and event SDKs are not currently included in the app build; Settings lets you turn off collection if they are added.

## What we collect

| What | When | Why |
|---|---|---|
| **Pet photo** | when you choose it | to make your 3D pet |
| **Selfie** (only a head-and-shoulders crop leaves your phone) | when you take it | to make your 3D character |
| **3D models** made from your photos | after making them | so your characters load in the game and on a new phone signed in to the same account |
| **Pet names** and pet details (cat or dog, status) | when you name your pet | to show your pets |
| **House frame photo** (optional) | when you put a picture in the frame | to show it in your in-game house; it is never sent to an AI provider |
| **Account ID and sign-in details** | when you start as a guest or sign in | Firebase Authentication holds the account ID and, for a linked sign-in, the provider and any email or profile details returned by that provider. Our server stores the account ID, account creation time, guest status, creation quotas, consent time and version, active job IDs and, if used, the house-frame photo path and update time. It does not copy your email or display name into its user record. |
| **Consent record** (time and version) | when you tap "I agree" | to prove you agreed before any photo was uploaded |
| **Technical data** (requests to our server, error logs) | while you play | to run the service, stop abuse and fix problems |
| **On-device telemetry** (up to 30 recent event names and short values for onboarding step, creation start/result, time to ready and first walk) | while you play | to help diagnose the flow locally. Development builds also write these events to the device log. The app does not attach your name, email, photo or account ID to these events. The Firebase Analytics and Crashlytics SDKs are absent from the current build, so these events and crash reports are not sent to them. |

Photos you take in the game with the in-game camera, and your game settings, stay on your phone unless you choose to save a photo to your Photos.

**Faces.** Your phone checks that a selfie shows exactly one face and cuts out the head and shoulders. This happens on your phone and nothing about the face shape is stored. We do not use face recognition to identify you.

## Who helps us (processors)

We only share your photos with companies that help make the characters, and only for that purpose.

| Company | What they receive | Why |
|---|---|---|
| **Google Firebase** (Google LLC / Google Ireland) | account ID and sign-in details, photos, models, pet records, consent, quotas and house-frame photo | sign-in, private storage and database (Firestore region `eur3`, Europe) |
| **Railway** (Railway Corp.) | requests to our server while it works | hosts our server |
| **OpenAI** (OpenAI, L.L.C.) | your pet photo or selfie, sent directly by our server | checks that a photo is safe and shows a cat, a dog or one person before generation. OpenAI does not use API data to train its models by default. |
| **Tripo** (Holymolly Limited) | your pet photo | redraws it through Tripo's `chat_image_2` (ChatGPT image) step, then makes the 3D pet and its walk. Our server sends the photo to Tripo, not directly to OpenAI for this image step. |
| **Meshy** (Meshy LLC) | your head-and-shoulders selfie crop | makes an image reference with Meshy's `gpt-image-2` (OpenAI GPT Image 2) option, then the 3D you and its walk. Our server sends the crop to Meshy for this step. |
| **Apple, Google** | — | if you choose to sign in with them, under their own privacy policies |

Tripo's terms say paid users' inputs and outputs are not used to train its AI technology; that protection depends on the account plan used for generation. Its privacy policy gives no fixed retention period for saved inputs and outputs and describes deletion with the relevant asset or account, subject to exceptions. Meshy's terms permit training on non-enterprise customer inputs and outputs unless a separate agreement says otherwise. Meshy says non-enterprise API outputs are deleted after three days, but does not give that same three-day promise for input images. Its terms also restrict submitting personally identifiable information, so the selfie workflow needs a confirmed written provider arrangement before this policy or the feature is published. These are provider terms, not a promise that deleting a WornTag photo also deletes a provider copy. Some providers process data outside the UK; the applicable transfer safeguards must be confirmed before publication.

<!-- Provider sources checked 2026-10-04: https://www.tripo3d.ai/terms ; https://www.tripo3d.ai/privacy ; https://www.meshy.ai/terms-of-use ; https://www.meshy.ai/privacy-policy ; https://docs.meshy.ai/en/api/image-to-image -->

## How long we keep it

| What | How long |
|---|---|
| A photo that fails the safety or pet check | deleted straight away |
| Pet photos and selfies | until you tap **Delete my photos** or delete your account |
| 3D models, pet names, house frame photo | until you delete your account |
| Consent record | for as long as your account exists, then deleted with it |
| Server error logs | subject to the hosting provider's operational retention settings; no fixed period is configured in this server code |
| On-device telemetry | only the 30 most recent events are held in app memory; they disappear when the app closes |

Provider copies follow the provider terms described above. WornTag's photo and account deletion endpoints remove the copies in our Firebase Storage; they do not directly remove a provider's copies.

## Your choices and rights

- **Delete my photos** (Settings): removes your photos from our server. Your pets stay.
- **Delete account** (Settings or Account): removes your account, photos, models, pets and records for good.
- You also have the right to ask for a copy of your data, to correct it, to object, and to complain to the UK Information Commissioner's Office (ico.org.uk). Write to privacy@worntag.com; we reply within one month.
- A guest account ("Try it free") lives on your phone. If you delete the app without signing in, we cannot return your pets.

**Our legal basis:** your consent for photos and characters (you can withdraw it any time by deleting your photos or account); performance of our agreement with you to run your account; our legitimate interest in keeping the service safe and fixing crashes (you can object by turning off **Share crash reports** in Settings).

## Children

WornTag is not for children under 13, and you must not upload photos of other people, including children. If you think a child has given us data, write to us and we will delete it.

## Security

Photos and models are stored privately in your account. Every request needs your sign-in; download links last 15 minutes; provider keys never reach the app.

## Changes

If we change this policy in a way that affects your photos, the app will ask you to agree again before your next upload.

**Contact:** privacy@worntag.com · Janakjk Sapkota, 2 Gorse Rise, NG3 1BS, United Kingdom
