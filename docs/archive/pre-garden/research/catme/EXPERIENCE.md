# Experience, Cat Behaviour, Room, and Visual Design

## Current prototype reality

The 2026-09-24 repository status and focused code inspection describe an iOS Unity prototype, not a verified public app. It has a HomeRoom with the local known-good animated cat; NavMesh movement; a call action; direct petting with purr/haptic intent; laser play; a reusable ball/mouse toy; feeding with an occluded eating presentation; covered sleep/wake; energy and local save; camera orbit/pinch; a local photo/journal memory direction; and development controls for loading a saved model or choosing a photo and requesting server generation.

The first five behaviours are supported by repository contracts/status reports and their batch validations, but all are still subject to real device feel. The generation panel is gated to development/debug builds. Its source supports Editor file selection and an iOS photo picker and explicitly submits a selected photo to the configured server. The status says the real photo upload/generation, server button on the live service, and iPhone picker/network/touch use have not been tested. A successful return from checkout or an unsigned build does not count as customer validation. The physical iPhone launch is blocked by signing/provisioning in the recorded status.

Current visual work includes the warm cream and pale-oak arched-window direction, furniture and a portrait room camera, according to current status and decisions. It has been visually inspected in Unity runtime renders, not on a validated phone. The currently known-good cat has a walk clip but no supported idle/sit/stretch/groom/pounce set. Eating and sleeping are partly sold through occlusion and procedural presentation. There is no evidence yet that users find the room attractive or the cat convincing.

### Likely first-session journey and friction

1. Open HomeRoom and see a cat in a furnished room. Does the screen explain what can be touched, and is the cat large enough in portrait?
2. Call/pet/try a toy. Does the user understand the difference between camera orbit and interaction? Does the response visibly acknowledge touch quickly?
3. Feed or put the cat to sleep. Are these delightful or simply chores/temporary UI buttons?
4. Choose a personal photo in the debug generation surface. This is a developer control, not finished consumer onboarding. There is no validated photo guidance, time promise, preview/consent language, purchase choice, error recovery, or deletion flow established by this audit.
5. Return later. Local persistence and journal scaffolding exist, but repeated use and a satisfying reason to return have not been tested.

## Player promise and core loop hypotheses

Candidate promise: **“A little home for a cat that feels like yours.”** Its user-visible proof must be the cat’s identity plus behaviour. Candidate short session:

`Open → notice what the cat is doing → make one invitation (pet/call/toy) or simply watch → get a specific response → keep or discover one moment → leave without guilt.`

This should be compared with two alternatives: (1) build and decorate a cozy cat home; (2) make/share a personalized cat image or clip. A companion loop without varied cat behaviour risks becoming passive viewing. A care loop can create chores. A room loop may be more rewarding but requires content volume. A creator can be satisfying once but may not earn repeat use.

### Emotional design finding and recommendation

The useful design question is not how to make the cat display affection constantly. It is how to make the player notice small, specific, contingent responses and feel that their invitation mattered. Cat body-language guidance notes cats are subtle, that small changes matter, and that a single cue such as purring is not enough to infer comfort. A 2017 virtual-pet study reports a survey of 774 and a VR lab study with 30 participants. In that VR setting, greater player control increased engagement duration by about 33% versus limited control, and participants described weak emotional responses or absent awareness of the player's actions as breaking immersion. The study also reports participant desires for context-sensitive behavior, individual personality, and relationship continuity. **This is unusually relevant evidence for responsive interaction, but VR dog/pet findings do not prove mobile cat-game behavior or commercial demand.** CatMe should test the transferable design implication: autonomy alone risks passive viewing; control alone risks puppet-like responses; try a player invitation followed by a believable cat choice and clear feedback. [Full paper](https://www.researchgate.net/profile/Chaolan-Lin/publication/321446731_Exploring_Affection-Oriented_Virtual_Pet_Game_Design_Strategies_in_VR_Attachment_Motivations_and_Expectations_of_Users_of_Pet_Games/links/5a2216c5a6fdcc8e866521ad/Exploring_Affection-Oriented-Virtual-Pet-Game-Design-Strategies-in-VR-Attachment-Motivations-and-Expectations-of-Users-of-Pet-Games.pdf); [Cats Protection body language](https://www.cats.org.uk/help-and-advice/cat-behaviour/cat-body-language)

**Lead game thesis to test:** a tiny, personal “cat moment” game: each visit offers an ambient scene and one optional invitation, while the cat retains limited, legible autonomy. It is closer to a living portrait plus toy interactions than a needs-management simulator. The emotional unit is a brief, recognizable exchange: “I called; the ears turned; my cat decided whether to come.” The player is not rewarded for forcing a response, and can simply watch. This middle path is supported by the VR study's warning against unresponsive companions and its signal that more direct control can sustain engagement; the mobile design test should measure the balance rather than import the VR result as a rule.

Why this best fits the small-scale goal: it can make a few people feel strongly served without requiring a large catalogue, multiplayer, live events, or a daily quest schedule. A tiny audience still needs a reason to reopen; the first tests must show a voluntarily repeated moment, not merely a positive first reaction. Neko Atsume’s low-pressure observation and photo sharing provide a useful design precedent, while its broad cat collection differs from CatMe’s one-personal-cat identity. A secondary design analysis describes its simple loop, open-endedness, choice and camera sharing; this is an independent interpretation rather than developer testimony. [Neko Atsume design breakdown](https://alexiamandeville.medium.com/game-design-breakdown-the-simplicity-of-neko-atsume-a8616a937a47); current US listing describes “place playthings and snacks” then wait and watch. [Neko Atsume 2 US listing](https://apps.apple.com/us/app/neko-atsume-2/id6499131935?platform=ipad)

### What the game should look and feel like

**Lead visual direction (recommendation):** “a believable little stage, softened into illustration.” Use a real-feeling cat silhouette and owner-specific coat pattern, but avoid fur-level photorealism until the motion system is equally convincing. Use simplified, tactile room materials, warm daylight, broad value contrast, quiet colors, and a small number of recognizable props. This hybrid is intended to preserve “that is my cat” while hiding generated mesh defects and allowing consistent animation. It is not yet user-tested. Avoid a full storybook flattening if it erases coat detail, and avoid glossy toy plastic if it makes the cat feel like a collectible object.

**Composition:** the cat is the first visual priority and should occupy enough portrait-screen area for coat and posture to read without zooming. Keep a clean floor action zone in the lower/middle frame, a bright window or resting focal point behind/above, and furniture to the edges as framing rather than occlusion. Owner steering on 2026-09-24 selected first-person room movement as the next prototype direction: keep the cat large and readable while the player approaches food, calls, throws, and follows the cat. Keep third-person orbit optional for comparison, with a “Find cat” action in first-person and touch zones that do not conflict with the cat or toy. UI should be sparse, lower-screen and contextual; avoid persistent need bars and shop buttons over the cat. The first-person direction still needs customer and phone testing.

**Light, palette, and motion:** soft directional window light should reveal coat markings and facial plane, with gentle contact shadows; avoid bloom, deep black shadows, high-gloss reflections, and animated camera shake. Use restrained warm neutrals as room support and one accent color for actionable toys. Idle motion should have pauses; anticipation should be readable before the pounce; the consequence should be visible; recovery should be charming but brief. Sound should add close, quiet foley (paw taps, fabric, toy roll, room tone) and optional music rather than masking the cat. Haptics should be light, optional, and limited to meaningful contact. Every important event must remain legible with sound off.

This recommendation is based on the design need to keep the individual cat legible, the current Unity room direction, and adjacent products' contrasting presentations: the US Neko Atsume listing has a simple observation loop, Usagi Shima explicitly describes hand-drawn/animated art and a decorated island, and My Talking Angela layers fashion/activity systems and frequent event art. These prove the products choose distinct coherent art/game bundles; they do not establish which CatMe audience will prefer. [Usagi Shima US listing](https://apps.apple.com/us/app/usagi-shima-cute-bunny-game/id1632728038); [My Talking Angela 2 US listing](https://apps.apple.com/us/app/my-talking-angela-2/id1536584509)

### Three interaction storyboards

| Interaction | Trigger → anticipation → action → response | Sound / camera / duration | Variation and payoff | Current gap |
|---|---|---|---|---|
| “Did you hear me?” | Player taps call → cat pauses and turns one ear/head toward the sound → waits a beat → walks over if nearby/available, or looks then resumes its chosen activity. | Tiny room-tone duck; ear/head cue; camera stays still. 3–7 seconds. | Different response timing and route, but never fake personality claims. Payoff: the player sees their input registered and the cat retains agency. | Current call moves to a destination; response acting and varied availability need validation. |
| “Box inspection” | Player places/taps box → cat notices, approaches, sniffs edge → puts paw in, circles, enters or abandons it. | Cardboard rustle, paw taps; slight camera ease only after the cat commits. 8–15 seconds. | Three outcomes and a return glance; one playful stumble may occur when entering/exiting. Payoff: humorous small story caused by a clear object. | Needs authored interaction animation, contact staging, and outcome state. |
| “Almost caught it” | Player drags a feather/toy → cat tracks, lowers body, butt wiggle, pounce → toy slips/rolls a short distance or is caught → cat bats/holds, then settles. | Soft toy scrape, paw impact, brief silence before pounce; no screen shake. 6–12 seconds. | Catch/miss/recover weighted to be satisfying; every chase ends in catch or calm stop. Payoff: tension, comedy, and successful completion rather than frustration. | Existing laser/ball movement does not yet establish an authored pounce, contact, or chase finish. |

Each interaction should have a clear result within about one second of the input (not necessarily complete within one second), then a short anticipation, one readable action, contact/reaction, and a natural stopping point. Proposed timing is a prototyping target, not measured optimum. Provide tap-to-interact plus optional drag only where spatially meaningful. Avoid making the player repeatedly tap to activate hidden states.

### “Jiggly and funny” without making the cat feel weightless

Make the comedy come from recognizable feline cause-and-effect, then amplify timing slightly: rear-end wiggle before a pounce; a too-big paw reach that nudges the toy away; head disappearing into a box while the tail stays visible; a careful step onto a soft cushion followed by a small balance correction; one paw shake after touching something surprising; freeze-and-look after a toy rolls under furniture. Keep the spine and paws grounded. The secondary jiggle belongs in tail, ears, fur/soft body overlap, and brief recoil—not whole-body elastic squash on every step. A short hold before the action and a clean look-back can make a small motion funny without exaggerated face acting.

Design the joke as: **clear stimulus → readable preparation → slightly unexpected but plausible outcome → cat notices outcome → settles**. For example, the cat crouches at a feather, wiggles, pounces, misses by a small amount, watches the feather roll, then sits as if that was the plan. The player should understand why it happened. If randomness changes the outcome, preserve a catch/recovery ending so it feels playful rather than broken. This is an authored animation/timing recommendation; the target animation has not been prototyped or tested with players.

### Visit design

- **First minute:** see cat immediately; one quiet self-directed behavior; a one-sentence hint (“Tap the feather to invite play”); play the complete toy sequence; see one save/share opportunity only after the moment has earned it. Do not require account, photo, store, tutorial screens, or care meters before first delight.
- **First five minutes:** try one other invitation (call or pet), understand the cat can choose, optionally name the cat and select/add a distinctive marking in a simple preview. Offer photo import as an optional later path with clear privacy and failure terms. Let the user stop after a completed moment.
- **Second visit:** a changed but familiar room state (cat resting elsewhere, toy shifted, light changed) plus a small “last time” memory/album trace. No guilt text, streak, missed reward, hunger penalty, or claim that the pet missed the player.
- **After several days:** one new observed behavior, object use, or player-made room change should be enough to create fresh interest; do not promise endless novelty. Test actual unscripted return before building a content cadence.

### Comparison of product forms for the CatMe scope

| Form | 30 seconds / 5 minutes | Repeat and content burden | Emotional fit / monetization fit | Main failure |
|---|---|---|---|---|
| Personal cat companion (lead) | Brief glance or one responsive invitation / multiple interactions and a photo moment. | Repeats depend on authored behavior variation and continuity; modest room content. | Strongest identity potential; one-time custom cat or a few behavior-rich objects could fit. | Novelty fades if cat has few states; likeness or animation defects damage trust. |
| Cozy observation/collection | Check scene / set props, wait and collect moments. | Naturally supports discovery catalog but expects many cats/items and ongoing additions. | Relaxed; décor and optional small purchases fit. | CatMe has one principal cat, so lacks collection breadth unless it dilutes identity. |
| Mischief/physics sandbox | See a funny accident / move toys, observe chains of antics. | High replay if system-driven, but physics/animation tuning and content testing rise. | Humor and share clips can fit. | Random chaos may feel cheap or break individual-cat realism. |
| Home/decor game | Place/change one object / plan and stage the room. | High item demand; much art/catalogue and layout UX. | Strong cosmetic sales fit; cat risks becoming accessory. | Expensive content treadmill and decorating may overshadow cat relationship. |
| Photo/video creator | Make one shareable image/clip / try styles and edit. | Repeat depends on new templates and sharing use; creator operations. | Clear one-time unlock or paid export possible. | One-off tool, weak return loop, AI output competition. |
| Keepsake/memory space | Save one meaningful moment / revisit a photo timeline. | Low mechanical content but high trust, privacy, and sensitive tone burden. | Personal and possibly giftable. | Emotional promises can hurt or feel exploitative; don't combine grief acquisition with playful monetization without direct research. |
| Cat adventure/puzzle | Start a goal / progress through authored levels. | Strong goals but high level/animation/content burden and different genre promise. | Paid experience/chapters possible. | Moves the product away from personal companion and requires much more authored content. |

The working answer to “Would it remain enjoyable with a standard cat?” must be **yes for the core interaction**, or CatMe is simply selling a novelty model. Personalization should intensify recognition and memory rather than hide a weak game. Conversely, if users only want the personalized cat image, choose a creator product deliberately rather than forcing a repetitive game loop. This is a falsifiable split test, not a product conclusion.

Proposed visit lengths to test, not requirements: 20–40 seconds for an ambient glimpse; 2–5 minutes for one interaction and a discovery; 5–10 minutes for decorating or a toy sequence. A reason to return should come from a new behaviour, player-created context, a memory, or a visible ongoing relationship—not an alarm that the cat suffers when ignored.

## Behaviour sketches to prototype

| Moment | Sequence | Player payoff | Implementation implication / risk |
|---|---|---|---|
| Notice a toy | Pause and orient → watch toy → creep/approach → pounce or bat → inspect outcome → resume or choose another action. | Anticipation, surprise, small comedy. | Timing, target gaze, paw contact and failure/recovery need clarity. Current ball/laser loop validates pathing, not an authored pounce. |
| Call | Hear/notice → ear/head orientation → brief decision → approach at pace matching context → arrive/focus. | Cat seems to have noticed the player rather than teleporting into compliance. | Current call is movement toward an authored destination; subtle head/ear action depends on suitable rig controls. Do not imply real cognition. |
| Petting | Player strokes where invited → cat leans/rubs or purrs; occasionally turns away or pauses. | Responsive touch and agency. | Current direct stroke reaction exists; avoiding arbitrary rejection and demonstrating consent/comfort matters. |
| Box / new object | Detect → sniff → peer → paw/tap → enter or abandon → settle / surprise exit. | Cat-like mischief and an emergent little story. | Requires short animation variety and collision/prop authoring. A box is useful only if interaction works. |
| Window / light patch | Cat spots movement/light → settles and tracks → shifts gaze/position → may return later. | Quiet ambient presence. | Captures observation play; avoid adding constant UI or forced reward. |
| Sleep and return | Wind down → go to familiar rest place → small sleep cue → wake / greeting on a later visit. | Continuity and welcome. | Current hidden sleep flow is an approximation. Never show sadness, lost bond or illness because player was away. |
| Funny near-miss | Stalk → pounce slightly short / toy rolls away → puzzled look → quick recovery. | Warm humour from specific cause and reaction. | Animation readability and sound timing are the joke; random slapstick can break tone. |

Cats communicate subtly; Cats Protection recommends reading body, context and posture rather than interpreting one signal such as purring as happiness. Use ears, head orientation, relaxed posture, whiskers and tail as behavioural cues only where rig support and animation permit. [Body language](https://www.cats.org.uk/help-and-advice/cat-behaviour/cat-body-language). Their play guidance relates play to hunting and recommends toys at distance from the player's body, informing chase loops. [Cats and play](https://www.cats.org.uk/help-and-advice/cat-behaviour/cats-and-play)

Test every interaction with sound off and with one hand. If the cause/effect is unreadable without help text, improve staging and feedback. Purring is a useful sound but should not be the only signal. The player needs to perceive “I touched here; the cat reacted like this.”

## Room and interface research hypotheses

The existing room has a warm cream/oak direction. Compare it at actual portrait-phone scale against a storybook illustrated room and a clean, toy-like stylized room. Use identical cat, action, framing, contrast, and UI so participants compare style rather than content. Ask which feels personal, calm, playful, or generic; which shows markings clearly; and which they expect to play in.

The room should stage actions: leave open floor for a toy, place destinations at edges, use furniture for scale and cover, keep the cat legible in the portrait camera, and frame at least one meaningful focal point. Props should have a behaviour or a deliberate visual role. Prioritize a few interactive objects that create different behaviours over many decorative objects that add navigation clutter.

Test three room plans: (A) small owner-like home with a few personal props, (B) aspirational cozy designer room, (C) uncluttered behavioral stage with one window/bed/toy. Compare cat visibility, navigation, emotional tone and desire to decorate. Check performance and touch hit areas on target phones. The existing `RoomAssetPopulation` decisions preserve a central route and warm arch reference; these are project choices, not user-preference findings.

Interface principle to test: the room and cat are the main screen; controls appear only when useful, have direct labels/icons, respect safe areas, and do not obscure the cat. Offer a clear return to home camera and a simple interaction affordance. Avoid a dashboard of meters until users demonstrate that needs/relationship values improve the experience.

## Visual references and directions

1. **Warm believable miniature home:** cream plaster, pale oak, soft daylight, readable coat markings, restrained UI. Matches current implementation direction. Risk: generic catalog furniture, low realism animation mismatch, or clutter obscures cat.
2. **Illustrated storybook companion:** flatter or painterly material language with expressive silhouette; can make varied generated fur less uncanny. Risk: less apparent “this is my actual cat.”
3. **Playful tactile toy world:** soft rounded props, richer colour, expressive action and comedy. Risk: could drift toward a children’s game and conflict with a quiet personal companion.

Reference products: Cats & Soup describes its environment as a fairytale-like illustration/forest ASMR; Pocket Love emphasizes pastel colour, sweet animations, furniture and screenshot sharing; Neko Atsume demonstrates a calm observation setting; My Cat uses AR, touch reactions and photo memories. These are presentation references, not evidence that CatMe should imitate them. See source links in `EVIDENCE.md`.

Phone-size art test: make 3 stills plus a 10-second identical action clip per direction. Show cat front/side/turning and a key coat marking. Ask for first impression before brand explanation, then recognition, tone, clarity, screen preference and likely action. Do not show only polished stills if motion is the product claim.

## Prototype improvements suggested by this first pass

1. Before adding more features, polish one end-to-end toy interaction with anticipation, readable paw/contact feedback, one variation, and satisfying camera framing.
2. Make the cat’s current action legible at rest; the walk-only clip makes idle behavior a central experience gap. Obtain or create an appropriately licensed multi-clip quadruped asset only after specifying required clips and testing asset quality, rig compatibility, licence and phone budgets. No purchase is recommended yet.
3. Reduce developer-facing generation UI from customer concept tests; present neutral onboarding concepts (photo, time expectation, preview, privacy/deletion, failure recovery). Do not call provider generation for routine tests.
4. Validate portrait camera, gesture arbitration, sound, haptics, and scene readability on an actual phone. Editor validations do not establish tactile comfort.
5. Keep care state forgiving. Product contract already says no pet death or bond loss; validate that values help customers understand their cat rather than create upkeep pressure.

## Unanswered experience questions

- Is the strongest fun in watching the cat, acting on toys, recognizing the user's pet, decorating, or making shareable images?
- What first reaction is worth returning for after the first photo surprise?
- How many authored actions/variations are needed before an individual cat feels alive?
- Does owner-specific coat recognition increase emotional engagement over a high-quality generic cat?
- Is the room personal, aspirational, or simply a stage?
- Which controls are intuitive without labels, and which physical interactions actually feel good?
- What tone integrates care, comedy, calm, and memory without confusing the brand?
