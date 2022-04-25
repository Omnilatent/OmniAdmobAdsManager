using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using System;
using Omnilatent.AdMob;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    public delegate void InterstitialDelegate(bool isSuccess = false);
    public AdsManager.InterstitialDelegate interstitialFinishDelegate;
    public AdsManager.InterstitialDelegate interstitialLoadedDelegate;

    AdObject currentInterstitialAdObj = new AdObject();
    AdObject loadingInterstitialAdObj = new AdObject();

    public Action<AdPlacement.Type, EventArgs> onInterstitialLoaded;
    public Action<AdPlacement.Type, AdFailedToLoadEventArgs> onInterstitialFailedToLoad;
    public Action<AdPlacement.Type, EventArgs> onInterstitialOpening;
    public Action<AdPlacement.Type, EventArgs> onInterstitialClosed;
    public Action<AdPlacement.Type, AdErrorEventArgs> onInterstitialFailedToShow;
    public Action<AdPlacement.Type, EventArgs> onInterstitialImpression;
    public Action<AdPlacement.Type, AdValueEventArgs> onInterstitialPaidEvent;

    bool lastInterstitialRequestIsFailed = false;

    //IAdsNetworkHelper function
    public void RequestInterstitialNoShow(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
    {
        string id = CustomMediation.GetAdmobID(placementId);
        loadingInterstitialAdObj.placementType = placementId;
        RequestAdmobInterstitialNoShow(id, onAdLoaded, showLoading);
    }

    //IAdsNetworkHelper function
    public void ShowInterstitial(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdClosed)
    {
        currentInterstitialAdObj.placementType = placementId;
        ShowInterstitial(true, onAdClosed, cacheInterstitial, placementId.ToString());
    }

    /// <param name="onAdLoaded">Function to call after the ads is loaded</param>
    public void RequestAdmobInterstitialNoShow(string newInterstitialId, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
    {
        if (noAds != null && noAds())
        {
            onAdLoaded();
            return;
        }

        if (onAdLoaded != null)
        {
            //if a callback is required, assign callback and start a timeout coroutine
            interstitialLoadedDelegate = onAdLoaded;
            if (showLoading)
                //Manager.LoadingAnimation(true);
                coTimeoutLoad = StartCoroutine(CoTimeoutLoadInterstitial());
        }
#if UNITY_EDITOR
        OnInterstitialLoaded(false);
        //Manager.LoadingAnimation(false);
#endif

        if (this.interstitial != null)
        {
            if (!this.interstitial.IsLoaded())
            {
                //An ad exist but has not loaded yet, destroy it
                Debug.Log("Previous Interstitial load is not finished");
                this.interstitial.OnAdClosed -= HandleInterstitialClosed;
                this.interstitial.Destroy();
                this.interstitial = null;
            }
            else
            {
                //An ad exist and is loaded, return load success immediately
                //.Log("Cached Ads loaded success, showing");
                HandleInterstitialLoadedNoShow(null, null); //if a previous interstitial was loaded but not shown, show that interstitial
                return;
            }
        }

        if (this.interstitial == null)
        {
            //No ad exist, load new ad and assign callback to it
            this.interstitial = new InterstitialAd(newInterstitialId);
            this.interstitial.LoadAd(this.CreateAdRequest());
            this.interstitial.OnAdClosed += HandleInterstitialClosed;
            this.interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoadNoShow;
            this.interstitial.OnAdLoaded += HandleInterstitialLoadedNoShow;
            this.interstitial.OnAdDidRecordImpression += HandleInterstitialImpression;
            this.interstitial.OnPaidEvent += HandleInterstitialPaidEvent;
            this.interstitial.OnAdFailedToShow += HandleInterstitialFailedToShow;
            this.interstitial.OnAdOpening += HandleInterstitialOpening;

            lastInterstitialRequestIsFailed = false;
            //("added listener failed load");
        }
    }

    /// <param name="cacheNextInter">Cache next interstitial ads</param>
    /// <param name="logOriginName">For tracking where this interstitial came from</param>
    public void ShowInterstitial(bool showLoading = true, AdsManager.InterstitialDelegate onAdClosed = null, bool cacheNextInter = false, string logOriginName = "")
    {
        if (onAdClosed != null)
        {
            interstitialFinishDelegate = onAdClosed;
        }

        if (noAds != null && noAds())
        {
            OnInterstitialFinish(false);
            return;
        }

        if (this.interstitial != null && this.interstitial.IsLoaded())
        {
            //An ad is loaded and ready to show
            //cacheInterstitial = cacheNextInter;
            LogEvent("InterstitialShow_" + logOriginName);
            this.showingAds = true;
            this.interstitialTime = this.time;
            this.interstitial.Show();
#if UNITY_EDITOR
            OnInterstitialFinish(true);
#endif
            //("show inter success");
            return;
        }

        if (lastInterstitialRequestIsFailed)
        {
            //No ad is available
            OnInterstitialFinish(false);
        }
        else
        {
            //Old logic, this part shouldn't execute
            RequestInterstitial();
            this.interstitial.OnAdLoaded += HandleInterstitialLoaded;
            //this.interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoad;
            //("added listener load");
        }
    }
}
