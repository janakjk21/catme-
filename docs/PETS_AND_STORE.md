# Pets and store

## Pets

- Each pet is a `PetProfile` in `Scripts/Pets/PetRoster.cs`: id, name, species (cat or dog), the original photo on this phone, the server id, the downloaded model path, and whether a stand-in model is used. Saved per phone in the app data folder.
- One pet is **active**: it walks with the player. The garden reloads the characters when the active pet changes and has its own model.
- **Your pets** (pause menu, `Screens/PetsScreen.cs`): the list of pets, "Walk with [Name]" to switch, and **Add a pet**.
- **Add a pet** runs onboarding steps Cat or dog -> Pet photo, then back to the garden while it is made. No selfie: the person is made once.
- A cat without its own model uses the bundled cat. Dogs use the built-in corgi until real dog models are switched on.
- Free creations per account: **2 pets and 1 person** (server quota). When a creation is rejected or fails, the credit is not used.

## Store (layout only)

`Screens/StoreScreen.cs`, catalogue in `Scripts/Pets/StoreCatalog.cs`. Everything is free: **Get** only marks an item as owned on this phone ("Everything here is free while the shop is being built.").

| Tab | Items |
|---|---|
| Toys | Soft ball, Feather wand, Squeaky mouse, Rope tug, Soft disc |
| Food | Salmon bites, Tuna flakes, Chicken dinner, Treat jar |
| Your bag | items you own, shared by every pet |

## Payments (not built; roadmap task 11)

Decisions the owner still has to make: what is paid (more pets, items, or both), prices, and whether there is a subscription. The build will be:

1. App Store in-app purchases in the app.
2. The server checks each receipt with Apple and adds credits to a wallet in Firestore (`users/{uid}`), with a ledger entry per change.
3. Creations spend from the wallet instead of the free quota.

No payment code, prices or product ids exist yet.
