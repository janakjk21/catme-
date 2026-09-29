# CatMe Research Recommendation (Provisional)

Date: 2026-09-24. Market scope: United States first; UK comparison second. This is a provisional direction after desk research and source/code review, not a market validation result.

## Recommended working thesis

Test CatMe as **a small, personal cat-moment game for cat owners**, initially recruiting in the US and comparing a smaller UK group. The emotional promise to test is: **“A little place to notice the familiar, funny things your cat does.”** The player sees their cat notice an invitation, choose to approach/play/ignore, and make a small moment worth smiling at or keeping. “That is my cat” should be supported by coat/shape identity and repeated behavior, not only by an AI conversion reveal. This recommendation is an inference from real-cat body-language guidance, competitor patterns, and the project's current room/interactions—not a finding from CatMe players, since no players have been interviewed.

Why this is the leading hypothesis: personalization offers a reason to care about this particular cat; adjacent apps demonstrate observation, care, touch, customization, room design, photo capture and collection separately; CatMe’s current prototype already has one room and several focused interactions. The internal feature foundation makes this direction testable without claiming that customers already want it.

The scale goal is deliberately small: the owner described 10–15 people a day or about 100 a month. At that scale, optimize for a coherent, sustainable experience and direct feedback from a small community. Do not plan a live-ops machine or infer a large addressable market from household/game-spend totals. First define whether “100/month” means installs, active users, or paying users.

**Owner camera direction (2026-09-24):** lead the next playable prototype with first-person movement through the room. The player should walk up to pour food, call the cat to their current position, throw a ball, and turn to follow the cat. Keep third-person available for a paired comparison. This updates the earlier recommendation to test a guided three-quarter cat-eye composition; it records the owner's choice while customer preference remains untested.

## Best case against it

The central promise may be technically and economically expensive while the audience prefers the cheap, polished, authored cats and larger content loops already available. A user may enjoy seeing their cat converted once, export an image, and never return. Current animation limitations make this risk real: the known-good cat currently has a walk clip, and the real photo pipeline/phone experience remain unverified. If owner recognition does not substantially improve preference or repeated visits, stop treating per-user 3D generation as the core product.

## Experience direction to prototype first

- **Look:** soft, believable 3D with lightly simplified/tactile room materials; recognizable real-cat coat and silhouette; warm but restrained daylight; broad value contrast; uncluttered open floor; room props at edges. If motion and model defects look uncanny, move toward a more illustrated 3D style. Do not finalize the art from a still render.
- **Camera:** portrait, first-person at human eye height, stable during play, with ground-plane room movement, limited look pitch, pinch zoom, and a “Find cat” control. Keep the third-person orbit mode for comparison. Controls should never cover the cat's head or toy.
- **Feel:** low-pressure, close and observant. One invitation, a small beat of anticipation, response, a clear end. The cat has agency; no sad “you were gone” messaging, streak loss, hunger penalty, or paywall on affection.
- **Humor:** grounded in feline action: box hesitation, toy almost-catch, curious paw test, awkward recovery. Humor should be readable and brief, not constant slapstick or human facial acting.
- **Room:** stage behavior rather than sell square footage. The window offers quiet watching/light; box offers exploration; blanket/bed offers settling; clear floor supports chase. Every paid object must show its visual and behavioral function honestly.
- **Sound/touch:** light room tone and specific close foley; optional music; optional subtle haptic on contact/catch. The action must still read with sound off and no haptics.
- **First minute:** immediate cat view, one short hint, one complete toy interaction, then optional pet/call and name/photo choice. No account, care tutorial, generation paywall, or shop before the first satisfying moment.

These are recommendations to test, not user-validated style decisions. Detailed sequence and alternatives are in `EXPERIENCE.md`.

## Product alternatives to compare before locking scope

| Thesis | Strength | Weakness | Next test |
|---|---|---|---|
| Personalized cat companion (lead) | Own cat identity, intimate and differentiated; reuses focused room prototype. | Requires dependable likeness, animation and repeat behaviour. | Compare personal vs authored cat in identical polished interaction. |
| Cozy cat room creator | Room items and layout can create expression and monetizable content. | Requires a large coherent catalogue; cat may become decoration. | Room mood board and item use test: does cat behaviour change how people decorate? |
| Cat photo/video maker | Clear, quick output and potential gift/share use. | Often one-off; competes with photo AI editors and social tools. | Test actual creation result vs game concept; observe willingness to share/pay. |
| Observation/idle cat game | Strong fit for calm short visits; less input/control burden. | Crowded category; personalization may not matter. | A/B observation clip against active toy interaction clip. |
| Pet memorial experience | Emotionally meaningful to a subset and older photo use is supported in concept. | Sensitive, different product promise and support burden. | Separate opt-in qualitative research; no main-brand memorial claim yet. |

## Recommended next build order (only after tests)

1. **Validate the interaction slice:** first-person walking, call, food placement, and one toy interaction with noticeable anticipation, turn, play outcome, variation, interruption cleanup, sound and framing. Compare against third-person on a phone.
2. **Validate cat identity:** compare authored customization and one representative generated path under the same actions. Do not optimize the generation front-end until people recognize and accept output.
3. **Validate tone and camera:** compare three visual directions and first-person versus third-person at real portrait size with the same cat/action; choose based on clarity, comfort, and affect.
4. **Validate return:** ask participants to return voluntarily over a week. Identify what they do without reminders.
5. **Validate purchase:** test transparent options after players have experienced the free core. Start with one personal-cat offer and one behavior-changing toy/room offer rather than a wide store.
6. **Then decide production architecture, economy and release scope.** Complete target-phone performance, privacy, licensing, error recovery and support work.

## Provisional product decisions

- Primary customer for study: current cat owner with a strong attachment and interest in casual/cozy mobile games.
- Secondary audience to explore: people who cannot keep a cat and want gentle cat-themed play; do not claim equivalent companionship or wellbeing.
- Defer as primary: children/families and bereaved owners until dedicated trust and user research.
- Player promise: one named cat, one small home, a few distinct and understandable behaviours, a quiet welcome, and local memory moments.
- Core interaction: observe → invite → cat notices/decides → playful or calming response → small discovery.
- Visual direction: test warm miniature home against more stylized directions. Do not assume the existing room art wins.
- Creation approach: benchmark stable authored model customization against generated geometry; likely hybrid is the cost/quality candidate, not a conclusion.
- Free offer hypothesis: let users understand and enjoy a complete starter interaction before photo generation or payment.
- Paid offer hypothesis: test a transparent one-time personalized cat creation and a small behavior-changing room/toy pack. Keep affection/basic care available. Defer subscriptions and disruptive ads until recurring value is demonstrated.

## Small-scale commercial shape

Do not start by choosing between “sell skins” and “sell the whole app” from category totals. Both models exist in adjacent storefronts: Neko Atsume/Usagi Shima combine a free loop with optional objects/expansions/support; an Apple Arcade version of Neko Atsume offers a subscription-distributed whole experience; My Talking Angela sells activity/style packs and subscriptions in a much larger live content system. For CatMe, test two clear alternatives after the demo: (1) complete one-time paid experience with optional later room packs, versus (2) free complete starter loop and one-off personal-cat or behavior-pack purchase. Choose based on clarity, no-purchase enjoyment, customer preference, and actual delivery cost.

At 100 new installs/month, a hypothetical 5% purchase rate at $4.99 gives five orders and $24.95 gross for that month's install cohort, before platform fees, taxes, refunds, generation, service, content and labor. At 10%, ten orders and $49.90 gross. This is arithmetic, not a forecast; 100 active monthly players would have different cohort economics. The research does not yet establish if any price or format works. A small direct-support/tip option can coexist with a complete product, but should never be framed as the price of care or emotional closeness.

## First five experiments

1. **Emotional job interviews:** US cat owners first, then UK comparison and non-owner cozy players; past-behavior questions before concepts. Decision: who and which real moment is worth serving.
2. **Three art treatments at phone size:** warm miniature, illustrated 3D, tactile toy-like; same cat/action/sound-off. Decision: visual style, camera and whether current room direction survives.
3. **Interaction storyboard/playtest:** call response, box investigation, toy catch/miss/recover. Decision: best emotional beat and smallest polished playable slice.
4. **Identity versus game test:** same interaction for generic, coat-customized and photo-derived cat; disclose imperfect cases. Decision: whether photo personalization is first-run core, optional upgrade, or separate creator feature.
5. **Small, realistic offer test:** compare whole experience, personal-cat creation, interactive room/toy pack, and no purchase in USD and GBP. Decision: what customers support and what cost model is feasible; then, only when ready, validate with a real transparent transaction.

The sample sizes, thresholds, and protocols are in `VALIDATION.md`; none of these experiments has been run yet.

## Evidence that would change this recommendation

- Players prefer a one-off image/video and show no interest in repeated companion use → pivot toward creator, or stop the 3D game direction.
- Personalized identity fails to improve recognition or return versus a polished authored cat → remove photo-derived model generation from the core promise.
- Room design and furniture produce more engagement than cat interaction → investigate room creator as the main loop.
- People prefer idle observation and dislike being prompted to act → reduce care controls and emphasize ambient behaviours.
- Generation costs, failure rate, latency or mobile asset size cannot fit acceptable pricing/quality → adopt curated authored cats or stop personalization.
- Memory users find the brand/play tone insensitive → keep memorial use separate or exclude it from product messaging.
- Users do not return after initial novelty and no small content loop changes that → do not expand economy or content catalogue.

## What is not known yet

No primary customer research, current US/UK keyword volume, independent 15-product in-app teardown, phone-size visual preference test, photo benchmark, real generation bill, payer conversion, retention cohort, production phone validation, or trademark clearance was completed. These are decisive gaps; this recommendation must remain provisional until tested.

## Trust, accessibility, and sustainable delivery gates

Treat personal photos and a saved cat as customer content. Use a system picker, allow a full no-upload route, explain processors and retention before transfer, provide a working delete/export path, and never promise that an individual memory survives device change until backup/restore is proven. Do not add public sharing or cloud accounts just to enable the first emotional moment. If likely child access cannot be supported safely in both the UK and US, define an appropriate audience posture and obtain product-specific legal review before release.

The room and animation should work in silence, with reduced motion, and with accessible controls. Preserve the cat's emotional readability when haptics, audio, or exaggerated motion are off. Test menu/onboarding accessibility with VoiceOver and TalkBack before launch, and test the actual first interaction with disabled players if the product advances.

Use a sustainable content budget: one polished interaction, a few reusable response states, one room, and clear end states. Measure creator hours, device QA, generation failure support, and update obligations. Do not build a subscription promise or a large decor/event catalogue until players demonstrate repeated use and the maker can sustain the content cadence.

For a small audience, evaluate acquisition by meaningful player quality: did the visitor understand the store promise, reach a complete cat moment, return voluntarily, and support the product without pressure? Store experiments may be underpowered near 100 monthly visitors; start with honest direct feedback and source-tagged small cohorts, and report exact counts rather than treating tiny percentages as stable market rates.
