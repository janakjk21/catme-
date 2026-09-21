# CatMe Home Mobile Game Implementation Reference

## Product goal

Create a mobile-first virtual-pet game in which a user turns a photograph of their cat into a recognizable, rigged 3D companion living in a small interactive home.

The first complete product lets the user:

1. Upload a cat photograph.
2. Approve or regenerate the prepared reference image.
3. Approve or regenerate the static 3D cat.
4. Send only the approved model through quadruped rigging.
5. Download the game-ready cat into the mobile app.
6. Watch it explore the room.
7. Call and pet it.
8. Play a laser-pointer game.
9. See its energy change with activity and rest.
10. Watch it enter its cat house and sleep.

The room is the game interface. Toys and activities exist as physical room objects.

## System architecture

Use three independent layers:

```text
Generation service       Product service          Unity mobile client
------------------       ---------------          -------------------
Reference image          Cat ownership            Room and camera
Static 3D model          Version manifest         Navigation and physics
Quadruped rig            Save state               Animation controller
Walk retarget            Signed downloads         Toys, sound, haptics
Mobile optimization      Purchase state           Energy and behaviour
```

Keep `clonecatme` as the generation laboratory and browser preview. CatMe Home is the Unity mobile game. Both consume the same server contract.

Provider API keys, provider task IDs, polling and provider errors stay on the server. The mobile client never calls Meshy or Tripo directly.

## Current working cat-generation path

```text
Uploaded photo
→ prepare an isolated, consistent cat reference
→ Meshy creates a textured static GLB
→ save the static GLB as an immutable version
→ user selects and approves one version
→ upload that GLB to Tripo
→ Tripo rig compatibility check
→ Tripo quadruped rig
→ Tripo quadruped walk retarget
→ validate skeleton and animation
→ save the animated GLB as a separate version
```

Current Tripo settings:

```json
{
  "rig": {
    "model": "v2.5-20260210",
    "rig_type": "quadruped",
    "spec": "tripo",
    "out_format": "glb"
  },
  "walk": {
    "animation": "preset:quadruped:walk",
    "out_format": "glb",
    "bake_animation": true,
    "export_with_geometry": true,
    "animate_in_place": true
  }
}
```

Current local endpoints:

| Endpoint | Purpose |
|---|---|
| `POST /api/cat-generation` | Start photo-to-static-cat generation |
| `GET /api/cat-generation/status` | Read and advance the generation job |
| `GET /api/cat-generation/models` | List saved Meshy versions |
| `GET /api/cat-generation/model?version=...` | Download a saved static version |
| `POST /api/cat-generation/tripo` | Rig one selected static version |
| `GET /api/cat-generation/tripo/status` | Read and advance the rigging job |
| `GET /api/cat-generation/tripo/models` | List completed animated versions |
| `GET /api/cat-generation/tripo/model?version=...` | Download an animated GLB |

Current server ownership:

- `server/meshy.js`: reference and static model generation.
- `server/tripo.js`: upload, rig check, quadruped rig, walk retarget and status.
- `server/catGeneration.js`: orchestration, version history, resumption and routes.
- `server/generationTransfer.js`: bounded asset downloads.

## Production generation stages

### 1. Source photograph

Validate the file before provider spending. Confirm that a cat is visible, the image is large enough, and the intended cat is known when multiple animals appear.

Store the original privately. Generate a small preview thumbnail separately.

### 2. Reference approval

Show the isolated reference image before 3D generation.

Actions:

- Use this reference
- Try again
- Upload another photo

The reference must preserve coat colour, markings, eye colours, body proportions, fur length, tail and face.

### 3. Static 3D approval

Show the static cat in a neutral viewer. Let the user rotate, zoom, compare, accept, regenerate or replace the source photo.

Store each candidate under an immutable version ID. Never overwrite an approved version.

### 4. Paid rigging

Only an approved static version enters Tripo:

1. Upload GLB.
2. Run rig check.
3. Require `riggable: true` and `rig_type: quadruped`.
4. Create quadruped rig.
5. Download and retain the rigged result.
6. Apply walk retarget.
7. Download animated GLB.
8. Validate locally.
9. Build optimized mobile package.
10. Mark the cat ready.

Persist provider task IDs before polling. Resume failed status reads or downloads without creating a second paid task.

### 5. Automated output validation

Before delivery, verify:

- GLB parses.
- A skinned mesh exists.
- Skeleton exists.
- At least one animation exists.
- Walk duration is usable.
- Transforms contain no invalid values.
- Bounds are plausible.
- Forward and up axes are recorded.
- Feet remain close to the floor during a sampled walk.
- Model is within the mobile asset budget.

Generate a short front, side and walking preview for internal inspection. Failed models enter review instead of being silently delivered.

## Game-ready cat package

Do not send a large provider GLB directly to the mobile player. Produce:

```text
cats/<cat-id>/<version>/
  cat-mobile.glb
  cat-preview.webp
  manifest.json
```

Manifest example:

```json
{
  "schemaVersion": 1,
  "catId": "mochi_001",
  "version": "cat_v3",
  "modelUrl": "cats/mochi_001/cat_v3/cat-mobile.glb",
  "previewUrl": "cats/mochi_001/cat_v3/cat-preview.webp",
  "rig": {
    "type": "quadruped",
    "spec": "tripo",
    "forwardAxis": "+Z",
    "upAxis": "+Y",
    "heightMeters": 0.35,
    "floorOffset": 0.0
  },
  "animations": {
    "walk": "ACTUAL_CLIP_NAME_FROM_GLB"
  },
  "quality": {
    "approved": true,
    "mobileValidated": true
  }
}
```

Read animation names from the model. Do not assume one fixed clip name.

Mobile targets:

- Aim for a 10–25 MB playable package where quality permits.
- Preserve the provider original separately.
- Verify skeleton and animation after compression.
- Use mobile texture compression and an ordinary 2K texture tier.
- Keep fur metalness at zero.
- Use high enough roughness to prevent plastic fur.
- Never run an optimization step that prunes animation clips.

## Unity source layout

```text
Assets/CatMe/
  Art/
    Environments/
    Props/
    UI/
  Audio/
  Data/
    CatDefinitions/
    ToyDefinitions/
  Prefabs/
    Cat/
    Room/
    Toys/
  Scenes/
    Bootstrap.unity
    CatCreation.unity
    HomeRoom.unity
    ModelTest.unity
  Scripts/
    API/
    Cat/
    Camera/
    Core/
    Input/
    Room/
    Save/
    Toys/
    UI/
  Shaders/
  Tests/
```

Begin with the known-good local GLB in `ModelTest`. Connect remote downloads only after local import, skinning and animation are proven.

## Runtime modules

### CatApiClient

- Starts generation.
- Reads job state.
- Retrieves the approved manifest.
- Downloads the mobile GLB.
- Converts server failures into player-readable states.

It contains no Meshy or Tripo credentials.

### CatAssetCache

- Caches by cat ID and version.
- Verifies byte size and checksum.
- Retains the last known-good model.
- Deletes old versions only after the new version loads.
- Supports explicit redownload.

### CatLoader

- Loads the GLB at runtime.
- Applies manifest scale, forward correction and floor offset.
- Finds skinned meshes, skeleton and clips.
- Connects the model to the animation controller.
- Reports invalid models instead of spawning an invisible cat.

Use one runtime GLB importer initially. Prove PBR material, skinning and animation preservation before adding another.

### CatMotor

- Owns NavMesh destinations.
- Turns smoothly.
- Accelerates and decelerates.
- Detects arrival.
- Synchronizes movement speed with walk playback.

### CatAnimationController

Stable state names:

```text
Idle
Walk
PetReaction
Play
Pounce
EnterSleep
Sleep
Wake
```

Missing clips may temporarily use procedural or hidden transitions. Replace those treatments later without changing gameplay state names.

### CatNeeds

Owns energy, mood, hunger and bond values. It does not select animations.

### CatBrain

Chooses the next activity using needs, personality, player commands and room context. It sends commands to CatMotor and CatAnimationController.

### InteractionPoint

Every functional prop publishes authored positions:

```text
CatHouse: approach, enter, sleep, exit
FoodBowl: approach, eat
Window: approach, watch
ScratchPost: approach, scratch
ToyBasket: player pickup
```

### ToyController

Common toy contract:

```text
CanStart(catState)
BeginInteraction()
UpdateTarget(worldPosition)
CompleteInteraction(result)
CancelInteraction()
```

Implement the laser first.

## Energy and relationship

Use four needs:

| Need | Purpose |
|---|---|
| Energy | Controls activity and sleep |
| Mood | Responds to play and affection |
| Hunger | Enables occasional food interactions |
| Bond | Permanent relationship progression |

Energy bands:

| Energy | Behaviour |
|---:|---|
| 76–100 | Initiates play and moves quickly |
| 46–75 | Normal exploration and play |
| 21–45 | Slower movement and shorter play |
| 1–20 | Prefers quiet activities and bed |
| 0 | Sleeps |

Prototype costs:

| Activity | Energy |
|---|---:|
| Wandering | Slow drain |
| Come when called | -2 |
| Laser session | -5 to -10 |
| Pounce | -3 |
| Treat | +5 |
| Short rest | +10 |
| Full sleep | Gradual recovery |

Low energy changes the activity rather than locking the player out. A tired cat can be petted, photographed, fed, watched at the window or sent to bed.

Persist `lastUpdatedAt` and calculate capped offline recovery when the app resumes.

## Behaviour selection

Use a finite utility system rather than open-ended AI:

```text
sleep = lowEnergy + nightBonus + nearbyBed
play = energy + mood + visibleToy + playfulness
eat = hunger + visibleFood
explore = energy + curiosity + timeSinceExplore
approachOwner = bond + calledBonus
```

Possible behaviours:

- Sleep
- Play
- Eat
- Explore
- Watch window
- Approach owner
- Idle

Add cooldowns and small randomness. Expose winning scores in development builds for diagnosis.

## Room design

Build one small room with:

1. Open play area.
2. Cat house and sleep area.
3. Food area.
4. Window observation point.
5. Toy storage area.

Keep furniture near boundaries and the first NavMesh simple.

First props:

- Cat house
- Rug
- Toy basket
- Laser pointer
- Ball
- Food and water bowls
- Scratching post
- Window
- Sofa or chair
- Framed version of the uploaded photograph

Each large prop must support an interaction or establish useful scale.

## Mobile camera and input

Camera states:

- `Home`: elevated room view; cat is about 15–25% of screen height.
- `Play`: softly follows cat and toy; cat is about 30–40%.
- `CloseUp`: brief call, pet, meow and photograph moments.

Constrain orbit, vertical angle and zoom. Prevent the camera entering walls, moving below the floor or remaining extremely close to the cat.

Normal room mode:

- One-finger drag rotates camera.
- Pinch zooms.
- Tap cat focuses or pets.
- Tap a prop starts its interaction.

Toy mode:

- One-finger drag controls the active toy.
- Exit returns to room mode.

A drag must never control the toy and camera simultaneously.

## First game: laser chase

1. Player taps the laser prop.
2. A red dot appears on a valid floor position.
3. Cat tracks it with its head.
4. After a short reaction delay, cat follows.
5. Movement and walk playback accelerate together.
6. Cat performs a short pounce near the target.
7. Sound, particles and haptic feedback confirm a catch.
8. Energy decreases.
9. Cat rests or leaves when the session ends.

Add reaction delay, curved paths, occasional hesitation and a maximum chase duration. Perfect finger following looks mechanical.

Calibrate walk playback against real NavMesh velocity. Clamp it to a believable range such as 0.65–1.3 until a run clip exists.

## Sleep with limited animation

1. Cat walks to the house entrance.
2. Camera moves so the entrance hides the body transition.
3. Walking model enters and becomes hidden.
4. A sleeping representation appears inside.
5. Breathing, quiet audio and occasional `Z` particles begin.
6. Room lighting becomes warmer.
7. Energy recovers.
8. Wake reverses the hidden transition.

Later replace this with enter-bed, sleep-loop and wake clips.

## Sound and haptics

Audio groups:

- Room ambience
- Cat voice
- Purring and breathing
- Toys
- UI

Haptic moments:

- Soft pulse when petting begins.
- Gentle repeating purr pattern.
- Stronger pulse when a toy is caught.
- Two short pulses when the cat responds to its name.

Provide independent sound and haptic settings.

## Prototype save data

```json
{
  "catId": "mochi_001",
  "catVersion": "cat_v3",
  "name": "Mochi",
  "energy": 64,
  "mood": 72,
  "hunger": 30,
  "bond": 18,
  "personality": {
    "playfulness": 0.8,
    "curiosity": 0.6,
    "affection": 0.7,
    "sleepiness": 0.4
  },
  "lastActivity": "window",
  "lastUpdatedAt": "2026-09-21T17:00:00Z"
}
```

Start with local persistence. Add account sync after validating the core loop.

## Milestones

### 0. Preserve the generation fixture

- Keep one known-good animated GLB.
- Record its animation name, axis, bounds and size.
- Use it for normal game work without provider calls.

Exit: fixture remains independently loadable.

### 1. Unity model and room

- Mobile portrait project.
- `ModelTest` and `HomeRoom` scenes.
- Local GLB loads with correct materials.
- Correct scale, floor and forward direction.
- Elevated constrained camera.

Exit: cat renders correctly on a physical phone.

### 2. Locomotion

- NavMesh.
- Tap destination.
- Smooth turning and stopping.
- Playback synchronized with velocity.

Exit: ten consecutive paths complete without sideways movement, collision or severe sliding.

### 3. Living behaviour

- Procedural idle.
- Explore and call actions.
- Energy drain and offline recovery.
- Cat-house destination and hidden sleep transition.

Exit: cat alternates independently between exploration and rest.

### 4. Laser game

- Physical laser prop.
- Valid floor targeting.
- Chase and pounce approximation.
- Sound and haptics.
- Energy cost.

Exit: a new user understands the interaction without instructions.

### 5. Runtime cat download

- Unity calls the CatMe server.
- Downloads manifest and GLB.
- Caches by version.
- Reloads after restart.
- Handles failure and oversized assets.

Exit: three visibly different generated cats load and complete locomotion tests.

### 6. Creation funnel

- Photo upload.
- Reference approval.
- Static-model approval.
- Paid rigging boundary.
- Progress and recovery states.
- Cat arrival sequence.

Exit: an external tester creates a cat without developer help.

### 7. Product validation

- Petting.
- Memory camera.
- Bond progression.
- Focused analytics.
- TestFlight or internal Android build.

Exit: measure resemblance approval, generation completion, first play, session duration, return and willingness to pay.

## Analytics events

```text
photo_uploaded
reference_approved
reference_regenerated
static_model_approved
static_model_regenerated
rig_started
rig_completed
rig_failed
cat_entered_room
cat_called
cat_petted
toy_selected
laser_session_completed
cat_slept
memory_captured
session_completed
```

Do not send the source photograph through analytics.

## Spending controls

- Do not rig immediately after static generation.
- Require static-model approval.
- Allow one active paid job per cat version.
- Persist provider task IDs before polling.
- Resume status and download failures.
- Use a known-good local fixture during game development.
- Keep provider credentials on the server.

## Deferred features

Do not block the prototype on multiple rooms, multiplayer, clothing, full feeding inventory, procedural strand fur, conversational AI, AR, multiple pets, subscriptions or cloud accounts.

## First implementation task

```text
Known-good local animated GLB
→ ModelTest import
→ verify materials, skin and animation
→ normalize scale and axes
→ HomeRoom placement
→ NavMesh destination
→ smooth forward walk
→ stop at cat-house entrance
→ hidden sleep transition
```

Connect the generation server only after this sequence works reliably on a physical mobile device.
