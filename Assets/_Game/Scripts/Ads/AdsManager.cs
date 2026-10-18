using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using SerapKeremGameKit._Logging;

namespace _Game.Ads
{
    /// <summary>
    /// Central ad controller.
    ///
    /// Self-bootstrapping: a GameObject is created automatically at runtime, so the scene does
    /// not need to be touched at all. Query <see cref="Exists"/> before using it.
    ///
    /// Everything degrades gracefully — if the SDK fails to initialise or the device is offline,
    /// gameplay continues exactly as before, just without ads.
    /// </summary>
    public sealed class AdsManager : MonoBehaviour
    {
        private const string LogPrefix = "[Ads] ";
        private const int MaxRewardedRetries = 6;
        private const int MaxInterstitialRetries = 4;

        public static AdsManager Instance { get; private set; }
        public static bool Exists => Instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[AdsManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<AdsManager>();
            go.AddComponent<AdsRuntimeUI>();
        }

        /// <summary>
        /// One rewarded ad unit and its load state. Both rewarded placements get their own
        /// unit so their performance can be compared in the AdMob console.
        /// </summary>
        private sealed class RewardedSlot
        {
            public string UnitId;
            public RewardedAd Ad;
            public bool Loading;
            public int Retry;
            public float RetryAt;
            public Action OnEarned;
            public Action OnUnavailable;
            public bool EarnedThisShow;

            public bool Ready => Ad != null && Ad.CanShowAd();
        }

        private readonly Dictionary<string, RewardedSlot> _rewarded = new();

        private InterstitialAd _interstitialAd;
        private bool _interstitialLoading;
        private int _interstitialRetry;
        private float _interstitialRetryAt;
        private bool _sdkReady;

        private int _completedLevelsSinceInterstitial;
        private float _lastInterstitialTime = -999f;
        private int _extraLifeOffersThisLevel;

        /// <summary>Set when the player earns the double-coins reward for the current win.</summary>
        public bool DoubleCoinsUnlocked { get; private set; }

        public bool SdkReady => _sdkReady;
        public bool InterstitialReady => _interstitialAd != null && _interstitialAd.CanShowAd();

        /// <summary>True when the "+1 life" placement has a loaded ad ready to show.</summary>
        public bool RewardedReady => IsRewardedReady(AdsConfig.RewardedExtraLifeUnitId);

        public bool CanOfferExtraLife =>
            _extraLifeOffersThisLevel < AdsConfig.MaxExtraLifeOffersPerLevel;

        public bool IsRewardedReady(string unitId)
        {
            return _rewarded.TryGetValue(unitId, out var slot) && slot.Ready;
        }

        private RewardedSlot Slot(string unitId)
        {
            if (_rewarded.TryGetValue(unitId, out var existing)) return existing;

            var created = new RewardedSlot { UnitId = unitId, RetryAt = -1f };
            _rewarded[unitId] = created;
            return created;
        }

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            try
            {
                MobileAds.RaiseAdEventsOnUnityMainThread = true;
            }
            catch (Exception e)
            {
                TraceLogger.LogWarning(LogPrefix + "Could not flag main-thread ad events: " + e.Message);
            }

            ApplyRequestConfiguration();

            // GDPR / EEA: ask the UMP SDK for the current consent state and show the
            // Google-provided form when one is required. Ads only initialise after the
            // flow finishes, so no personalised request goes out before consent.
            RequestConsentThenInitialize();
        }

        private void RequestConsentThenInitialize()
        {
            try
            {
                var parameters = new ConsentRequestParameters
                {
                    TagForUnderAgeOfConsent = false,
                };

                ConsentInformation.Update(parameters, updateError =>
                {
                    if (updateError != null)
                    {
                        TraceLogger.LogWarning(LogPrefix + "Consent update failed: " + updateError.Message);
                        InitializeMobileAds();
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        if (formError != null)
                        {
                            TraceLogger.LogWarning(LogPrefix + "Consent form error: " + formError.Message);
                        }

                        InitializeMobileAds();
                    });
                });
            }
            catch (Exception e)
            {
                // If the UMP dependency is unavailable, never block the game's ads.
                TraceLogger.LogWarning(LogPrefix + "Consent flow unavailable: " + e.Message);
                InitializeMobileAds();
            }
        }

        private void InitializeMobileAds()
        {
            MobileAds.Initialize(status =>
            {
                _sdkReady = true;
                TraceLogger.Log(LogPrefix + "SDK initialised.");

                // Warm both rewarded units and the interstitial up front so a placement
                // never has to wait for a download when the player asks for it.
                PreloadRewarded(AdsConfig.RewardedExtraLifeUnitId);
                PreloadRewarded(AdsConfig.RewardedDoubleCoinsUnitId);
                PreloadInterstitial();
            });
        }

        /// <summary>
        /// Keeps served creatives inside the store listing's PEGI 3 / Everyone rating.
        /// Must run before <see cref="MobileAds.Initialize"/>.
        /// The under-13 flags stay Unspecified because the app is not directed at children.
        /// </summary>
        private static void ApplyRequestConfiguration()
        {
            try
            {
                var requestConfiguration = new RequestConfiguration
                {
                    MaxAdContentRating = MaxAdContentRating.G,
                    TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                    TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.Unspecified,
                };

                MobileAds.SetRequestConfiguration(requestConfiguration);
            }
            catch (Exception e)
            {
                TraceLogger.LogWarning(LogPrefix + "Could not apply ad request configuration: " + e.Message);
            }
        }

        private void Update()
        {
            if (!_sdkReady) return;

            float now = Time.unscaledTime;

            // Retry pump. Iterate a snapshot so Slot() lookups can't invalidate the loop.
            foreach (var unitId in new List<string>(_rewarded.Keys))
            {
                if (!_rewarded.TryGetValue(unitId, out var slot)) continue;
                if (slot.Ad != null || slot.Loading) continue;
                if (slot.Retry >= MaxRewardedRetries) continue;
                if (now < slot.RetryAt) continue;

                PreloadRewarded(unitId);
            }

            if (_interstitialAd == null && !_interstitialLoading &&
                _interstitialRetry < MaxInterstitialRetries && now >= _interstitialRetryAt)
            {
                PreloadInterstitial();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Rewarded

        private void PreloadRewarded(string unitId)
        {
            var slot = Slot(unitId);
            if (slot.Loading || slot.Ad != null) return;

            slot.Loading = true;

            try
            {
                RewardedAd.Load(unitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
                {
                    slot.Loading = false;

                    if (error != null || ad == null)
                    {
                        slot.Retry++;
                        slot.RetryAt = Time.unscaledTime + Mathf.Min(30f, 2f * slot.Retry);
                        TraceLogger.LogWarning(LogPrefix + $"Rewarded load failed ({unitId}, attempt {slot.Retry}): " +
                                               (error != null ? error.GetMessage() : "null ad"));
                        return;
                    }

                    slot.Retry = 0;
                    slot.Ad = ad;
                    RegisterRewardedEvents(slot, ad);
                    TraceLogger.Log(LogPrefix + "Rewarded ad ready: " + unitId);
                });
            }
            catch (Exception e)
            {
                slot.Loading = false;
                TraceLogger.LogWarning(LogPrefix + "Rewarded load threw: " + e.Message);
            }
        }

        private void RegisterRewardedEvents(RewardedSlot slot, RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                slot.Ad = null;
                slot.Retry = 0;
                PreloadRewarded(slot.UnitId);

                // The video is over — only now do we know whether the reward was earned.
                Action earned = slot.EarnedThisShow ? slot.OnEarned : null;
                slot.OnEarned = null;
                slot.OnUnavailable = null;
                slot.EarnedThisShow = false;

                if (earned != null) earned();
                else TraceLogger.Log(LogPrefix + "Rewarded closed without earning a reward: " + slot.UnitId);
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                slot.Ad = null;
                TraceLogger.LogWarning(LogPrefix + "Rewarded show failed: " + error.GetMessage());
                PreloadRewarded(slot.UnitId);

                Action unavailable = slot.OnUnavailable;
                slot.OnEarned = null;
                slot.OnUnavailable = null;
                slot.EarnedThisShow = false;

                if (unavailable != null) unavailable();
            };
        }

        /// <summary>
        /// Shows a rewarded video.
        /// <paramref name="onRewarded"/> fires only when the user actually earned the reward.
        /// <paramref name="onUnavailable"/> fires when the ad could not be shown at all.
        /// </summary>
        public void ShowRewarded(Action onRewarded, Action onUnavailable, string unitId = null)
        {
            unitId ??= AdsConfig.RewardedExtraLifeUnitId;
            var slot = Slot(unitId);

            if (!slot.Ready)
            {
                TraceLogger.LogWarning(LogPrefix + "Rewarded not ready; requesting a fresh load: " + unitId);
                PreloadRewarded(unitId);
                onUnavailable?.Invoke();
                return;
            }

            slot.OnEarned = onRewarded;
            slot.OnUnavailable = onUnavailable;
            slot.EarnedThisShow = false;

            try
            {
                // The closed/failed handlers registered at load time resolve the outcome.
                slot.Ad.Show(_ =>
                {
                    slot.EarnedThisShow = true;
                    TraceLogger.Log(LogPrefix + "User earned reward: " + unitId);
                });
            }
            catch (Exception e)
            {
                TraceLogger.LogWarning(LogPrefix + "Rewarded show threw: " + e.Message);
                slot.OnEarned = null;
                slot.OnUnavailable = null;
                onUnavailable?.Invoke();
            }
        }

        #endregion

        #region Interstitial

        private void PreloadInterstitial()
        {
            if (_interstitialLoading) return;
            _interstitialLoading = true;

            try
            {
                InterstitialAd.Load(AdsConfig.InterstitialUnitId, new AdRequest(),
                    (InterstitialAd ad, LoadAdError error) =>
                    {
                        _interstitialLoading = false;

                        if (error != null || ad == null)
                        {
                            _interstitialRetry++;
                            _interstitialRetryAt = Time.unscaledTime +
                                                   Mathf.Min(60f, 3f * _interstitialRetry);
                            TraceLogger.LogWarning(LogPrefix +
                                $"Interstitial load failed (attempt {_interstitialRetry}): " +
                                (error != null ? error.GetMessage() : "null ad"));
                            return;
                        }

                        _interstitialRetry = 0;
                        _interstitialAd = ad;

                        ad.OnAdFullScreenContentClosed += () =>
                        {
                            _interstitialAd = null;
                            PreloadInterstitial();
                        };
                        ad.OnAdFullScreenContentFailed += (AdError err) =>
                        {
                            _interstitialAd = null;
                            TraceLogger.LogWarning(LogPrefix + "Interstitial show failed: " + err.GetMessage());
                            PreloadInterstitial();
                        };
                    });
            }
            catch (Exception e)
            {
                _interstitialLoading = false;
                TraceLogger.LogWarning(LogPrefix + "Interstitial load threw: " + e.Message);
            }
        }

        /// <summary>
        /// Call when a level is completed. Shows an interstitial once every
        /// <see cref="AdsConfig.InterstitialEveryNLevels"/> levels, subject to a cooldown.
        /// Returns true if an ad was shown.
        /// </summary>
        public bool NotifyLevelCompleted()
        {
            _completedLevelsSinceInterstitial++;

            if (_completedLevelsSinceInterstitial < AdsConfig.InterstitialEveryNLevels) return false;
            if (Time.unscaledTime - _lastInterstitialTime < AdsConfig.InterstitialCooldownSeconds) return false;

            if (!InterstitialReady)
            {
                PreloadInterstitial();
                return false;
            }

            try
            {
                _interstitialAd.Show();
                _interstitialAd = null;
                _lastInterstitialTime = Time.unscaledTime;
                _completedLevelsSinceInterstitial = 0;
                PreloadInterstitial();
                return true;
            }
            catch (Exception e)
            {
                TraceLogger.LogWarning(LogPrefix + "Interstitial show threw: " + e.Message);
                return false;
            }
        }

        #endregion

        #region Level state

        /// <summary>Resets per-level ad state. Call when a level starts.</summary>
        public void ResetLevelState()
        {
            _extraLifeOffersThisLevel = 0;
            DoubleCoinsUnlocked = false;
        }

        /// <summary>Marks that the extra-life offer has been used up for this level.</summary>
        public void ConsumeExtraLifeOffer()
        {
            _extraLifeOffersThisLevel++;
        }

        /// <summary>Unlocks double coins after a successful rewarded view.</summary>
        public void UnlockDoubleCoins()
        {
            DoubleCoinsUnlocked = true;
        }

        #endregion
    }
}
