# Design and layout

How every WornTag screen looks, reads and behaves. The approved onboarding design is https://claude.ai/artifact/BQxtq2FK8TeEqgXXXNbL1M; match it.

## Principles

1. **One job per screen.** One question or one action. If a screen needs two, it is two screens.
2. **Picture left, words right.** The game is landscape. The left half shows what the player is making or doing; the right half says it and holds the buttons.
3. **One main button.** One dark button per screen. Anything else is a quiet link or a light button.
4. **Never trap, never skip.** No skip buttons in the journey. Back only before the pet photo is chosen.
5. **A child can read it.** Short, plain, active words. Say what happens and what you get.
6. **Calm, warm, real.** Bone paper, ink, one brass accent. No gradients, no neon, no emoji.
7. **The pet is the hero.** Real photos and the 3D pet are the decoration; the interface stays quiet.

## Colours (`Garden/CatMeUIStyle.cs`)

| Token | Hex | Use |
|---|---|---|
| `Mist` (Warm Bone) | `#F3EEE5` | screen background |
| `Paper` (Paper White) | `#FFFCF7` | cards, light buttons, fields |
| `Tile` | `#EAE3D7` | chips, tip circles, picture card rim |
| `Pine` / `Ink` (Worn Ink) | `#272321` | headings, main button |
| `BodyInk` | `#4F4843` | body text |
| `MutedInk` | `#766E68` | notes and placeholders |
| `Sunset` (Old Brass) | `#C5A261` | the only accent: brand dot, current step, ready chip |
| `Danger` | `#B3452C` | errors only |

## Type (`Resources/Fonts`)

| Role | Font | Size on the 1920 x 1080 canvas |
|---|---|---|
| Heading | Fraunces SemiBold | 64 (two lines at most) |
| Body | DM Sans Regular | 32 to 34 |
| Button | DM Sans Bold | 32 to 34 |
| Note | DM Sans Regular | 22 to 24 |

`CatMeUIStyle.FontFor` picks Fraunces for bold text of 44 and above, DM Sans otherwise.

## Layout grid

Canvas 1920 x 1080, scaled with screen size, content inside the phone's safe area. Coordinates are from the centre.

| Area | Position |
|---|---|
| Picture card (no top bar) | centre (-470, 0), 880 x 1000 |
| Picture card (below top bar) | centre (-470, -60), 860 x 860 |
| Right column | starts at x = 80 (`RightX`), about 800 wide |
| Heading, sub-line, tips, button | top to bottom in the right column; tips 82 apart |
| Back (top left), Close (top right) | (-860, 470) and (860, 470), round, 44 pt tap target or larger |
| Progress dashes | (0, -490), one dash per step |

Corners: buttons and chips are full pills; cards are 48 pt. Shadows are one soft shadow behind a raised surface (`CatMeUIStyle.SetElevation`), never a hard border.

## Components (`Screens/UiKit.cs`)

| Need | Use |
|---|---|
| Main button with arrow | `UiKit.NestedButton` (dark, `Pine`) |
| Light or provider button | `UiKit.Button`, or `ProviderButton` in `OnboardingFlow` (icon left) |
| Text link | `UiKit.Link` |
| Round icon button (back, close, HUD) | `UiKit.IconButton` |
| Picture card | `UiKit.Bezel` + `UiKit.CoverFrame` (use `focusX`/`focusY` to keep faces and subjects in frame) |
| Round photo | `UiKit.RoundPhoto` |
| Chip | `UiKit.Chip` |
| Progress | `UiKit.Rail` |
| Icons | `UiKit.Icon` with names from `Screens/UiIcons.cs` (line icons drawn in code) |
| Dialog, toast, loading or failed screen | `Screens/Moments.cs` (`ConfirmDialog`, `Toast`, `ScreenState`) |

Build new screens from these pieces. Do not hand-style a one-off button or card.

## Words

All player-facing text lives in `COPY.md`; these are the rules for writing it.

- Sentence case. No exclamation marks in buttons, labels or errors; the pet's speech may use them.
- A button says what happens ("Take a selfie", "Say hello"), and the same action keeps the same name everywhere.
- Errors say what went wrong and what to do: "It's too dark to see them. Try again near a window."
- No filler, no jargon, no "AI", "generate", "upload" in player-facing text; say "make", "photo", "3D copy".

## Motion and feel

- Buttons press down slightly when tapped. Screen changes are instant today; a short fade between screens is planned. Always respect `CatMe.ReduceMotion`.
- Haptics: one soft tap for confirmations (`World/Haptics`), respecting the Vibration setting.
- No looping decoration. Motion only for a change the player caused or something becoming ready.

## Checking a screen

1. Add the screen to `Screens/UiPreview.cs` and render it with `CatMe.Editor.UiCapture.Run` (batch mode on an APFS copy of the project; set `CATME_RENDER_OUT`).
2. Compare it with the design artboard: picture framing, one main button, no overlapping text.
3. Then play it in Play Mode, and finally on an iPhone.

## Skills to use

These Claude Code skills are installed. Load the one that fits before starting the work.

| Work | Skill |
|---|---|
| Any Canvas, RectTransform or layout change in Unity | `ui-ugui` |
| Driving or checking the Unity editor, builds, tests | `unity-cli` (ask the owner before connecting to a running editor) |
| Adding or checking Unity packages | `unity-package-management` |
| Designing a new screen or flow | `frontend-design`, `design-mobile-apps`, `ui-ux-pro-max` |
| Making a design feel premium and less generic | `high-end-visual-design`, `design-taste-frontend` |
| Mockup images for a screen | `imagegen-frontend-mobile` |
| Any words the player reads | `copywriting` |
| Firebase sign-in questions | `firebase-auth-basics` |

Design prototypes are made as Claude Design artifacts first, approved by the owner, then built in Unity with the components above.
