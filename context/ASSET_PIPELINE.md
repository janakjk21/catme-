# Cat Asset and Generation Contract

## Development fixture

Use `LocalFixtures/Cats/mochi-tripo-walk.glb` for ordinary gameplay development. It is a known-good generated walking cat and must not trigger another provider request.

Discover these values from each GLB:

- animation clip names
- forward axis
- up axis
- bounds and height
- floor offset
- skeleton and skinned meshes

Do not hardcode a provider's current clip name or assume all cats face the same direction.

## Production boundary

The Unity client never owns provider credentials or calls Meshy or Tripo directly.

```text
photo
→ server prepares reference
→ Meshy static GLB
→ user approves version
→ Tripo quadruped rig and walk
→ server validates and optimizes
→ versioned manifest plus mobile GLB
→ Unity downloads and caches
```

Provider work remains in sibling repository `../clonecatme`. Open it only when a task explicitly concerns the generation server or its API contract.

## Runtime package

The server should eventually deliver:

```text
cats/<cat-id>/<version>/
  cat-mobile.glb
  cat-preview.webp
  manifest.json
```

The manifest owns version, URLs, checksum, byte size, scale, axes, floor offset, animation mapping, and validation flags.

## Mobile quality target

- Preserve coat colour and markings.
- Preserve PBR base colour, normal, and roughness where present.
- Fur metalness is zero.
- Aim for a 10–25 MB playable package where visual quality permits.
- Preserve the provider original separately.
- Never remove skeletons, skin weights, or animation clips during optimization.

Remote download, caching, and API integration begin only after the local fixture works in `ModelTest` and `HomeRoom`.
