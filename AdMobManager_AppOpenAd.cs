using GoogleMobileAds.Api;
using Omnilatent.AdMob;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    //App Open Ad event
    public Action<AdPlacement.Type, AdValue> onAOAdPaidEvent;
    public Action<AdPlacement.Type, AdError> onAOAdFailedToPresentFullScreenContent;
    public Action<AdPlacement.Type> onAOAdDidPresentFullScreenContent;
    public Action<AdPlacement.Type> onAOAdDidDismissFullScreenContent;
    public Action<AdPlacement.Type> onAOAdDidRecordImpression;

    AppOpenAd appOpenAd;
    AdsManager.InterstitialDelegate onAppOpenAdClosed;
    AdPlacement.Type currentAppOpenAdPlacement;

    public void RequestAppOpenAd(AdPlacement.Type adID, RewardDelegate onAdLoaded)
    {
        CacheAdmobAd.PreloadAd<AppOpenAd>(adID, onAdLoaded);
    }

    /*public void ShowAppOpenAd(AdPlacement.Type adID, AdsManager.InterstitialDelegate onAdClosed = null)
    {
        onAppOpenAdClosed = onAdClosed;
        ShowAppOpenAd();
    }*/

    public void ShowAppOpenAd(AdPlacement.Type adID, AdsManager.InterstitialDelegate onAdClosed = null)
    {
        AppOpenAd appOpenAdReady;
        CacheAdmobAd.AdStatus cacheAdState = CacheAdmobAd.GetReadyAd<AppOpenAd>(adID, out appOpenAdReady, true);

        if (appOpenAdReady == null || showingAds)
        {
            onAdClosed?.Invoke(false);
            return;
        }

        if (appOpenAdReady != null)
        {
            appOpenAdReady.OnAdFullScreenContentClosed += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
                    appOpenAdReady = null;
                    showingAds = false;
                    onAdClosed?.Invoke(true);
                    onAOAdDidDismissFullScreenContent?.Invoke(adID);
                });
            };
            appOpenAdReady.OnAdFullScreenContentFailed += (args) =>
            {
                QueueMainThreadExecution(() =>
                {
                    Debug.LogFormat("Failed to present the ad (reason: {0})", args.GetMessage());
                    appOpenAdReady = null;
                    showingAds = false;
                    onAdClosed?.Invoke(false);
                    onAdClosed = null;
                    onAOAdFailedToPresentFullScreenContent?.Invoke(adID, args);
                });
            };
            appOpenAdReady.OnAdFullScreenContentOpened += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    showingAds = true;
                    onAOAdDidPresentFullScreenContent?.Invoke(adID);
                });
            };
            appOpenAdReady.OnAdPaid += (args) =>
            {
                QueueMainThreadExecution(() =>
                {
                    onAOAdPaidEvent?.Invoke(adID, args);
                    Debug.LogFormat("Received paid event. (currency: {0}, value: {1}", args.CurrencyCode, args.Value);
                });
            };
            appOpenAdReady.OnAdImpressionRecorded += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    onAOAdDidRecordImpression?.Invoke(adID);
                });
            };

            float currentTimeScale = Time.timeScale;
            appOpenAdReady.Show();
#if UNITY_EDITOR
            Time.timeScale = currentTimeScale;
#endif
        }
    }
}