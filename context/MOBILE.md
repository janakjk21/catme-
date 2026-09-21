# Mobile Contract

## Delivery order

Develop in the Unity Editor first, then validate on iOS first. Add Android device support after the core interaction loop is convincing. Keep gameplay code and input abstractions cross-platform.

Xcode and Unity iOS Build Support are required before installing on an iPhone, but they do not block editor development.

## Camera

- Portrait-first layout.
- Elevated view so the cat's legs and movement remain readable.
- One-finger drag rotates the room camera.
- Pinch zooms within authored limits.
- Prevent wall penetration, below-floor angles, and extreme close-ups.
- Home view target: cat occupies roughly 15–25% of screen height.
- Play view target: cat occupies roughly 30–40%.

Use three coordinated modes:

- `Room`: elevated guided orbit with limited swipe rotation and pinch zoom.
- `CatFocus`: tap the cat to move closer; drags over the cat become petting.
- `Activity`: frames the cat and the active prop, then returns to the room view.

The default experience should remain composed like a living diorama while still letting the player inspect the cat closely.

## Input modes

Normal room mode:

- drag rotates camera
- pinch zooms
- tap cat focuses or pets
- tap prop starts an interaction

Toy mode:

- drag controls the active toy
- exiting restores camera control

A gesture must never control the toy and camera at the same time.

## Device-only validation

Use a physical phone to validate:

- touch gesture conflicts
- haptic strength and timing
- audio balance
- thermal behavior
- memory pressure
- frame pacing
- model load time
- safe areas and portrait UI

Editor behavior alone cannot complete these checks.
