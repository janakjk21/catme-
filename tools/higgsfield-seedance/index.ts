import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { config as loadEnv } from 'dotenv';
import { config, higgsfield } from '@higgsfield/client/v2';

const currentDir = path.dirname(fileURLToPath(import.meta.url));
loadEnv({ path: path.join(currentDir, '.env.local') });

const credentials = process.env.HF_CREDENTIALS;

if (!credentials) {
  console.error(
    'Missing HF_CREDENTIALS. Add it to tools/higgsfield-seedance/.env.local in KEY_ID:KEY_SECRET format.'
  );
  process.exit(1);
}

// Configure once at process startup. Never log or expose the credential value.
config({ credentials });

async function main(): Promise<void> {
  const result = await higgsfield.subscribe(
    'bytedance/seedance-2.5/text-to-video',
    {
      input: {
        prompt: 'A cinematic scene at sunset',
        duration: 5,
        resolution: '720p',
        aspect_ratio: '16:9',
      },
      withPolling: true,
    },
  );

  if (result.isNsfw) {
    throw new Error('Higgsfield moderated the request; no video was generated.');
  }

  if (result.isCanceled) {
    throw new Error('Higgsfield canceled the request; no video was generated.');
  }

  if (result.isFailed) {
    throw new Error('Higgsfield generation failed; no video was generated.');
  }

  if (!result.isCompleted) {
    throw new Error('Higgsfield returned without a completed generation.');
  }

  const videoUrl = result.jobs[0]?.results?.raw?.url;
  if (!videoUrl) {
    throw new Error('Higgsfield completed the request but returned no video URL.');
  }

  console.log(videoUrl);
}

main().catch((error: unknown) => {
  const message = error instanceof Error ? error.message : 'Unknown Higgsfield error';
  console.error(`Seedance generation did not complete successfully: ${message}`);
  process.exitCode = 1;
});
