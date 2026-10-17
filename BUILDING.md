# Building the Android App Bundle

The build runs entirely on GitHub Actions — no Unity Editor is installed locally.
Workflow: `.github/workflows/build-android.yml`

Everything is configured **except one secret**: `UNITY_LICENSE`. Unity requires a licence
to run the Editor headlessly, and a Unity **Personal** licence can only be issued by
Unity Hub. There is no CI-only way to obtain one; see the notes at the bottom.

---

## One-time: get `UNITY_LICENSE`

This needs Unity Hub once. Unity Hub is **not** the Unity Editor — it is roughly 250 MB
and is only used to mint a licence file. It can be uninstalled straight afterwards.

1. **Install Unity Hub** — https://unity.com/download

   > The C: drive on this machine is nearly full. The installer lets you choose a
   > different destination folder — point it at `E:\UnityHub` or similar. Do not install
   > any Editor version; only Hub itself is needed.

2. **Launch Hub and sign in** with the Unity account tied to this project
   (`syedalisaleem14@gmail.com`).

3. **Activate a free licence**
   `Preferences` (the gear icon) → `Licenses` → `Add` → **`Get a free personal license`**
   → accept the terms.

   Even if a licence already shows as active, complete this step — it is what writes the
   file to disk.

4. **Confirm the file exists** at:

   ```
   C:\ProgramData\Unity\Unity_lic.ulf
   ```

5. **Upload it as a secret** (from the repository root):

   ```powershell
   gh secret set UNITY_LICENSE -R syedalisaleem/arrows-android `
     -b (Get-Content 'C:\ProgramData\Unity\Unity_lic.ulf' -Raw)
   ```

That's it. You can now uninstall Unity Hub if you want.

---

## Running a build

Push to `main`, or trigger it manually:

```powershell
gh workflow run build-android.yml -R syedalisaleem/arrows-android
```

First build takes roughly 40–90 minutes. Later builds reuse the cached `Library` folder
and are considerably faster.

The signed `.aab` appears in the run's **Artifacts** section as `Arrows-AAB-<run number>`.

---

## Secrets reference

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Contents of `Unity_lic.ulf`. **Currently missing — this is the only blocker.** |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `ANDROID_KEYSTORE_BASE64` | Base64 of the release keystore |
| `ANDROID_KEYSTORE_PASS` | Keystore + key password (alias `arrows`) |

---

## Ad configuration

| Placement | Format | Code hook |
|---|---|---|
| +1 life when out of lives | Rewarded | `Level.HandleLivesDepleted()` |
| Double coins on the win panel | Rewarded | `UIRootController.ShowWin()` |
| Between levels (every 2, 60s cooldown) | Interstitial | `UIRootController.OnNextLevelRequested()` |

Ad unit IDs live in `Assets/_Game/Scripts/Ads/AdsConfig.cs`.
The App ID must be kept in sync in
`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` — the build copies it
into `AndroidManifest.xml`.

---

## Why there is no CI-only licence path

All three options were tested and are dead ends for a Unity Personal account:

- **Manual activation** (`license.unity3d.com/manual`) — Unity removed Personal support.
  Unity's own docs: *"Manual activation ... doesn't support Unity Pro assigned seats,
  Unity Personal, or floating license subscriptions. To activate a license for Unity
  Personal, log in to the Unity Hub."*
- **`unity-editor -batchmode -username ... -password ...`** — exits 0 but grants nothing:
  `[Licensing::Client] Error: ... Found 0 entitlement groups and 0 free entitlements`
- **`game-ci/unity-activate`** — `License activation strategy could not be determined.`
  Its "personal licence support" means *accepting a `.ulf`*, not credentials.
- `game-ci/unity-request-activation-file` is formally **deprecated** by GameCI.

The `.ulf` is not bound tightly enough to the machine that created it to matter — GameCI's
documented flow is to activate on Windows and build on Ubuntu CI.
