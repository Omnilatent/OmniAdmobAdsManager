using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using UnityEngine.Networking;

/* CHANGE LOG:
 * 27/7/2020: Add timeout load to RequestInterstitialNoShow
 * 1/9/2020: Get ad ID from CustomMediation's function instead of using switch() to get directly from AdConst
 */

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    public const float TIME_BETWEEN_ADS = 10f;
    const float TIMEOUT_LOADAD = 12f;
    public static string appId;
    public static string bannerId;
    public AdSize currentBannerSize = AdSize.Banner;

    public static string videoId;
    public static string interstitialId;

    public delegate bool NoAdsDelegate();
    public NoAdsDelegate noAds;
    bool lastInterstitialRequestIsFailed = false;
    [SerializeField] bool cacheInterstitial; //cache interstitial. Work with one single interstitial ad id

    Coroutine coTimeoutLoad;

    public delegate void BoolDelegate(bool reward);
    public RewardDelegate adsVideoRewardedCallback;

    public delegate void InterstitialDelegate(bool isSuccess = false);
    public AdsManager.InterstitialDelegate interstitialFinishDelegate;
    public AdsManager.InterstitialDelegate interstitialLoadedDelegate;
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

    private RewardBasedVideoAd rewardBasedVideo;

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

        this.rewardBasedVideo = RewardBasedVideoAd.Instance;
        this.rewardBasedVideo.OnAdClosed += HandleRewardedAdClosed;
        this.rewardBasedVideo.OnAdCompleted += HandleVideoCompleted;
        this.rewardBasedVideo.OnAdRewarded += HandleUserEarnedReward;

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
    public void RequestBanner(string placementId, AdSize adSize)
    {
        if (this.bannerView == null)
        {
            AdMobManager.bannerId = placementId;
            currentBannerSize = adSize;
            // Create a smart banner at the bottom of the screen.
            this.bannerView = new BannerView(placementId, adSize, AdPosition.Bottom);

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

    public void ShowBanner(string placementId, AdSize adSize, float delay = 0f, AdsManager.InterstitialDelegate onAdLoaded = null)
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
            RequestBanner(placementId, adSize);
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
                Debug.Log("Previous Interstitial load is not finished");
                this.interstitial.OnAdClosed -= HandleInterstitialClosed;
                this.interstitial.Destroy();
                this.interstitial = null;
            }
            else
            {
                //.Log("Cached Ads loaded success, showing");
                HandleInterstitialLoadedNoShow(null, null); //if a previous interstitial was loaded but not shown, show that interstitial
                return;
            }
        }

        if (this.interstitial == null)
        {
            this.interstitial = new InterstitialAd(newInterstitialId);
            this.interstitial.LoadAd(this.CreateAdRequest());
            this.interstitial.OnAdClosed += HandleInterstitialClosed;
            this.interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoadNoShow;
            this.interstitial.OnAdLoaded += HandleInterstitialLoadedNoShow;

            lastInterstitialRequestIsFailed = false;
            //("added listener failed load");
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

    /// <param name="cacheNextInter">Cache next interstitial ads</param>
    /// <param name="logOriginName">For tracking where this interstitial came from</param>
    public void ShowInterstitial(bool showLoading = true, AdsManager.InterstitialDelegate onAdClosed = null, bool cacheNextInter = false, string logOriginName = "")
    {
        //if (AdMobManager.instance.time - AdMobManager.instance.interstitialTime < TIME_BETWEEN_ADS)
        //{
        //    OnInterstitialFinish();
        //    return;
        //}

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
            OnInterstitialFinish(false);
        }
        else
        {
            //Old logic, this part shouldn't execute
            if (showLoading)
            {
                //Manager.LoadingAnimation(true);
#if UNITY_EDITOR
                //Manager.LoadingAnimation(false);
#endif
            }
            RequestInterstitial();
            this.interstitial.OnAdLoaded += HandleInterstitialLoaded;
            //this.interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoad;
            //("added listener load");
        }
    }

    public bool IsDestroyedInterstitial()
    {
        return (this.interstitial == null);
    }

    void HandleInterstitialClosed(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            this.showingAds = false;
            DestroyInterstitial();
            OnInterstitialFinish(true);

            if (Application.platform == RuntimePlatform.Android && cacheInterstitial)
            {
                RequestInterstitial();
            }
        });
    }

    void HandleInterstitialLoaded(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);

            OnInterstitialLoaded(true);

            ShowInterstitial();
        });
    }

    void HandleInterstitialLoadedNoShow(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdLoaded -= HandleInterstitialLoadedNoShow;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);

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

    void HandleInterstitialFailedToLoad(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
            //Manager.LoadingAnimation(false);

            OnInterstitialFinish(false);

            lastInterstitialRequestIsFailed = true;
            ShowError(args);
        });
    }

    void HandleInterstitialFailedToLoadNoShow(object sender, EventArgs args)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            this.interstitial.OnAdLoaded -= HandleInterstitialLoaded;
            this.interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoadNoShow;
            //Manager.LoadingAnimation(false); //let main AdsManager handle this

            OnInterstitialLoaded();

            lastInterstitialRequestIsFailed = true;
            ShowError(args);
        });
    }

    IEnumerator CoTimeoutLoadInterstitial()
    {
        var delay = new WaitForSeconds(TIMEOUT_LOADAD);
        yield return delay;
        HandleInterstitialFailedToLoadNoShow(null, new AdFailedToLoadEventArgs() { Message = "Timeout" });
    }
    #endregion

    void ShowError(EventArgs args, string prefix = "ad")
    {
        var adFailed = args as AdFailedToLoadEventArgs;
        if (adFailed != null)
        {
            print(string.Format("{0} load failed, message: {1}", prefix, adFailed.Message));
        }
    }

    void LogEvent(string eventName)
    {
        //FirebaseManager.LogEvent(eventName);
    }

    public void ShowBanner(AdPlacementType placementId, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        string id = CustomMediation.GetAdmobID(placementId, AdMobConst.BANNER_ID);
        ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), 0f, onAdLoaded);
    }

    public void ShowInterstitial(AdPlacementType placementId, AdsManager.InterstitialDelegate onAdClosed)
    {
        ShowInterstitial(true, onAdClosed, cacheInterstitial, placementId.ToString());
    }

    public void RequestInterstitialNoShow(AdPlacementType placementId, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
    {
        string id = CustomMediation.GetAdmobID(placementId, AdMobConst.INTERSTITIAL);
        RequestAdmobInterstitialNoShow(id, onAdLoaded, showLoading);
    }

    public void Reward(AdPlacementType placementId, RewardDelegate onFinish)
    {
        string id = CustomMediation.GetAdmobID(placementId, AdMobConst.REWARD_ID);
        RewardAdmob(onFinish, id);
    }
}