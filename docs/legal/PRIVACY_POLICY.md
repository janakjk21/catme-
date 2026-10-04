# WornTag Privacy Policy

> **DRAFT for review.** Written to match how the app and server work on 2026-10-04. Fill in every `[bracketed]` item, check the provider terms marked below, and have it reviewed by a lawyer before publishing at https://worntag.com/privacy. Update `CreationClient.ConsentVersion` in the app whenever this page changes in a way players must agree to again.

**Last updated:** [date of publishing]

WornTag ("we", "us") is a game where you walk in a garden with a 3D pet made from a photo of your pet, and a 3D you made from your selfie. This policy explains what we collect, why, who helps us, how long we keep it and how you can delete it.

**Who we are.** WornTag is operated by [legal name of the owner or company], [postal address], United Kingdom. Contact: [privacy@worntag.com]. We are the controller of your personal data under the UK GDPR.

## The short version

- You choose to give us a photo of your pet and a selfie. We use them only to make your 3D characters and check the photos are safe.
- Your photos and characters are private to your account. Nobody else in the game can see them.
- You can delete your photos, or your whole account, any time in **Settings**.
- No ads. No tracking across other apps. We never sell your data.
- Anonymous crash reports help us fix problems; you can turn them off in **Settings**.

## What we collect

| What | When | Why |
|---|---|---|
| **Pet photo** | when you choose it | to make your 3D pet |
| **Selfie** (only a head-and-shoulders crop leaves your phone) | when you take it | to make your 3D character |
| **3D models** made from your photos | after making them | so your characters load in the game and on a new phone signed in to the same account |
| **Pet names** and pet details (cat or dog, status) | when you name your pet | to show your pets |
| **House frame photo** (optional) | when you put a picture in the frame | to show it in your in-game house; it is never sent to an AI provider |
| **Account ID** | when you start ("Try it free"), or sign in with Google or Apple | to keep your pets in your account. If you sign in with Google or Apple we receive your account ID from them; [list any other fields actually stored, e.g. email if requested] |
| **Consent record** (time and version) | when you tap "I agree" | to prove you agreed before any photo was uploaded |
| **Technical data** (requests to our server, error logs) | while you play | to run the service, stop abuse and fix problems |
| **Crash reports and usage events** (what went wrong in a crash, device model and OS version, and a few anonymous events such as "reached the photo step", "pet ready" with how long it took, "first walk finished" and the reason code when a photo is refused) | while you play, unless you turn off **Share crash reports** in Settings | to find and fix crashes and see where new players get stuck. They carry no name, email, photo or account ID, and are not used for advertising. |

Photos you take in the game with the in-game camera, and your game settings, stay on your phone unless you choose to save a photo to your Photos.

**Faces.** Your phone checks that a selfie shows exactly one face and cuts out the head and shoulders. This happens on your phone and nothing about the face shape is stored. We do not use face recognition to identify you.

## Who helps us (processors)

We only share your photos with companies that help make the characters, and only for that purpose.

| Company | What they receive | Why |
|---|---|---|
| **Google Firebase** (Google LLC / Google Ireland) | account ID, photos, models, pet records; crash reports and anonymous usage events (Firebase Crashlytics, Google Analytics for Firebase) | sign-in, private storage and database (Firestore region `eur3`, Europe); finding crashes and where players get stuck |
| **Railway** (Railway Corp.) | requests to our server while it works | hosts our server |
| **OpenAI** (OpenAI, L.L.C.) | your pet photo or selfie | checks that a photo is safe and shows a cat, a dog or one person before anything is made. OpenAI does not use data sent through its API to train its models by default. |
| **Tripo** ([legal entity of Tripo3D]) | your pet photo | makes the 3D pet and its walk |
| **Meshy** ([legal entity of Meshy]) | your selfie crop | makes the 3D you and its walk. Meshy uses an image model ([OpenAI gpt-image]) as one step. |
| **Apple, Google** | — | if you choose to sign in with them, under their own privacy policies |

[Check before publishing: Tripo's and Meshy's API terms on data retention and whether API uploads are used for training; record what you find here.] Some of these companies are in the United States. Where your data leaves the UK we rely on the UK International Data Transfer Agreement or Addendum, or the UK–US data bridge where it applies.

## How long we keep it

| What | How long |
|---|---|
| A photo that fails the safety or pet check | deleted straight away |
| Pet photos and selfies | until you tap **Delete my photos** or delete your account [or: automatically after 30 days, once auto-deletion is built] |
| 3D models, pet names, house frame photo | until you delete your account |
| Consent record | for as long as your account exists, then deleted with it |
| Server logs | up to [30] days |
| Crash reports | 90 days (Crashlytics default) |
| Usage events | [2 months: set "Event data retention" in Google Analytics to 2 months] |

Provider copies are kept under their own retention terms [fill in from their terms].

## Your choices and rights

- **Delete my photos** (Settings): removes your photos from our server. Your pets stay.
- **Delete account** (Settings or Account): removes your account, photos, models, pets and records for good.
- You also have the right to ask for a copy of your data, to correct it, to object, and to complain to the UK Information Commissioner's Office (ico.org.uk). Write to [privacy@worntag.com]; we reply within one month.
- A guest account ("Try it free") lives on your phone. If you delete the app without signing in, we cannot return your pets.

**Our legal basis:** your consent for photos and characters (you can withdraw it any time by deleting your photos or account); performance of our agreement with you to run your account; our legitimate interest in keeping the service safe and fixing crashes (you can object by turning off **Share crash reports** in Settings).

## Children

WornTag is not for children under 13, and you must not upload photos of other people, including children. If you think a child has given us data, write to us and we will delete it.

## Security

Photos and models are stored privately in your account. Every request needs your sign-in; download links last 15 minutes; provider keys never reach the app.

## Changes

If we change this policy in a way that affects your photos, the app will ask you to agree again before your next upload.

**Contact:** [privacy@worntag.com] · [postal address]
