# WornTag website: SEO and marketing direction

This is the WornTag-specific application of the brand guide and upstream SEO/marketing material. WornTag remains a pre-launch companion concept; Slap3D product claims and its keyword list do not describe WornTag and must not be copied into its pages.

## Source review

Fetched `main` from `https://github.com/janakjk21/doll-me-` on 27 September 2026. The local DollMe checkout was already at the fetched commit `3f9e6a7` (`Improve guest funnel and SEO discovery`), so no branch merge was needed. Reviewed the SEO audit prompt, root SEO/growth requirements, US keyword/content review, README, requirements and production-readiness notes:

- `.prompts/seo-audit.md`
- `seo.md` (Version 33 planned growth requirements)
- `docs/seo-keywords-us.md`
- `README.md`, `docs/README.md`, `docs/production.md`, `docs/requirement.txt`, `PRODUCTION_READINESS.md`

Those documents belong to Slap3D/DollMe. Their useful general guidance is applied here: unique per-page metadata, accurate initial HTML, valid status codes, canonical URLs, a sitemap and robots file, meaningful internal links, lightweight public pages, honest availability/pricing statements, and no mass-produced keyword pages. Their slap-game keyword targets, paid funnel, and gameplay promises are explicitly excluded.

On 27 September 2026, also reviewed the open-source [AgriciDaniel/claude-seo repository](https://github.com/AgriciDaniel/claude-seo/tree/e77e783e38eeb738424eb72117abbd2dacdd88af), version 2.4.0, using its technical, content, image, schema, sitemap and AI/agent-readiness audit guidance. The repository is a Claude Code plugin; its runtime/extensions were not installed or run. Its recommendations were applied selectively and checked against current Google Search Central guidance. See [SEO_AUDIT_2026-09-27.md](SEO_AUDIT_2026-09-27.md) for the scoped findings, changes and remaining verification.

## WornTag positioning

- Lead with recognition and presence: **Closer than a photo.**
- Describe the product as in development. No app-store download, public upload, price, supported-device list, or customer testimonial is claimed as available. The consent-based beta email list is open and stored in Railway PostgreSQL; do not imply that beta access itself is open or guaranteed.
- Be direct that a generated likeness is not an animal, does not contain memories or consciousness, and cannot bring a pet back.
- Use the existing warm home and pet imagery. Label illustrations as concepts; do not present them as screenshots or real customer photos.
- Avoid using grief as an urgency tactic. The message should welcome people celebrating a living pet and people revisiting old photographs.

## Current public page set

| Page | Purpose |
|---|---|
| `/` | Explain WornTag in one screen and route visitors to the product explanation. |
| `/how-it-works.html` | Explain the planned photo-to-companion experience, boundaries, and development status. |
| `/faq.html` | Answer availability, uploads, privacy, price, supported devices, and memory-related questions clearly. |
| `/about.html` | Explain the brand idea without invented founder biography. |
| `/stories.html` | Publish original editorial reflections, explicitly not customer testimonials. |
| `/privacy.html` | State the limits of the current pre-launch site and external Google Fonts request. Replace before app/account/photo features launch. |
| `/terms.html` | Scope this pre-launch website only; replace with reviewed service terms before the app launches. |
| `/beta.html` | Explain the small-beta invitation list, ask for separate email consent, and provide self-service removal. The live form writes only after a valid email and explicit consent are provided; it does not send messages automatically. |

## SEO and acquisition next steps

1. **Hosting verified 27 September 2026:** the separate Railway `worntag-website` service is deployed in `splendid-warmth` production. The canonical apex, Railway hostname, and `www` host respond as expected; `/health`, `/robots.txt`, and `/sitemap.xml` return 200. The sitemap lists eight intended public pages. The existing `doll-me-` service was not changed.
2. **Canonical host correction deployed and verified:** public DNS resolves the apex. All eight apex pages return 200; `www` returns one 301 to the matching apex path, and `www/index.html` redirects directly to `/` with the query preserved. The duplicate-host issue is resolved at the HTTP layer; Search Console's selected canonical still needs confirmation.
3. **Beta page and email storage live 27 September 2026:** `/beta.html` is deployed to Railway and `www.worntag.com`. The consent-based signup form stores normalized email, consent timestamp/version, and source in Railway PostgreSQL through a private Railway reference variable. It does not send mail or store photos/IP addresses. `/api/beta-signups/status` returns `{"open":true}`. A synthetic `example.invalid` address verified insert, duplicate submission, and removal; the test record was deleted. Railway resource usage is metered; monitor the project after the first full billing interval.
4. **Copy assessment:** the warm visual voice and “closer than a photo” positioning are cohesive, and the site is unusually transparent about concept imagery, unavailable features, and the limits of a digital likeness. The main copy risk is repetition of pre-launch caveats; the next content pass should show one concrete first-use moment and use genuine founder or tester evidence only when available. Do not turn concept imagery into implied product screenshots or testimonials.
5. Run mobile/desktop PageSpeed and keyboard/accessibility checks; no Core Web Vitals or lab score is claimed by the current audit.
6. Verify the domain property and indexing state in Search Console and Bing Webmaster Tools, submit the sitemap, and inspect Google-selected canonicals. A sitemap and HTTP 200 do not prove indexing.
7. Use actual Search Console query and landing-page data before selecting search themes. Do not borrow Slap3D's US keyword volumes or write thin landing pages from unverified terms.
8. Before the app launches or beta email collection opens, publish reviewed privacy/terms, confirmed support/contact details, actual store URLs, accurate device requirements and a verified beta invitation route. The current public site is a pre-launch website, not the mobile app launch.
9. Do not add `llms.txt` as a Google-ranking tactic; the current Google documentation and reviewed SEO framework say it does not improve Search visibility. No analytics tracker was added.

## Hosting boundary

The Railway project contains the existing production `doll-me-` service for `slap3d.com` and a separate `worntag-website` service for WornTag. There is no database service or database connection variable on `worntag-website` yet. The root and `www` records are the exact per-domain CNAME targets Railway issued, with matching Railway ownership TXT records. Both Cloudflare CNAMEs are DNS-only. Do not repoint or replace `doll-me-`, change unrelated DNS, or invent IP/CNAME targets. No commit or GitHub push was made; Railway deployment used the working website folder.
