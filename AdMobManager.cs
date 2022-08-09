﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using UnityEngine.Networking;
using Omnilatent.AdMob;
using Omnilatent.AdsMediation;

/* CHANGE LOG:
 * 27/7/2020: Add timeout load to RequestInterstitialNoShow
 * 1/9/2020: Get ad ID from CustomMediation's function instead of using switch() to get directly from AdConst
 */

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    public const float TIME_BETWEEN_ADS = 10f;

    /// <summary>
    /// Time waited when loading an interstitial ad before forcing a timeout.
    /// </summary>
    public static float TIMEOUT_LOADAD = 12f;

    /// <summary>
    /// Time waited when loading an rewarded ad before forcing a timeout.
    /// </summary>
    public static float TIMEOUT_LOADREWARDAD = 12f;

    public static string appId;
    public static string bannerId;
    [Obsolete] public AdSize currentBannerSize = AdSize.Banner;

    public static string videoId;
    public static string interstitialId;

    public delegate bool NoAdsDelegate();
    public NoAdsDelegate noAds;
    [SerializeField] bool cacheInterstitial; //cache interstitial. Work with one single interstitial ad id

    Coroutine coTimeoutLoad;

    public delegate void BoolDelegate(bool reward);
    public RewardDelegate adsVideoRewardedCallback; //For traditional Rewarded Video

    [Obsolete] public AdsManager.InterstitialDelegate bannerLoadedDelegate;
    public Action<AdPlacement.Type, AdFailedToLoadEventArgs> onBannerFailedToLoad;
    public Action<AdPlacement.Type, AdValueEventArgs> onBannerPaidEvent;


    private static AdMobManager _instance;

    public static AdMobManager instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject gO = Resources.Load<GameObject>("AdsManager");
                _instance = Instantiate(gO).GetComponent<AdMobManager>();
            }
            return _instance;
        }
    }

    //[SerializeField] bool m_ShowBannerOnStart = true;

    private BannerView bannerView;
    private AdmobBannerAdObject currentBannerAd;

    private InterstitialAd interstitial;

    private RewardResult rewardResult;

    [Obsolete]
    public bool isShowBanner
    {
        get;
        protected set;
    }

    public float interstitialTime
    {
        get;
        protected set;
    }

    public float time
    {
        get;
        protected set;
    }

    public bool showingAds
    {
        get;
        protected set;
    }

    #region Static

    /*public static void InterstitialNextScene(string nextSceneName, object data, string newInterstitialId, InterstitialSceneData.InterType interType = InterstitialSceneData.InterType.requestAndShow)
    {
        //AdsManager.instance.HideBanner();
        InterstitialSceneData interstitialSceneData = new InterstitialSceneData(nextSceneName, data,
                newInterstitialId, interType);
        Manager.Load(InterstitialDummyController.INTERSTITIALDUMMY_SCENE_NAME, interstitialSceneData);
    }*/
    #endregion

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            AdMobManager.appId = AdMobConst.ADMOB_APP_ID;
            AdMobManager.bannerId = AdMobConst.BANNER_ID;
            AdMobManager.interstitialId = AdMobConst.INTERSTITIAL;
            AdMobManager.videoId = AdMobConst.REWARD_ID;

            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        //MobileAds.Initialize(appId);
        MobileAds.Initialize((InitializationStatus status) => { Debug.Log($"Admob Init: {status}"); });

        /*this.rewardBasedVideo = RewardBasedVideoAd.Instance;
        this.rewardBasedVideo.OnAdClosed += HandleRewardedAdClosed;
        this.rewardBasedVideo.OnAdCompleted += HandleVideoCompleted;
        this.rewardBasedVideo.OnAdRewarded += HandleUserEarnedReward;*/

        //if (Application.platform == RuntimePlatform.Android)
        //{
        //    //this.RequestInterstitial();
        //    this.RequestRewardBasedVideo(videoId);
        //}

        //noAds += AdsManager.HasNoInternet;

        if (UnityMainThreadDispatcher.Instance() == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            go.AddComponent<UnityMainThreadDispatcher>();
        }
        //Debug.Log("OS: " + Application.platform + ". RAM: " + SystemInfo.systemMemorySize);
    }

    /*private void Update()
    {
        if (!showingAds)
        {
            time += Time.deltaTime;
        }
    }*/

    // Returns an ad request with custom ad targeting.
    private AdRequest CreateAdRequest()
    {
        return new AdRequest.Builder().Build();
    }

    #region Banner
    public void RequestBanner(AdPlacement.Type placementType, AdSize adSize, GoogleMobileAds.Api.AdPosition adPosition, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        string placementId = CustomMediation.GetAdmobID(placementType);
        if (this.currentBannerAd == null)
        {
            AdMobManager.bannerId = placementId;
            // Create a smart banner at the bottom of the screen.
            currentBannerAd = new AdmobBannerAdObject(placementType, onAdLoaded);
            currentBannerAd.BannerView = new BannerView(placementId, adSize, adPosition);

            // Load a banner ad.
            currentBannerAd.BannerView.OnAdFailedToLoad += OnBannerAdsFailedToLoad;
            currentBannerAd.BannerView.OnAdLoaded += OnBannerAdsLoaded;
            currentBannerAd.BannerView.OnPaidEvent += OnBannerPaidEvent;
            currentBannerAd.State = AdObjectState.Loading;
            currentBannerAd.BannerView.LoadAd(this.CreateAdRequest());
        }
    }

    void OnBannerAdsFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        ShowError(args);
        GetCurrentBannerAdObject().onAdLoaded?.Invoke(false);
        onBannerFailedToLoad?.Invoke(GetCurrentBannerAdObject().AdPlacementType, args);
        DestroyBanner();
    }

    void OnBannerAdsLoaded(object sender, EventArgs args)
    {
        if (this.currentBannerAd != null && currentBannerAd.State != AdObjectState.Closed)
        {
            currentBannerAd.BannerView.Show();
            currentBannerAd.State = AdObjectState.Showing;
        }
        GetCurrentBannerAdObject().onAdLoaded?.Invoke(true);
    }

    void OnBannerPaidEvent(object sender, AdValueEventArgs args)
    {
        QueueMainThreadExecution(() => { onBannerPaidEvent?.Invoke(GetCurrentBannerAdObject().AdPlacementType, args); });
    }

    public void DestroyBanner()
    {
        if (this.currentBannerAd != null)
        {
            currentBannerAd.BannerView.OnAdFailedToLoad -= OnBannerAdsFailedToLoad;
            currentBannerAd.BannerView.Destroy();
            currentBannerAd.BannerView = null;
            currentBannerAd = null;
        }
    }

    [Obsolete("Use ShowBanner(AdPlacement.Type) instead", true)]
    public void ShowBanner(string placementId, AdSize adSize, GoogleMobileAds.Api.AdPosition adPosition, float delay = 0f, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        if (noAds != null && noAds())
        {
            onAdLoaded?.Invoke(false);
            return;
        }
        bannerLoadedDelegate = onAdLoaded;
        if (adSize == null)
        {
            Debug.Log("Admob Banner No AdSize parameter");
            adSize = AdSize.Banner;
        }
        if (this.bannerView != null && AdMobManager.bannerId == placementId && currentBannerSize == adSize)
        {
            onAdLoaded?.Invoke(true);
            if (delay > 0 && Time.timeScale > 0)
            {
                Invoke("CoShowBanner", delay);
            }
            else
            {
                CoShowBanner();
            }
        }
        else
        {
            //.Log(string.Format("destroying current banner({0} {1}), showing new one", AdsManager.bannerId, currentBannerSize));
            DestroyBanner();
            //RequestBanner(placementId, adSize, adPosition);
        }

        isShowBanner = true;
    }

    void CoShowBanner()
    {
        if (noAds != null && noAds())
            return;

        if (this.bannerView != null)
        {
            this.bannerView.Show();
        }
    }

    public void HideBanner()
    {
        CancelInvoke("CoShowBanner");

        if (currentBannerAd != null && currentBannerAd.BannerView != null)
        {
            currentBannerAd.BannerView.Hide();
            currentBannerAd.State = AdObjectState.Closed;
        }
    }

    private AdmobBannerAdObject GetCurrentBannerAdObject(bool makeNewIfNull = true)
    {
        if (currentBannerAd == null)
        {
            Debug.LogError("currentBannerAd is null.");
            if (makeNewIfNull)
            {
                Debug.Log("New ad will be created");
                currentBannerAd = new AdmobBannerAdObject();
            }
        }
        return currentBannerAd;
    }
    #endregion

    #region Interstitial
    public bool IsDestroyedInterstitial()
    {
        return (this.interstitial == null);
    }

    IEnumerator CoTimeoutLoadInterstitial()
    {
        var delay = new WaitForSeconds(TIMEOUT_LOADAD);
        yield return delay;
        LoadAdError loadAdError = new LoadAdError(new Omnilatent.AdMob.CustomLoadAdErrorClient("Self Timeout"));
        HandleInterstitialFailedToLoadNoShow(null, new AdFailedToLoadEventArgs() { LoadAdError = loadAdError }); ;
    }
    #endregion

    void ShowError(EventArgs args, string prefix = "ad")
    {
        var adFailed = args as AdFailedToLoadEventArgs;
        if (adFailed != null)
        {
            print(string.Format("{0} load failed, message: {1}", prefix, adFailed.LoadAdError.GetMessage()));
        }
    }

    void LogEvent(string eventName)
    {
        //FirebaseManager.LogEvent(eventName);
    }

    public void ShowBanner(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        ShowBanner(placementId, Omnilatent.AdsMediation.BannerTransform.defaultValue, onAdLoaded);
    }

    public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        string id = CustomMediation.GetAdmobID(placementType);
        GoogleMobileAds.Api.AdPosition adPosition = GoogleMobileAds.Api.AdPosition.Bottom;
        if (bannerTransform.adPosition != Omnilatent.AdsMediation.AdPosition.Unset)
        {
            adPosition = (GoogleMobileAds.Api.AdPosition)bannerTransform.adPosition;
        }
        //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

        var adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        if (currentBannerAd != null && currentBannerAd.AdPlacementType == placementType)
        {
            onAdLoaded?.Invoke(true);
            currentBannerAd.BannerView.Show();
            currentBannerAd.State = AdObjectState.Showing;
        }
        else
        {
            //.Log(string.Format("destroying current banner({0} {1}), showing new one", AdsManager.bannerId, currentBannerSize));
            DestroyBanner();
            RequestBanner(placementType, adSize, adPosition, onAdLoaded);
        }
    }

    public void Reward(AdPlacement.Type placementId, RewardDelegate onFinish)
    {
        string id = CustomMediation.GetAdmobID(placementId);
        //RewardAdmob(onFinish, id);
        ShowCachedRewardedAd(placementId, onFinish);
    }

    public static void QueueMainThreadExecution(Action action)
    {
#if UNITY_ANDROID
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            action.Invoke();
        });
#else 
        action.Invoke();
#endif
    }

    #region Deprecated
    [Obsolete]
    public void RequestInterstitial()
    {
        if (noAds != null && noAds())
            return;

        if (this.interstitial != null && !this.interstitial.IsLoaded())
        {
            this.interstitial.OnAdClosed -= HandleInterstitialClosed;
            this.interstitial.Destroy();
            this.interstitial = null;
        }

        if (this.interstitial == null)
        {
            this.interstitial = new InterstitialAd(interstitialId);
            this.interstitial.LoadAd(this.CreateAdRequest());
            this.interstitial.OnAdClosed += HandleInterstitialClosed;
        }
    }

    [Obsolete]
    public void RequestInterstitial(string newInterstitialId)
    {
        if (noAds != null && noAds())
        {
            return;
        }


        if (this.interstitial != null && !this.interstitial.IsLoaded())
        {
            this.interstitial.OnAdClosed -= HandleInterstitialClosed;
            this.interstitial.Destroy();
            this.interstitial = null;
        }

        if (this.interstitial == null)
        {
            this.interstitial = new InterstitialAd(newInterstitialId);
            this.interstitial.LoadAd(this.CreateAdRequest());
            this.interstitial.OnAdClosed += HandleInterstitialClosed;
            this.interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoad;
            this.interstitial.OnAdLoaded += HandleInterstitialLoaded;

            lastInterstitialRequestIsFailed = false;
            //("added listener failed load");
        }
    }

    [Obsolete]
    public static bool RequestAndShowInterstitial(string newInterstitialId, AdsManager.InterstitialDelegate onAdClosed = null)
    {
        if (AdsManager.instance != null)
        {
            if (instance.noAds != null && instance.noAds())
            {
                onAdClosed();
            }
            else
            {
                if (onAdClosed != null)
                {
                    instance.interstitialFinishDelegate = onAdClosed;
                }
                instance.RequestInterstitial(newInterstitialId);
                instance.ShowInterstitial();
            }
        }

        return false;
    }

    [Obsolete]
    public static bool ShowInterstitialWithCallback(AdsManager.InterstitialDelegate onAdClosed = null, bool showLoading = true)
    {
        if (AdsManager.instance != null)
        {
            if (instance.noAds != null && instance.noAds())
            {
                onAdClosed();
            }
            else
            {
                if (onAdClosed != null)
                {
                    instance.interstitialFinishDelegate = onAdClosed;
                }
                instance.ShowInterstitial(showLoading);
            }
        }

        return false;
    }

    [Obsolete]
    void HandleInterstitialLoaded(object sender, EventArgs args)
    {
        QueueMainThreadExecution(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);

            OnInterstitialLoaded(true);

            ShowInterstitial();
        });
    }

    [Obsolete]
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
    #endregion
}