using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    //App Open Ad event
    public Action<AdPlacement.Type, AdValueEventArgs> onAOAdPaidEvent;
    public Action<AdPlacement.Type, AdErrorEventArgs> onAOAdFailedToPresentFullScreenContent;
    public Action<AdPlacement.Type, EventArgs> onAOAdDidPresentFullScreenContent;
    public Action<AdPlacement.Type, EventArgs> onAOAdDidDismissFullScreenContent;
    public Action<AdPlacement.Type, EventArgs> onAOAdDidRecordImpression;

    AppOpenAd appOpenAd;
    AdsManager.InterstitialDelegate onAppOpenAdClosed;
    AdPlacement.Type currentAppOpenAdPlacement;

    public void RequestAppOpenAd(AdPlacement.Type adID, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        string id = CustomMediation.GetAdmobID(adID);
        RequestAppOpenAd(id, onAdLoaded);
        currentAppOpenAdPlacement = adID;
    }

    public void RequestAppOpenAd(string adID, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        AdRequest request = new AdRequest.Builder().Build();

        // Load an app open ad for portrait orientation
        AppOpenAd.LoadAd(adID, Screen.orientation, request, ((appOpenAd, error) =>
        {
            if (error != null)
            {
                // Handle the error.
                Debug.LogFormat("Failed to load the ad. (reason: {0})", error.LoadAdError.GetMessage());
                onAdLoaded?.Invoke(false);
                AdsManager.LogError($"[{adID}]load failed.{error.LoadAdError.GetMessage()}", adID);
                return;
            }

            // App open ad is loaded.
            this.appOpenAd = appOpenAd;
            onAdLoaded?.Invoke(true);
        }));
    }

    public void ShowAppOpenAd(AdPlacement.Type adID, AdsManager.InterstitialDelegate onAdClosed = null)
    {
        onAppOpenAdClosed = onAdClosed;
        ShowAppOpenAd();
    }

    public void ShowAppOpenAd()
    {
        if (appOpenAd == null || showingAds)
        {
            onAppOpenAdClosed?.Invoke(false);
            onAppOpenAdClosed = null;
            return;
        }

        appOpenAd.OnAdDidDismissFullScreenContent += HandleAdDidDismissFullScreenContent;
        appOpenAd.OnAdFailedToPresentFullScreenContent += HandleAdFailedToPresentFullScreenContent;
        appOpenAd.OnAdDidPresentFullScreenContent += HandleAdDidPresentFullScreenContent;
        appOpenAd.OnPaidEvent += HandleOpenAdPaidEvent;
        appOpenAd.OnAdDidRecordImpression += HandleOpenAdDidRecordImpression;

        appOpenAd.Show();
    }

    private void HandleOpenAdPaidEvent(object sender, AdValueEventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onAOAdPaidEvent?.Invoke(currentAppOpenAdPlacement, args);
            Debug.LogFormat("Received paid event. (currency: {0}, value: {1}",
                args.AdValue.CurrencyCode, args.AdValue.Value);
        });
    }

    private void HandleAdDidDismissFullScreenContent(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
            appOpenAd = null;
            showingAds = false;
            onAppOpenAdClosed?.Invoke(true);
            onAppOpenAdClosed = null;
            //LoadOpenAd();
            onAOAdDidDismissFullScreenContent?.Invoke(currentAppOpenAdPlacement, args);
        });
    }

    private void HandleAdFailedToPresentFullScreenContent(object sender, AdErrorEventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            Debug.LogFormat("Failed to present the ad (reason: {0})", args.AdError.GetMessage());
            appOpenAd = null;
            showingAds = false;
            onAppOpenAdClosed?.Invoke(false);
            onAppOpenAdClosed = null;
            //LoadOpenAd();
            onAOAdFailedToPresentFullScreenContent?.Invoke(currentAppOpenAdPlacement, args);
        });
    }

    private void HandleAdDidPresentFullScreenContent(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            showingAds = true;
            onAOAdDidPresentFullScreenContent?.Invoke(currentAppOpenAdPlacement, args);
        });
    }

    private void HandleOpenAdDidRecordImpression(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onAOAdDidRecordImpression?.Invoke(currentAppOpenAdPlacement, args);
        });
    }
}
