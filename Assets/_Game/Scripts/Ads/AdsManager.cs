using System;
using GoogleMobileAds.Api;
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

        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private bool _rewardedLoading;
        private Action _onRewardEarned;
        private Action _onRewardUnavailable;
        private bool _rewardEarnedThisShow;
        private bool _interstitialLoading;
        private int _rewardedRetry;
        private int _interstitialRetry;
        private float _rewardedRetryAt;
        private float _interstitialRetryAt;
        private bool _sdkReady;
        private string _pendingRewardedUnitId;

        private int _completedLevelsSinceInterstitial;
        private float _lastInterstitialTime = -999f;
        private int _extraLifeOffersThisLevel;

        /// <summary>Set when the player earns the double-coins reward for the current win.</summary>
        public bool DoubleCoinsUnlocked { get; private set; }

        public bool SdkReady => _sdkReady;
        public bool RewardedReady => _rewardedAd != null && _rewardedAd.CanShowAd();
        public bool InterstitialReady => _interstitialAd != null && _interstitialAd.CanShowAd();
        public bool CanOfferExtraLife => _extraLifeOffersThisLevel < AdsConfig.MaxExtraLifeOffersPerLevel;

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

            MobileAds.Initialize(status =>
            {
                _sdkReady = true;
                TraceLogger.Log(LogPrefix + "SDK initialised.");
                PreloadRewarded(AdsConfig.RewardedUnitId);
                PreloadInterstitial();
            });
        }

        private void Update()
        {
            // Lightweight retry pump for failed loads.
            if (!_sdkReady) return;
            float now = Time.unscaledTime;

            if (_rewardedAd == null && !_rewardedLoading && _rewardedRetry < MaxRewardedRetries && now >= _rewardedRetryAt)
            {
                PreloadRewarded(_pendingRewardedUnitId ?? AdsConfig.RewardedUnitId);
            }

            if (_interstitialAd == null && !_interstitialLoading && _interstitialRetry < MaxInterstitialRetries && now >= _interstitialRetryAt)
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
            if (_rewardedLoading) return;
            _rewardedLoading = true;
            _pendingRewardedUnitId = unitId;

            try
            {
                RewardedAd.Load(unitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
                {
                    _rewardedLoading = false;

                    if (error != null || ad == null)
                    {
                        _rewardedRetry++;
                        _rewardedRetryAt = Time.unscaledTime + Mathf.Min(30f, 2f * _rewardedRetry);
                        TraceLogger.LogWarning(LogPrefix + $"Rewarded load failed ({_rewardedRetry}): " +
                                               (error != null ? error.GetMessage() : "null ad"));
                        return;
                    }

                    _rewardedRetry = 0;
                    _rewardedAd = ad;
                    RegisterRewardedEvents(ad);
                    TraceLogger.Log(LogPrefix + "Rewarded ad ready.");
                });
            }
            catch (Exception e)
            {
                _rewardedLoading = false;
                TraceLogger.LogWarning(LogPrefix + "Rewarded load threw: " + e.Message);
            }
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                _rewardedAd = null;
                _rewardedRetry = 0;
                PreloadRewarded(_pendingRewardedUnitId ?? AdsConfig.RewardedUnitId);

                // Decide the outcome now that the video has actually finished.
                Action earned = _rewardEarnedThisShow ? _onRewardEarned : null;
                _onRewardEarned = null;
                _onRewardUnavailable = null;
                _rewardEarnedThisShow = false;

                if (earned != null) earned();
                else TraceLogger.Log(LogPrefix + "Rewarded closed without earning a reward.");
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                _rewardedAd = null;
                TraceLogger.LogWarning(LogPrefix + "Rewarded show failed: " + error.GetMessage());
                PreloadRewarded(_pendingRewardedUnitId ?? AdsConfig.RewardedUnitId);

                Action unavailable = _onRewardUnavailable;
                _onRewardEarned = null;
                _onRewardUnavailable = null;
                _rewardEarnedThisShow = false;

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
            unitId ??= AdsConfig.RewardedUnitId;

            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                TraceLogger.LogWarning(LogPrefix + "Rewarded not ready; requesting a fresh load.");
                PreloadRewarded(unitId);
                onUnavailable?.Invoke();
                return;
            }

            _onRewardEarned = onRewarded;
            _onRewardUnavailable = onUnavailable;
            _rewardEarnedThisShow = false;

            try
            {
                // The closed/failed handlers registered at load time resolve the outcome.
                _rewardedAd.Show(_ =>
                {
                    _rewardEarnedThisShow = true;
                    TraceLogger.Log(LogPrefix + "Reward earned by user.");
                });
            }
            catch (Exception e)
            {
                TraceLogger.LogWarning(LogPrefix + "Rewarded show threw: " + e.Message);
                _onRewardEarned = null;
                _onRewardUnavailable = null;
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
                InterstitialAd.Load(AdsConfig.InterstitialUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
                {
                    _interstitialLoading = false;

                    if (error != null || ad == null)
                    {
                        _interstitialRetry++;
                        _interstitialRetryAt = Time.unscaledTime + Mathf.Min(60f, 3f * _interstitialRetry);
                        TraceLogger.LogWarning(LogPrefix + $"Interstitial load failed ({_interstitialRetry}): " +
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

            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
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

        /// <summary>Marks that the player earned (or declined) the extra-life offer.</summary>
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
