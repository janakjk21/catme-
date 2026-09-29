# CatMe Brand, Positioning, and Trust

## Initial assessment

“CatMe” is short and memorable, but the name alone does not tell a new customer whether this is a cat game, a photo-to-3D maker, a care app, or a memorial tool. The name needs a clear subtitle and a visible first promise. This is a positioning issue to test; this research pass did not conduct trademark clearance or a comprehensive name search.

The existing mix of “personal cat,” “virtual pet,” “room design,” “photo memory,” and “pet loss” spans distinct expectations. They may not belong in one store story. The first release should choose a primary job, then let secondary uses exist only if they do not confuse onboarding and trust.

## Brand territories to test

| Territory | Promise | Tone / visual | Risk / test |
|---|---|---|---|
| A little home for your cat | “Bring a cat that feels like yours into a little home of its own.” | Warm, personal, calm; close cat framing, soft room light, real coat character. | Could sound like photo-to-3D guarantee. Test what users expect from “feels like yours.” |
| Let the cat be a cat | “A playful cat with a mind of its own, right on your phone.” | Humour, curiosity, readable action; tactile props and expressive timing. | A single walk clip cannot deliver the promise; needs authored animation/variation. |
| Keep a small moment | “Make and keep little moments with the cat you love.” | Gentle, private, memory-oriented photos and room scenes. | Can imply memorial or grief service; requires consent, archive/export/delete design and separate customer research. |

Test three descriptors in randomized order. Ask participants to explain the app to a friend, predict what happens after install, identify any claim they distrust, and choose a store screenshot/video they would tap. Do not begin by explaining the technology.

## First visual communication hypothesis

Lead with the cat’s face/marking plus a room interaction, not an abstract AI/3D claim. In a short store sequence, show: (1) one cat and a clear promise, (2) identity or chosen cat creation, (3) a specific interaction with readable cause and response, (4) room objects with purpose, (5) a candid memory or discovery. If likeness quality is not reliable, do not lead with “your cat brought to life.” A misleading screenshot creates poor-fit downloads and trust damage.

The icon must remain distinct at small size; compare a personalized face silhouette, a cat in an arched window, and a simple paw/room mark. Test recognizability in a store-result grid, not only full-screen mockups. Validate brand name availability, domain/app-store collisions, and marks in relevant territories before legal or launch decisions.

## Art-direction decision linked to the game, not just the logo

The brand's visual signature should come from **a real individual cat sharing a small, tactile place**, not from generic paw-print marks, AI gradients, or a catalogue of unrelated cute furniture. Lead with a close portrait of the cat's distinct coat and eyes, then a second frame showing a specific self-contained interaction in the room. Use a small warm accent (for example, muted coral or amber) for the toy/interaction cue, restrained cream/oak/leaf materials for the room, and quiet typography. Keep the cat's face/markings visually consistent between app icon, onboarding, room, and exported photo.

The current warm cream/oak and arch direction is a reasonable *prototype baseline*, not a validated final style. The strongest fallback is a slightly more illustrated/stylized 3D treatment if generated fur/geometry defects make the believable-room approach uncanny. Do not choose the style based on room beauty alone: compare coat readability, body-language readability, intimate scale, and whether players interpret the game as personal rather than child-focused. A room concept that looks luxurious but hides the cat is a product failure.

**15-second video structure to test:** (0–3s) familiar cat marking/face; (3–6s) finger taps feather, cat notices with ear/head before moving; (6–11s) stalk, pounce, small comic outcome; (11–13s) cat settles in its window-light spot; (13–15s) simple promise and install cue. Avoid implying exact 3D likeness unless representative owner-tested results meet the promise. US and UK store screenshots should use the exact storefront language and pricing; do not use unrelated massive-market statistics as the emotional pitch.

## Emotional boundaries and privacy

Current CatMe internal contracts support old pet photos without requiring a person to label a pet as living or deceased. That is a thoughtful inclusion hypothesis, but it is not permission to market simulated reunion or grief relief. A playful cat-game brand and a memorial promise could feel jarring together.

Use neutral language around upload: what will be sent, to whom, why, how long retained, whether a result is reusable, how to delete, and whether the image will train a model. Offer an explicit option to skip photo creation and use a starter cat. Never infer a pet’s personality, health, feelings, or consent from one photo. Local journal traits currently have neutral defaults and do not read uploaded photos; preserve that boundary unless validated and transparently described.

## Trust journey checklist

| Moment | Needed clarity |
|---|---|
| Store page | Does the app generate a playable character, a picture, or both? Show real representative results. |
| Before selecting photo | What image quality/pose works? Can the user continue without uploading? |
| Before upload | What is transmitted, who processes it, retention/deletion, and any fee. |
| During generation | Time range, progress meaning, cancellation/retry, and what happens if it fails. |
| Preview | Let user judge and reject/adjust a poor likeness before purchase or commitment. |
| Save/share | Explain local vs cloud storage and make sharing opt-in. |
| Return | Welcome without guilt; no “your cat missed you” manipulative claims. |
| Memory or loss context | User controls tone, dates, sharing, reminders, and deletion; do not assume the emotional state. |

## Questions to resolve

- Does “CatMe” read as playful, personal, child-focused, generic, or memorial to real users?
- Is the name too close to existing cat products or search terms? Needs trademark/store research.
- Should remembrance be a quiet capability or a separate opt-in experience? Test with affected people, not by inference.
- Which proof in a screenshot earns trust: coat resemblance, action animation, room quality, or privacy policy promise?

## Privacy, family trust, and photo onboarding

CatMe's first photo use is an occasional, user-selected image, so the product should use the operating-system photo picker rather than request full gallery access. Google Play treats photos/videos as sensitive data and says apps with one-time or infrequent access should use a picker; the user should still be able to try an authored starter cat if they decline. Apple and Android provide system pickers for access to selected media only. [Google Play policy](https://support.google.com/googleplay/android-developer/answer/14115180?hl=en-CA) [Android Photo Picker](https://developer.android.com/training/data-storage/shared/photopicker) [Apple PhotosPicker](https://developer.apple.com/documentation/photosui/photospicker)

Before a photo leaves the phone, state in plain language: the image will be sent to the named processor, for this one purpose, whether it is retained and for how long, whether it is used for training, how to delete the source/result, and what happens if generation fails. Use the same policy on the upload screen and store privacy disclosures. A photo of a pet can also contain a person, home interior, location metadata, or a child; do not call it non-sensitive just because the subject is an animal. Confirm whether image metadata is stripped, who can access it, how long logs persist, and whether generated assets can be recovered or removed.

For the first version, keep play usable without upload, avoid accounts and social sharing unless a tested need requires them, and prefer local save plus user-initiated export. Do not claim permanent backup or device transfer until iOS and Android restore behavior is verified. Current game state and companion journal are stored under Unity `persistentDataPath`; the generated cat is cached separately. The journal filename uses a hash of the cat asset path, which could create a new profile if the asset identity changes. These are implementation observations, not a verified device migration result. Apple distinguishes backed-up support/document data from purgeable cache/temp data; classify the generated GLB intentionally and test actual upgrade, restore, uninstall, and deletion behavior before promising continuity. [Apple file-system guidance](https://developer.apple.com/library/content/documentation/FileManagement/Conceptual/FileSystemProgrammingGuide/FileSystemOverview/FileSystemOverview.html)

The UK Children's Code can cover apps and games likely to be accessed by children even when they are not aimed at them. In the US, COPPA assessment depends on child-directed status or actual knowledge of collecting personal information from under-13 users; the FTC amended its rule in 2025. A cozy cat game may attract family use, so choose deliberately between an adult/general-audience design that is safe by default and a child-directed product with the corresponding controls. Do not add child accounts, chat, targeted ads, public uploads, or social discovery casually. Complete a product-specific legal review before store release; these notes are risk flags, not legal advice. [ICO scope](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/childrens-information/childrens-code-guidance-and-resources/age-appropriate-design-a-code-of-practice-for-online-services/services-covered-by-this-code/) [FTC COPPA](https://www.ftc.gov/legal-library/browse/rules/childrens-online-privacy-protection-rule-coppa)

## Accessible warmth and humor

The expressive cat should not require sound, color, rapid motion, tiny targets, or repeated precision taps to understand. Give controls generous hit areas and labels; communicate notice/catch/settle through visible action plus optional sound/haptics; add captions or visual equivalents for meaningful audio cues; test larger text and screen-reader access for menus. Offer calm/reduced-motion behavior that reduces repeated camera movement and exaggerated jiggle while preserving the interaction. Apple's current guidance recommends motion be purposeful, optional, brief, and not the sole way to communicate; Unity documents native mobile accessibility support, but CatMe's current scene is not verified as accessible. [Apple accessibility](https://developer.apple.com/design/human-interface-guidelines/accessibility/) [Apple motion](https://developer.apple.com/design/human-interface-guidelines/motion) [Unity accessibility](https://docs.unity3d.com/6000.0/Documentation/Manual/accessibility.html)

For humor, make the *timing* playful rather than the animal's body grotesque: a long suspicious look at a box, a paw that misses then resets, a tiny overcorrection, an interrupted pounce, or a dignified recovery. Keep the cat's proportions and motion comfortable to watch, make outcomes legible without audio, and let players disable camera shake, bounce emphasis, and haptics. Test humor separately for warmth, uncanny reaction, and stress; do not describe an animation as a real cat's feelings.

## Store discovery and small-audience learning

Build a listing that demonstrates the actual loop in its first images: recognizable cat, one input, cat response, and a quiet room. Test one promise at a time (personal cat vs playful cat moment vs cozy room) rather than changing icon, screenshots, and description together. Apple and Google both provide listing experiments; both rely on enough traffic, and Google can report “more data needed.” With a target of only about 100 people monthly, prioritize a handful of direct conversations, creator referrals with trackable links, and clear source attribution over underpowered A/B claims. The small goal does not make store quality irrelevant; it changes how much confidence a tiny experiment can support. [Apple PPO](https://developer.apple.com/app-store/product-page-optimization/) [Google Play experiments](https://support.google.com/googleplay/android-developer/answer/12053285?hl=en)

Do not confuse keyword discovery with keyword volume. First capture the exact US and UK store suggestions, test likely intent clusters (virtual cat, cat simulator, cat game, cozy pet game, make my cat 3D, cat photo app), and use separate store-page concepts that match each actual product job. Search demand tools can prioritize language; only install behavior and interviews show whether those searchers want CatMe. Apple's custom product pages can route distinct feature/query intent to different truthful pages, but require a distributed product and traffic to learn from. [Apple custom pages](https://developer.apple.com/app-store/custom-product-pages/)

## Content workload and the small-team constraint

The product direction must fit a small, sustainable release cadence. Every new behavior requires a readable animation, interruption handling, mobile QA, localization of any prompts, capture/framing review, and an accessible feedback path. Every room item must either create a behavior or strongly improve the composition. Start with one fully polished sequence and one room arrangement, then measure whether people replay it or return; do not promise seasonal live operations, dozens of toys, multi-cat households, and cloud memories as a launch bundle.

For each proposed addition, estimate: design/art hours, animation/rig hours, engineering, QA devices and languages, asset size/performance, recurring support, localization, store media, and whether it is a one-time or continuing obligation. Keep a release ledger of feature scope and maintenance owner. Prefer reusable behavior modules (notice → approach/decline → interact → settle) and shared object interactions over bespoke one-off scenes. This is a production hypothesis derived from CatMe's constrained prototype and small audience goal; validate actual build time on the first vertical slice.

### Launch and continuity questions

- Does the app remain a good, complete visit if the player never uploads a photo, disables sound/haptics, or leaves for weeks?
- Is the cat/room state recoverable after update, device restore, reinstall, changed generated asset, and low storage? What is intentionally local, backed up, downloadable, or purgeable?
- Can a player export their favorite moment and delete every source photo/model without an account or support request?
- What is the smallest content cadence the maker can sustain without reducing the quality of animation, device support, and replies to players?
- Does the app invite children? If yes or likely, what age-appropriate privacy, purchase, consent, and safety design is required in each launch territory?
- Can every important cat action be understood visually in silence and with reduced motion?
