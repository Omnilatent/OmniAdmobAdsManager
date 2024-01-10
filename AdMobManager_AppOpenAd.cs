using GoogleMobileAds.Api;
using Omnilatent.AdMob;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    //App Open Ad event
    public Action<AdPlacement.Type, AppOpenAd, AdValue> onAOAdPaidEvent;
    public Action<AdPlacement.Type, AppOpenAd, AdError> onAOAdFailedToPresentFullScreenContent;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdDidPresentFullScreenContent;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdBeforePresentFullScreenContent;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdDidDismissFullScreenContent;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdDidRecordImpression;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdUserClickEvent;
    public Action<AdPlacement.Type, AppOpenAd> onAOAdLoaded;
    public Action<AdPlacement.Type, AppOpenAd, AdError> onAOAdFailedToLoad;
    public Action<AdPlacement.Type> onAOAdRequested;

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
            onAOAdBeforePresentFullScreenContent?.Invoke(adID, appOpenAdReady);
            appOpenAdReady.OnAdFullScreenContentClosed += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
                    appOpenAdReady = null;
                    showingAds = false;
                    onAdClosed?.Invoke(true);
                    onAOAdDidDismissFullScreenContent?.Invoke(adID, appOpenAdReady);
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
                    onAOAdFailedToPresentFullScreenContent?.Invoke(adID, appOpenAdReady, args);
                });
            };
            appOpenAdReady.OnAdFullScreenContentOpened += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    showingAds = true;
                    onAOAdDidPresentFullScreenContent?.Invoke(adID, appOpenAdReady);
                });
            };
            appOpenAdReady.OnAdClicked += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    onAOAdUserClickEvent?.Invoke(adID, appOpenAdReady);
                });
            };
            appOpenAdReady.OnAdPaid += (args) =>
            {
                QueueMainThreadExecution(() =>
                {
                    onAOAdPaidEvent?.Invoke(adID, appOpenAdReady, args);
                    Debug.LogFormat("Received paid event. (currency: {0}, value: {1}", args.CurrencyCode, args.Value);
                });
            };
            appOpenAdReady.OnAdImpressionRecorded += () =>
            {
                QueueMainThreadExecution(() =>
                {
                    onAOAdDidRecordImpression?.Invoke(adID, appOpenAdReady);
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