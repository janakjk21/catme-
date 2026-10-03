# Words

Every word the player reads. This file is the source: the app is changed to match it. Rules are in `DESIGN.md` (Words). Use the `copywriting` skill for new text.

"In app" marks text already in the code. Everything below is in the app as of 2026-10-03 (roadmap task 8), still to be checked in Play Mode. `[Name]` is the pet's name; use "your cat" or "your dog" before it has one.

## Onboarding (in app)

The screens and their words are in `ONBOARDING_FLOW.md`.

## Tutorial (first walk)

| Step | Hint | Button |
|---|---|---|
| Walk | Drag to walk. | none |
| Call | Call [Name]. They will come to you. | Call [Name] |
| Ball | Roll the ball. Watch [Name] run after it. | Throw the ball, link Not now |
| Done | You did it. The garden is yours. | none |

When the tutorial is skipped, the Done line is not shown.

## Play screen

Pet button shows the pet's name. Fan: **Ball**, **Roam**, **Call**. Corner buttons: **Camera**, **Pause**.

## Near places

| Place | Button |
|---|---|
| Bench | Sit down |
| Tent | Step inside |
| House | Go into the house |
| Inside, leaving | Go back outside |

In the house: Throw ball, Feed, Bed, Go out (in app).

Feeding: bowl button "Fill the bowl". Feed from the dock away from the bowl: "[Name] is hungry. Fill the bowl." (the pet leads to the bowl). Filled: "Dinner time. Here comes [name]." Eating: "[Name] is eating." Done: "All gone. [Name] loved that." Within 45 s: "[Name] is full. Maybe later." Hungry on the walk: "Follow [name] home for dinner." (the pet leads to the house).

House frame: "Add a photo here". Cat guide: "Follow me. Let's put a photo in the frame." Contextual action: "Add a photo", "Change photo", or "Save photo" after an unsuccessful upload. Picker: "Choose a photo for the house frame". Account saved: "Your photo is saved in your account." Cloud failure: "Saved on this phone. Use Save photo at the frame to try again." Local failure: "That photo could not be saved. Choose another photo or free some space."

## Status lines

| Moment | Text |
|---|---|
| Loading | Getting your garden ready (in app) |
| Rain stops | The rain has stopped. (in app) |
| In the tent | Listen to the rain. (in app) |
| Feeding | Dinner time. (in app) |
| Pet asleep | [Name] is asleep. |

## What the pet says (in app, `Pets/CompanionBehavior.cs`)

| Moment | Cat | Dog |
|---|---|---|
| Rain | It's raining. This way. / Rain! Come on. / Let's get out of this. | Rain! Quick, this way! / Come on, come on! / Wet! Let's go! |
| Follow me | Follow me. / This way. / Over here. | This way! This way! / Follow me! / Come see! |
| Waiting | Are you coming? / Hurry up. / I'm waiting. | Hurry! Are you coming? / Come on, come on! / Over here! |
| In the tent | Dry at last. / Here. It's cosy. / In we go. | Made it! / In here! In here! / Dry! Good! |
| Bench | Can we rest here? / A nice spot to sit. / Shall we sit a while? | Can we sit? Please? / Rest here? Okay? / Sit time! |
| Home | Shall we head home? / It's getting late. / Home soon? | Home? Dinner? / Can we go home now? / Home! Home! |
| Hungry | I'm getting hungry. / Is it dinner time? / I could eat something. | I'm so hungry! / Is it food time? / Snacks? Please? |
| Visitor | Who's that? / Someone's coming. | Who's that? Who's that? / A friend! / Ooh, a dog! |
| Butterfly | Oooh. / Look at that. / Mine. | Wheee! / Wait for me! / Got it! Almost! |
| Petted | Purr. / Mmm. / More. | Yes! Again! / Best day! / More, more! |
| Called | Here. / Hello. / Yes? | Here! / Coming! / Right here! |

## Pause menu (in app)

Your pets · Store · Photo album · Account · Settings · Keep exploring

## Settings

| Setting | Line under it (in app) |
|---|---|
| Sound | Hear the garden and your pet |
| Guidance | Show helpful prompts while you walk |
| Story notes | Show quiet story lines |
| Story linger | Give story lines more reading time |
| Reduce motion | Keep movement and transitions gentle |
| Haptics | Feel a light tap on actions |
| Delete my photos | Remove your photos from our server. Your pets stay. |
| Delete account | Remove your account, pets and photos for good. |

## Messages

| When | Text |
|---|---|
| No internet | No internet. Your pet is saved on this phone. Try again when you're back online. |
| Still being made | [Name] is still being made. It takes a few minutes. |
| Making failed | We couldn't make [Name] this time. Try again. |
| Photo too dark | It's too dark to see them. Try again near a window. |
| No cat or dog found | We can't see a [cat/dog] in this photo. Try another one. |
| More than one | There is more than one in this photo. Try one with only [your pet/you]. |
| No face in the selfie | We can't see your face in this photo. Try a selfie in good light. |
| Face too small | Your face is too small in this photo. Take a closer selfie. |
| Empty album | No photos yet. Tap the camera to take one. |
| Empty pets list | Just you for now. Add a pet. |
| Delete photos confirm | Delete your photos? Your photos will be removed from our server and this phone. Your pets stay. Buttons: Delete my photos, Keep my photos |
| Delete account confirm | Delete your account? Your pets and photos will be removed for good. Buttons: Delete account, Keep my account |
