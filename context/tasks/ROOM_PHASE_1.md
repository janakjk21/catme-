# Luna Task: Room Phase 1 Blockout

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/UNITY_PROJECT.md`
4. `context/MOBILE.md`
5. This task file

Open `docs/CATME_EXPERIENCE_DESIGN_V1.md` only if a requirement below is ambiguous. Do not inspect the sibling web project.

## Objective

Create a clean, correctly scaled blockout of the first CatMe home room in `Assets/CatMe/Scenes/HomeRoom.unity`. This phase proves space, camera composition, navigation clearance, and interaction locations. It is not the final art pass.

## Coordinate contract

- Unity `+Y` is up.
- Room center on the floor is world `(0, 0, 0)`.
- Back wall is toward `+Z`.
- Open camera side is toward `-Z`.
- Room interior: approximately `6.5 m` wide, `5.0 m` deep, and `2.8 m` high.
- Keep authored room content under one root named `Room_Blockout`.

## Required hierarchy

```text
Room_Blockout
  Architecture
    Floor
    BackWall
    LeftWall
    RightWall
  Zones
    OpenPlayZone
    WindowZone
    SleepZone
    FeedingZone
    ToyZone
    ShopZone
  Props_Blockout
    Rug
    Sofa
    WindowPerch
    CatHouse
    FeedingNook
    FoodPacket
    ToyBasket
    ShopTablet
    MemoryFrame
  InteractionPoints
    CallDestination
    WindowApproach
    SleepApproach
    SleepInside
    FeedingApproach
    ToyApproach
    ShopViewPoint
  CameraRig
    CameraPivot
    HomeAnchor
    LeftAnchor
    RightAnchor
  Navigation
    NavMeshSurface
  ScaleReference
```

Use ordinary Unity primitives and simple materials. Do not depend on the existing room GLB for this phase.

## Layout

Keep the central floor open for walking and laser play.

| Element | Approximate position | Requirement |
|---|---:|---|
| Open play zone | `(0, 0, 0)` | At least `3.2 × 2.4 m` clear floor |
| Window perch | `(-1.8, 0, 2.15)` | Against back wall |
| Cat house | `(2.15, 0, 1.75)` | Entrance faces open floor |
| Feeding nook | `(-2.9, 0, -1.25)` | Against left wall; bowl faces wall so cat head can be hidden |
| Toy basket | `(2.35, 0, -1.55)` | Near front-right boundary, outside central play area |
| Shop tablet | `(2.85, 0.8, -0.25)` | On right wall or a shallow side table |
| Sofa | `(2.55, 0, 0.65)` | Along right boundary without blocking cat-house path |
| Rug | `(0, 0.01, -0.1)` | Marks open play space without collision |
| Memory frame | `(-2.4, 1.45, 2.47)` | Back wall decoration placeholder |

Adjust small offsets when necessary, but preserve the zones and open center.

## Interaction points

Interaction points are visible named transforms, not gameplay implementations.

- `CallDestination`: near `(0, 0, -1.35)`, facing the primary camera.
- `WindowApproach`: reachable floor position in front of the perch.
- `SleepApproach`: reachable position directly outside the cat-house entrance.
- `SleepInside`: hidden position inside the cat house.
- `FeedingApproach`: position where the cat faces the left wall and its head enters the feeding nook.
- `ToyApproach`: reachable position beside the toy basket.
- `ShopViewPoint`: framing target for the shop tablet.

Orient the transforms so their local `+Z` points in the direction the cat should face.

## Camera blockout

Use a perspective camera suitable for portrait display.

- Suggested field of view: `40–45` degrees.
- `CameraPivot`: approximately `(0, 0.75, 0)`.
- `HomeAnchor`: approximately `(0, 3.4, -6.8)`, looking near `(0, 0.7, 0.2)`.
- Add left and right anchors that show the room from attractive three-quarter angles without seeing through open walls.
- The complete room and the `ScaleReference` must be visible from `HomeAnchor`.
- Do not implement swipe, pinch, Cinemachine transitions, or follow behavior in this phase.

## Scale reference

Add a non-gameplay capsule or primitive approximately `0.35 m` tall named `CatScaleReference`. It represents a realistic cat and verifies that furniture, doors, bowls, and camera distance are believable. Give it a distinctive neutral material and disable interaction.

## Navigation

- Add one `NavMeshSurface` for the floor.
- Furniture and walls need appropriate colliders.
- Rug and decorative markers must not obstruct navigation.
- Bake a continuous NavMesh through the open floor.
- Confirm reachable paths from the center to `CallDestination`, `WindowApproach`, `SleepApproach`, `FeedingApproach`, and `ToyApproach`.
- Do not add a cat agent or movement code yet.

## Blockout appearance

- Warm off-white walls.
- Medium warm wood floor.
- Muted rug color.
- Simple warm key light and soft fill appropriate for URP.
- No detailed textures, paid assets, post-processing experiments, or final decoration.
- Keep the room readable and realistic in proportion rather than visually finished.

## In scope

- `HomeRoom.unity`
- Small reusable blockout materials or editor helpers under `Assets/CatMe/`
- NavMesh surface and scene colliders
- Named anchors, zones, and interaction transforms

## Out of scope

- Cat loading, animation, or movement
- Camera gesture code
- Petting, feeding logic, toys, sleep logic, needs, or bond
- UI, shop logic, tokens, purchases, login, analytics, or saving
- Meshy, Tripo, OpenAI, Supabase, or any network/API request
- Final environment models, texture hunting, or Asset Store downloads
- Changes to `Bootstrap` or `ModelTest`

## Acceptance checks

1. `HomeRoom.unity` opens without missing references or new Console errors.
2. Portrait Game view shows a coherent room from `HomeAnchor`.
3. The `0.35 m` cat reference looks correctly scaled relative to the room and props.
4. The central `3.2 × 2.4 m` play space remains unobstructed.
5. Every required zone, prop, camera anchor, and interaction point exists with the exact specified name.
6. A continuous NavMesh reaches every required approach point.
7. Feeding and sleeping geometry visibly support the planned occlusion techniques.
8. No unrelated scene, API, cat, UI, or economy code is introduced.

## Validation

- Let Unity import and compile.
- Inspect the Console.
- Open `HomeRoom` in portrait Game view.
- Inspect the baked NavMesh and each approach point in Scene view.
- Do not run provider calls or broad unrelated tests.

## Completion report

Report:

- files created or changed
- final room dimensions
- any adjusted prop positions and why
- camera anchor positions and field of view
- NavMesh reachability result
- Console result
- remaining room-specific risks
- concise proposed update for `context/STATUS.md`
