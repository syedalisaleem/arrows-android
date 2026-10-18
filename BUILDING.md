# Building the Android App Bundle

The build runs entirely on GitHub Actions — no Unity Editor is installed locally.
Workflow: `.github/workflows/build-android.yml`

Everything is configured and the build is green. `game-ci/unity-builder@v4` compiles on a
GitHub-hosted runner and publishes the signed bundle as a run artifact.

---

## How the Unity licence works

GameCI needs three values, and `UNITY_SERIAL` is the one that actually triggers activation.
Its `activate.sh` matches on:

```bash
if [[ -n "$UNITY_SERIAL" && -n "$UNITY_EMAIL" && -n "$UNITY_PASSWORD" ]]; then
```

then runs:

```bash
unity-editor -serial "$UNITY_SERIAL" -username "$UNITY_EMAIL" \
             -password "$UNITY_PASSWORD" -projectPath "/BlankProject"
```

`UNITY_LICENSE` alone is **not** forwarded into the container, so setting only that secret
produces `License activation strategy could not be determined.`

### One-time: obtain the Personal serial

A Unity Personal serial starts with `F`. It is not displayed anywhere in the Unity Hub UI.
The only tool that will mint one is Unity's own licensing client, which ships inside the
Hub installer — the installer needs administrator rights, the client does not.

```powershell
# 1. Ask the licensing client for an activation request
.\Unity.Licensing.Client.exe --generate-alf-request `
    --activation-product-name Unity `
    --activation-product-version 6000.0.58f2 `
    --activation-file-path .\Unity_v6000.0.58f2.alf

# 2. Mint the ULF using the signed-in Hub session's access token.
#    --accessToken also accepts --username/--password instead.
.\Unity.Licensing.Client.exe --activate-ulf --accessToken <token>
```

Step 2 writes `C:\ProgramData\Unity\Unity_lic.ulf`. The serial is the `<DeveloperData>`
value, base64-decoded with the first four bytes dropped:

```powershell
$ulf = Get-Content 'C:\ProgramData\Unity\Unity_lic.ulf' -Raw
$b64 = [regex]::Match($ulf, '<DeveloperData\s+Value="([^"]+)"').Groups[1].Value
$bytes = [Convert]::FromBase64String($b64)
-join ($bytes[4..($bytes.Length - 1)] | ForEach-Object { [char]$_ })
```

Current serial: `F4-GBUN-78Q5-MSJK-8F5P-6C7Z`

### One-time: store the secrets

```powershell
gh secret set UNITY_SERIAL   -R syedalisaleem/arrows-android -b 'F4-GBUN-78Q5-MSJK-8F5P-6C7Z'
gh secret set UNITY_EMAIL    -R syedalisaleem/arrows-android
gh secret set UNITY_PASSWORD -R syedalisaleem/arrows-android
```

Backups of the ULF and the release keystore are in `E:\build\keystore\`.

---

## Running a build

Push to `main`, or trigger it manually:

```powershell
gh workflow run build-android.yml -R syedalisaleem/arrows-android
```

A cold build takes roughly 20–60 minutes. Later builds reuse the cached `Library` folder
and are considerably faster.

The signed `.aab` appears in the run's **Artifacts** section as `Arrows-AAB-<run number>`.

---

## Secrets reference

| Secret | Purpose |
|---|---|
| `UNITY_SERIAL` | Unity Personal serial (`F…`). **This is what activates the build.** |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_LICENSE` | Optional, contents of `Unity_lic.ulf`. Not enough on its own — v4 does not forward it into the container. |
| `ANDROID_KEYSTORE_BASE64` | Base64 of the release keystore |
| `ANDROID_KEYSTORE_PASS` | Keystore + key password (alias `arrows`) |

Release signing identity: `CN=Syed Ali, OU=Development, O=SyedAli, L=Lahore, ST=Punjab, C=PK`,
RSA 2048 / SHA256withRSA. Verify a bundle with:

```powershell
jarsigner -verify -verbose -certs Arrows.aab
```

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

## Licence dead ends

These look promising and do **not** work for a Unity Personal account:

- **`license.unity3d.com/manual`** — Personal support was removed. Uploading an `.alf`
  only offers Pro/Plus. Older posts describing a "hidden Personal option" no longer apply.
- **`unity-editor -username … -password …`** — authenticates fine
  (`Successfully updated the access token`) then returns
  `Found 0 entitlement groups and 0 free entitlements`. The account's entitlement is a
  machine-bound *named-user* licence, so an unbound container is granted nothing.
- **`-createManualActivationFile`** — Unity 6 checks the licence before honouring it and
  exits with `No valid Unity Editor license found` without writing an `.alf`.
- **`game-ci/unity-activate`** — its "personal licence support" means *accepting a `.ulf`*,
  not credentials.
- **`game-ci/unity-request-activation-file`** — formally deprecated by GameCI.

The serial route works because `UNITY_SERIAL` requests a *fresh* serial activation for
whatever machine the container presents, so nothing stays tied to the PC that minted the
ULF.

A Personal ULF carries a rolling expiry of about one month. Every build re-activates from
the serial, so CI keeps working unattended; if activation ever starts failing, re-mint the
ULF and re-read the serial with the snippet above.
