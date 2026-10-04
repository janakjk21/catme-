# Pet sounds: which clip, when, and how to keep it from getting annoying

The goal: a pet that sounds alive, not a sound button. A real cat meows a few times an hour, mostly when it wants something; a real dog barks rarely on a calm walk and breathes, sighs and pants the rest of the time. **Silence is part of the design.** When in doubt, play nothing and let the hearts or the animation say it.

## 1. What we have

Two packs were bought (royalty-free WAV, 44.1 kHz, stereo); originals archived in `../catme-asset-archive/audio-sources/`.

### Kitten Meow Pack (Stormwave Audio), 10 clips

| File | Length | Character | Use as |
|---|---:|---|---|
| `Kitten Meow 6.wav` | 0.14 s | tiny chirp | **chirp**: quick acknowledgement |
| `Kitten Meow 1.wav` | 0.65 s | short meow | **hello-short** |
| `Kitten Meow 10.wav` | 1.12 s | medium | **hello** |
| `Kitten Meow 2.wav` | 1.31 s | medium | **hello** |
| `Kitten Meow 9.wav` | 1.38 s | medium | **hello** |
| `Kitten Meow 3.wav` | 1.49 s | medium | **ask** (wants something) |
| `Kitten Meow 4.wav` | 1.64 s | longer | **ask** |
| `Kitten Meow 5.wav` | 1.68 s | longer | **ask** |
| `Kitten Meow 7.wav` | 1.72 s | longer | **ask** |
| `Kitten Meow 8.wav` | 2.40 s | long, plaintive | **plead**: hungry / wants to go home only |

Listen to each before assigning; move a clip to another set if it sounds wrong there. Lengths are measured; the "character" column is a first guess from length.

These are **kitten** voices. For an adult cat, play them at pitch **0.88-0.95** (section 4); keep 1.0-1.08 only if the pet is a kitten.

### Large Dog Bark Pack (Stormwave Audio), 7 clips (German Shepherd)

| File | Length | Use as |
|---|---:|---|
| `Large Dog Bark 1.wav` | 0.18 s | **bark** |
| `Large Dog Bark 2.wav` | 0.18 s | **bark** |
| `Large Dog Bark 3.wav` | 0.20 s | **bark** |
| `Large Dog Bark 5.wav` | 0.20 s | **bark** |
| `Large Dog Bark 6.wav` | 0.24 s | **bark** |
| `Large Dog Bark 4.wav` | 0.26 s | **bark** (slightly heavier) |
| `Large Dog Bark 7.wav` | 0.53 s | **double-bark** (excited) |

This pack has **barks only**. There are no recordings of panting, sighs, whines, eating or drinking, so those keep today's generated stand-ins in `PetSounds` until recordings are bought (section 9). Small dogs (Chihuahua, etc.) should not use a German Shepherd bark unaltered (section 4).

Existing clips kept: `cat-purr.mp3` (purr loop), `cat_mewfood.wav` (eating), `cat-meow.mp3` (add to **hello** if it fits), plus ambience.

## 2. Import (files already placed, 2026-10-04)

The clips are already in the project, renamed by set, so they load by prefix:

| Folder | Files |
|---|---|
| `Assets/CatMe/Audio/Resources/CatMeAudio/Cat/` | `cat-chirp-1` (Meow 6), `cat-helloshort-1` (Meow 1), `cat-hello-1..3` (Meows 10, 2, 9), `cat-ask-1..4` (Meows 3, 4, 5, 7), `cat-plead-1` (Meow 8) |
| `Assets/CatMe/Audio/Resources/CatMeAudio/Dog/` | `dog-bark-1..6` (Barks 1, 2, 3, 5, 6, 4), `dog-barkdouble-1` (Bark 7) |

Load with `Resources.LoadAll<AudioClip>("CatMeAudio/Cat")` and group by the name before the last `-`.

- **Import settings in code, not by hand:** add an `AssetPostprocessor` in `Assets/CatMe/Editor/PetAudioImport.cs` for paths under `CatMeAudio/Cat` and `CatMeAudio/Dog`: Force To Mono on, Load Type Decompress On Load, Compression ADPCM, Preload on. The purr loop: Vorbis quality 0.6, Compressed In Memory.
- Trim leading silence if a clip starts late (more than ~30 ms); match loudness within a set (about -16 LUFS for meows, -18 for barks so barks never jump out). If that needs an audio editor, list the clips for the owner instead.
- The original zips are kept outside the project in `../catme-asset-archive/audio-sources/` for the licence record.

## 3. Code changes in `Pets/PetSounds.cs`

Today `PetSounds.Clip()` caches **one clip per cue**, so every "hello" is the identical file at identical pitch: that is what makes it repetitive. Replace it with sets:

1. **Clip sets, not clips.** Load every clip whose name starts with the set prefix (`cat-hello-*`). A set may be empty; then fall back as today.
2. **Shuffle bag, no repeats.** Pick from a shuffled list; never play the same clip twice in a row, and with 4+ clips avoid the last two. Refill and reshuffle when the bag is empty, making sure the first pick of the new bag isn't the last one played.
3. **Variation on every play.** Pitch random ±6% around the species base, volume ±12%, and for meows a start delay of 0-120 ms so a reaction isn't machine-instant. Use one `AudioSource` per pet (`spatialBlend` 1, min distance 1.5 m, max 20 m, logarithmic) and set `pitch` per play; don't use `PlayOneShot` when you need pitch (it ignores per-shot pitch), and don't use `AudioSource.PlayClipAtPoint` (it spawns a new object each time and can't be cooled down).
4. **Cooldowns (the most important rule).**
   - Per cue: each cue has a minimum gap (table below). A trigger inside the gap plays **nothing** (or a chirp if allowed).
   - Per pet "voice budget": at most **1 vocalisation every 6 s**, and at most **6 per minute** for a cat and **4 per minute** for barks. Purr and breathing don't count.
   - Never two vocalisations overlapping on the same pet.
5. **Chance, not certainty.** Most triggers play with a probability (table below). Repeated triggers in a short time lower the chance (a tap 3 times in 10 s: 100%, then 40%, then chirp only).
6. **Priority.** If a high-priority cue arrives (call answered, reveal), it can cut a low one (chirp); a low one never interrupts a high one.
7. **Respect settings.** Sound off: nothing. Reduce motion has no effect on sound. Inside rooms, volume ×0.85; during rain, vocal volume ×0.8 (the rain is loud) and skip chirps.
8. Keep the public API (`Play`, `PlayAt`, `Clip`) so the call sites don't all change; add `PetSounds.Say(PetVoice voice, Transform pet)` and route `PlayAt` through it.

## 4. Species and size voices

| Pet | Base pitch | Notes |
|---|---:|---|
| Adult cat | 0.92 | kitten pack pitched down |
| Kitten | 1.04 | |
| Large dog (shepherd, lab, husky) | 1.00 | |
| Medium dog | 1.12 | |
| Small dog (chihuahua, pomeranian) | 1.30 | until real small-dog barks exist; buy the Stormwave **Small Dog Bark Pack** to replace this |

Store size on the pet (`PetProfile.voice` = small / medium / large, chosen once from breed or the model's height, default medium). Don't change a pet's voice later; the same pet must always sound like itself.

## 5. When to play what

| Moment (code) | Cat | Dog | Chance | Cooldown |
|---|---|---|---:|---:|
| Player taps the pet (`CatPetInput.Tap`) | chirp; every 3rd tap at most a hello-short | soft pant (stand-in); 1 bark at most every 4th tap | 100% chirp / 35% hello | 4 s |
| Press and hold to pet (`CatPetInput.StartPet`) | purr loop, fades in 0.4 s, out 0.6 s; no meows while held | panting loop (stand-in), then 1 content sigh on release | 100% | none |
| Call answered (`GardenCompanionDemo.CallCat`) | hello (when it arrives, not when called) | 1 bark, or double-bark if it was far away | 80% | 20 s |
| Ball: pet reaches / brings back the ball (`UpdateCat` ~400, ~452) | chirp on pounce | bark or double-bark when dropping it at your feet | 60% | 15 s |
| Pet invites you somewhere (`Session/CompanionLeader`: tent in rain, bench, home) | ask, once at the start of the lead | 1 bark at the start | 100% once | per lead |
| Hungry / wants to go home (`CompanionLeader`) | **plead** (the only place `Kitten Meow 8` plays), repeat at most once after 25 s if you don't follow | 1 soft whine (stand-in) | 100% / 50% | 25 s |
| Feed button (`Interactions.Feed`) | ask | double-bark | 100% | 30 s |
| Eating (`Interactions.EatRoutine`) | `cat_mewfood` crunch loop for the eating time; no meows | crunch stand-in | 100% | none |
| Full after eating (`EatRoutine` end) | purr 3 s | content sigh (stand-in) | 100% | none |
| New photo in the frame (`Interactions.AdmireFrame`) | hello-short | 1 bark | 70% | 60 s |
| Enter the tent (`Interactions.EnterTent`) | nothing (the rain line and fire are enough) | nothing | 0% | |
| Settle by the fire / sleep (`SettleByFire`, `SleepRoutine`) | purr, quiet (vol 0.4), fade out after 6 s | sigh (stand-in) | 100% | none |
| Reveal in the garden (`CatMeAppFlow.RevealInGarden`) | hello, the clearest clip in the set | double-bark | 100% | once |
| Idle while walking (new, optional) | a chirp now and then | nothing (breathing only) | once per 60-120 s, random | 60 s |
| Notices a visitor (dog visitor, wildlife) | nothing, or a chirp at most | 1 bark at most, only for the visiting dog | 30% | 90 s |

## 6. Never do

- Never play the same clip twice in a row, or on a fixed rotation (1, 2, 3, 1, 2, 3 is as annoying as one clip).
- Never meow or bark on every tap, every step or every UI button.
- Never play a vocalisation from UI code (menus, toasts, buttons); pet sounds come only from the pet.
- Never stack sounds: one voice at a time per pet.
- Never use the plead clip for anything but hungry / going home, or it loses its meaning.
- Never loop meows or barks; only purr, panting and eating may loop.
- Never play pet sounds during onboarding screens or menus; pause them while a menu sheet is open.

## 7. Tests (EditMode, in `Assets/CatMe/Tests`)

Seeded random, fake clock:
- 200 picks from a set of 5 never repeat back-to-back, and use every clip at least 30 times.
- Cooldowns: two Hello triggers 2 s apart produce one sound; the per-minute budget caps a burst of 20 triggers.
- Pitch stays within base ±6% for each species and size.
- Sound off produces no plays.

## 8. Check by ear (owner, Play Mode)

Walk for 5 minutes with the cat and then the dog, tapping, calling and playing ball as a player would. Pass if you never hear the same clip twice in a row, never hear more than one meow or bark in any 6 s, and the plead meow appears only when the pet asks to go home.

## 9. Still missing (to buy or generate)

Cat: 2-3 more purr variations, a trill, a sleepy yawn. Dog: happy panting loop, content sigh, soft whine, eating crunch, drinking, shake-off, and **small-dog barks** (Stormwave Small Dog Bark Pack). Footsteps for both on grass, wood and path. Until then the generated stand-ins stay for those cues only.
