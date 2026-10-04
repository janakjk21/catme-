# App Review notes (draft)

WornTag can be opened with **Continue without signing in**. For repeatable review, the owner should provide a dedicated Google test account in App Store Connect and add that account's Firebase UID to `CATME_REVIEW_UIDS` on Railway. Do not put its password in this repository.

Flow: choose cat or dog, take one clear pet photo, agree to the photo/AI notice, take a head-and-shoulders selfie, then walk in the garden while the pet is being made. Creation normally takes 3-8 minutes. The garden shows progress and then reveals the ready pet. A failed photo check offers a retry. No purchase is required in this version; the Store is hidden.

Menu > Account > Your data contains **Delete my photos** and **Delete account**. Pet photos and selfies are sent with consent to the WornTag API, which uses Tripo, Meshy and OpenAI for checking and generation. Firebase handles sign-in, records and private storage. The privacy policy and terms are linked from Login and Settings.

Before submission, the owner must verify the review account, UID allowlist, quotas, production providers, policies and full flow on a device. Remove the UID after review if it is no longer needed.
