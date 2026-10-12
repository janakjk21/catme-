import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { config as loadEnv } from "dotenv";
import {
  higgsfield,
  config as configureHiggsfield,
  APIError,
  AuthenticationError,
  NotEnoughCreditsError,
  TimeoutError,
} from "@higgsfield/client/v2";

// Server-side only: credentials come from .env.local (git-ignored), never from source.
const here = dirname(fileURLToPath(import.meta.url));
loadEnv({ path: resolve(here, ".env.local"), quiet: true });

const MODEL = "bytedance/seedance-2.5/text-to-video";

function fail(message: string): never {
  console.error(message);
  process.exit(1);
}

const credentials = process.env.HF_CREDENTIALS?.trim();
if (!credentials) {
  fail("HF_CREDENTIALS is not set. Add it to tools/higgsfield/.env.local as key-id:key-secret.");
}
if (!/^[^:\s]+:[^:\s]+$/.test(credentials)) {
  fail("HF_CREDENTIALS must be in key-id:key-secret format.");
}
// Video generation can outlast the SDK default 5-minute poll limit.
configureHiggsfield({ credentials, maxPollTime: 15 * 60 * 1000 });

async function main(): Promise<void> {
  const result = await higgsfield.subscribe(MODEL, {
    input: {
      prompt: "A cinematic scene at sunset",
      duration: 5,
      resolution: "720p",
      aspect_ratio: "16:9",
    },
    withPolling: true,
  });

  // status is typed narrowly by the SDK; compare as string so "canceled" is handled too.
  const status: string = result.status;
  switch (status) {
    case "completed": {
      const url = result.video?.url;
      if (!url) {
        fail(`Request ${result.request_id} completed but the response contained no video URL.`);
      }
      console.log(`Video URL: ${url}`);
      return;
    }
    case "nsfw":
      fail(`Request ${result.request_id} was rejected by moderation. No video was generated.`);
    case "failed":
      fail(`Request ${result.request_id} failed. No video was generated.`);
    case "canceled":
    case "cancelled":
      fail(`Request ${result.request_id} was canceled. No video was generated.`);
    default:
      fail(`Request ${result.request_id} ended in unexpected status "${status}". No video URL.`);
  }
}

main().catch((error: unknown) => {
  if (error instanceof AuthenticationError) {
    fail("Authentication failed: check HF_CREDENTIALS.");
  }
  if (error instanceof NotEnoughCreditsError) {
    fail("Not enough credits on the Higgsfield account.");
  }
  if (error instanceof TimeoutError) {
    fail(`Timed out waiting for the request to finish (${error.message}). No video URL; it may have been canceled or still be running.`);
  }
  if (error instanceof APIError) {
    fail(`Higgsfield API error (${error.statusCode ?? "unknown status"}): ${error.message}`);
  }
  fail(`Unexpected error: ${error instanceof Error ? error.message : String(error)}`);
});
