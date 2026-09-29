# Unity Project Contract

## Runtime stack

- Unity `6000.6.2f1`
- Universal Render Pipeline `17.6.0`
- Input System `1.20.0`
- Cinemachine `6.6.0`
- AI Navigation `2.0.14`
- glTFast `6.2.0`
- UGUI `2.6.0`

Do not add another package when an installed package already covers the need.

## Scene ownership

- `Bootstrap`: startup and persistent services.
- `ModelTest`: isolated GLB, material, skeleton, animation, scale, and axis diagnosis.
- `GardenCompanion`: current playable scene for owner movement, cat companion behavior, and ball play.
- `HomeRoom`: retained legacy scene; do not extend it as the active game direction.

Keep asset diagnosis isolated in `ModelTest`. Move a character into `GardenCompanion` only after it passes asset validation.

## Authored source

- Game source and committed assets: `Assets/CatMe/`
- Large local test models: `LocalFixtures/`
- Product specification: `docs/`
- Small agent handoffs: `context/`

Never inspect or edit Unity-generated `Library/`, `Temp/`, `Logs/`, `Obj/`, or `UserSettings/` as source.

## Validation ladder

Use only the lowest step that proves the change:

1. Unity script compilation and Console inspection.
2. Relevant isolated scene in Play Mode.
3. `GardenCompanion` interaction check.
4. Physical device check for touch, haptics, audio, rendering, memory, or performance.

Do not run broad checks after documentation-only or isolated low-risk edits.
