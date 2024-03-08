using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class BannerAdFocus : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_IOS
        AdMobManager.instance.onInterstitialClosed += (a, b) => OnApplicationFocus(true);
        AdMobManager.instance.onAOAdDidPresentFullScreenContent += (a, b) => OnApplicationFocus(true);
        AdMobManager.instance.onInterstitialOpening += (a, b) => OnApplicationPause(true);
        AdMobManager.instance.onAOAdBeforePresentFullScreenContent += (a, b) => OnApplicationPause(true);
#endif
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            BannerItem.QueueMainThreadExecution(() =>
            {
                StopAllCoroutines();
                StartCoroutine(DelayTocallAction(AdMobManager.instance.InstanceBannerAdWrapper.ShowBannerFocus));
            });
        }
        else
        {
            BannerItem.QueueMainThreadExecution(() =>
            {
                AdMobManager.instance.InstanceBannerAdWrapper.HideBannerFocus();
            });
        };
    }

    IEnumerator DelayTocallAction(Action action)
    {
        yield return new WaitForSeconds(0.5f);
        action.Invoke();
    }
}

[System.Serializable]
public class BannerAdWrapper
{
    const string rmcf = "time_refresh_ad_banner";
    const string focus_condition = "focus";
    const string obj_condition = "obj";

    private Dictionary<AdPlacement.Type, BannerItem> bannerAdItems;
    public AdMobManager manager;
    public float timeReloadAd;
    public Dictionary<string, bool> keyShowBanner;
    public UnityEvent OnChangeEvent;

    public BannerAdWrapper(AdMobManager manager)
    {
        this.manager = manager;
        InitObject();
        bannerAdItems = new Dictionary<AdPlacement.Type, BannerItem>();
        keyShowBanner = new Dictionary<string, bool>();
        keyShowBanner.Add(focus_condition, true);
        keyShowBanner.Add(obj_condition, true);
        OnChangeEvent = new UnityEvent();
#if UNITY_IOS
        AdMobManager.instance.onAOAdBeforePresentFullScreenContent += (a, b) => HideAll();
        AdMobManager.instance.onInterstitialOpening += (a, b) => HideAll();
#endif
        timeReloadAd = 10;
        FirebaseRemoteConfigHelper.CheckAndHandleFetchConfig(FetchRMCF);
    }

    private void InitObject()
    {
        var obj = new GameObject("Banner Ad Focus");
        obj.AddComponent<BannerAdFocus>();
    }

    private void FetchRMCF(object sender, bool success)
    {
        if (success)
        {
            timeReloadAd = FirebaseRemoteConfigHelper.GetInt(rmcf, 10);
        }
    }

    public void LoadAd(AdPlacement.Type placementId, bool collapsiable, AdPosition position)
    {
        if (!keyShowBanner.ContainsKey(placementId.ToString()))
        {
            keyShowBanner.Add(placementId.ToString(), true);
        }
        if (AdsManager.Instance.DoNotShowAds(placementId))
        {
            keyShowBanner[placementId.ToString()] = false;
            Debug.LogWarning($"Ad {placementId} was hide by AdsManager!");
            return;
        }
        keyShowBanner[placementId.ToString()] = true;
        if (!bannerAdItems.ContainsKey(placementId))
        {
            var banner = new BannerItem(this, placementId, collapsiable, position);
            bannerAdItems.Add(placementId, banner);
        }
        bannerAdItems[placementId].RequestAd();
    }

    public void ShowBanner(AdPlacement.Type placementId)
    {
        if (!CheckShowAd(placementId))
        {
            Debug.Log("<color=red>Ad Banner don't show! </color>" + placementId);
            Debug.Log($"<color=red>{ keyShowBanner[focus_condition]},{keyShowBanner[obj_condition]},{keyShowBanner[placementId.ToString()]}</color>");
            return;
        }
        foreach (var e in bannerAdItems)
        {
            e.Value.IsShowing = e.Key.Equals(placementId);
        }
    }

    private bool CheckShowAd(AdPlacement.Type placementId)
    {
        return keyShowBanner[focus_condition] && keyShowBanner[obj_condition] && keyShowBanner[placementId.ToString()];
    }

    public void HideAll()
    {
        foreach (var e in bannerAdItems)
        {
            e.Value.IsShowing = false;
        }
    }

    internal void ShowBannerFocus()
    {
        keyShowBanner[focus_condition] = true;
        OnChangeEvent.Invoke();
    }

    internal void HideBannerFocus()
    {
        keyShowBanner[focus_condition] = false;
        HideAll();
        OnChangeEvent.Invoke();
    }

    internal void ShowBannerObj()
    {
        keyShowBanner[obj_condition] = true;
        OnChangeEvent.Invoke();
    }

    internal void HideBannerObj()
    {
        keyShowBanner[obj_condition] = false;
        HideAll();
        OnChangeEvent.Invoke();
    }
}

[System.Serializable]
public class BannerItem
{
    private const int NUMBER_RELOAD = 10;
    private const int MILLISECONDS_DELAY_RELOAD = 2000;
    private BannerView _bannerView;
    private BannerView _cacheBannerView;
    private AdRequest adRequest;
    private AdPlacement.Type placementId;
    private bool collapsiable;
    private AdPosition position;
    private BannerAdWrapper wrapper;
    private bool isShow = false;
    private bool isRequest = false;
    private int numberLoad = 0;
    private float nextTimeRefresh = 0;
    private float timeRefresh => wrapper.timeReloadAd;

    public bool IsShowing
    {
        get => isShow;
        set
        {
            if (value)
            {
                ShowBanner();
            }
            else
            {
                HideBanner();
            }
        }
    }

    public BannerItem(BannerAdWrapper wrapper, AdPlacement.Type placementId, bool collapsiable, AdPosition position)
    {
        this.wrapper = wrapper;
        this.placementId = placementId;
        this.collapsiable = collapsiable;
        this.position = position;
    }

    public void RequestAd()
    {
        if (_bannerView != null)
        {
            ShowBanner();
            if (Time.time < nextTimeRefresh || timeRefresh == -1)
            {
                return;
            }
        }
        if (isRequest)
        {
            return;
        }
        nextTimeRefresh = Time.time + timeRefresh;
        Debug.Log($"Request New Banner: {placementId}");
        string _adUnitId = CustomMediation.GetAdmobID(placementId);
        AdSize adaptiveSize =
                 AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _cacheBannerView = new BannerView(_adUnitId, adaptiveSize, position);
        adRequest = new AdRequest();
        if (collapsiable)
        {
            adRequest.Extras.Add("collapsible", "bottom");
            adRequest.Extras.Add("collapsible_request_id", Guid.NewGuid().ToString());
        }
        LoadBanner(0);
        ListerToCacheAdEvent();
    }

    private void LoadBanner(int count = -1)
    {
        if (count != -1)
        {
            numberLoad = count;
        }
        else
        {
            numberLoad++;
        }
        if (numberLoad > NUMBER_RELOAD)
        {
            Debug.LogError($"Out of reload banner {placementId}");
            isRequest = false;
            _cacheBannerView?.Destroy();
            return;
        }
        isRequest = true;
        wrapper.manager.onBannerRequested?.Invoke(placementId);
        _cacheBannerView.LoadAd(adRequest);
#if UNITY_EDITOR
        FakeEventBanner();
#endif
    }

    private void FakeEventBanner()
    {
        isRequest = false;
        _bannerView?.Destroy();
        _bannerView = _cacheBannerView;
        ListenToAdEvents();
        wrapper.manager.onBannerLoaded?.Invoke(placementId, _bannerView);
        if (!IsShowing)
        {
            _bannerView.Hide();
        }
    }

    private void ListerToCacheAdEvent()
    {
        _cacheBannerView.OnBannerAdLoaded += () =>
        {
            isRequest = false;
            _bannerView?.Destroy();
            _bannerView = _cacheBannerView;
            ListenToAdEvents();
            wrapper.manager.onBannerLoaded?.Invoke(placementId, _bannerView);
            if (!IsShowing)
            {
                _bannerView.Hide();
            }
        };
        _cacheBannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.Log($"<color=red>Banner load fail {placementId}: <color=red>" + error.GetMessage());
            wrapper.manager.onBannerFailedToLoad?.Invoke(placementId, _bannerView, error);
            LoadBanner();
        };
    }

    private void ListenToAdEvents()
    {
        _bannerView.OnAdPaid += (AdValue adValue) => QueueMainThreadExecution(() =>
        {
            wrapper.manager.onBannerPaidEvent?.Invoke(placementId, _bannerView, adValue);
        });
        _bannerView.OnAdImpressionRecorded += () => QueueMainThreadExecution(() =>
        {
            wrapper.manager.onBannerImpression?.Invoke(placementId, _bannerView);
        });
        _bannerView.OnAdClicked += () => QueueMainThreadExecution(() =>
        {
            wrapper.manager.onBannerUserClick?.Invoke(placementId, _bannerView);
        });
    }
    public static void QueueMainThreadExecution(Action action)
    {
#if UNITY_ANDROID
        UnityMainThreadDispatcher.Instance().Enqueue(() => { action.Invoke(); });
#else
        action.Invoke();
#endif
    }

    private void HideBanner()
    {
        Debug.Log("Banner Hide " + placementId);
        _bannerView?.Hide();
        isShow = false;
        wrapper.manager.onBannerHide?.Invoke(placementId, _bannerView);
    }

    private void ShowBanner()
    {
        if (_bannerView == null)
        {
            Debug.LogError("Banner is not show by banner view is null!" + placementId);
            isShow = false;
        }
        else
        {
            Debug.Log("Banner show!" + placementId);
            _bannerView.Show();
            isShow = true;
            wrapper.manager.onBannerShow?.Invoke(placementId, _bannerView);
        }

    }
}
