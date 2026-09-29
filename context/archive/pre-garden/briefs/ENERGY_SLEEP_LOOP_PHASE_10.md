# Luna Task: Energy, Rest, and Local Return Phases 10–11

## Agent configuration

- Model: `gpt-5.6-luna`
- Reasoning effort: `medium`
- Work on this phase only.

## Required context

Read only:

1. Repository `AGENTS.md`
2. `context/STATUS.md`
3. `context/GAMEPLAY.md`
4. `context/ARCHITECTURE.md`
5. `context/MOBILE.md`
6. This task file

Inspect only these implementation surfaces and direct dependencies:

- `Assets/CatMe/Scripts/Cat/HomeRoomCatIntegration.cs`
- `Assets/CatMe/Scripts/Cat/CatMotor.cs`
- `Assets/CatMe/Scripts/Cat/CatSleepInteraction.cs`
- `Assets/CatMe/Scripts/Toys/LaserToyInteraction.cs`
- `Assets/CatMe/Scripts/Save/` if it already exists
- existing Call, petting, and camera input components only where needed for regression
- existing Phase 4, 7, 8, and 9 batch validators
- `Assets/CatMe/Scenes/HomeRoom.unity`

Do not inspect the sibling web project, generated Unity folders, the full product specification, or unrelated systems.

## Objective

Connect laser play and sleeping into the prototype's first understandable needs loop:

```text
play lowers energy
→ low energy ends play
→ cat walks to its house
→ hidden sleep restores energy
→ cat wakes when rested
→ play becomes available again
```

Add one Energy value, one small temporary mobile meter, and a versioned local save that restores stable Energy/rest state after the app closes. These phases prove behavior, feedback, and return continuity. They do not add hunger, mood, bond, punishment, accounts, cloud sync, rewards, or economy.

## Preserved foundation

Preserve:

- ModelTest acceptance and exactly one validated local cat
- hierarchy, scale, material, skeleton, animation, and floor contact
- Phase 4 locomotion and explicit diagnostic route
- Phase 5 Call
- Phase 6 camera
- Phase 7 direct petting and purr
- Phase 8 manual Sleep/Wake, hidden transition, renderer restoration, and `Zzz`
- Phase 9 toy activation, target validation, camera ownership, reusable dot, catch response, and three-catch session
- existing room, interaction points, and baked NavMesh

Use only the validated local GLB. Do not change the room, NavMesh, camera limits, locomotion tuning, imported model, or animation clips.

## Energy model

Create one focused component:

```text
Assets/CatMe/Scripts/Cat/CatEnergy.cs
```

`CatEnergy` owns:

- current energy
- clamping and thresholds
- laser catch cost
- sleep recovery
- low-energy rest request
- rested wake request
- one temporary energy meter
- focused diagnostics

Use these prototype defaults unless an existing implementation constraint requires a very small adjustment:

```text
maximum energy: 100
starting energy: 100
energy cost per successful laser catch: 20
low-energy threshold: 25
automatic wake threshold: 80
sleep recovery: approximately 15 energy per second
```

Use unscaled delta time for recovery so validation is deterministic. Clamp energy to `0–100`. Energy never becomes negative.

Do not add passive awake drain. Call, petting, camera use, and app idle time do not consume energy in this phase.

## Ownership and integration

- Compose `CatEnergy` after the existing motor, sleep, and laser components are ready.
- Pass direct component references from `HomeRoomCatIntegration`; do not use repeated scene-wide searches.
- Add the smallest explicit events or methods needed for laser catches and sleep state changes.
- `LaserToyInteraction` may consult a narrow play-availability property or callback supplied by `CatEnergy`; do not make it own energy values.
- `CatSleepInteraction` remains the owner of walking to the house, entry, hidden sleep, wake, renderer visibility, and its control.
- `CatEnergy` requests sleep or wake through explicit public methods. It must not translate the cat, manipulate the NavMeshAgent, hide renderers, or change the `Zzz` cue directly.
- `CatMotor` remains the only owner of navigation and gameplay-root movement.

Do not add `CatBrain`, a general needs framework, event bus, singleton, service locator, or dependency-injection framework.

## Play drain and low-energy behavior

- Deduct energy only after a successful laser catch completes and the cat's temporary catch pose has returned to neutral.
- Each catch deducts exactly one cost. Duplicate events must not double-charge.
- At energy above the low threshold, the current laser session continues normally.
- When a catch lowers energy to or below `25`, finish or cancel the current laser session cleanly after that catch.
- Release the laser pointer, dot, camera reservation, procedural pose, and motor activity lock before requesting rest.
- Reject attempts to start a new laser session while energy is at or below the low threshold.
- A rejected low-energy toy tap must not create a dot, move the cat, vibrate, or take camera ownership.
- Call and petting can remain available until the sleep activity takes ownership; do not add punitive global disablement.

## Automatic rest and recovery

- After a low-energy laser shutdown, request sleep through the existing Phase 8 flow.
- The cat must walk to `SleepApproach`, enter the existing house, become hidden, and show the existing `Zzz` cue.
- Begin recovery only after `CatSleepInteraction` reaches `SleepingHidden`.
- Do not recover while walking to the house, entering, awake, or exiting.
- Update energy smoothly while sleeping.
- At energy `80`, request wake once through the existing Phase 8 wake flow.
- After wake completes, normal Call, petting, manual Sleep, camera, and laser availability return.
- If the player manually starts Sleep at higher energy, recover using the same rule and wake automatically at `80` or immediately if already at or above it after the entry completes.
- Guard every automatic sleep and wake request so it is sent once per transition.

No guilt messaging, illness, death, bond loss, failure state, or punishment is allowed.

## Temporary energy meter

- Add one small portrait-safe UGUI meter labelled `Energy`.
- Reuse the existing runtime Canvas and safe-area layout.
- Place it away from the Call and Sleep controls.
- Use one background and one fill image; update fill without creating objects every frame.
- Use a calm normal colour and a readable low-energy colour below the threshold.
- The meter must not block room gestures outside its own small rectangle.
- Keep it functional and plain. Do not introduce final HUD art, icons, animation systems, menus, or a general UI framework.

## Local save and return

Create one focused component and serializable data contract:

```text
Assets/CatMe/Scripts/Save/CatLocalSave.cs
Assets/CatMe/Scripts/Save/CatSaveData.cs
```

Persist a small versioned JSON file under `Application.persistentDataPath`. Do not use PlayerPrefs for gameplay state.

Save only stable data:

```text
schemaVersion
energy
stable state: Awake or Sleeping
savedAtUtc Unix timestamp
```

Requirements:

- Use schema version `1` and keep external JSON separate from scene objects.
- Load once after the HomeRoom cat, Energy, Sleep, and Laser components are ready.
- Missing save starts with Energy `100` and Awake state.
- Corrupt, incomplete, non-finite, out-of-range, or unsupported-version data falls back safely without crashing or partially applying state.
- Clamp restored Energy to `0–100`.
- Save after meaningful Energy or stable sleep-state changes, on app pause/background, and on quit.
- Debounce ordinary writes so recovery does not write every frame.
- Write through a temporary file and replace the final file only after serialization succeeds, so interruption cannot leave a half-written primary save.
- Do not serialize GameObjects, component references, transforms, NavMesh paths, pointers, current laser targets, procedural poses, transitions, or activity locks.
- Never restore a laser session, Call movement, pet stroke, entry/exit transition, or partially completed catch.
- A saved `Awake` state returns awake with the restored Energy and all normal controls available.
- A saved `Sleeping` state applies capped offline recovery, then either resumes the existing sleep flow if still below wake threshold or returns awake if already rested.
- Cap offline recovery to at most `50` Energy and ignore negative/future elapsed time.
- Offline recovery is allowed only when the stable saved state was `Sleeping`.
- After load, normal runtime recovery and automatic wake continue through the existing Phase 10 logic.
- Do not add guilt messages, daily streaks, notifications, calendar logic, or background execution.

Use a test-specific filename or injected storage path for batch validation. Validators must not overwrite the player's ordinary local save.

## Public validation hooks

Expose read-only diagnostics for:

- current and maximum energy
- total energy spent
- total energy recovered
- processed catch count
- low-energy trigger count
- automatic sleep request count
- automatic wake request count
- whether play is currently allowed
- whether recovery is active
- minimum and maximum observed energy
- number of meter instances
- whether a save was loaded or defaulted
- successful save count
- successful load count
- corrupt-save fallback count
- offline Energy recovered
- restored stable state

Provide a narrow validation-only way to start energy at a controlled value or apply a validated catch result without simulating screen input. It must use the same production drain and threshold logic.

Log one concise Energy validation summary with `PASS` or `FAIL`. Do not log every recovery frame.

## In scope

- `CatEnergy.cs` and `.meta`
- `CatLocalSave.cs`, `CatSaveData.cs`, and `.meta` files
- minimal explicit catch notification and clean cancellation support in `LaserToyInteraction`
- minimal explicit sleep/wake request or state notification support in `CatSleepInteraction`
- HomeRoom composition with direct references
- one temporary Energy meter
- one focused combined Phase 10–11 batch validator
- `context/STATUS.md` update after validation

## Out of scope

- Hunger, mood, bond, relationship stages, rewards, tokens, currency, progression, or economy
- Accounts, cloud sync, remote storage, login, notifications, background execution, or cross-device migration
- Passive awake drain, real-time clocks, daily limits, punishment, illness, or death
- Feeding, meow action, memories, shop, new toys, or autonomous exploration
- New animations, audio, particles, CatFocus, Activity camera, lighting, or room changes
- Model optimization, asset generation, providers, API calls, uploads, or downloads
- Final UI and production iOS completion claims

## Acceptance checks

1. Unity compiles with zero new C# errors.
2. ModelTest and the explicit Phase 4 11-leg validator still report `PASS`.
3. Phase 7 petting, Phase 8 two-cycle Sleep/Wake, and Phase 9 two-session laser validators still report `PASS`.
4. Normal HomeRoom starts with Energy `100`, one meter, and all existing interactions available.
5. Each completed laser catch deducts exactly `20` once; rejected targets and cancelled gestures deduct nothing.
6. Energy clamps to `0–100` and no passive awake drain occurs.
7. A catch that crosses the low threshold cleanly ends the laser activity, restores its pose and pointer ownership, hides the dot, and releases its motor lock.
8. Low energy prevents a new laser session without moving the cat or taking input ownership.
9. The low-energy flow requests sleep once, uses the existing approach and hidden transition, and does not bypass `CatSleepInteraction` or `CatMotor`.
10. Recovery begins only in `SleepingHidden`, rises smoothly, and stops outside that state.
11. At Energy `80`, wake is requested once; after wake, the cat is visible at `SleepApproach` and existing interactions resume.
12. The meter accurately reflects normal, low, recovering, and rested energy without duplicate instances.
13. Two low-energy rest/recovery cycles work without duplicate charges, sleep/wake requests, UI, locks, or transform drift.
14. Camera gesture ownership and laser camera delta remain unchanged.
15. The combined Phase 10–11 validator reports `PASS`, Unity Console has zero current C# errors, and `git diff --check` passes.
16. A versioned JSON save persists Energy, stable Awake/Sleeping state, and UTC save time under `Application.persistentDataPath`.
17. Missing save data defaults safely to Energy `100` and Awake.
18. Corrupt, invalid, and unsupported-version save data falls back safely without crashing or partially restoring state.
19. Awake saves restore Energy and normal controls without restoring transient movement, laser, pointer, or pose state.
20. Sleeping saves apply only capped offline recovery, never more than `50` Energy, and either resume sleep or return rested according to the wake threshold.
21. Pause/background and quit paths flush the latest stable state; ordinary recovery writes remain debounced.
22. The save validator uses isolated test storage and leaves the player's normal save untouched.
23. Two save/reload cases and one corrupt-save case pass without duplicate UI, components, locks, or drift.
24. No other needs, provider, room, animation, final UI, cloud, account, or economy system is introduced.

## Validation

1. Run a focused Unity C# compile.
2. Run Phase 4, Phase 7, Phase 8, and Phase 9 batch validators.
3. In normal HomeRoom, confirm Energy starts at `100` and the meter appears once without overlapping controls.
4. Complete laser catches and confirm exact `20` deductions only for successful catches.
5. Cross the low threshold and confirm the laser session releases cleanly before automatic sleep begins.
6. Attempt to restart laser play at low energy and confirm rejection without input capture or movement.
7. Confirm recovery occurs only while hidden and the meter updates smoothly.
8. Confirm automatic wake at `80` and restored Call, petting, Sleep, camera, and laser availability.
9. Complete a second low-energy rest/recovery cycle.
10. Save an Awake state at a known Energy, reload through the production loader, and confirm exact restoration with no transient activity.
11. Save a Sleeping state with a controlled past timestamp, reload, and confirm capped offline recovery plus the correct sleep/wake decision.
12. Load corrupt and unsupported-version fixtures and confirm safe default behavior.
13. Confirm validation uses isolated test storage and does not modify the normal player save.
14. Run the focused combined Phase 10–11 validator and confirm `PASS`.
15. Run `git diff --check`.
16. Make no provider calls and no production build.

Physical portrait layout, perceived drain/recovery pacing, and pause/background lifecycle behavior remain device checks.

## Completion report

Report:

- files changed
- energy defaults and thresholds
- exact catch drain behavior
- low-energy laser cancellation and rejection behavior
- automatic sleep, recovery, and wake results
- meter placement and instance count
- save location, schema, atomic-write behavior, and debounce behavior
- Awake restore, Sleeping restore, offline recovery, and corrupt-save results
- confirmation that transient activities are never restored
- two-cycle counters and duplication results
- Phase 4, 7, 8, and 9 regression results
- compile and Console result
- remaining physical-device checks
