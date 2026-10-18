namespace _Game.Ads
{
    /// <summary>
    /// All AdMob identifiers in one place.
    ///
    /// These are the live values from the AdMob console. The App ID here must always match
    /// the one in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset — the build
    /// reads the App ID from that asset and writes it into AndroidManifest.xml, and the SDK
    /// will refuse to start if the two disagree.
    /// </summary>
    public static class AdsConfig
    {
        // ---------------------------------------------------------------------
        // AdMob identifiers
        // ---------------------------------------------------------------------

        /// <summary>AdMob App ID for the Arrows Android app.</summary>
        public const string AndroidAppId = "ca-app-pub-7129470803481646~1513994194";

        /// <summary>
        /// Rewarded unit shown when the player runs out of lives.
        ///
        /// NOTE: you created two rewarded units and the screenshots don't show which is
        /// which. If you labelled them the other way round in the AdMob console, swap the
        /// two values below — the game behaves identically either way, this only affects
        /// which unit's reporting each placement lands in.
        /// </summary>
        public const string RewardedExtraLifeUnitId = "ca-app-pub-7129470803481646/7406194139";

        /// <summary>Rewarded unit shown on the win panel to double the coins.</summary>
        public const string RewardedDoubleCoinsUnitId = "ca-app-pub-7129470803481646/5180767308";

        /// <summary>Interstitial unit shown between levels.</summary>
        public const string InterstitialUnitId = "ca-app-pub-7129470803481646/3163696791";

        // ---------------------------------------------------------------------
        // Behaviour tuning
        // ---------------------------------------------------------------------

        /// <summary>
        /// Show an interstitial after this many completed levels.
        /// 2 means levels 1, 3, 5 ... are followed by an ad. Never use 1 — it doubles the
        /// friction, especially across a 500-level progression.
        /// </summary>
        public const int InterstitialEveryNLevels = 2;

        /// <summary>Minimum seconds between two interstitials.</summary>
        public const float InterstitialCooldownSeconds = 60f;

        /// <summary>Offer "+1 life" at most this many times per level attempt.</summary>
        public const int MaxExtraLifeOffersPerLevel = 1;

        /// <summary>Multiplier applied to the win reward when the double-coins ad is watched.</summary>
        public const int DoubleCoinsMultiplier = 2;
    }
}
