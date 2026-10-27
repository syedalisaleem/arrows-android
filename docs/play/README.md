# Arrows 1.0.23 — Play Store and Closed-Testing Kit

Use this sheet to move from the ready build into a **Play Store closed test**
and then to **production**. Current release artifacts are already built,
signed, and byte-verified; you do not need to rebuild before the first upload.

## Release facts

| Item | Value |
|---|---|
| Package | `com.syedali.arrows` |
| VersionCode | `21` |
| VersionName | `1.0.23` |
| Build source | GitHub Actions run `39195513416`, commit `20af5fa` |
| AAB | `E:\build\arrow-store\Arrows-1.0.23.aab` |
| APK | `E:\build\arrow-store\Arrows-1.0.23.apk` |
| AAB SHA-256 | `4BE2DB780FD7DA783AD88693B9A655BF2E21292087A11627336B40A97C36E234` |
| APK SHA-256 | `5E72A626A380C5D50F2D336A759C1337DE502A4AE05155C5A6ED517816D26175` |
| Contact | `syedalisaleem14@gmail.com` |
| Privacy policy | <https://syedalisaleem.github.io/arrows-android/privacy/> |

The store copy at `STORE_LISTING.md` is the canonical Play listing text.

## Before you upload

Do these in Play Console once, in order:

1. **App content**
   - **Privacy policy** — use the URL above.
   - **Ads** — yes, the app contains ads.
   - **In-app purchases** — none.
   - **Data safety** — the app reads/stores level/coins progress on the device
     and uses Google `ADS` + `AD_ID` for consent and personalized ad auctions.
   - **Content rating** — create/reuse a `G` rating questionnaire. The AdMob
     request ceiling is already restricted to `G` in `AdsManager`.

2. **AdMob data**
   - Create the **app** and the **three ad units** in your AdMob console if you
     have not already. The app and unit IDs encoded in the build are:
     - App: `ca-app-pub-7129470803481646~1513994194`
     - Interstitial: `ca-app-pub-7129470803481646/3163696791`
     - Rewarded extra life: `ca-app-pub-7129470803481646/7406194139`
     - Rewarded double coins: `ca-app-pub-7129470803481646/5180767308`
   - Keep the **same app ID** in AdMob that the manifest uses.

3. **Play App Signing**
   - The `.aab` above is signed with your **upload key**, not the final store
     key. Enroll in Play App Signing on first upload and Google will store the
     app signing key.
   - Never rotate or delete the local keystore file referenced by the
     `ANDROID_KEYSTORE_BASE64` GitHub secret.

4. **Screenshots and feature graphic**
   - Current assets live in `E:\build\arrow-store`.
   - Phone screenshots:
     - `phone-01.png`, `phone-02.png`, `phone-03.png`, `phone-04.png`
   - 7-inch and 10-inch tablet folders:
     - `tablet-01.png`, `tablet-02.png`, `tablet-03.png`, `tablet-04.png`
   - Feature graphic: `feature-1024x500.png`
   - `contact-sheet.png` is a quick visual check, not a Play asset.
   - If you refresh screenshots, reuse the v22 build so store graphics match
     what the app actually shows.

## Closed testing plan

### 1. Create/testing track

- Create a **closed testing** release track titled `Closed beta`.
- Upload only `Arrows-1.0.23.aab` there. Do **not** upload any `.apk` to Play.
- Keep the country list **worldwide** or at least include all tester countries.
- Set the release note from `STORE_LISTING.md` §12.

### 2. Invited testers

- Open the closed-testing **testers** tab.
- Create an email list with **12 distinct Gmail accounts** on different physical
  devices. This matters because some new personal developer accounts must run a
  **14-day trial with 12 opted-in testers** to access production.
- Do **not** put the same person on the list twice with slight wildcard variants.

### 3. Tester kit

- Name: **Arrows (Play Store closed beta)**
- Product: `com.syedali.arrows`
- Version to install: **1.0.23**
- Start here: <https://play.google.com/store/apps/details?id=com.syedali.arrows>
- Privacy: <https://syedalisaleem.github.io/arrows-android/privacy/>
- Email for feedback: <mailto:syedalisaleem14@gmail.com>
- Device OS target: **Android 7 (API 24) or newer**

### 4. Manifest / release criteria

Before you tell the game is ready, it must pass this check:

- Install without Play Protect warnings.
- First open:
  - Google consent form (EEA/UK).
  - Level 1 board appears with 3 arrows.
  - Line width and tail/head styling look consistent across the 10 handcrafted levels.
  - Tap on a line selects the *nearest* line within 0.5 world units.
  - Pinch to zoom the board.
  - `LEVELS` button opens the level list and only enables the levels you have unlocked.
  - Replaying an unlocked level works; it must not lock later levels away.
- Play through levels 1, 2, 11, 25, 50, 100, 200, 300, 400, 500.
  - No arrow lengths missing.
  - No line crosses another line.
  - No duplicate or touching arrowheads.
  - All lines eventually solve a level.
- The fifth collision (when lives reach 0) shows the rewarded `+1 life` prompt
  and respects the decline path.
- A win offers the rewarded double-coins button when the rewarded ad is loaded.
- Two successive wins eventually hit the interstitial path.
- Solved line disappears cleanly with **no lingering collision between the
  head trail and another body**.
- Privacy policy URL opens.
- No signed file mismatch; the AAB APK signature scheme is v2 + v3.

## After the closed test

- If all checks pass, simply continue as production after the **14-day**
  closed-testing requirement.
- If any ad unit still shows old assets, update consent/AdsManager config via
  Play Console content rating.
- Before production, rotate any leaked Unity or keystore credentials.

## What's still open outside the kit

| Issue | Where | Status | Next action |
|---|---|---|---|
| Device test of all 500 lines | none available here | Verify levels 11, 25, 50, 100, 200, 300, 400, 500 on a real device. | Closed test |
| Play App Signing enrollment | Play Console | Do on first `.aab` upload. | Play Console |
| Data safety declaration | Play Console | App uses `ADS`, `AD_ID`, and local PlayerPrefs progression. | Play Console |
| AdMob console setup repeats manifest | AdMob console | Use the exact IDs above. | AdMob |
| Credential rotation | GitHub secrets + Unity account | Some historical secrets were exposed. | Rotate before production |
| Privacy policy content | `docs/privacy/index.html` | Already live at GitHub Pages. | Done |

## What we can NOT test here

- Actual device performance and AdMob ad fill.
- Real Play Store screenshots and graphics.
- Sign-in and Play Protect warnings with a real Google account.
- Ads client behavior mid-stream.

Ask if you want a **country/locale list**, a **copy-ready store/support list**, or
**content rating questionnaire text** to paste into Play Console.
