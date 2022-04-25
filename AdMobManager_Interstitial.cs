using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using System;
using Omnilatent.AdMob;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    public delegate void InterstitialDelegate(bool isSuccess = false);

    /// <summary>
    /// Act as intermediate to invoke onAdLoaded
    /// </summary>
    public AdsManager.InterstitialDelegate interstitialLoadedDelegate;
    public AdsManager.InterstitialDelegate interstitialFinishDelegate;

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
                DestroyInterstitial();
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
            this.interstitial.OnAdFailedToShow += HandleInterstitialFailedToShow;

            //optional callback
            this.interstitial.OnAdDidRecordImpression += HandleInterstitialImpression;
            this.interstitial.OnPaidEvent += HandleInterstitialPaidEvent;
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

    public void DestroyInterstitial()
    {
        if (this.interstitial != null)
        {
            this.interstitial.OnAdClosed -= HandleInterstitialClosed;
            this.interstitial.Destroy();
            this.interstitial = null;
        }
    }

    #region Callbacks

    void HandleInterstitialFailedToLoadNoShow(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoadNoShow;
            //Manager.LoadingAnimation(false); //let main AdsManager handle this

            OnInterstitialLoaded();

            lastInterstitialRequestIsFailed = true;
            ShowError(args);
        });
    }

    void HandleInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);
            onInterstitialFailedToLoad?.Invoke(loadingInterstitialAdObj.placementType, args);
            OnInterstitialFinish(false);

            lastInterstitialRequestIsFailed = true;
            ShowError(args);
        });
    }

    void HandleInterstitialClosed(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            this.showingAds = false;
            DestroyInterstitial();
            OnInterstitialFinish(true);
            onInterstitialClosed?.Invoke(currentInterstitialAdObj.placementType, args);

            /*if (Application.platform == RuntimePlatform.Android && cacheInterstitial)
            {
                RequestInterstitial();
            }*/
        });
    }

    void HandleInterstitialLoadedNoShow(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdLoaded -= HandleInterstitialLoadedNoShow;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);
            onInterstitialLoaded?.Invoke(loadingInterstitialAdObj.placementType, args);
            OnInterstitialLoaded(true);
        });
    }

    void OnInterstitialLoaded(bool isSuccess = false)
    {
        if (interstitialLoadedDelegate != null)
        {
            interstitialLoadedDelegate(isSuccess);
            interstitialLoadedDelegate = null;
        }
        if (coTimeoutLoad != null)
        {
            StopCoroutine(coTimeoutLoad);
            coTimeoutLoad = null;
        }
    }

    /// <summary>
    /// Invoke interstitialFinishDelegate and set it to null
    /// </summary>
    void OnInterstitialFinish(bool isSuccess = false)
    {
        if (interstitialFinishDelegate != null)
        {
            this.interstitialFinishDelegate(isSuccess);
            this.interstitialFinishDelegate = null;
        }
    }

    void HandleInterstitialImpression(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onInterstitialImpression?.Invoke(currentInterstitialAdObj.placementType, args);
        });
    }
    void HandleInterstitialPaidEvent(object sender, AdValueEventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onInterstitialPaidEvent?.Invoke(currentInterstitialAdObj.placementType, args);
        });
    }

    void HandleInterstitialFailedToShow(object sender, AdErrorEventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onInterstitialFailedToShow?.Invoke(currentInterstitialAdObj.placementType, args);
        });
    }

    void HandleInterstitialOpening(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            onInterstitialOpening?.Invoke(currentInterstitialAdObj.placementType, args);
        });
    }
    #endregion
}
