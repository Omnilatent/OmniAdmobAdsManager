using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using UnityEngine.Networking;
using Omnilatent.AdMob;

/* CHANGE LOG:
 * 27/7/2020: Add timeout load to RequestInterstitialNoShow
 * 1/9/2020: Get ad ID from CustomMediation's function instead of using switch() to get directly from AdConst
 */

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    public const float TIME_BETWEEN_ADS = 10f;
    public static float TIMEOUT_LOADAD = 12f;
    public static float TIMEOUT_LOADREWARDAD = 12f;
    public static string appId;
    public static string bannerId;
    public AdSize currentBannerSize = AdSize.Banner;

    public static string videoId;
    public static string interstitialId;

    public delegate bool NoAdsDelegate();
    public NoAdsDelegate noAds;
    [SerializeField] bool cacheInterstitial; //cache interstitial. Work with one single interstitial ad id

    Coroutine coTimeoutLoad;

    public delegate void BoolDelegate(bool reward);
    public RewardDelegate adsVideoRewardedCallback; //For traditional Rewarded Video

    public AdsManager.InterstitialDelegate bannerLoadedDelegate;


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

    private InterstitialAd interstitial;

    private RewardResult rewardResult;

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
    public void RequestBanner(string placementId, AdSize adSize, AdPosition adPosition)
    {
        if (this.bannerView == null)
        {
            AdMobManager.bannerId = placementId;
            currentBannerSize = adSize;
            // Create a smart banner at the bottom of the screen.
            this.bannerView = new BannerView(placementId, adSize, adPosition);

            // Load a banner ad.
            this.bannerView.OnAdFailedToLoad += OnBannerAdsFailedToLoad;
            this.bannerView.OnAdLoaded += OnBannerAdsLoaded;
            this.bannerView.LoadAd(this.CreateAdRequest());
        }
    }

    void OnBannerAdsFailedToLoad(object sender, EventArgs args)
    {
        ShowError(args);
        DestroyBanner();
        bannerLoadedDelegate?.Invoke(false);
    }

    void OnBannerAdsLoaded(object sender, EventArgs args)
    {
        if (this.bannerView != null && isShowBanner)
            this.bannerView.Show();
        bannerLoadedDelegate?.Invoke(true);
    }

    public void DestroyBanner()
    {
        if (this.bannerView != null)
        {
            this.bannerView.OnAdFailedToLoad -= OnBannerAdsFailedToLoad;
            this.bannerView.Destroy();
            this.bannerView = null;
        }
    }

    public void ShowBanner(string placementId, AdSize adSize, AdPosition adPosition, float delay = 0f, AdsManager.InterstitialDelegate onAdLoaded = null)
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
            RequestBanner(placementId, adSize, adPosition);
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
        if (noAds != null && noAds())
            return;

        CancelInvoke("CoShowBanner");

        if (this.bannerView != null)
        {
            this.bannerView.Hide();
        }

        isShowBanner = false;
    }
    #endregion

    #region Interstitial
    public void DestroyInterstitial()
    {
        if (this.interstitial != null)
        {
            this.interstitial.OnAdClosed -= HandleInterstitialClosed;
            this.interstitial.Destroy();
            this.interstitial = null;
        }
    }

    public bool IsDestroyedInterstitial()
    {
        return (this.interstitial == null);
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

    void OnInterstitialFinish(bool isSuccess = false)
    {
        if (interstitialFinishDelegate != null)
        {
            this.interstitialFinishDelegate(isSuccess);
            this.interstitialFinishDelegate = null;
        }
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
        AdPosition adPosition = (AdPosition)bannerTransform.adPosition;
        ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);
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
    #endregion
}