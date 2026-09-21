# CatMe Experience Design V1

Status: product design baseline for the first playable mobile prototype.

## 1. Product vision

CatMe is a realistic ambient companion experience. A player can live with a digital version of their cat, preserve an older pet photograph as a companion, or choose a professionally authored virtual cat.

The emotional promise is simple: open a small living room and find a cat that appears to remember, recognize, and respond to the player.

The first prototype validates the room and relationship before adding login, payment, photo upload, generation, or cloud sync.

## 2. Experience principles

1. **The cat feels alive when the player does nothing.** It pauses, watches, explores, rests, and reacts to the room.
2. **The room is the interface.** The player touches physical props instead of navigating a dashboard of action buttons.
3. **Every input receives acknowledgment.** The cat may respond differently based on state, but the player never wonders whether a tap failed.
4. **Realism includes restraint.** Pauses, imperfect timing, subtle sound, and small movement are more convincing than constant activity.
5. **Care is gentle.** Absence never causes death, permanent harm, lost bond, or guilt messages.
6. **The personal cat remains the hero.** Purchasable Studio Cats add choice without making generated cats feel incomplete.
7. **Limited animation becomes environmental design.** Furniture, camera framing, audio, particles, and procedural motion cover actions that lack dedicated clips.

## 3. Audience modes

The product supports three motivations through the same experience:

- **Personal companion:** recreate a current cat.
- **Memory companion:** use an older photograph without selecting a memorial label.
- **Virtual companion:** name a free or purchased Studio Cat.

Language uses “your companion” and avoids asking whether the source cat is living or deceased.

## 4. First playable journey

```text
Launch
→ room fades in at the current local time
→ named local test cat continues an activity or notices the player
→ player observes, calls, or touches the cat
→ player feeds or starts laser play through a room prop
→ needs and bond respond
→ a candid or player-created memory becomes available
→ cat eventually rests or sleeps
→ local state is saved
```

The prototype begins directly in the room. Account, payment, photo upload, and generation arrive after this loop is convincing.

## 5. Room concept

Use one warm, realistic, modern apartment room composed as a portrait-friendly living diorama.

```text
┌──────────────────────────────────────┐
│ Window perch        Cat house        │
│                                      │
│ Scratch area        Sofa/rest area   │
│                                      │
│          Open play floor             │
│                                      │
│ Feeding nook        Toy basket       │
│                                      │
│ Memory frame        Shop tablet      │
└──────────────────────────────────────┘
               Primary camera
```

### Spatial rules

- Keep the center open for readable walking and laser play.
- Place large furniture near boundaries.
- Give every functional prop one or more authored approach points.
- Preserve navigation clearance around movable decorations.
- Use the window, rug, furniture, bowls, and cat house to establish believable cat scale.
- Keep the feeding bowl inside a shallow wall-facing nook to hide unsupported eating motion.
- Small toys and decorations use authored placement slots rather than unrestricted physics placement.

## 6. Camera and touch model

### Room mode

- Elevated three-quarter view.
- One-finger drag rotates within authored horizontal and vertical limits.
- Pinch zooms between room overview and inspection distance.
- Camera collision prevents entry into walls, furniture, and the floor.
- After input ends, composition settles gently while preserving the selected angle.
- A locator action can reframe a hidden cat.

### Cat focus mode

- Tap the cat to glide closer.
- The cat occupies approximately 35–45% of screen height.
- A drag beginning on the cat pets it.
- A drag beginning outside the cat rotates the camera.
- Pinch remains available within close-up limits.
- Back or tapping open room space returns to Room mode.

### Activity mode

- Prop selection frames the cat, destination, and active object.
- Laser play follows the cat and target within safe camera bounds.
- Feeding shows the cat from behind or from a covered side angle.
- Sleep frames the cat-house entrance so the transition is hidden.
- Completion returns smoothly to the prior room view.

## 7. Ambient cat behavior

The cat uses a finite utility system rather than conversational AI.

Possible activities:

- idle and observe
- short exploration
- window watching
- sofa or rug rest
- cat-house sleep
- investigate a placed toy
- approach the player
- request food
- short grooming approximation

Selection considers energy, mood, hunger, bond, local time, nearby props, cooldowns, and small randomness.

The cat should spend meaningful time stationary. Avoid selecting destinations continuously.

## 8. Interaction specifications

### Call

The player taps a small physical bell in the room.

1. Bell moves and sounds.
2. Cat immediately acknowledges through head direction, a pause, or sound.
3. Response delay and speed depend on energy and current activity.
4. Cat walks to an authored near-camera destination.
5. Camera enters Cat focus mode.

Sleeping cats may acknowledge before getting up. The action is never visually ignored.

### Pet

1. Player taps the cat to enter Cat focus mode.
2. A drag over valid body regions begins petting.
3. Cat uses subtle body wobble, head movement, breathing, or the best available reaction clip.
4. Purr audio ramps in.
5. Gentle haptics follow the stroke rather than vibrating continuously.
6. Mood responds immediately; bond gain is limited by cooldown and willingness.

### Feed

1. Food packet remains visible on a shelf or counter.
2. Player drags it to the feeding nook.
3. Pouring audio and a bowl material/state change confirm placement.
4. Cat notices and walks to the authored approach point.
5. Wall-facing geometry hides the cat's face and paws.
6. Eating audio, bowl movement, and subtle root or shoulder motion imply eating.
7. Hunger falls and a satisfied reaction finishes the sequence.
8. Preferred foods add a modest mood and bond bonus.

Basic food is always available. Special foods are optional consumables.

### Laser play

1. Player taps the physical laser pointer.
2. Input changes from camera control to toy control.
3. A red dot is constrained to valid floor surfaces.
4. Cat notices after a short variable delay.
5. Cat turns and follows using smooth navigation.
6. Walk playback tracks actual velocity.
7. Near the dot, a short procedural pounce or authored reaction plays.
8. Sound, particles, and one stronger haptic confirm a catch.
9. Energy decreases and the cat may rest after the session.

The dot should lead rather than drag the cat perfectly under the finger.

### Sleep

1. Low energy increases the cat-house utility score.
2. Cat walks to the entrance.
3. Camera frames the doorway to hide the unsupported transition.
4. Walking cat becomes hidden inside the house.
5. A sleeping representation, breathing motion, quiet audio, and occasional subtle `Z` effect begin.
6. Energy recovers and room lighting becomes calmer.
7. Wake reverses the covered transition.

## 9. Needs and bond

### Dynamic needs

- **Energy:** activity cost and sleep recovery.
- **Mood:** recent care, play, preferences, and environment.
- **Hunger:** gradual increase and feeding recovery.

These values affect behavior but never cause permanent damage.

### Permanent relationship

Bond grows slowly and never decreases:

```text
New Companion → Familiar → Trusting → Bonded → Inseparable
```

Bond sources include varied petting, needed feeding, completed play, memories, discoveries, return visits, and responding to preferences. Repetition has cooldowns and diminishing returns.

The compact HUD shows a heart and relationship label. Tapping it opens energy, mood, hunger, favorite item, and recent-memory details.

Bond stages unlock reactions, independent approaches, memory moments, and compatible activities. Bond cannot be bought.

## 10. Time, return, and memories

- Room lighting follows local morning, day, evening, and night.
- A manual override is available for comfort and accessibility.
- On launch, the cat sometimes greets the player and sometimes continues its activity.
- Offline recovery is capped.
- The app never says the cat suffered because the player was absent.

The player can use a physical camera object to take a photograph. The game also captures occasional candid moments when composition and behavior are suitable.

Memories contain cat, activity, date, room state, and bond stage. They provide a calm return incentive instead of a mandatory checklist.

## 11. Studio Cats and economy

The room contains a physical tablet or catalog. It opens the shop without destroying the current room state.

### Studio Cats

- One-time direct purchase.
- Real-time preview of the actual model and signature action.
- Realistic appearance, personality, favorite items, and authored animation set.
- Permanently owned and user-named.
- One active cat in the room for V1.

### Soft tokens

Earn tokens through completed play, new discoveries, memories, bond milestones, and occasional cat gifts.

Spend tokens on special food, toys, beds, scratching posts, and small decorations. Toys and decorations are permanent. Special food is consumable. Basic food remains available.

The first prototype may display the economy but does not need real payment or paid token packs.

## 12. Interface map

Persistent minimal HUD:

- cat portrait and name
- heart plus bond label
- token balance after economy unlocks
- sound/settings access
- optional cat locator

Room objects open focused surfaces:

- bell → Call
- cat → Cat focus and Pet
- food packet/bowl → Feed
- laser/toy basket → Play
- camera → Memories
- tablet/catalog → Shop and owned cats

Avoid a permanent bottom bar of duplicate actions.

## 13. Animation strategy

The known generated cat has a walk clip. V1 combines that clip with:

- speed and crossfade control
- root-level turning and navigation
- procedural breathing and small body motion
- audio and haptics
- camera emphasis
- prop movement
- occluded transitions
- optional static alternate representations inside covered props

Do not deform bones blindly or promise actions the rig cannot perform. Every procedural treatment must be tested across the supported generated-cat rig contract.

## 14. First playable scope

### Required

- Local cat loads with correct material, scale, floor, and forward axis.
- Walk animation and translation remain synchronized.
- Warm room with functional zones and constrained camera.
- Autonomous idle/explore/rest selection.
- Call, pet, feed, laser, and covered sleep sequences.
- Energy, mood, hunger, bond, and local save.
- Purr, meow, interaction audio, and haptic hooks.
- Player photograph plus one candid-memory path.

### Represented but not operational

- Shop tablet can use local preview data.
- Studio Cat ownership can use local test data.
- Tokens can be earned and spent locally.

### Deferred

- Login and account recovery
- Real payments
- Photo upload and creation funnel
- Remote generation and runtime download
- Cloud save
- Multiple active cats
- Multiple rooms
- Notifications and analytics

## 15. Prototype acceptance

A first-time tester should be able to:

1. Find and observe the cat without instruction.
2. Call it and see a clear response.
3. Discover petting through touch.
4. Place food and understand that the cat is eating.
5. Start and finish laser play.
6. Understand energy and bond without feeling punished.
7. Move and zoom the camera without losing the room.
8. Leave after a short visit feeling that the cat had its own behavior.

Implementation begins with isolated model validation, then locomotion, then the room and interactions in this document.
