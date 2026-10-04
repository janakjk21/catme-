# Menus, account and settings: flow and design

The reference for every screen the player opens from the garden: the menu, Your pets, Account, Settings, Photo album, Store, and the in-garden buttons. Change these screens against this document, and update it in the same change when a decision moves.

Sits beside `DESIGN.md` (whole-app rules) and `ONBOARDING_FLOW.md` (first-run journey). Where this file and `DESIGN.md` disagree about colour, **this file wins** for the screens it covers, and `DESIGN.md` should be updated to match.

---

## 1. Look

**White, warm bone and quiet sage.** Calm, light and soft, like paper in a garden. No black slabs.

### Colour tokens

From the WornTag brand guide (`worntag_repo_patch/docs/brand/BRAND_GUIDE.md`), plus two sage shades the app needs for contrast. Add the missing ones to `Garden/CatMeUIStyle.cs`; never type a hex value in a screen.

| Token | Hex | Use |
|---|---|---|
| `Paper` | `#FFFCF7` | menu sheets, cards, light buttons |
| `Mist` (Warm Bone) | `#F3EEE5` | full-screen backgrounds (Account), list rows inside a sheet |
| `Stone` *(new)* | `#D7CEC2` | hairline dividers, toggle track when off |
| `SageDeep` *(new)* | `#5F6E61` | **primary buttons**, active toggle, selected tab, icons on light tiles. White text on it is 5.2:1. |
| `Sage` *(new; brand "Quiet Sage")* | `#879487` | secondary icons, "walking now" chip text, progress fill. Not for body text (3.1:1). |
| `SageTint` *(new)* | `#E4E9E1` | selected card or row background, chips, the highlighted pet |
| `Ink` | `#272321` | headings only |
| `BodyInk` | `#4F4843` | body text |
| `MutedInk` | `#766E68` | notes, captions |
| ~~`Sunset` (Old Brass)~~ | `#C5A261` | **Retired on these screens.** No brass anywhere: not the brand dot, not progress, not chips. |
| `Danger` | `#B3452C` | destructive buttons and their text, errors |

**Rules**
- The **main button** on any sheet is `SageDeep` with Paper text. Replace today's near-black `Pine`/`Ink` main buttons on these screens ("Keep exploring", "Walk with …", "Get", "Back to garden").
- Light buttons are `Paper` with a 2 pt `Stone` outline, or `Mist` fill with `BodyInk` text.
- Destructive actions are `Danger` text on `Paper` with a `Danger` outline. They are never the largest button and always confirm first.
- A selected item uses `SageTint` fill and a `SageDeep` 3 pt outline, never black.
- Behind a sheet, dim the game with Ink at 35% alpha. Don't blur it; that's expensive on phones.

### Taste rules (from the `design-taste-frontend` review, 2026-10-04)

Why this palette: beige paper + brass + espresso ink is the most common AI-made "cozy brand" look, and the brand disappears in it. WornTag keeps the warm paper surfaces but takes its **one accent from the forest/sage family** instead of brass.

1. **One accent, locked.** Sage (`SageDeep` / `Sage` / `SageTint`) is the only accent; `Danger` is the only other colour, and only for destructive actions and errors. Today's violations to remove: black main buttons, gold progress dots, the brass brand dot, the blue "Cat" chip and peach "Dog" chip in Your pets, and the salmon / teal / tan letter tiles in the store.
2. **Cats and dogs look the same.** Species chips are both `SageTint` with `SageDeep` text; the word says the species, not a colour.
3. **One label per intent.** Every "close and return to the garden" action says **Back to the garden**. Retire "Keep exploring", "Done" and "Back to garden" (no "the"). Inside a sub-screen, the top-left back arrow returns to the menu; there is no second close button. Store has one entry point (the menu row).
4. **Buttons are one line and readable.** Labels are 1 to 3 words and never wrap ("Go inside", not "Go into the house"). Every label meets WCAG AA against its own background: no loose white text over the 3D scene (the Roam / Call / Ball labels today).
5. **No eyebrow labels.** Drop the small uppercase pill above titles ("ACCOUNT"; `Tag()` in `OnboardingFlow.cs` is used on 3 screens). The title is enough.
6. **No decorative dots.** Remove the brass dot before "WornTag" on Login. Dots are allowed only for real state (onboarding progress, the "being made" indicator).
7. **No placeholders that look unfinished.** No letter tiles for store items, no generic person icon for "You" (use the player's own portrait), no hand-drawn Apple or Google marks (use the official button artwork from each provider).
8. **One icon family.** Replace the 35 code-drawn icons in `Screens/UiIcons.cs` with one licensed family (Phosphor Regular is a good fit: soft, rounded, one stroke weight), imported as sprites at a single stroke weight. Don't mix families.
9. **Fraunces for titles only.** The brand guide names Fraunces, so it stays, but only for sheet and screen titles (56 and up). Buttons, rows, chips, notes and toasts are DM Sans.
10. **No em-dashes in player-facing text.** Today there are none; keep it that way (use a full stop, comma or colon).

### Icons (owner)
`Screens/UiIcons.cs` already loads a sprite from `Resources/UI/Icons/{name}.png` when one exists, before drawing its own. To switch to one family (Phosphor Regular suggested), add white-on-transparent PNGs, 128 × 128, with these names: `paw pause call walk ball sit arrow camera album gear store pets account bowl bed door up back close plus check trash share mail lock speaker vibrate bulb book clock motion heart`. Provider marks (`apple`, `google`) come from each provider's official button artwork, not from the icon family.

### Type, shape and spacing
- Fraunces SemiBold for sheet titles (56); DM Sans for everything else: row titles 34 Medium, row notes 24 Regular, buttons 32 Bold.
- Sheets: 40 pt corner radius, one soft shadow (`CatMeUIStyle.SetElevation`). Rows and tiles: 28 pt radius. Buttons: full pill.
- Spacing on an 8-point grid: 48 sheet padding, 24 between rows, 16 inside a row.
- Tap targets are at least 88 canvas units (44 pt) high; list rows are 112 high.
- Icons come from one licensed family (taste rule 8), tinted `SageDeep` on light or Paper on sage, 44 canvas units.

### Motion
- A sheet slides in 24 units from its edge and fades in over 0.22 s; it closes in 0.18 s. With Reduce motion on, fade only.
- Buttons press down to 0.96 scale (`CatMeButtonMotion`). A light haptic tap on confirm and on toggle.
- The game pauses (`Time.timeScale = 0`) while any sheet is open, and resumes when the last one closes.

---

## 2. Where things are

```
Garden HUD
 ├─ top right:    [camera]  [menu]           (round Paper buttons, SageDeep icon)
 ├─ bottom left:  floating joystick           (inside the safe area — see section 9)
 ├─ bottom right: [paw] → Call · Roam · Ball  (labels inside the buttons)
 └─ contextual:   one card above the paw button ("Go inside", "Sit down", "Fill the bowl")

Menu (right-side sheet)
 ├─ Your pets
 ├─ Photo album
 ├─ Store              (hidden until the store ships)
 ├─ Account
 ├─ Settings
 └─ [ Back to the garden ]
```

**Never show the menu, camera, joystick or paw buttons outside the garden.** Not on Login, not during onboarding, not on the Making screen, not in any full-screen state. Today the menu opens over Login and Making (launch audit item 27c).

---

## 3. The menu sheet

**Today:** a 2×3 grid of beige tiles with one empty cell, Settings sitting alone, a black "Keep exploring" button (`GardenCompanionDemo.cs`, menu built near the "Keep exploring" string).

**Should be:** a right-side sheet, full height of the safe area, 720 wide, `Paper`.

| Part | Spec |
|---|---|
| Header | "Menu" in Fraunces 56. Under it, the active pet: round photo 72, "Walking with Mochi", `MutedInk`. Tapping it opens Your pets. |
| Rows | One list, each row 112 high, `Mist` fill, 28 radius: icon in a 64 `SageTint` circle, title, chevron right. Order: **Your pets**, **Photo album**, **Store** (only when shipping), **Account**, **Settings**. |
| Account row note | Guest: "Guest · save your pets" in `SageDeep` with a small `Sage` dot (a nudge, not a warning colour). Signed in: "Signed in with Google". |
| Footer | Main button **Back to the garden** in `SageDeep` (the only wording for this action anywhere). |
| Close | Tap the dimmed game, swipe right, or the button. No separate X. |

Opening a row replaces the sheet with that screen. **Back returns to the menu, not to the garden**; the main button returns to the garden.

---

## 4. Account

The most important screen to get right: it holds the only way to keep pets across phones and the delete actions Apple requires.

### States

**A. Guest (anonymous)**
| Part | Content |
|---|---|
| Title | "You're playing as a guest" |
| Body | "Your pets live only on this phone. Save them to keep them if you get a new phone." |
| Main button | **Save my pets with Google** (official Google button style) |
| Second button | **Save with Apple** (official style; hidden until Sign in with Apple works) |
| Quiet link | "Sign out of this guest account" → confirm C1 |
| Danger zone | see below |

**B. Signed in**
| Part | Content |
|---|---|
| Title | "Your account" |
| Body | Provider and identity: "Signed in with Google · janak@…" (mask the middle of the email), or "Signed in with Apple". |
| Status | "Mochi, Miso and 2 more are saved to your account." |
| Light button | **Sign out** → confirm C2 |
| Danger zone | see below |

**Danger zone (both states)**, separated by a `Stone` divider and the heading "Your data":
- **Delete my photos** (light, `Danger` text): removes the pet photos and selfie; pets stay. → confirm C3
- **Delete account** (light, `Danger` text and outline): removes everything. → confirm C4
- Links: Privacy policy · Terms and AI policy · App version (`MutedInk`, 24)

### Confirms (use `Moments.ConfirmDialog`)
| Id | Title | Body | Buttons |
|---|---|---|---|
| C1 | "Lose these pets?" | "Guest pets can't be recovered after you sign out. Save them with Google first." | **Save with Google** (main) · "Sign out anyway" (`Danger` link) · "Cancel" |
| C2 | "Sign out?" | "Your pets stay saved to your account. Sign in again to see them." | **Sign out** (main) · "Cancel" |
| C3 | "Delete your photos?" | "We'll remove the photos you used to make your pets. Your pets stay." | **Delete photos** (`Danger`) · "Cancel" |
| C4 | "Delete your account?" | "This removes your pets, photos and saved garden from WornTag for good. It can't be undone." Requires typing nothing; a 3-second hold on the button instead. | **Hold to delete** (`Danger`) · "Cancel" |

### What each action must do
| Action | Must |
|---|---|
| Save with Google / Apple | Link the anonymous user (`FirebaseAccountService` `LinkWithCredential`); keep the uid, pets and garden; show "Saved. Mochi is safe." toast. If the Google account already has a WornTag account: dialog "This Google account already has pets. Switch to it?" (switch = sign out the guest after C1, then sign in). |
| Sign out | Save the session; sign out of Firebase; clear the in-memory roster, models, house frame and pending creation; **reload to Login**. Today it only calls `auth.SignOut()` and closes the screen (audit 31a). |
| Delete my photos | `DELETE /v1/photos`; delete local photo copies, drafts and house frame cache; toast "Your photos have been removed. Your pets stay." |
| Delete account | `DELETE /v1/account`; delete the local profile folder; sign out; reload to Login. Apple accounts: run Apple reauthorisation and token revocation first (audit item 12). |

### Data on the phone
Local data lives under **`Profiles/{uid}/`** (`Onboarding/AccountStorage.cs`, built 2026-10-04): pets, profile, store bag, drafts, downloads, the person model (`models/`), the photo album (`album/`) and the walk save (`session/`). The house frame cache was already per uid. The last account is remembered so the garden loads the right pets before Firebase restores the sign-in. The old shared folders (`Profiles/local-guest`, `Companion`, `JourneyPhotos`, `CatMeSession`) move once per phone into the first account that signs in. Signing in as a different person never shows the previous person's pets, photos or frame. Tests: `Tests/AccountStorageTests.cs`.

### Where it lives
`Screens/OnboardingFlow.cs` `ShowSignedIn` (rename to `ShowAccount`, both states), wired from `Garden/CatMeAppFlow.cs` `OpenAccount`; deletion in `CatMeAppFlow.ConfirmDeletion`.

---

## 4b. Login (applies the taste rules to the first screen)

The full flow is in `ONBOARDING_FLOW.md`; these are the visual rules.
- Keep the split: picture left, words right.
- Text stack, top to bottom: wordmark (no dot), title, one body line of **20 words or fewer** that includes the reassurance ("Sign in to keep your pet if you get a new phone."), then the buttons. **Nothing under the buttons.**
- Buttons: Continue with Google and Continue with Apple in each provider's official style, Apple shown only once it works; **Try it free** as one quiet secondary text button. No third look-alike button.
- The picture should be a real in-game capture or art painted from the real garden, so the login promises the game the player gets.

## 5. Settings

A sheet like the menu, titled "Settings", back arrow top left (returns to the menu).

| Section | Rows (toggle unless noted) | Note line |
|---|---|---|
| **Sound and feel** | Sound · Music (when music exists) · Haptics | "Hear the garden and your pet" · "Calm music while you walk" · "A light tap on actions" |
| **Help while you walk** | Hints *(was "Guidance")* · Pet's thoughts *(was "Story notes")* · Keep text on screen longer *(was "Story linger")* | "Short tips about what to try" · "What your pet seems to be thinking" · "More time to read" |
| **Comfort** | Reduce motion | "Softer camera and transitions" |
| **Controls** | Replay the controls guide (row with chevron) | |
| **Account and data** | Account (row with chevron, opens section 4) | |
| Footer | Privacy policy · Terms · "WornTag 1.0 (17)" | `MutedInk` 24 |

Toggles: track `Stone` off, `SageDeep` on, knob `Paper`. The whole row is the tap target, not just the switch. The sheet scrolls when it doesn't fit, with a fade at the bottom edge so scrolling is obvious.

Built 2026-10-04 (L2) in `CatMeAppFlow.BuildSettings`. Music is not listed until there is music. The delete buttons moved to Account.

---

## 6. Your pets

A full sheet (centre, 1560 wide) titled "Your pets", sub-line "Choose who walks with you today."

### Card (360 × 440, `Paper`, 28 radius)
- Portrait 200 round: the pet's **3D render** when the model exists (rendered once at load), otherwise its photo.
- Name (34 Medium) and species chip (`SageTint`, `SageDeep` text, the same for cats and dogs).
- State line, one of:
  - **Walking now**: card in `SageTint` with a `SageDeep` outline; button replaced by a "Walking now" chip.
  - **Ready**: main button "Walk with Mochi" (`SageDeep`).
  - **Being made**: thin `Sage` progress bar plus "Being made · about 3 min"; no button.
  - **Couldn't be made**: `Danger` text "We couldn't make Mochi" plus a light button "Try again".
  - **Stand-in** (dogs until real dog models ship): note "Stand-in until Juniper is ready".
- Long-press or a "…" button: **Rename**, **Remove from my pets** (confirm, `Danger`).

### Layout
- **First card is "Add a pet"**: dashed `Stone` outline, `SageDeep` plus icon, "Add a pet", and a note with pets left ("1 free pet left") or the store hint later.
- Horizontal list with snap; when cards overflow, show the edge of the next card **and** page dots under the list.
- Bottom row: "You": the person's own portrait, "Shared by every pet", link "Retake my selfie". Store button only when the store ships.

### Names
Trim spaces, 1–20 characters, letters, numbers, spaces and `'-`. Suggested names are the default selection on the Name screen.

Lives in `Screens/PetsScreen.cs`.

---

## 7. Photo album

Full sheet. Photo large (16:9) with left and right arrows plus swipe, and "3 of 12" under it. Caption under the counter, never behind buttons. Buttons in one row: **Save to Photos** (main, `SageDeep`) · Share · Delete (`Danger` link, confirm). Empty state: an illustration plus "No photos yet. Tap the camera on your walk." Captures hide the HUD, the development watermark and the console.

Lives in `Garden/JourneyExperience.cs` (gallery).

---

## 8. Store (layout only; payments out of scope)

Hidden from the menu until it ships. When shown: same sheet, tabs as pills (selected `SageDeep` with Paper text, unselected `Mist`), real item art (no letter tiles), price or "Free" chip, and "Owned" as a `SageTint` chip rather than a disabled button. One entry point only: the menu row. Remove the extra "Pet store" button from Your pets.

Lives in `Screens/StoreScreen.cs`, `Pets/StoreCatalog.cs`.

---

## 9. In-garden controls

| Control | Spec |
|---|---|
| Top-right buttons | Round 96, `Paper` at 92% alpha, `SageDeep` icon, inside the safe area, 24 apart. |
| Joystick | **Floating**: appears where the thumb lands in the left 40% of the safe area; resting hint at 22% from the bottom of the safe area; base 220, `Paper` at 35% plus a `Stone` ring; thumb 88 `Paper`. Travel 110, dead zone 0.15, eased response. Defer iOS system gestures on the bottom edge. |
| Paw button and its menu | Paw 120 `SageDeep` with Paper icon. Opens Call · Roam · Ball as 96 `Paper` buttons in an arc, **label inside or on a `Paper` pill under each**, never loose white text. |
| Contextual card | One at a time, fixed above the paw button: Paper pill, `SageDeep` icon, one line ("Go inside", "Sit down", "Fill the bowl"), arrow. Never placed over the player. |
| Toasts | Top centre, Paper pill, 4 s, one at a time; the pet's lines use Fraunces italic 30. |

Joystick and camera behaviour: launch audit item 31c–d.

---

## 10. Flow summary

```
Garden ──[menu]──> Menu sheet
                     ├─ Your pets ──> Walk with / Add a pet (onboarding: cat or dog → photo) / Rename / Remove
                     ├─ Photo album ──> Save / Share / Delete (confirm)
                     ├─ Account ──> Guest: Save with Google | Sign out (C1) | Delete photos (C3) | Delete account (C4)
                     │              Signed in: Sign out (C2) → Login | Delete photos (C3) | Delete account (C4) → Login
                     ├─ Settings ──> toggles · Replay guide · Account · Privacy · Terms
                     └─ Back to the garden
Back from any screen → Menu.   Main button on Menu → Garden.   Sign out / Delete account → Login (scene reload).
```

## 11. Done means
1. Every screen above is built from `UiKit` pieces with the tokens in section 1. A search of these screens for `CatMeUIStyle.Pine`/`Ink` on button fills, and for `Sunset` anywhere, finds nothing.
2. Taste check: one accent (sage) plus `Danger`; "Back to the garden" is the only close wording; no eyebrow `Tag()`, no decorative dots, no letter tiles or generic avatar; one icon family; every button label one line and AA-readable.
3. Rendered with `UiCapture` and compared with this document; no overlapping text, nothing outside the safe area.
4. In Play Mode: guest → Save with Google (or the editor stub) keeps pets; sign out returns to Login and the next account sees none of the previous pets; delete account returns to Login.
5. On an iPhone: menu, account, settings and joystick checked in both landscape directions.
