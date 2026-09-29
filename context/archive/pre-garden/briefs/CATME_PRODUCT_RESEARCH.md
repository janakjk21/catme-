# CatMe Product, Market, Experience, and Economy Research

## Assignment for Luna

Work through this task over multiple sessions. This is a research programme and a decision aid, not a request for a single essay. Save evidence and intermediate findings as you go, revisit conclusions when new evidence conflicts, and finish with a concrete recommendation for what CatMe should become before expanding the app.

**Central question:** Who would choose CatMe, what would they hope to feel or do, what experience would earn repeat use, and can that experience be delivered and sold sustainably?

CatMe currently proposes turning a photograph of a real cat into a recognizable 3D companion in a small interactive home. The prototype demonstrates parts of that idea. It does not establish demand, product fit, visual direction, enjoyment, retention, or willingness to pay. Treat the premise, audience, technology, art direction, and business model as hypotheses that research may change.

**Scale and research emphasis:** The owner is initially interested in a small product—roughly 10–15 people per day or around 100 people per month—not a mass-market audience. Define whether this means daily visitors, new users, monthly active players, or paying players before modelling. Do not let national pet ownership or total game spending dominate the assignment. Prioritize emotional connection, gameplay quality, a coherent visual/sensory experience, small-team sustainability, and what a small number of players might actually choose to buy. Research the US first and UK as a comparison, but make country differences serve product decisions rather than market-size claims.

**Additional cross-cutting gates:** Include privacy and photo trust; child/family access implications in US and UK; accessibility and reduced-motion support; save/memory portability; store discovery limits at low traffic; content and support workload; and a small-cohort success scorecard. Distinguish a desk-research finding from something only a player, account export, invoice, legal review, or physical device can establish. Do not fabricate these missing inputs to make the programme appear complete.

## Read first and scope

1. Read `AGENTS.md`, `context/README.md`, `context/STATUS.md`, and `context/PRODUCT.md`. Read `context/DECISIONS.md` only for decisions directly relevant to a question under study. Use `docs/CATME_MOBILE_GAME_IMPLEMENTATION.md` only when a cross-system design question requires it, as directed by the context router.
2. Inspect only the current prototype surfaces needed to make a truthful capability inventory. Do not scan Unity caches, generated models, local fixtures, or the sibling browser repository unless a specific research question requires them.
3. Use current internet research extensively. Check source dates, geography, platforms, and product versions. Cite direct sources beside material claims. Prefer store listings, developer materials, official platform guidance, credible research, and clearly attributed customer statements. Treat marketing claims and store reviews as evidence with limits.
4. Keep research and product recommendations separate from implementation. Do not buy assets, spend Meshy or Tripo credits, trigger generation, run ads, contact research participants, change live services, or build a shop as part of this task.
5. Do not commit, push, deploy, or publish. Keep the research and handoff local.

## Working method and files

Create `docs/research/catme/` and maintain these files as the work progresses:

| File | Purpose |
|---|---|
| `INDEX.md` | Phase checklist, current conclusion, next action, unanswered decisions, and links to all research files. Update after every session. |
| `EVIDENCE.md` | Source ledger with URL, title, publisher, date accessed, geography, observation, evidence type, limits, and which claim it supports. |
| `MARKET.md` | Customer segments, jobs, demand, search intent, competitors, and market sizing. |
| `EXPERIENCE.md` | Game forms, cat behaviour, interactions, first session, return session, rooms, art, sound, and usability. |
| `TECHNOLOGY.md` | Creation and animation approaches, mobile constraints, benchmark design, cost, and technical risks. |
| `ECONOMY.md` | What users might buy, value and fairness, pricing evidence, unit economics, and scenarios. |
| `BRAND.md` | Positioning territories, naming, visual identity, messaging, store presentation, and trust. |
| `VALIDATION.md` | Research question audit, interview and prototype study plans, experiments, criteria, and findings. |
| `RECOMMENDATION.md` | Decision-ready synthesis, alternatives, build order, risks, and evidence that could overturn the recommendation. |

Use tables where comparisons help. Place supporting screenshots or visual boards under `docs/research/catme/assets/` only when rights and source attribution are clear; links and descriptions are sufficient otherwise. Do not copy large amounts of copyrighted text or art into the repository.

At the beginning of each session, read `INDEX.md` and the one or two files needed for the current phase. Continue the next unchecked task. At the end, update the phase state, links, findings, open questions, and next action in `INDEX.md`. Never mark an evidence gap as completed merely because a confident answer can be written. Replace stale conclusions when later evidence changes them.

For each significant statement, distinguish **observed fact**, **customer statement**, **inference**, **hypothesis**, and **recommendation**. Give confidence and the reason for it. Show disagreements and contrary evidence. Do not invent search volume, downloads, revenue, retention, interview results, or cost estimates. When a tool or account is unavailable, state what remains unknown and provide a reproducible method to obtain the missing data.

## Phase 0 — Audit the questions and the prototype

Before broad research, review the questions in this brief. Identify which are leading, too broad, repeated, unanswerable through desk research, or dependent on another decision. For each high-priority question, state the decision it informs, assumption, evidence required, method, possible disconfirming result, and priority. Separate customer desire, experience quality, technical feasibility, and business viability. Select the ten questions that must be answered first.

Create a prototype reality sheet: what a player can do today from first launch through return; what works only in the Unity Editor; what was tested on a phone; what the photo-to-cat flow produces; time, quality, failure, and recovery; available cat movements and responses; the role of the room; and any likely confusion or disappointment. Inspect the current state rather than projecting planned features onto it.

**Gate:** `INDEX.md` lists the first ten questions, the present prototype's verified capabilities and gaps, and a justified research order.

## Phase 1 — Customers, needs, and market context

Investigate current cat owners, people using older photos, people unable to keep a cat, cozy-game players, families, cat-content creators, and gift buyers as separate possible audiences. Add or remove segments when evidence warrants. For each, research the situation that prompts interest, desired outcome, emotional need, existing alternatives, spending and time behaviour, reasons to install, reasons to leave, and reasons to return. Do not use “cat lovers” as a substitute for a target customer.

Ask what people already do to enjoy, remember, or share their cats; what frustrates them in those activities; how CatMe would fit into a normal day; who would value an ongoing companion versus a one-off creation; and who might find the concept uncomfortable. Study the differences among amusement, recognition, calm, care, creativity, belonging, and remembrance. Do not presume that one brand should serve all of them.

Evaluate only enough market context to inform CatMe's realistic reach. Never equate pet ownership or total app/game spend with demand. If a bottom-up model is included, center the owner's small target and show the funnel assumptions as unknown until measured: people reached, qualified interest, install/setup, retained players, purchases, and costs. Explain each limit; do not make mass-market TAM the central output. Compare relevant app purchase patterns, small-team content burden, and discovery channels in the US and UK.

**Gate:** Recommend a primary audience and use case, a possible secondary one, and audiences to defer. State the strongest argument against that selection.

## Phase 2 — Search demand and discoverability

Study web search and App Store/Google Play search separately, with the United States as the primary market and the UK as a comparison market. Expand into other markets only with evidence. Use autocomplete, related searches, storefront suggestions, competitor language, public discussions, Google Trends, and Keyword Planner where accessible. Record exact wording, intent, country, platform, date, available volume or relative trend, competition, and what a searcher expects to receive. Do not treat US population, pet-industry spend, or total mobile-game spend as CatMe demand or addressable revenue.

Begin with these seed clusters and expand using customers' language:

- Virtual cat, virtual pet cat, cat simulator, kitten game, cozy cat game, relaxing cat game.
- My cat game, make my cat a game, cat from photo, turn my cat into a character, photo to 3D cat.
- Pet companion app, digital pet, Tamagotchi cat, interactive cat app.
- Cat room game, cat care game, cat collection, play with a cat, cat decorating game.
- Pet memories, remember my cat, cat memorial app, pet photo animation.

Separate broad entertainment intent, personalized creation intent, recurring-companion intent, decorating intent, and memorial intent. Identify terms that would attract the wrong expectation. Do not treat Google Trends' normalized 0–100 values as search counts. Do not claim Apple keyword ranking from speculation. Review current official store guidance before proposing a title, subtitle, keywords, screenshots, or category.

**Gate:** Present three discovery positions and identify the one that combines customer intent, credible demand, competition, and honest product fit.

## Phase 3 — Competitors and substitutes

Inspect at least 15 current products across virtual pet and talking-cat games, cozy cat observation and collection, cat simulation and care, home decorating, pet-photo and AI creation, memorial tools, and unexpected substitutes. Include Neko Atsume 2, Cats & Soup, My Talking Angela 2, Pocket Love, My Cat, and relevant photo and memorial products. Verify availability, platform, geography, current pricing, and version.

For each product, record the promise shown by its name, icon, first three screenshots, subtitle, trailer, and onboarding; the actual core loop; session length; player control; cat design; room design; interface; audio; progression; monetization; and recent player praise and complaints. Use a transparent review sample that includes recent, favourable, and critical reviews. Distinguish a store description from verified behaviour and a review from representative survey data.

Compare promised experience with reported experience. Investigate complaints about resemblance, uncanny appearance, weak animation, repetitive tasks, slow setup, ads, subscriptions, paywalls, lost progress, notifications, and unresponsive cats. Build a visual reference board and a map of crowded conventions and unmet needs. Ask whether successful competitors owe their reach to game design, established characters, paid acquisition, brand, community, or content volume; do not infer causation from visible features alone.

**Gate:** Name the alternatives a target customer would compare with CatMe and identify a difference that would actually matter to that customer.

## Phase 4 — Which kind of product and game should this be?

Compare a personalized companion, cozy observation game, cat-mischief game, home and relationship game, interactive keepsake, photo/video creator, and cat adventure or puzzle game. For each, assess the audience, main player action, degree of cat autonomy, 30-second and five-minute sessions, repeat appeal, content burden, monetization fit, production cost, and primary failure mode.

Test two hard questions: Would the experience remain enjoyable with a standard cat? Would people still value the personalized cat without a game? The answers should determine whether personalization is the heart of the product, a setup step, or a shareable feature.

Compare product theses such as “a living portrait,” “a gentle companion,” “funny cat chaos,” “a cozy room to make your own,” and “a place to make cat memories.” Do not choose solely on novelty. Recommend a primary thesis and show why the others are less suitable at this stage.

**Gate:** State one clear player promise and the recurring action or observation that fulfils it.

## Phase 5 — Likeness and 3D technology

Research what makes an owner say “that is my cat”: coat pattern, distinctive markings, face, eyes, proportions, posture, and familiar behaviour. Investigate whether believable identity or photorealistic rendering matters more, and when realism makes motion defects more conspicuous. Determine whether people prefer a consistently animated approximation over a more accurate-looking but less responsive model.

Compare unique generated meshes, an authored customizable cat, an authored model with photo-derived appearance and controlled shape changes, several authored body types, a stylized or 2.5D approach, and multi-photo reconstruction where its capture requirements are practical. Examine likeness, hidden sides, fur, rig consistency, animation transfer, cleanup, generation delay, failure recovery, mobile performance, licence terms, and cost per **accepted playable cat**, including retries.

Design a fair benchmark using a varied, consented photo set: different coats, bodies, fur lengths, poses, lighting, and photo quality. Hold room, camera, lighting, and actions constant. Record owner recognition, independent appeal and motion judgments, visible defects, attempts, manual repair, time to play, file size, memory, frame rate, and full cost. Compare walking, turning, sitting, touching, and toy play rather than judging promotional stills. If a provider requires payment, provide a test proposal and expected cost without spending credits.

Research animation and behaviour separately from mesh creation: authored clips, transitions, foot placement, turning, attention and gaze, procedural adjustments, state-based decisions, and variation. Consider AI systems only when they solve a defined customer problem. Explain which visible problems require better art or animation, which need code, and which need interaction design. Assess Unity and alternative delivery approaches against the selected product, accounting for replacement cost and maintenance. Define target devices and performance, load, memory, download, heat, and battery budgets, then specify physical-device measurements.

**Gate:** Recommend an appearance and animation pipeline with a benchmark that could falsify the choice.

## Phase 6 — Behaviour, fun, usability, and emotional payoff

Study real cat behaviour and how games make subtle actions legible on a phone. Design specific moments for greeting, responding to a call, inviting or declining petting, noticing and stalking a toy, missing and recovering, investigating a box or sun patch, gentle mischief, settling, sleeping, and waking. Research what is true to cats and what needs deliberate exaggeration for readability.

For each proposed moment, specify trigger, anticipation, action, sound, player response, duration, variation, interruption, and emotional payoff. Show what is authored animation, procedural motion, behaviour logic, sound, camera work, and UI. Explain how repeated actions stay interesting without seeming random. Test whether humour, calm, recognition, and affection can coexist in one coherent tone.

Map what the player expects after each touch, what the cat actually does, and how the result is understood. Investigate delay, feedback, autonomy, unpredictability, repetition, one-handed play, sound-off play, and accessibility. Describe a complete first minute, first five minutes, second visit, and visit after several days. Identify a pleasant stopping point and a voluntary reason to return. Avoid using punishment for absence as a default return mechanism.

**Gate:** Storyboard three complete interactions and recommend the smallest polished slice that could test enjoyment and repeat interest.

## Phase 7 — Appearance, room, and sensory direction

Compare at least three coherent visual directions, such as a warm believable miniature home, a softly stylized storybook room, and a playful toy-like world. Evaluate resemblance, cat readability at real phone size, animation fit, room scale, camera, light, palette, materials, clutter, UI density, sound, production cost, and shareable images. Study how relevant games use rooms and furniture to create gameplay, discovery, and emotional meaning.

Ask whether players want their actual home, an aspirational home, a simple cat-focused stage, or a changeable set of spaces. Decide what a room object *does* as well as how it looks. For example: a box may invite investigation, a window may create quiet observation, and a blanket may create a resting ritual. Study floor plans and camera angles that keep the cat visible while preserving interesting actions around furniture.

Recommend one lead art direction and one fallback with annotated references. Propose a controlled phone-size comparison using the same cat, action, and room composition. Define how the first screen, store icon, first screenshots, short video, and memory photo should look and feel. Include animation rhythm, sound, haptics, and camera movement in the style recommendation.

**Gate:** Produce a visual direction that strengthens the chosen player promise and can be made consistently across generated or customized cats.

## Phase 8 — Brand, positioning, and trust

Test what “CatMe” communicates before explanation. Investigate naming distinction, search ambiguity, and possible trademark issues; flag formal legal review rather than asserting clearance. Develop three distinct brand territories with target audience, one-sentence promise, emotional tone, name and subtitle candidates, icon, typography, palette, imagery, screenshot sequence, a 15-second video concept, onboarding copy, and a return message.

Research whether recognition, humour, calm, affection, creation, and memories belong under one brand. Treat older pet photos and loss as a sensitive opt-in use case. Test relevant wording with that audience before making memorial claims. Do not claim the app infers a real cat's personality from a photo or provides therapeutic benefit without evidence.

Map trust from store impression to photo selection, upload, processing, first interaction, save, return, deletion, and support. What information, controls, or recovery does a player expect at each point? What happens after a poor likeness or failed generation? Study privacy expectations for photos, generated assets, local memories, export, and deletion.

**Gate:** Recommend positioning and a brand direction that match the actual experience and its strongest audience.

## Phase 9 — Economy, things to sell, and sustainability

Research the overall app market and the economics of CatMe, not only the gameplay. Examine which purchases customers might genuinely value: creating their cat, additional cats, authored Studio Cats, toys, interactive furniture, room themes, gardens, seasonal decor, accessories, photo or memory features, gifts, and content expansions. Ask whether value comes from expression, play, discovery, home-making, sharing, or remembering. Assess each idea with customers' likely emotional motivation and competing alternatives.

Separate items that create lasting interaction from purely visual items. A box the cat investigates, a window perch it uses, or a blanket it kneads may have different value than a colour change. Investigate which purchases would be visible in daily play, which are giftable or shareable, and which require costly new animations or systems. Evaluate coherent item previews and room placement so a buyer understands exactly how a purchase looks and behaves.

Compare one-time purchases of the whole experience, bundles, paid expansions, identity creation, interactive toys/furniture, cosmetic skins, subscriptions, optional ads, and soft earned currency. Identify what should be free, what might reasonably be sold at launch, and what should wait. Assess US and UK pricing using current competitor evidence and direct customer research plans. Model economics at tens or hundreds of users, including creator/support time, and avoid assuming a large audience. Keep basic care available and assess whether any offer would make affection, guilt, neglect, or a grief-related need into a purchase pressure point.

Build transparent conservative, middle, and optimistic models. Include acquisition, store fees, payment/refund effects, generation and retries, hosting, storage, customer support, content production, maintenance, payer conversion, repeat purchases, and retention. Show break-even sensitivity and the assumptions that matter most. Do not equate pet-industry spending with obtainable game revenue. Distinguish stated willingness to pay from actual purchase behaviour; propose small tests of concrete purchase choices before recommending a large shop or catalogue.

**Gate:** Recommend an initial free experience and no more than a few possible offers, with customer value, evidence, cost, fairness, and tests for each.

## Phase 10 — Primary validation and experiment order

Design research to challenge desk findings. Do not present online comments as interviews. Provide a recruitment screener, non-leading interview guide, consent considerations for personal cat photos, prototype tasks, observation sheet, and synthesis method. Include people who dislike the concept. Start interviews with existing habits and alternatives before showing CatMe. Randomize concept statements, room images, and behaviour clips where practical. Observe people using the phone prototype without guidance once physical-device validation is possible.

Specify what can be learned from an interview, storyboard, render, animation clip, playable interaction, and several days of actual use. Test recognition with owners' cats where appropriate, first-minute comprehension, delight, trust, repeat use, and realistic choices between paid and free alternatives. Separate interest, stated intent, behaviour, and purchase evidence. Qualitative samples can reveal problems but cannot estimate population prevalence.

Sequence small tests so each resolves a major uncertainty before broader development: customer need and promise; visual direction; likeness and animation; one complete interaction; unguided phone use; repeated visits; then purchase choices. State the competing hypotheses, measurement, success and failure criteria, and next decision for each. Label numerical thresholds as proposed decision rules rather than established market facts.

**Gate:** Present five immediate experiments and the exact product decision each could change.

## Final recommendation and handoff

Write `RECOMMENDATION.md` for someone who has not followed the research. Answer directly:

1. Who is CatMe for first, and what are they trying to achieve?
2. What promise should its first release make, and what search language expresses that demand?
3. Which product and game form best delivers the promise?
4. What makes its cat recognizable, alive, funny, understandable, and worth revisiting?
5. What should the room, camera, art, sound, and interface do?
6. How should a cat be created and animated, and what must be benchmarked first?
7. What should be free, what might be sold, and can the economics work?
8. What should we build, change, remove, or defer in the existing prototype?
9. What evidence remains missing, and what findings would make us pivot or stop?

Rank recommendations by customer value, evidence strength, development cost, and risk. Show at least three viable product theses and three brand territories before selecting one. Include a candid case against the preferred direction. Do not hide uncertainty in a confident summary.

For each proposed change, use: **observed problem → affected customer → proposed change → expected benefit → evidence → smallest test → success/failure result → cost and dependencies**. Separate immediate experiments, first-release scope, and later possibilities.

When finished, update `context/STATUS.md` concisely with the research outcome, decision, remaining validation work, and next concrete task. Do not turn `STATUS.md` into a research diary. Report the research files created, conclusions, sources, methods, limits, and unresolved decisions.

## Starter sources to verify and expand

These are leads, not an exhaustive or permanently current source list:

- [Apple App Store search guidance](https://developer.apple.com/app-store/search/)
- [Google Trends data interpretation](https://support.google.com/trends/answer/4365533?hl=en-GB)
- [Google Ads Keyword Planner](https://ads.google.com/intl/en_uk/home/tools/keyword-planner/)
- [Cats Protection body-language guide](https://www.cats.org.uk/help-and-advice/cat-behaviour/cat-body-language)
- [Apple Object Capture photo guidance](https://developer.apple.com/documentation/realitykit/capturing-photographs-for-realitykit-object-capture/)
- [Meshy quadruped rigging guide](https://docs.meshy.ai/en/webapp/guides/3d-model/rigging)
- [Unity target-device profiling](https://docs.unity3d.com/6000.0/Documentation/Manual/profiling-target-device.html)
- [IDEO Design Kit: determine what to prototype](https://www.designkit.org/methods/determine-what-to-prototype.html)
