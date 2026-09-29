# Character Asset and Generation Contract

## Routine development

Use known-good local cat and owner assets for ordinary garden gameplay work. Do not spend Tripo or Meshy credits for routine testing. Use `ModelTest` to diagnose individual assets before loading them in `GardenCompanion`.

For every GLB, inspect rather than assume:

- embedded clip names, durations, and loop behavior
- forward/up axes, bounds, normalized height, and floor offset
- skeleton and skinned meshes
- supported facial or body controls
- texture sizes, material count, triangle count, and memory cost

Do not hardcode provider-specific clip names or assume cat and human assets share one rig. Preserve skeletons, skin weights, and required animation clips during optimization.

## Current prototype and production boundary

The recorded garden prototype uses Tripo for cat creation/rigging/walk and Meshy for the owner-avatar image/model/rig/walk path. Its local Unity client has been recorded calling providers directly with an ignored credential file included in local builds. This is insecure outside private testing. Do not distribute it. Before public release, remove bundled keys and direct clients, move generation behind authenticated HTTPS jobs, define photo consent/deletion, and provide progress, retry, review, and fallback. Do not start paid jobs without explicit budget approval.

The prototype keeps source photos and generated walking GLBs in phone-local persistent storage. Cloud recovery is not implemented. Keep the previous working pair until new assets are validated and loaded successfully.

## Runtime asset delivery

A future secure service should provide versioned cat and owner assets plus a manifest containing checksum, byte size, scale, axes, animation mapping, and validation flags. Keep the provider original separate from optimized mobile files.

Optimization must preserve likeness, coat markings, skinning, and supported animation. Set measurable download, memory, and frame-time targets on the minimum supported phone before choosing compression or level-of-detail settings. The existing garden demo's recorded character files total about 71 MB before app packaging, so asset size needs attention before distribution.
