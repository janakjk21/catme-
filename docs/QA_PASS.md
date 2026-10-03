# QA pass: play the app and report bugs

A brief for an agent (Codex or Claude) or a person: play WornTag the way a player would, compare it with the docs, and write down everything that is wrong. **Report only; do not fix anything in this pass.**

## Before you start

1. Read `docs/ONBOARDING_FLOW.md`, `docs/GAMEPLAY_AND_WORLD.md`, `docs/COPY.md` and `docs/QUALITY.md`. They say what the app is supposed to do.
2. Open `Assets/CatMe/Scenes/GardenCompanion.unity` in Unity 6000.6.2f1. Keep the Unity window **in front**: in the background Play Mode freezes at frame 1.
3. Turn off Console > **Error Pause**, or Play Mode stops on the first logged error.

## Credits: read this

Live 3D generation is **on**. Every new pet or selfie spends real Tripo and Meshy credits. In this pass:

- Make **at most one** new creation (one cat plus one selfie), and only if the owner said so for this run.
- For everything else, use an account that already has its pet: the onboarding is skipped, and the garden opens with the existing models.

## Driving Unity

- **By hand:** press Play and click.
- **By agent:** the Unity CLI drives the open editor (`unity command editor_play`, `unity command eval '...'`, `unity command capture_game_view -- --save_path Temp/qa/01.png`). The project already has the `com.unity.pipeline` package. `capture_game_view` saves under `Assets/`; delete the files afterwards.
- **Photos without the file dialog:** in Play Mode, set `CatMe.Garden.DeviceBridge.EditorAutoPickPet` and `EditorAutoPickHuman` to image paths (examples in `Assets/CatMe/Resources/Onboarding/Generated/`).
- **Start as a new player:** sign out (pause > Account > Sign out), stop Play Mode, and move `~/Library/Application Support/CatMe/WornTag/` aside.

## What to check

1. **Onboarding:** every row of the screen table in `ONBOARDING_FLOW.md`, every word against `COPY.md`, and the picture framing and layout against the design (https://claude.ai/artifact/BQxtq2FK8TeEqgXXXNbL1M).
2. **Returning player:** relaunch skips the login and goes to the garden.
3. **Garden:** drag to walk, the pet button (Ball, Roam, Call), tap and hold the pet, bench, tent and house buttons, the camera, every pause menu item, settings switches.
4. **Pet behaviour:** follows, leads to the tent in rain, speaks, cat versus dog differences (`GAMEPLAY_AND_WORLD.md`).
5. **World:** the day changing, rain, butterflies, visiting dog, wildlife, the pond.
6. **Odd cases** in `QUALITY.md`: no internet, app closed while a pet is being made.
7. **Console:** every warning and error during the run.

## How to report

Write the results to `context/QA_REPORT.md`, newest pass on top, one row per problem:

| # | Where | What happened | What should happen (doc and section) | Steps | Severity | Screenshot |
|---|---|---|---|---|---|---|

Severity: **blocker** (cannot continue), **major** (wrong behaviour), **minor** (looks or words), **idea** (not a bug). End with what was not tested and why.
