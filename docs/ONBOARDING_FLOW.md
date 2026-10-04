# Onboarding flow

The one flow every agent builds and tests against. Approved design: https://claude.ai/artifact/BQxtq2FK8TeEqgXXXNbL1M (landscape, cat row and dog row).

## The journey

```
Login  ->  Cat or dog  ->  Pet photo  ->  Selfie  ->  Making (wait)  ->  Name  ->  Reveal  ->  Garden
```

A player is never asked to skip. There is no Back, Close or Skip once the pet photo is chosen.

## Screens and words

All screens are landscape, picture on the left, words and buttons on the right.

| # | Screen | Words | Buttons |
|---|---|---|---|
| 1 | Login | WornTag. "Your pet, in a garden, with you." "Show us a photo of your pet. We make a little 3D pet that looks just like them. Then you can walk together." Note: "Signing in saves your pet if you get a new phone." | Continue with Apple, Continue with Google, link "Try it free, no sign-in" |
| 2 | Cat or dog | "Who is coming on the walk?" "Pick one. You can add more pets later." | Two cards: Cat, Dog |
| 3 | Pet photo | "Show us your cat" (or dog). "Take or pick one photo. We use it to build a 3D copy." Tips: whole face (dog: "from nose to tail"), lots of light, only your pet | Choose a photo, link "Take a photo instead" |
| 3a | Consent (first photo only, again when `ConsentVersion` changes) | "Before we make your pet". Ticks: your photos go to our 3D partners to build your pet and you; they stay private to your account; delete them any time in Settings; only use photos of your own pet and yourself. Small print names Tripo, Meshy and OpenAI, says results are made by AI, and that agreeing confirms 13 or older. | I agree (then the photo picker opens), Not now, links Privacy policy and Terms and AI policy |
| 4 | Selfie | "Now show us you." "One happy photo of your face. We make a little 3D you to walk with your pet." Tips: face clear, lots of light, only you. Note: "Your photos are private. You can delete them any time in Settings." | Take a selfie (front camera only; "Open Settings" link if camera access was refused) |
| 5 | Making | "[Name] is being made". Four steps listed: Checking the photo, Waiting in line, Building a 3D copy, Almost ready. A ring of dots turns around the pet they already have. Note: "This takes a few minutes. Your garden opens when it is ready." | none (no Back, no Close) |
| 6 | Name | "What is their name?" "Pick a name. You can change it later." Three suggested names | That's their name |
| 7 | Reveal | "Look who is here! Meet [Name]." "Say hello to [Name]. You can walk together now." | Say hello (opens the garden with the new pet and person loaded) |

If making fails: the screen says "We could not make [Name]" with the reason, **Try again** (same photos) and "Choose another photo". The player stays on this screen; the garden opens only after the pet and the person have arrived.

## Rules

- **Sign-in.** Try it free (Firebase anonymous) works everywhere. Google works on iPhone (not in the editor). Apple needs a paid Apple Developer team; without one it shows a clear message. A guest who signs in with Google or Apple is linked, so their pets are kept. There is no email login.
- **Already signed in.** Open a server session, merge account pets missing from the local roster, and restore their models plus the owner model from fresh signed links. Then go to the garden if the player has a pet, otherwise to Cat or dog.
- **Selfie only (iPhone).** The selfie comes from the front camera; there is no library option. It must show exactly one face; the phone keeps only the head and shoulders and uploads that crop. No face, several faces or a tiny face keeps the player on the selfie screen with a message from `COPY.md`. If camera access was refused, the screen says so and offers "Open Settings". The editor uses a file dialog without these checks.
- **Pet photo check (iPhone).** Apple's on-device animal detector looks for a cat or a dog. The wrong animal ("This looks like a dog…") or more than one pet is refused with "Choose another" only. If it sees no animal at all the player may still "Use it anyway", because the server makes the final decision (`PHOTO_TO_3D.md`, Photo checks).
- **Making.** The pet's server job starts as soon as the pet photo is chosen; the selfie joins the same job when taken, and the player waits on the Making screen until both are downloaded. The phone never calls Meshy or Tripo.
- **Account in the pause menu** opens the same login page with a Close button, or "You are signed in" with Sign out and Delete account. Settings also offers Delete my photos and Delete account, each using the shared confirmation dialog. Photo deletion keeps models and pets; account deletion clears local account data and returns to Login.
- **Words** are short, plain and active, readable by a child.
- **Inside the house after onboarding:** the cat leads toward the large wall frame. When the player walks close, the shared contextual button says "Add a photo" or "Change photo". It opens the existing photo picker. Keep a local copy and save the photo to the authenticated account through the API; restore it when the room opens again. No generation job is created. Cancel keeps the current picture. A failed cloud save keeps the local photo and offers "Save photo" to retry.

## Where it lives

| Part | File |
|---|---|
| Every onboarding screen | `Assets/CatMe/Scripts/Screens/OnboardingFlow.cs` |
| Wiring: sign-in, server, garden, toasts | `Assets/CatMe/Scripts/Garden/CatMeAppFlow.cs` |
| Photo picker, Apple sign-in, Firebase account | `Assets/CatMe/Scripts/Garden/DeviceBridge.cs` |
| Upload, create, wait, download | `Assets/CatMe/Scripts/Onboarding/CreationClient.cs`, `MobileApi.cs` |
| Shared look (buttons, cards, icons) | `Assets/CatMe/Scripts/Screens/UiKit.cs`, `UiIcons.cs`, `Garden/CatMeUIStyle.cs` |

## Done means

1. Runs in Unity Play Mode from Login to Garden with "Try it free" and the server's test models.
2. The same with one real cat once the server keys are set.
3. The same on a physical iPhone.
