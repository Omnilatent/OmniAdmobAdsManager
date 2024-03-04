using System.Collections;
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
    [SerializeField] internal bool cacheInterstitial; //cache interstitial. Work with one single interstitial ad id

    Coroutine coTimeoutLoad;

    public delegate void BoolDelegate(bool reward);

    public RewardDelegate adsVideoRewardedCallback; //For traditional Rewarded Video

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
    public  NativeAdWrapper InstanceNativeAdWrapper;

    private RewardResult rewardResult;

    [SerializeField] private bool forceRewardAdLoadSuccessOnEditor; //fix Prefab Ad is null error on editor

    [Obsolete] public bool isShowBanner { get; protected set; }

    public float interstitialTime { get; internal set; }

    public float time { get; protected set; }

    public bool showingAds { get; internal set; }
    
    /// <summary>
    /// If true, Interstitial's OnAdClosed will be forced to invoke on ad show
    /// </summary>
    public static bool CallInterClosedOnOpeningInEditor = false;

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
        InstanceNativeAdWrapper = new NativeAdWrapper(this);

        //Debug.Log("OS: " + Application.platform + ". RAM: " + SystemInfo.systemMemorySize);
    }

    /*private void Update()
    {
        if (!showingAds)
        {
            time += Time.deltaTime;
        }
    }*/

    /*IEnumerator CoTimeoutLoadInterstitial()
    {
        var delay = new WaitForSeconds(TIMEOUT_LOADAD);
        yield return delay;
        LoadAdError loadAdError = new LoadAdError(new Omnilatent.AdMob.CustomLoadAdErrorClient("Self Timeout"));
        HandleInterstitialFailedToLoadNoShow(null, new AdFailedToLoadEventArgs() { LoadAdError = loadAdError }); ;
    }*/

    internal void ShowError(AdError adFailed, string prefix = "ad")
    {
        if (adFailed != null)
        {
            print(string.Format("{0} load failed, message: {1}", prefix, adFailed.GetMessage()));
        }
    }

    internal void LogEvent(string eventName)
    {
        //FirebaseManager.LogEvent(eventName);
    }

    public void Reward(AdPlacement.Type placementId, RewardDelegate onFinish)
    {
        string id = CustomMediation.GetAdmobID(placementId);
        //RewardAdmob(onFinish, id);
#if UNITY_EDITOR
        if (forceRewardAdLoadSuccessOnEditor)
        {
            onFinish?.Invoke(new RewardResult(RewardResult.Type.Finished));
            return;
        }
#endif
        ShowCachedRewardedAd(placementId, onFinish);
    }

    public static void QueueMainThreadExecution(Action action)
    {
#if UNITY_ANDROID
        UnityMainThreadDispatcher.Instance().Enqueue(() => { action.Invoke(); });
#else
        action.Invoke();
#endif
    }

    #region Banner

    public Action<AdPlacement.Type, BannerView, AdError> onBannerFailedToLoad;
    public Action<AdPlacement.Type, BannerView, AdValue> onBannerPaidEvent;
    public Action<AdPlacement.Type, BannerView> onBannerLoaded;
    public Action<AdPlacement.Type, BannerView> onBannerShow;
    public Action<AdPlacement.Type, BannerView> onBannerHide;
    public Action<AdPlacement.Type, BannerView> onBannerUserClick;
    public Action<AdPlacement.Type> onBannerRequested;
    private BannerWrapper _bannerWrapper;

    internal BannerWrapper bannerWrapper
    {
        get
        {
            if (_bannerWrapper == null)
                _bannerWrapper = new BannerWrapper(this);
            return _bannerWrapper;
        }
    }

    public void ShowBanner(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        ShowBanner(placementId, Omnilatent.AdsMediation.BannerTransform.defaultValue, onAdLoaded);
    }

    public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
        AdsManager.InterstitialDelegate onAdLoaded = null)
    {
        bannerWrapper.ShowBanner(placementType, bannerTransform, onAdLoaded);
    }

    public void HideBanner()
    {
        bannerWrapper.HideBanner();
    }

    public void DestroyBanner()
    {
        bannerWrapper.DestroyBanner();
    }

    #endregion

    #region Interstitial

    public Action<AdPlacement.Type, InterstitialAd> onInterstitialLoaded;
    public Action<AdPlacement.Type, InterstitialAd, AdError> onInterstitialFailedToLoad;
    public Action<AdPlacement.Type, InterstitialAd> onInterstitialOpening;
    public Action<AdPlacement.Type, InterstitialAd> onInterstitialClosed;
    public Action<AdPlacement.Type, InterstitialAd, AdError> onInterstitialFailedToShow;
    public Action<AdPlacement.Type, InterstitialAd> onInterstitialImpression;
    public Action<AdPlacement.Type, InterstitialAd> onInterstitialClicked;
    public Action<AdPlacement.Type, InterstitialAd, AdValue> onInterstitialPaidEvent;
    public Action<AdPlacement.Type> onInterstitialRequested;

    private InterstitialWrapper _interstitialWrapper;

    internal InterstitialWrapper interstitialWrapper
    {
        get
        {
            if (_interstitialWrapper == null)
                _interstitialWrapper = new InterstitialWrapper(this);
            return _interstitialWrapper;
        }
    }

    public void ShowInterstitial(AdPlacement.Type placementType, AdsManager.InterstitialDelegate onAdClosed)
    {
        interstitialWrapper.ShowInterstitial(placementType, onAdClosed);
    }

    public void RequestInterstitialNoShow(AdPlacement.Type placementType, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
    {
        interstitialWrapper.RequestInterstitialNoShow(placementType, onAdLoaded, showLoading);
    }

    #endregion
}