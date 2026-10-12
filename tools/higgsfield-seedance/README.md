# Higgsfield Seedance 2.5 smoke test

Server-side TypeScript example for Higgsfield Seedance 2.5. Keep this tool outside the Unity runtime; credentials must never be embedded in the mobile app.

## Setup

```bash
cd tools/higgsfield-seedance
npm install
cp .env.example .env.local
```

Edit `.env.local` locally and set a fresh credential in `KEY_ID:KEY_SECRET` format:

```bash
HF_CREDENTIALS=YOUR_KEY_ID:YOUR_KEY_SECRET
```

`.env.local` and `node_modules/` are ignored by Git.

## Run

This command creates a billable 5-second 720p Seedance 2.5 video and waits for completion:

```bash
npm run seedance
```

On success, the process prints only the generated video URL.

The example exits non-zero for missing credentials, failed requests, canceled requests, moderated requests, incomplete responses, or completed responses without a video URL.

## Type-check

```bash
npm run typecheck
```

Model: `bytedance/seedance-2.5/text-to-video`

Prompt: `A cinematic scene at sunset`

Parameters: `duration=5`, `resolution=720p`, `aspect_ratio=16:9`.
