# Gameplay and the world

What happens after onboarding: the map, how the player and the pet move, what they can do, and how the world changes. Describes what is built today; "Planned" marks what is not.

## The map

One place, `Scenes/Places/Place_HomeGarden_r2.unity`, loaded into `GardenCompanion.unity` by `Session/SessionDirector`. Built by `Editor/PlaceSceneBuilder` (rebuild it there rather than hand-editing the scene).

```
   woods          house (enter: home room)
   |-------- walled garden: lawn, flower beds ---------|
                         gate
   riverside walk:  bench . . bench . . tent . . bench . . far end
   river (fenced) ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ far bank, wildlife
```

| Part | What it is |
|---|---|
| Walled garden | where the player starts: open lawn, flower beds along the walls, the house at the back |
| Gate | opens onto the riverside walk; the river is only visible beyond it |
| Riverside walk | the main route, with three benches facing the river and the tent halfway |
| Tent | a 3.4 m A-frame beside the path with a lantern at the entrance (lit at dusk). Inside: a 5 x 5 m canvas room with a small fire in a stone ring (flames, sparks, flickering light, crackle; `World/CampFire`), rug, sleeping bag, crate and hanging lantern. The pet settles on its cushion facing the fire. Rain sound and "The rain has stopped." only if it was raining; "Go out" is available after 3 s. |
| House | "Go into the house": an inside room (`Resources/Rooms/HouseRoom_r3.prefab`) with the supplied cat house and a large food bowl, feed, bed, ball and go out. Walk up to the empty bowl and "Fill the bowl": brown kibble pours in, the pet walks over, eats it down piece by piece (about 7 s), hearts pop, then it is full for 45 s. "Feed" from the dock makes the pet lead you to the bowl. While the frame is empty the cat leads toward it; after a new photo the pet goes to look at it, meows and hearts pop. Its nearby Add a photo / Change photo action opens the existing picker. A private account photo and local cache restore it next time; failed uploads offer Save photo retry. |
| Edges | invisible walls keep the pair on the route; trees, hedges and the far bank fill the view |
| Look | Japanese garden at golden hour: cherry blossom, torii, stone lanterns, warm light |

Benches are kept clear of trees so they are always visible. The walled garden has a small pond (left of the path, near the gate): dark bed, river water, a ring of the garden's own rocks; no loose boulders on the lawn.

## The player and the pet

| | Cat | Dog |
|---|---|---|
| Walks beside the owner at | 0.85 m to the side, 1.5 m ahead | 0.6 m to the side, 1.0 m ahead |
| Pace | normal | 25% quicker |
| Hurries back when further than | 3 m | 2.4 m |
| Wanders to look at things within | 4.5 m | 5.5 m |
| Likes the water | no preference | drawn to it |
| Notices a visitor at | 4.8 m | 9 m |
| Speaks | calm and short ("Follow me.") | eager ("This way! This way!") |

Numbers live in `Pets/CompanionBehavior.cs`; each species owns its own lines.

**The pet leads too** (`Session/CompanionLeader`): when it rains it heads for the tent ("It's raining. This way."); it invites a rest at benches; late in the walk it says it wants to go home or is hungry and trots toward the house, waiting for you (it gives up after a minute if you don't follow). A hungry pet goes straight to its bowl inside. Hunger is only words and a walk home, never a meter.

**Curiosity** (`Session/CatCuriosity`): now and then the pet trots to a nearby spot and looks at it. When the player walks up, a keepsake is collected.

## Controls

| Control | Does |
|---|---|
| Drag on the screen | walk |
| Pet button (bottom) | opens Ball, Roam (the pet wanders) and Call |
| Tap the pet | it comes over, meows, a heart pops |
| Press and hold the pet | it stays still, the camera lowers, it purrs with soft vibration |
| Button near a bench, tent or house | Sit down, Step inside, Go into the house |
| Camera (top right) | take a photo for the album |
| Pause (top right) | Your pets, Store, Photo album, Account, Settings |
| Walk near the house photo frame | the cat leads to it; a contextual Add a photo / Change photo button opens the picker. The picture fits without stretching and is saved to the player's account. Cancel keeps the previous photo. |

## The first walk (tutorial)

Three steps, each shown as one short hint (`Onboarding/TutorialController`, words in `COPY.md`): **walk** ("Drag to walk.") -> **call** the pet ("Call [Name]. They will come to you.") -> **roll the ball** and watch it chase. The ball step can be put off. After that the garden is free play.

## The living world

| System | What it does |
|---|---|
| Day (`World/DayDirector`) | each day has a type (clear, partly cloudy, misty, showery, rainy; never the same twice running) and a line saying so ("A clear, sunny day."). Light moves from afternoon to sunset and blue hour over about 18 minutes; after about 2.5 minutes of night the screen dims and a new day begins. A relaunch resumes the same day unless it was night or more than 3 hours have passed. |
| Weather (`Session/PlaceWeather`) | rain, with wind before it; the pet leads to the tent. Drops come from the Rain Particles pack (capped for phones) and leave a splash and ring where they hit; roofs stop them. Without the pack the place falls back to code-built rain. |
| Butterflies (`World/Butterflies`) | eight species from the AI cartoon butterflies pack drift through flowers, flap while flying and fold their wings when they land, scatter when you walk close; gone at dusk and in rain |
| Visiting dog (`World/DogVisitor`) | a neighbour's dog walks the riverside path, stops, wags, sits, walks on |
| Wildlife (`World/Wildlife`) | deer, horse and chickens across the river, scenery only |
| Sound | garden ambience, rain, purr, meow, UI taps; Sound setting turns it off |

## The camera

A third-person camera follows the pair at a comfortable height (`Garden/CompanionCamera`), with per-area framing (`Garden/CameraZone`). Inside the tent and on a bench it switches to a first-person view with the pet in front.

## Rules for changing gameplay

- Keep one map. A new area is added to `Place_HomeGarden_r2` through `PlaceSceneBuilder`, not as a new scene.
- A new interaction is an `InteractionPoint` on the map plus one contextual button, never a new menu.
- Pet behaviour differences go in `CompanionBehavior` (cat and dog), not in `if (dog)` checks elsewhere.
- No timers that punish the player, no hunger meters, no failure states in the walk.
- Test in Play Mode, then on an iPhone, before calling a gameplay change done.

## Planned

- The player's own downloaded dog model (dogs use the built-in corgi today).
