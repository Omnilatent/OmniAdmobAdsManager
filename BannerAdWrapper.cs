using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BannerAdWrapper
{
    const string rmcf = "time_refresh_ad_banner";
    private Dictionary<AdPlacement.Type, BannerItem> bannerAdItems;
    public AdMobManager manager;
    AdPlacement.Type lastBannerShow;
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
        if (bannerAdItems.ContainsKey(placementId))
        {
            var banner = bannerAdItems[placementId];
            banner.RequestAd();
        }
        else
        {
            var banner = new BannerItem(this, placementId, collapsiable, position);
            banner.RequestAd();
            bannerAdItems.Add(placementId, banner);
        }
    }

    public void ShowBanner(AdPlacement.Type placementId)
    {
        if (!isShow)
        {
            return;
        }
        foreach (var e in bannerAdItems)
        {
            e.Value.IsShowing = e.Key.Equals(placementId);
        }
        lastBannerShow = placementId;
    }

    public void HideAll()
    {
        isShow = false;
        foreach (var e in bannerAdItems)
        {
            if (e.Value.IsShowing)
            {
                lastBannerShow = e.Key;
            }
            e.Value.IsShowing = false;
        }
    }

    public void ShowBanner()
    {
        isShow = true;
        ShowBanner(lastBannerShow);
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
        string _adUnitId = CustomMediation.GetAdmobID(placementId);
        AdSize adaptiveSize =
                 AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _cacheBannerView = new BannerView(_adUnitId, adaptiveSize, position);
        adRequest = new AdRequest();
        if (collapsiable)
        {
            adRequest.Extras.Add("collapsible", "bottom");
        }
        LoadBanner(0);
        ListerToCacheAdEvent();
    }

    private async void LoadBanner(int count = -1)
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
        else
        {
            await Task.Delay(MILLISECONDS_DELAY_RELOAD);
        }
        isRequest = true;
        wrapper.manager.onBannerRequested.Invoke(placementId);
        _cacheBannerView.LoadAd(adRequest);
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
        _bannerView?.Hide();
        isShow = false;
        wrapper.manager.onBannerHide?.Invoke(placementId, _bannerView);
    }

    private void ShowBanner()
    {
        if (_bannerView == null)
            isShow = false;
        _bannerView.Show();
        isShow = true;
        wrapper.manager.onBannerShow?.Invoke(placementId, _bannerView);
    }
}
