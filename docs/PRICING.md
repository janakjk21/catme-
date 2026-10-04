# Pricing and payments

The decided plan (2026-10-04). Build it after the Apple Developer account exists. Player-facing wording lives in `docs/legal/TERMS_AND_AI_POLICY.md` section 5 and must match this file.

## Model
WornTag is **paid-only**: there is no free tier. A pet is made only after the player has paid, so every Tripo / Meshy run is covered. Payments go **only through Apple in-app purchase** (no Stripe, no links to outside payment).

## Products

| Product | Type | Price (US / UK) | Gives |
|---|---|---|---|
| `worntag.solo.monthly` | auto-renewing subscription, 1 month | $4.99 / £4.99 | month 1: first pet + the 3D self; later months: 1 new pet or 1 re-make |
| `worntag.plus.monthly` | auto-renewing subscription, 1 month | $7.99 / £7.99 | 3 new pets + 1 re-make a month |
| `worntag.family.monthly` | auto-renewing subscription, 1 month | $12.99 / £12.99 | 6 new pets + 3 re-makes a month |
| `worntag.pet.extra` | consumable | $2.99 / £2.99 | 1 extra pet, any plan |

- All three subscriptions in **one subscription group** ("WornTag"), so upgrading or downgrading replaces the plan (Apple prorates).
- **Introductory offer** (optional, decide before launch): first month Solo $2.99, Plus $4.99, Family $8.99, "pay up front" type (no free trial: a free trial would make pets before payment).
- Allowances reset at each renewal; they do not roll over. Unused extra pets stay on the account.
- Apple **Family Sharing** off for these products (one player's pets are tied to one account).
- **Billing grace period** on (up to 16 days for monthly) so a failed card doesn't put pets to sleep at once.

## Costs and margins (for reference)

Per creation: pet ≈ $0.86 (Tripo image 10 + model 30 + rig 25 credits at $0.01, +30% retries, + photo check); person ≈ $0.90 (Meshy, rate to confirm). Fixed: ≈ $20-30 a month (Railway, Apple $99/yr, domain). Apple keeps 15% (Small Business Program); UK/EU prices include 20% VAT.

| Plan | We keep US / UK | Month 1 typical | Later month, worst case (all used) |
|---|---|---|---|
| Solo $4.99 | $4.24 / $3.53 | +$2.48 / +$1.77 | +$3.38 / +$2.67 |
| Plus $7.99 | $6.79 / $5.66 | positive | ≈ +$3.35 / +$2.20 |
| Family $12.99 | $11.04 / $9.20 | positive | ≈ +$3.30 / +$1.45 |
| Extra pet $2.99 | $2.54 / $2.12 | +$1.68 / +$1.26 | |

About 10 subscribers cover fixed costs. Ads only pay back at about $1 per install with 7%+ paying; grow organically first.

## Where the paywall goes
Login → Cat or dog → Consent → Pet photo → Selfie → Name → **Paywall** → making starts.
- Shows the three plans (Solo highlighted as the default), price per month, what each gives, "Renews monthly until cancelled. Cancel anytime in Settings.", **Restore purchases**, links to Terms and Privacy.
- "Continue without signing in" still works (guest account), but reaches the paywall too.
- Adding a pet later (Your pets > Add a pet): uses the monthly allowance; when it's used up, offers the **Extra pet** or an upgrade.

## When a subscription ends (lapse)
| State | Player sees |
|---|---|
| Active or in grace period | everything |
| Lapsed | the sleeping pet: "Mochi is fast asleep. Come back and wake them." **Wake Mochi** resubscribes. Garden, room and walks locked; Settings, Account and deletion still work. |
| Lapsed 83 days | notification / email: "Your pets and photos will be deleted in 7 days unless you come back." |
| Lapsed 90 days | server deletes photos, models and pet records (same path as Delete account data, keeping the account and purchase records). |
| Resubscribes within 90 days | everything restored, no new generation |

## Server (`worntag-api`)
- Entitlements per account in Firestore (`users/{uid}/entitlements`, `purchases`, `ledger`; rules already reserve these as server-write-only): plan, status (active / grace / lapsed), period end, allowance left this period, extra pets left.
- **Verify every purchase server-side** (App Store Server API signed transactions, or RevenueCat webhooks) and listen to **App Store Server Notifications V2** for renewals, cancellations, refunds, grace and expiry. Never trust the app's word.
- `POST /v1/pets` checks the entitlement (allowance or extra pet) inside the same transaction as today's quota; a refusal returns a clear code (`SUBSCRIPTION_REQUIRED`, `ALLOWANCE_USED`).
- A daily job applies the 83-day warning and the 90-day deletion.
- Review accounts (`CATME_REVIEW_UIDS`) keep working in the sandbox.

## App (Unity)
- Purchase, restore and status through the chosen SDK; paywall screen per `docs/MENUS_AND_ACCOUNT.md` look; Account screen shows the plan, renewal date, **Manage subscription** (opens Apple's subscription settings) and Restore purchases.
- The sleeping-pet lapse state.
- Sandbox testing with a StoreKit configuration file and sandbox testers.

## Open decision
**SDK: RevenueCat or Unity In-App Purchases** (both have installed skills: `integrate-revenuecat`, `revenuecat-purchase-flow`, `revenuecat-paywall`, `revenuecat-testing-setup`, and Unity's `implement-in-app-purchases`). RevenueCat handles receipts, renewals, grace and webhooks (free to about $2.5k a month revenue, then 1%); Unity IAP plus our own App Store Server API check has no fee but more code. **Use the skill for the chosen SDK; don't write StoreKit or receipt code from memory.** When decided, add the SDK company to the privacy policy's processor list if it receives purchase data (RevenueCat does).

## App Store Connect checklist
Products created with these IDs, prices per storefront, localized names and descriptions, a review screenshot for each product, the subscription group and display order, the introductory offer (if used), grace period on, App Store Server Notifications V2 URL set to the API, sandbox testers created. In-app purchases are submitted **with** the first app version.
