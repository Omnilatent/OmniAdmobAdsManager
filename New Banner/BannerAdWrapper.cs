using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class BannerAdWrapper
{
    const string rmcf = "time_refresh_ad_banner";
    private Dictionary<AdPlacement.Type, BannerItem> bannerAdItems;
    public AdMobManager manager;
    public float timeReloadAd;
    bool isShow = false;

    public BannerAdWrapper(AdMobManager manager)
    {
        this.manager = manager;
        bannerAdItems = new Dictionary<AdPlacement.Type, BannerItem>();
#if UNITY_IOS
        AdMobManager.instance.onAOAdBeforePresentFullScreenContent += (a, b) => HideAll();
        AdMobManager.instance.onInterstitialOpening += (a, b) => HideAll();
#endif
        timeReloadAd = 10;
        FirebaseRemoteConfigHelper.CheckAndHandleFetchConfig(FetchRMCF);
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
        if (AdsManager.Instance.DoNotShowAds(placementId))
        {
            Debug.LogWarning($"Ad {placementId} was hide by AdsManager!");
            return;
        }
        isShow = true;
        if (!bannerAdItems.ContainsKey(placementId))
        {
            var banner = new BannerItem(this, placementId, collapsiable, position);
            bannerAdItems.Add(placementId, banner);
        }
        bannerAdItems[placementId].RequestAd();
    }

    public void ShowBanner(AdPlacement.Type placementId)
    {
        if (!isShow)
        {
            Debug.LogError("Ad Banner don't show! " + placementId);
            return;
        }
        foreach (var e in bannerAdItems)
        {
            e.Value.IsShowing = e.Key.Equals(placementId);
        }
    }

    public void HideAll()
    {
        isShow = false;
        foreach (var e in bannerAdItems)
        {
            e.Value.IsShowing = false;
        }
    }
}

[System.Serializable]
public class BannerItem
{
    private const int NUMBER_RELOAD = 3;
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
        isRequest = false;
        _bannerView?.Destroy();
        _bannerView = _cacheBannerView;
        ListenToAdEvents();
        wrapper.manager.onBannerLoaded?.Invoke(placementId, _bannerView);
        if (!IsShowing)
        {
            _bannerView.Hide();
        }
#endif
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
            Debug.LogError($"Banner load fail {placementId}: " + error.GetMessage());
            wrapper.manager.onBannerFailedToLoad?.Invoke(placementId, _bannerView, error);
            LoadBanner();
        };
    }

    private void ListenToAdEvents()
    {
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            wrapper.manager.onBannerPaidEvent?.Invoke(placementId, _bannerView, adValue);
        };
        _bannerView.OnAdImpressionRecorded += () =>
        {
            wrapper.manager.onBannerImpression?.Invoke(placementId, _bannerView);
        };
        _bannerView.OnAdClicked += () =>
        {
            wrapper.manager.onBannerUserClick?.Invoke(placementId, _bannerView);
        };
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
