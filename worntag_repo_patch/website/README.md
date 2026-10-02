# WornTag website

A responsive marketing site served from static HTML by a small Node HTTP server. The beta-list endpoints use Railway PostgreSQL; the public beta form is open and stores consented email addresses. The copy and concept imagery present WornTag as in development; there are no working app downloads, photo uploads, payments, or customer testimonials.

## Pages

- `/` — brand introduction and clear development status
- `/how-it-works.html` — product concept and its limits
- `/faq.html` — availability, photo, privacy, price and device questions
- `/about.html` — brand story
- `/stories.html` — editorial reflections, not customer testimonials
- `/blog.html` and `/blog/*.html` — guides written to be found and cited (see SEO_MARKETING.md)
- `/privacy.html` — pre-launch site privacy notice
- `/terms.html` — pre-launch website terms
- `/beta.html` — beta invitation page with active consent-based email storage and self-service removal
- `/404.html` — helpful not-found page

SEO decisions are recorded in [`../docs/SEO_MARKETING.md`](../docs/SEO_MARKETING.md), with the latest findings in [`../docs/SEO_AUDIT_2026-09-27.md`](../docs/SEO_AUDIT_2026-09-27.md). Brand direction is in [`../docs/brand/BRAND_GUIDE.md`](../docs/brand/BRAND_GUIDE.md).

## Run locally

Requires Node 22.12 or newer.

```sh
npm start
```

Open `http://localhost:3000`. The server uses Railway's `PORT` when deployed and exposes `/health` for its health check.

## Railway and Cloudflare

The site is deployed as `worntag-website`, separate from the existing `doll-me-` service. Keep that service and its `slap3d.com` domain unchanged. The website listens on Railway's injected `PORT`, and `/health` returns a small readiness response.

The canonical host is `https://worntag.com`. Requests to `www.worntag.com` and `/index.html` redirect to the apex homepage form; verify these redirects after every deployment.

The brand icon is provided as SVG, a multi-size ICO, a 32px PNG fallback, and a 180px Apple touch icon. All HTML pages reference these files; the server's public-file allowlist must include each icon path.

The beta route uses Railway PostgreSQL through the `DATABASE_URL` Railway reference variable. The production `Postgres` service is connected and `/api/beta-signups/status` returns `{"open":true}`. The server creates only a `beta_signups` table when a database is available; it stores normalized email, consent timestamp/version and source, never pet photos or IP addresses. The live form was checked with a synthetic `example.invalid` address: first insert and duplicate submission both succeeded, then self-service removal succeeded. The test row was removed. Railway bills database resources based on actual usage; monitor the project usage after the first full billing interval. Never expose database credentials or signup data through the public server.

Cloudflare `worntag.com` and `www.worntag.com` records target the separate Railway service. Preserve both existing domains and other DNS records during future changes.

## Before a public product launch

- Replace this pre-launch privacy notice and website terms with reviewed service-specific documents.
- Publish a real support/contact route, actual app store links, confirmed platform requirements and price.
- Add a working early-access mechanism only after its delivery and data handling are configured.
- Verify metadata, status codes, redirects, JSON-LD, sitemap, mobile/desktop performance, accessibility and social previews on the live host; submit the sitemap in Search Console and Bing Webmaster Tools.

## Deployment

Railway service `worntag-website` (project `splendid-warmth`) is connected to GitHub repo `janakjk21/catme-`, branch `main`, root directory `worntag_repo_patch/website`, watch path `/worntag_repo_patch/website/**`. A push to `main` that changes files in this folder deploys to production (worntag.com). Changes elsewhere in the repo, such as the Unity project, do not deploy. Check `/health` and the sitemap after a deploy.
