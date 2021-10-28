using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    AppOpenAd appOpenAd;
    AdsManager.InterstitialDelegate onAppOpenAdClosed;

    public void RequestAppOpenAd(AdPlacement.Type adID, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        string id = CustomMediation.GetAdmobID(adID, AdMobConst.APP_OPEN_AD);
        RequestAppOpenAd(id, onAdLoaded);
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
            return;
        }

        appOpenAd.OnAdDidDismissFullScreenContent += HandleAdDidDismissFullScreenContent;
        appOpenAd.OnAdFailedToPresentFullScreenContent += HandleAdFailedToPresentFullScreenContent;
        appOpenAd.OnAdDidPresentFullScreenContent += HandleAdDidPresentFullScreenContent;
        appOpenAd.OnPaidEvent += HandleOpenAdPaidEvent;

        appOpenAd.Show();
    }

    private void HandleOpenAdPaidEvent(object sender, AdValueEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.LogFormat("Received paid event. (currency: {0}, value: {1}",
                e.AdValue.CurrencyCode, e.AdValue.Value);
        });
    }

    private void HandleAdDidDismissFullScreenContent(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
            appOpenAd = null;
            showingAds = false;
            onAppOpenAdClosed?.Invoke(true);
            onAppOpenAdClosed = null;
            //LoadOpenAd();
        });
    }

    private void HandleAdFailedToPresentFullScreenContent(object sender, AdErrorEventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.LogFormat("Failed to present the ad (reason: {0})", args.AdError.GetMessage());
            appOpenAd = null;
            showingAds = false;
            //LoadOpenAd();
        });
    }

    private void HandleAdDidPresentFullScreenContent(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            showingAds = true;
        });
    }
}
