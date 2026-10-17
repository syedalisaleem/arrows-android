namespace _Game.Ads
{
    /// <summary>
    /// All AdMob identifiers in one place.
    ///
    /// ⚠️ THESE ARE GOOGLE'S OFFICIAL TEST IDS. They serve real, clickable test ads and are
    /// safe for development. Replace every value below with your own AdMob IDs before you
    /// publish, then rebuild the AAB.
    ///
    ///   AdMob console → Apps → your app → Ad units
    ///   App ID  looks like  ca-app-pub-################~##########
    ///   Unit ID looks like  ca-app-pub-################/##########
    ///
    /// The App ID is also written into GoogleMobileAdsSettings.asset; keep the two in sync.
    /// </summary>
    public static class AdsConfig
    {
        // ---------------------------------------------------------------------
        // TODO: replace with your real AdMob IDs
        // ---------------------------------------------------------------------

        /// <summary>AdMob App ID (used for SDK initialisation / manifest meta-data).</summary>
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";

        /// <summary>
        /// Rewarded video unit. Shared by both rewarded placements ("+1 life" and
        /// "double coins"). If you want to compare their performance, create two units in the
        /// AdMob console and give each its own constant here.
        /// </summary>
        public const string RewardedUnitId = "ca-app-pub-3940256099942544/5224354917";

        /// <summary>Interstitial unit — shown between levels.</summary>
        public const string InterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";

        // ---------------------------------------------------------------------
        // Behaviour tuning
        // ---------------------------------------------------------------------

        /// <summary>
        /// Show an interstitial after this many completed levels.
        /// 2 means: levels 1, 3, 5 ... are followed by an ad. Never use 1 — it doubles the
        /// friction of a game that only has 10 levels.
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
