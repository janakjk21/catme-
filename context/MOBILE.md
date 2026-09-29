# Garden Mobile Contract

## Delivery approach

Develop in the Unity Editor, then validate on iOS first. Add Android device support after the garden walk-and-ball loop is convincing. Keep gameplay and input abstractions cross-platform. A build succeeding does not complete a mobile milestone.

## Camera and composition

Use the approved slightly elevated trailing view behind the owner. Keep both the owner and cat visible at the agreed scale, with enough garden path ahead to read direction and clues. Avoid forced shake, sudden camera snaps, or automatic close-ups that take control away; the first reunion close-up is a brief authored opening beat that returns smoothly to gameplay framing.

## Touch controls

- Put owner movement in a comfortable lower-left touch region.
- Show one clear ball interaction when appropriate. Keep aiming and throwing separate from camera look and movement.
- Reserve the initiating pointer for a ball gesture; cancel cleanly if a pinch or interruption occurs.
- Pair sound-based clues with visual cues. Provide extra search guidance and a skip option.
- Keep touch targets large and UI clear of both characters, clues, and the ball path.

## Layout

Respect safe areas in supported phone orientations and adapt to changing aspect ratios. Reflow rather than relying on a single device resolution. Keep transient hints away from the owner-cat pair and movement control.

## Device-only validation

On a physical phone, verify supported orientations, touch comfort, safe areas, owner/cat scale, follow behavior, camera framing, haptics, audio balance, thermal behavior, memory use, frame pacing, character loading, ball play, photo selection, save/reload, offline recovery, and the full find-your-cat opening. Editor captures, navigation checks, and successful builds do not prove these.
