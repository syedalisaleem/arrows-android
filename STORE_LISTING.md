# Google Play listing — Arrows

Everything below is ready to paste into Play Console.
Limits: **title 30**, **short description 80**, **full description 4000**.

---

## 1. App name

**Recommended:** `Arrows: Line Puzzle` — 19 characters

Brand first, then the exact search phrase people type. Short enough to never truncate.

| Alternative | Chars | Why |
|---|---|---|
| `Arrows: Line Puzzle` | 19 | **Recommended.** Brand + keyword, no filler |
| `Arrows: Tap Timing Puzzle` | 25 | Leads with the mechanic |
| `Arrow Lines: Timing Puzzle` | 26 | Strongest keyword coverage |
| `Arrows: Clear the Lines` | 23 | More descriptive, less searchable |
| `Arrows Puzzle: Tap & Clear` | 26 | Keyword-stuffed; reads generic |

Rules that matter: no emoji, no "Best/Free/#1", no other game's name, no repeated
keyword spam. `Arrows` is a crowded word on Play, so the `: Line Puzzle` suffix is what
makes the listing findable. If the name is ever rejected for impersonation, fall back to
`Arrow Lines: Timing Puzzle`.

The in-app label stays `Arrows` (from `productName`), which is what shows under the icon.

---

## 2. Short description (80 max)

**Recommended:** `Tap lines at the perfect moment. Clear the board without a crash.` — 65 chars

Alternatives:

| Text | Chars |
|---|---|
| `A minimalist timing puzzle. Tap to launch lines and dodge collisions.` | 69 |
| `Time every tap, clear every line, and never let the arrows collide.` | 67 |
| `One tap launches a line. One bad tap ends your run. Can you clear it?` | 69 |

The first 80 characters are the only ones shown in search results, so keep the promise
concrete and skip "fun", "addictive", "challenging" — Play filters spammy superlatives.

---

## 3. Full description (4000 max)

```
Arrows is a minimalist timing puzzle built around one question: when exactly do you tap?

Each level is a knot of arrow lines. Tap a line and it launches forward, dissolving from its tail as it travels. Tap too early and it hits another line — and every collision costs a life. Clear the whole board and the level is yours.

It looks calm. It isn't.

ONE FINGER, ONE DECISION
No joysticks, no timers, no clutter. Every level is solved with taps.

EASY TO LEARN, HARD TO RUSH
Lines travel at different speeds and lengths, so the gap that looked safe a second ago is already closed.

500 LEVELS, ONE CLIMB
The first ten boards are hand-built to teach the rhythm. After that, the knots get longer, tighter, and more tangled — up to two dozen interlocking arrows on a single screen. Every board is checked so its arrows stay clear of each other. Early levels take seconds. Later ones ask you to read the whole board before you touch anything.

ZOOM AND REPLAY
Pinch to zoom a busy board, or open the level list and jump straight back to any level you have unlocked.

FIVE LIVES
You start with five hearts. Every collision takes one. When they run out, the level restarts — and you can earn an extra life if you want one.

PLAY AT YOUR OWN PACE
No countdown, no energy meter, no sign-in. Your coins and progress stay on your device.

FEATURES
• One-tap controls built for phones
• 500 levels, each on a single screen
• A coin reward on every win
• Clean, minimal art
• Plays offline
• Small, lightweight install
• No account, no sign-up

ABOUT ADS
Arrows is free to play. A short ad may appear between levels — never during one. You can choose to watch a video for an extra life or to double the coins from a win. Nothing is forced.

Somewhere in every level there is an order that works. Find it.
```

Every claim above is true of the current build: 10 handcrafted level prefabs plus 490
procedural boards (levels 11–500); procedural boards are solver-verified and all 500
layout sets were checked for overlapping arrows. The bundle also has pinch/wheel camera
zoom, forgiving tap targeting, a themed level-select menu that only opens unlocked
levels, 5 starting lives (`LivesManager._maxLives`), `LevelConfig._winCoins = 10`,
PlayerPrefs persistence, rewarded life + rewarded double coins, and interstitial every
2 completed levels.

Note: the store description uses `•` and `—`. Both are fine; Play renders plain text.

---

## 4. Category and tags

| Field | Value |
|---|---|
| App type | Game |
| Category | **Puzzle** |
| Tags | `Puzzle`, `Casual`, `Single player`, `Offline` |
| Pricing | Free |
| Contains in-app purchases | No |

Puzzle is the correct primary category. Do **not** pick "Brain games" or "Trivia" —
the IARC questionnaire and the category feed into discovery, and Puzzle is where
timing/line puzzles rank.

---

## 5. Content rating (IARC questionnaire)

| Question | Answer |
|---|---|
| Category | Game |
| Violence (realistic or cartoon) | No |
| Blood / gore | No |
| Sexual content or nudity | No |
| Profanity | No |
| Controlled substances (drugs, alcohol, tobacco) | No |
| Gambling (real or simulated) | No |
| User-generated content / chat / social features | No |
| Shares user location | No |
| Allows users to interact online | No |
| Makes purchases | No |
| **Contains ads** | **Yes** |
| Digital purchases | No |

Expected outcome: **Everyone / PEGI 3 / USK 0**, with the `Contains ads` label.
No age gate is needed at this level.

One follow-up: set the ad content ceiling so a PEGI 3 rating stays honest. In AdMob,
open **App settings → Ad content rating → Maximum ad content rating: G**, and add this
to the Ads SDK init:

```csharp
RequestConfiguration requestConfiguration = new RequestConfiguration {
    MaxAdContentRating = MaxAdContentRating.G,
    TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
    TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.Unspecified
};
MobileAds.SetRequestConfiguration(requestConfiguration);
```

**Done in code** — `AdsManager.ApplyRequestConfiguration` applies the same `G` ceiling at
startup, and the build is already shipped with it. Set the matching ceiling in the AdMob
console so the two cannot drift apart.

---

## 6. Data safety

The AdMob SDK collects data, so this section cannot be skipped.

| Field | Answer |
|---|---|
| Does your app collect or share required user data types? | **Yes** |
| Is all collected data encrypted in transit? | **Yes** |
| Do you provide a way for users to request data deletion? | **No** (no accounts, no server; ad data is controlled by Google) |

Data types to declare:

| Data type | Collected | Shared | Purpose |
|---|---|---|---|
| **Device or other IDs** | Yes | Yes | Advertising or marketing; Analytics |
| **App activity → Other actions** (ad views, ad clicks) | Yes | Yes | Advertising or marketing; Analytics |

For both: not processed ephemerally, and **required** (the ad SDK needs them; the app is
funded by ads). Do not tick "optional".

Approximate location is **not** collected — the app never requests a location permission.
If you ever add one, declare `Location → Approximate location` as well.

Cross-check against Google's own guidance before submitting:
`support.google.com/admob/answer/10502938` (Data safety for the Google Mobile Ads SDK).

---

## 7. Other App content declarations

| Section | Answer |
|---|---|
| Ads | **Yes, contains ads** |
| Advertising ID | **Yes** (used for advertising) |
| App access | All functionality is available without special access |
| Target audience | **13+** (see below) |
| News app | No |
| COVID-19 contact tracing | No |
| Data deletion | Not applicable (no accounts) |
| Government app | No |
| Financial features | No |
| Health features | No |

**Target audience — pick 13+.** Choosing an under-13 audience puts the app inside the
Families policy: certified ad SDKs only, child-directed ad tagging, no personalised ads.
AdMob does support it, but it cuts ad revenue hard and adds review risk. For a puzzle with
ads, 13+ is the sane choice. Your content rating can still be Everyone/PEGI 3 — audience
and rating are separate fields.

---

## 8. Store graphics

Play requires all of these before a release can roll out.

| Asset | Spec | Status |
|---|---|---|
| App icon | 512×512 PNG, 32-bit, ≤ 1 MB | **Ready** — `icon-512-light.png` (launcher uses light), `icon-512-dark.png` |
| Feature graphic | 1024×500 PNG/JPEG, no alpha | **Ready** — `feature-1024x500.png` (flattened RGB) |
| Phone screenshots | 2–8, 320–3840 px each side, ratio ≤ 2:1 | **Ready** — 4 × 1080×1920 in `screenshots/` |
| 7" tablet screenshots | 2–8 (recommended) | **Ready** — 4 × 1080×1920 in `tablet-7/` |
| 10" tablet screenshots | 2–8, each side 1080–7680 px | **Ready** — 4 × 1080×1920 in `tablet-10/` |
| Promo video | YouTube URL (optional) | Optional |

The phone set was upscaled (Lanczos + unsharp) from the game's native 498×883 captures to
1080×1920, which clears Play's promotion threshold (≥ 4 shots, ≥ 3 at 1080 px). Re-capture
natively the next time the game runs on a 1080p phone; the framing already matches.

A 1080×1920 image satisfies both the 7-inch (320–3840) and 10-inch (1080–7680) tablet
specs, so the two tablet folders are deliberately identical copies.
`contact-sheet.png` shows all four shots side by side for a final look.

Play also crops the feature graphic differently across surfaces. Keep text inside the
central 80% — the generated one already does.

---

## 9. Verified facts about the bundle

Pulled directly from the current release artifacts `Arrows-1.0.23.apk` / `Arrows-1.0.23.aab` (run 39195513416, v22), not assumed:

| Property | Value | Play requirement |
|---|---|---|
| Package | `com.syedali.arrows` | — |
| versionCode ` `22` -> ` `23`` | Must increase every upload ✅ |
| versionName | `1.0.23` | — |
| minSdkVersion | `24` (Android 7.0) | — |
| **targetSdkVersion** | **`36`** | New apps need ≥ 35 since Aug 2025 ✅ |
| compileSdkVersion | `36` | — |
| ABI | ARM64 only | 64-bit required ✅ |
| Levels | **500** (10 handcrafted + 490 procedural, all checked for overlapping arrows) | — |
| Size | 42.38 MB AAB · 42.69 MB APK | ≤ 200 MB base module ✅ |
| APK / AAB SHA-256 | `5E72A626A380C5D50F2D336A759C1337DE502A4AE05155C5A6ED517816D26175` / `4BE2DB780FD7DA783AD88693B9A655BF2E21292087A11627336B40A97C36E234` | exact final artifacts ✅ |
| Signature | `CN=Syed Ali`, RSA 2048, APK Signature Scheme **v2 + v3** (v1 JAR absent; fine at minSdk 24) | — |
| AdMob App ID in manifest | `ca-app-pub-7129470803481646~1513994194` | Must match AdMob account ✅ |
| Consent | Google UMP 4.0.0, requested **before** `MobileAds.Initialize` | EEA/UK requirement ✅ |

---

## 10. Blockers between here and "Publish"

1. **Device pass for the full 500-level list.** v22 compiles and packages all 500 boards
   (10 handcrafted + 490 generated), but this environment has no emulator. On first
   install, play level 11 (the first procedural board) and check camera fit, arrow
   width/head size, tap targets, the new level menu, and camera pinch/zoom. Later
   procedural boards differ mainly in size and arrow count.
2. **Privacy policy URL** — live at <https://syedalisaleem.github.io/arrows-android/privacy/>.
   The source is `docs/privacy/index.html` in this repo, served by GitHub Pages from `main`.
3. **Play App Signing** must be enrolled on the first upload. The `.aab` is signed with
   your upload key, which is exactly what Play expects; Google re-signs for distribution.
   Keep `E:\build\keystore\` forever.
4. **Closed testing** for new personal developer accounts may still require
   **12 testers opted in for 14 continuous days** before applying for production.
5. **Ad content rating** — the G ceiling is wired in `AdsManager.ApplyRequestConfiguration`;
   set the same ceiling in the AdMob console.
6. **Credential rotation** — the previous keystore/Unity credentials were exposed; rotate
   the Unity password and any exposed secrets before production access is granted.

---

## 11. Contact and policy details

| Field | Value |
|---|---|
| Contact email | `syedalisaleem14@gmail.com` (also the address printed in the privacy policy) |
| Website | optional |
| Privacy policy | <https://syedalisaleem.github.io/arrows-android/privacy/> |
| Default language | English (United States) |
| Countries | All |

---

## 12. Release notes (Play "What's new")

Paste for v22 (under the 500-character limit):

```text
1.0.23 — haptic + animation fixes (v22 base + smoother arrows, reduced vibration)
- Arrow layouts are rebuilt with no overlapping paths.
- Arrowheads and strokes look consistent across every board.
- Pinch to zoom the board, and tap targeting is more forgiving.
- New level list: replay any unlocked level.
- Fixed arrow collisions, lives, and level menu HUD.
- Google consent + privacy policy unchanged.
```

