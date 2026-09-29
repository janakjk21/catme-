# 3D Creation, Animation, and Production Research

## Current technical baseline (repository evidence)

Project status states Unity 6000.6.2f1, URP, Input System, Cinemachine, AI Navigation, glTFast and UGUI are installed; HomeRoom has NavMesh routes and the known-good local cat. The known-good GLB is about 19.6MB in the recorded status and currently has only a walk clip. A separate downloaded generated model was reported at 33MB in prior validation. The debug photo panel accepts a 12MB photo and caps a downloaded model at 200MB. The local generation flow is not a release architecture claim, and the status says real upload/generation and phone interaction remain unverified.

Technical output must optimize **accepted playable cat**, not a still render. A model that looks excellent but cannot turn, touch, idle, or run within mobile constraints does not satisfy the proposed product promise.

## Approaches to compare

| Approach | Upside | Main risk / cost | Best use to test |
|---|---|---|---|
| Fully generated mesh from one photo | Strong novelty; potentially unique silhouette/coat. | One view cannot reveal hidden sides; shape/texture artifacts, uncertain rig, large assets, retry and moderation cost. | Compare only if vendor exposes a repeatable pipeline and cost logs; score in motion. |
| Authored cat with coat/eye/body customization | Predictable rig, animations, size and mobile budget; curated high-quality defaults. | May feel generic or fail unusual markings/body shapes. | Baseline to establish quality and determine which identity features drive recognition. |
| Authored body/rig plus photo-informed appearance/shape adjustments | Balances consistent motion with visible individual identity. | Mapping photos to textures/shape correctly is hard; front-only photo can mislead. | Strong candidate if coat/face recognition improves without breaking model. |
| Several authored breeds/body templates plus customization | More silhouette coverage and curated rendering. | Asset and testing burden grows; breed labels may not fit mixed cats. | Compare template selection to freeform model generation; avoid forcing breed identity. |
| Stylized authored model / 2.5D | Hides small geometry defects, lighter and easier to animate consistently. | May weaken “this is my cat” or expectation of living 3D room. | Compare if customers value personality and presence more than photorealism. |
| Multi-photo photogrammetry / reconstruction | More views can improve spatial shape for suitable static objects. | A living cat moves; capture burden is high and reconstructed mesh still needs rig/animation. | Treat as long-term technical research, not default mobile onboarding. |

Apple Object Capture docs explain that photogrammetry uses overlapping multi-angle images and can fail or lose quality when the object deforms or moves. A cat is articulated, so this is a warning about capture burden, not a direct evaluation of current neural 3D systems. [Apple capture guidance](https://developer.apple.com/documentation/realitykit/capturing-photographs-for-realitykit-object-capture/)

Meshy currently documents auto-rigging for quadrupeds and export to Unity/Unreal. That is vendor capability evidence only. Test animation deformation, foot contact, face/ears, tail, orientation, scale, exports, commercial rights and current price using cat cases. [Meshy guide](https://docs.meshy.ai/en/webapp/guides/3d-model/rigging)

## Benchmark design

### Input set

With explicit participant consent, collect representative cat photos across short/long hair, tabby/calico/black/white/colourpoint, flat/long face, body size, mixed breed, front/side poses, indoor light, low-resolution images and clutter. Keep identifiers and photos private; define deletion. Do not upload to vendors without consent and documented terms.

### Fixed test sequence

For every candidate method, use the same target scale, room, camera, lighting, and interaction sequence:

1. Neutral front, 3/4, side view.
2. Walk across frame and turn.
3. Stop, look toward an authored point, and settle.
4. Respond to petting and toy motion.
5. Enter a partly occluded rest/feeding presentation.

Score owner recognition (owner explains why), independent identity judgments, naturalness in motion, defects, texture/marking fidelity, animation coverage, foot slide, orientation, loaded bytes/time, memory, mobile frame time, temperature/battery, failure recovery, number of attempts, manual repair, provider and storage cost. Report distributions and failures; never show only best outputs. Have raters view randomized conditions without provider labels.

### Decision rule

First decide minimum acceptable resemblance and motion with target users. Then compare cost and throughput. If a simpler authored model meets player recognition and behaviour goals substantially more reliably, prefer it. If photo-based identity materially increases recognition and intent, test whether its cost, delay and failure rate can be supported. Set thresholds with participants and technical device profiles; do not invent industry standards.

## Behaviour and animation pipeline

Separate five layers: mesh/materials; animation clips; rig controls; navigation/interaction; behaviour selection. The current loader discovers clips and handles scale/floor/orientation; do not assume a provider’s model has the same rig. Prefer authored clips for distinctive moments and simple bounded procedural additions for look direction, ear/head orientation, tail/idle variation, contact anticipation, and camera staging only when bones are confirmed.

Potential minimum library to test against the chosen promise: idle/settle, walk, turn, look/notice, sit or crouch, pounce/bat, pet response/rub, rest/sleep transition, wake/stretch. These names are not a commitment to acquire every clip. Identify which three or four moments create the greatest emotional payoff first. Blend transitions and retarget across representative cats; test no idle clip and missing rig bones as graceful fallback cases.

Do not introduce conversational AI, cloud sync, multiplayer, AR, or a new engine before evidence ties it to a target customer problem. AR is already a feature in adjacent apps; its presence alone is not differentiation. Every added technology brings latency, testing, privacy, support, and maintenance costs.

### Minimum viable animation plan for emotional readability

The desired feeling does not require dozens of locomotion clips at the beginning. It requires a small handful of well-staged responses. Prioritize: (1) neutral idle with breath/weight shift, (2) notice (head/ears/eyes if rig supports), (3) walk and turn, (4) one play sequence with crouch–pounce–paw/contact–recover, and (5) one affiliative response (cheek rub or lean) only if the selected cat rig can perform it cleanly. Sleeping is less valuable than a credible in-session response for the first test.

Separate the perceived “life” of the cat into authored animation, bounded behavior selection, and staging. Variation can come from different gaze targets, response delays, routes, toy outcomes, and pause lengths; randomness alone will look erratic. A procedural head/ear turn is only valid if the asset has usable controls. If not, author the behavior into a clip or use a curated rig. Better animation can fix floaty contact or stiff turns; behavior logic can make the cat notice and choose; interaction design can make the tap target and outcome clear. Do not try to solve all three by buying a more realistic mesh.

For the benchmark, create a test clip on one target phone before commissioning additional content: idle → call notice → approach → touch/lean → toy pounce/catch, all in the current room. Compare (A) best available known-good cat + newly authored staging; (B) an authored customizable cat with same rig/clip set; and (C) one generated model. Have an owner look at a realistic photo pair and the live animation. Record not just “cute” but: recognized which cat and which markings, believed it noticed the input, whether contact looked grounded, and whether they wanted to watch a second interaction. A still-only preference cannot decide this technology path.

## Device and production gates

- Use target-device profiling for CPU/GPU, animation, memory, load time, thermal and battery behaviour; Unity explicitly supports profiling a running target build. [Unity profiling](https://docs.unity3d.com/6000.0/Documentation/Manual/profiling-target-device.html)
- Test at least a current supported iPhone and a representative lower/mid-range Android before declaring mobile readiness.
- Set budgets after a baseline build: app download size; per-cat bytes; first view time; memory high-water; stable frame pacing; battery/heat; photo upload size; generation time and retry rate.
- Compare quality at portrait display distance, not only Scene view or desktop monitor.
- Examine commercial model rights, provider storage/deletion, license grants, model reuse, service uptime, and support terms before depending on any provider.
- For the current development setup, the default server is loopback and only works on the Mac/editor; mobile needs a reachable service endpoint, verified signing/build and phone QA.

## Accessibility and input architecture

The expressive cat can be 3D while the controls and feedback remain accessible. Use visible, labeled controls for call/toy/camera/menu; do not rely on a player discovering a tiny hotspot in the room. Keep core play operable without precise dragging, repeated taps, audio, or haptics. Offer alternative cues for important sounds, honor OS text scaling and Reduce Motion where feasible, and test VoiceOver/TalkBack for menus and photo steps. A reduced-motion mode should tone down camera movement, bounce emphasis, and repeated environmental animation while preserving the cat's understandable response. The cat itself cannot be fully represented by a screen reader, so provide concise state descriptions and a clear actionable UI layer.

Unity 6 documents native bridges for features such as screen readers, dynamic text, bold text and captions; Apple recommends purposeful/optional motion and multimodal feedback. This indicates a feasible design path, not that the present Unity scene already implements it. Add accessibility as a first-slice acceptance criterion, before the room accumulates custom controls. [Unity accessibility fundamentals](https://docs.unity3d.com/6000.0/Documentation/Manual/accessibility.html) [Apple HIG](https://developer.apple.com/design/human-interface-guidelines/accessibility/) [Apple Motion](https://developer.apple.com/design/human-interface-guidelines/motion)

## Photo and save-data boundary

Because photo creation is an occasional action, request one chosen photo through the platform picker; do not ask for broad library permissions. Support cancellation and offer the authored cat immediately. Show the data flow before upload and separate source-photo retention, generated GLB retention, local cache, analytics, and provider logs. Record consent version and deletion behavior without retaining extra image data. Check uploaded image size/type and strip metadata if not required. Make retry and cancellation explicit.

The current local state files and journal live under `Application.persistentDataPath`, while the active generated GLB is cached under the same platform-managed area. This establishes app-private local storage but not cross-device or reinstall continuity. Confirm the exact iOS path/backup behavior produced by Unity, decide whether user-created cat assets should be backed up or exported, and exclude only reproducible caches from backup. Add test cases for update, device migration, app reinstall, deleted source photo, generated asset replacement, storage pressure, and full local deletion before making continuity promises. Google and Apple picker/privacy guidance is summarized in `BRAND.md`.

If CatMe is likely to be used by children, photo uploads and generated/shareable output add a trust and compliance burden. Audience and age posture are product decisions, not merely an App Store age-rating field. Complete platform-policy review and jurisdiction-specific counsel before enabling a child-facing service or collecting child data.

## Recommendation status

**Hypothesis:** benchmark a consistent authored quadruped rig with adjustable coat/markings against one provider-generated cat before investing further in unrestricted unique meshes. This is not a final pipeline decision. The key comparison is owner recognition *during the same interactions* and the cost per accepted playable result.
