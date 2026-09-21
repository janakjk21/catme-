# Gameplay Contract

## Stable cat states

Use stable gameplay names even while some animations are approximated:

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

`CatMotor` owns navigation and velocity. `CatAnimationController` owns animation. `CatNeeds` owns values. `CatBrain` chooses activities. Keep these responsibilities separate.

## Locomotion quality

- Rotate toward a destination before and during movement.
- Accelerate and decelerate smoothly.
- Synchronize walk playback speed with actual movement velocity.
- Stop without severe foot sliding.
- Apply an asset-specific forward correction once at the model root.
- Validate ten consecutive paths before calling locomotion complete.

## First behavior loop

```text
Idle or explore
→ player calls cat
→ cat walks toward an authored destination
→ player pets or starts laser play
→ energy falls
→ cat walks to its house
→ hidden sleep transition
→ energy recovers
```

Low energy changes behavior instead of disabling interaction.

## Needs and relationship

Use four values with different responsibilities:

| Value | Behavior |
|---|---|
| Energy | Falls through activity and recovers through rest and sleep |
| Mood | Responds to recent care, play, environment, and frustration |
| Hunger | Rises gradually and falls when fed |
| Bond | Long-term relationship progression; increases slowly and never decreases |

Low needs create gentle behavior changes rather than punishment. The cat cannot become permanently ill, die, or lose bond because the player was away.

### Bond feedback

Show bond as a heart meter paired with a relationship label. Avoid presenting affection as only a raw percentage.

Suggested stages:

```text
New companion → Familiar → Trusting → Bonded → Inseparable
```

Petting, feeding, playing, responding to needs, taking memories, and returning over time add bond. Repeating the same action rapidly has diminishing returns and a cooldown so tapping cannot manufacture a complete relationship in one session.

Each bond gain should produce small visible feedback: a heart pulse, a gentle haptic, a sound, or a new reaction. Bond stages unlock reactions, activities, memory poses, and small room moments rather than raw power.

## Ambient time and return loop

- Room lighting follows local morning, daytime, evening, and night.
- Settings provide a manual lighting override.
- Returning players may discover a candid cat photograph or a small behavior moment.
- Memories record the cat, activity, room state, and date without turning play into a daily checklist.
- Offline recovery is capped and the cat welcomes the player without guilt messaging.
- On app open, the cat sometimes greets the player and sometimes continues its current activity.
- Players can take photographs, while the game also captures occasional candid memory moments.

## Room interactions

Props publish authored interaction points. The first room needs an open play area, cat house, food area, window point, toy storage, and useful scale references.

First toy: laser pointer. The cat reacts after a short delay, follows a valid floor target, approximates a pounce, and receives sound plus haptic feedback on a catch.

### Feeding with limited animation

- Display a food packet or container as a physical room object.
- The player places food into a bowl or feeding slot.
- Position the bowl against a wall or inside a feeding nook so the cat's face and front paws are naturally hidden.
- The cat walks to a precise authored approach point and stands in place.
- Camera framing, eating audio, bowl movement, and subtle procedural shoulder or head-root motion sell the eating action.
- Finish with a small satisfied reaction, hunger reduction, and limited bond feedback.

The occlusion is part of the room design, not a temporary debug treatment.

## Limited-animation rule

When a clip is missing, use a short procedural motion, camera framing, particles, sound, or an occluded transition. Do not block the prototype on obtaining a complete animation library.
