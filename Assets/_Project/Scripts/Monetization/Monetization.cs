using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    // ------------------------------------------------------------------------------------------
    // Monetization layer. Game code only ever talks to these interfaces, never to an ad or IAP SDK.
    // Right now the Null implementations are wired in so the game flow can be built and tested.
    // Later you add one class per SDK (for example LevelPlayAdService, UnityIapService) that
    // implements the same interface and swap it in Monetization.Init().
    //
    // Rules baked in (matches the brief: no pay-to-win):
    //  - Ads never appear while driving or racing. Interstitials only on the results screen.
    //  - Rewarded ads are always optional: double race reward, free repair, one revive.
    //  - IAP sells only: Remove Ads, cosmetic packs, and a supporter pack. Nothing that changes handling.
    // ------------------------------------------------------------------------------------------

    public enum AdPlacement
    {
        ResultsInterstitial,
        RewardedDoubleReward,
        RewardedFreeRepair,
        RewardedRevive
    }

    public static class ProductIds
    {
        public const string RemoveAds = "ml_remove_ads";
        public const string SupporterPack = "ml_supporter_pack";
        public const string PaintPack = "ml_cosmetic_paint_pack";
        public const string WheelPack = "ml_cosmetic_wheel_pack";
        public const string DecalPack = "ml_cosmetic_decal_pack";

        public static readonly string[] All =
        {
            RemoveAds, SupporterPack, PaintPack, WheelPack, DecalPack
        };
    }

    public interface IAdService
    {
        bool IsInitialized { get; }
        void Initialize();
        /// <summary>Call before showing any ad. false = user has not consented to personalised ads (or region requires a consent form first).</summary>
        void SetPersonalisedAdsAllowed(bool allowed);
        bool IsRewardedReady(AdPlacement placement);
        void ShowRewarded(AdPlacement placement, Action<bool> onFinished);
        bool IsInterstitialReady();
        void ShowInterstitial(Action onClosed);
    }

    public interface IPurchaseService
    {
        bool IsInitialized { get; }
        void Initialize();
        bool IsOwned(string productId);
        string LocalisedPrice(string productId);
        void Purchase(string productId, Action<bool> onFinished);
        void RestorePurchases(Action<bool> onFinished);
    }

    /// <summary>Stand-in used until a real SDK is added. In the Editor and development builds ads "succeed" instantly.</summary>
    public sealed class NullAdService : IAdService
    {
        public bool IsInitialized { get; private set; }
        bool personalised;

        bool CanSimulate { get { return Application.isEditor || Debug.isDebugBuild; } }

        public void Initialize() { IsInitialized = true; }
        public void SetPersonalisedAdsAllowed(bool allowed) { personalised = allowed; }
        public bool IsRewardedReady(AdPlacement placement) { return IsInitialized && CanSimulate; }
        public bool IsInterstitialReady() { return IsInitialized && CanSimulate; }

        public void ShowRewarded(AdPlacement placement, Action<bool> onFinished)
        {
            Debug.Log("[Ads] (stub) rewarded " + placement + " personalised=" + personalised);
            if (onFinished != null) onFinished(CanSimulate);
        }

        public void ShowInterstitial(Action onClosed)
        {
            Debug.Log("[Ads] (stub) interstitial");
            if (onClosed != null) onClosed();
        }
    }

    /// <summary>Stand-in purchase service. Ownership is kept in PlayerPrefs so Remove Ads can be tested.</summary>
    public sealed class NullPurchaseService : IPurchaseService
    {
        public bool IsInitialized { get; private set; }
        readonly HashSet<string> owned = new HashSet<string>();

        public void Initialize()
        {
            for (int i = 0; i < ProductIds.All.Length; i++)
            {
                if (PlayerPrefs.GetInt("ML_IAP_" + ProductIds.All[i], 0) == 1) owned.Add(ProductIds.All[i]);
            }
            IsInitialized = true;
        }

        public bool IsOwned(string productId) { return owned.Contains(productId); }
        public string LocalisedPrice(string productId) { return "--"; }

        public void Purchase(string productId, Action<bool> onFinished)
        {
            bool ok = Application.isEditor || Debug.isDebugBuild;
            Debug.Log("[IAP] (stub) purchase " + productId + " -> " + ok);
            if (ok)
            {
                owned.Add(productId);
                PlayerPrefs.SetInt("ML_IAP_" + productId, 1);
            }
            if (onFinished != null) onFinished(ok);
        }

        public void RestorePurchases(Action<bool> onFinished)
        {
            if (onFinished != null) onFinished(true);
        }
    }

    public static class Monetization
    {
        public static IAdService Ads { get; private set; }
        public static IPurchaseService Iap { get; private set; }

        public static void Init(IAdService ads = null, IPurchaseService iap = null)
        {
            Iap = iap ?? new NullPurchaseService();
            Ads = ads ?? new NullAdService();
            Iap.Initialize();
            Ads.Initialize();
            GameServices.Register(Ads);
            GameServices.Register(Iap);
        }
    }

    /// <summary>When an interstitial is allowed. Call CanShowInterstitial() on the results screen only.</summary>
    public static class AdPolicy
    {
        public const float MinSecondsBetweenInterstitials = 180f;
        public const int MinRacesBetweenInterstitials = 2;

        static float lastShownTime = -9999f;
        static int racesSinceLast = 0;

        public static bool AdsRemoved
        {
            get { return Monetization.Iap != null && Monetization.Iap.IsOwned(ProductIds.RemoveAds); }
        }

        public static void RegisterRaceFinished() { racesSinceLast++; }

        public static bool CanShowInterstitial()
        {
            if (AdsRemoved) return false;
            if (Monetization.Ads == null || !Monetization.Ads.IsInterstitialReady()) return false;
            if (racesSinceLast < MinRacesBetweenInterstitials) return false;
            return Time.realtimeSinceStartup - lastShownTime >= MinSecondsBetweenInterstitials;
        }

        public static void MarkInterstitialShown()
        {
            lastShownTime = Time.realtimeSinceStartup;
            racesSinceLast = 0;
        }

        /// <summary>Rewarded ads are always allowed (even with Remove Ads owned, they are a player choice). Remove Ads can also grant the reward for free if you prefer.</summary>
        public static bool CanOfferRewarded(AdPlacement placement)
        {
            return Monetization.Ads != null && Monetization.Ads.IsRewardedReady(placement);
        }
    }
}
