# Security and Privacy Contract

Apply these requirements when work touches networking, photographs, generated assets, local storage, permissions, analytics, purchases, or release builds.

## Secrets and provider access

- Unity must never contain Meshy, Tripo, Supabase service-role, OpenAI, signing, or other privileged credentials.
- Keep secrets in server environment configuration and out of Git, logs, screenshots, manifests, and client builds.
- The client talks only to CatMe-controlled endpoints.
- Paid provider operations require server-side authorization, idempotency, spending limits, and persisted task IDs.

## Photo privacy

- Treat uploaded cat photos as private user content.
- Request only permissions required for photo selection or capture.
- Do not send source photos to analytics, crash metadata, or diagnostic logs.
- Show the user what will be uploaded and support replacement or deletion when the product service is introduced.
- Strip unnecessary image metadata on the server before provider processing.

## Asset downloads

- Use HTTPS and short-lived authorized URLs in production.
- Validate manifest schema, cat ownership, version, expected byte size, and checksum before activating a download.
- Enforce download and decompressed-size limits.
- Parse generated GLBs as untrusted input and fail closed on malformed files, invalid transforms, missing required components, or impossible bounds.
- Keep the last known-good cat until a new version has fully downloaded and loaded successfully.

## Local data

- Store only data needed for play and recovery.
- Do not store provider credentials or raw provider responses.
- Version save data and handle invalid or newer schemas safely.
- Use platform-protected storage later for authentication tokens or purchase receipts.
- Never treat client-side ownership, currency, entitlement, or purchase values as authoritative.

## Networking and logs

- Use bounded timeouts, retries with backoff, cancellation, and explicit failure states.
- Do not retry paid task creation blindly.
- Redact authorization headers, signed URLs, user identifiers, and local filesystem paths from production logs.
- Keep development diagnostics behind development-build checks.

## Unity and mobile release

- Request the minimum iOS and Android permissions.
- Remove unused platform capabilities and development endpoints before release.
- Pin Unity packages and review dependency changes deliberately.
- Keep signing certificates, provisioning profiles, keystores, and passwords outside the repository.
- Recheck privacy disclosures when photo upload, analytics, accounts, advertising, or purchases are added.

## Security review triggers

Use a focused security review before enabling remote downloads, photo upload, authentication, analytics, payments, cloud saves, deep links, or public distribution. Local offline gameplay changes normally need only ordinary correctness validation.
