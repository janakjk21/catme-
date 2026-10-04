# WornTag docs

These docs are the source of truth. Code follows them; when code and a doc disagree, the doc is right until the owner changes it.

## Start here

1. [ROADMAP.md](ROADMAP.md): the ordered task list. Work only on the current task (named in [../context/STATUS.md](../context/STATUS.md)).
2. The doc that task follows, from the list below.

## All docs

| Doc | Read it for |
|---|---|
| [ROADMAP.md](ROADMAP.md) | what to build next, in order, and when each task is done |
| [ONBOARDING_FLOW.md](ONBOARDING_FLOW.md) | the onboarding flow: login, cat or dog, pet photo, selfie, garden |
| [DESIGN.md](DESIGN.md) | design principles, colours, type, layout grid, components, which skills to use |
| [MENUS_AND_ACCOUNT.md](MENUS_AND_ACCOUNT.md) | menu, account, settings, your pets, album, store and garden controls: flow, states and the white / bone / sage look |
| [LAUNCH_AUDIT.md](LAUNCH_AUDIT.md) | pre-launch audit: every open issue with file references, fixes and "done when"; task order in `context/CLAUDE_TASKS.md` |
| [COPY.md](COPY.md) | every word the player reads |
| [GAMEPLAY_AND_WORLD.md](GAMEPLAY_AND_WORLD.md) | the map, how the player and the pet move, controls, weather and wildlife |
| [PHOTO_TO_3D.md](PHOTO_TO_3D.md) | every step from a photo to the 3D pet: app, API, Firebase, Tripo and Meshy |
| [PETS_AND_STORE.md](PETS_AND_STORE.md) | the pet list, adding pets, the store, free creations, payments |
| [ACCOUNTS_AND_PRIVACY.md](ACCOUNTS_AND_PRIVACY.md) | sign-in, what data we keep, deleting photos and accounts |
| [ARCHITECTURE.md](ARCHITECTURE.md) | how the app, server and Firebase fit together, and where code lives |
| [SETUP.md](SETUP.md) | running in the editor, Firebase, server setup, iPhone build and signing |
| [XCODE_BUILD.md](XCODE_BUILD.md) | every step to put the app on an iPhone: export, pods, signing, install, known problems |
| [ASSETS.md](ASSETS.md) | art, models, third-party packs and their rules |
| [QA_PASS.md](QA_PASS.md) | brief for an agent or person to play the app and report every bug |
| [QUALITY.md](QUALITY.md) | how to test, checklists, odd cases, performance budgets |
| [RELEASE.md](RELEASE.md) | TestFlight and App Store checklist |

## Which doc wins

1. `ROADMAP.md` decides what is worked on.
2. `ONBOARDING_FLOW.md`, `MENUS_AND_ACCOUNT.md`, `COPY.md` and `DESIGN.md` decide what the player sees.
3. The other docs decide how it is built.

Change the doc first, then the code, in the same change. Lasting decisions go in [../context/DECISIONS.md](../context/DECISIONS.md).

## Words we use

| Word | Means |
|---|---|
| WornTag | the product and brand. "CatMe" is the old internal name, still in code, the bundle id and the Firebase project |
| Pet | the player's cat or dog |
| Owner, person, "you" | the player's own 3D character |
| Creation | one server job that turns a photo into a 3D model |
| Place, map | the garden scene the pair walks in (there is one) |
| Test models | stand-in models the server uses until live generation is on |
