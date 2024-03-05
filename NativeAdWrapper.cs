using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class NativeAdWrapper
{
    public Action<AdPlacement.Type, NativeAd, AdError> onNativeFailedToLoad;
    public Action<AdPlacement.Type, NativeAd, AdValue> onNativePaid;
    public Action<AdPlacement.Type, NativeAd, bool> onNativeLoaded;
    public Action<AdPlacement.Type, NativeAd> onNativeShow;
    public Action<AdPlacement.Type> onNativeClosed;
    public Action<AdPlacement.Type, NativeAd> onNativeUserClick;
    public Action<AdPlacement.Type> onNativeRequested;
    public Action<AdPlacement.Type, EventArgs> OnNativeImpression;


    private Dictionary<AdPlacement.Type, NativeAdItem> nativeAdItems;
    private AdMobManager manager;
    private NativeAdItem globalAd;
    private bool loadNativeConfig = false;

    public NativeAdWrapper(AdMobManager manager)
    {
        this.manager = manager;
        nativeAdItems = new Dictionary<AdPlacement.Type, NativeAdItem>();
        GetGlobalData();
    }

    private void GetGlobalData()
    {
        Debug.Log("Native begin load global!");
        globalAd = new NativeAdItem(AdPlacement.Common_Native, this);
        globalAd.IsRefreshData = false;
        globalAd.RequestAd();
        nativeAdItems.Add(AdPlacement.Common_Native, globalAd);
    }

    public void LoadNativeAd(AdPlacement.Type placementId)
    {
        if (AdsManager.Instance.DoNotShowAds(placementId))
        {
            Debug.LogWarning($"Ad {placementId} was hide by AdsManager!");
            return;
        }
        if (nativeAdItems.ContainsKey(placementId))
        {
            var native = nativeAdItems[placementId];
            native.RequestAd();
        }
        else
        {
            globalAd.RequestAd();
            var native = new NativeAdItem(placementId, this);
            native.RequestAd();
            nativeAdItems.Add(placementId, native);
        }
    }
}

[System.Serializable]
public class NativeAdItem
{
    public bool IsRefreshData = true;
    private NativeAd nativeAdData;
    private NativeAdWrapper manager;
    private AdPlacement.Type placementId;
    private bool isRequesting = false;
    private float nextTimeRefresh = 0;
    private const int NUMBER_RELOAD = 3;

    private float timeRefresh => AdsManager.TIME_BETWEEN_ADS;

    public NativeAdItem(AdPlacement.Type placementId, NativeAdWrapper manager)
    {
        this.placementId = placementId;
        this.manager = manager;
    }

    public void RequestAd()
    {
        if (nativeAdData == null && !IsRequesting)
        {
            Request(0);
            return;
        }

        //Debug.Log("Get Native ads from cache: " + placementId);
        manager.onNativeLoaded?.Invoke(placementId, NativeAdData, false);
        if (IsRefreshData && Time.time > nextTimeRefresh)
        {
            Request(0);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="count">number call for reload</param>
    private void Request(int count)
    {
        isRequesting = true;
        nextTimeRefresh = Time.time + timeRefresh;
        string unitId = CustomMediation.GetAdmobID(placementId);
        AdLoader adLoader = new AdLoader.Builder(unitId).ForNativeAd().Build();
        manager.onNativeRequested?.Invoke(placementId);
        adLoader.OnNativeAdLoaded += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                //this.adLoader = adLoader;
                isRequesting = false;
                OnNativeAdLoaded(a, b);
            });
        adLoader.OnNativeAdClosed += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.onNativeClosed?.Invoke(placementId);
            });
        adLoader.OnNativeAdClicked += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.onNativeUserClick?.Invoke(placementId, NativeAdData);
            });
        adLoader.OnNativeAdImpression += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.OnNativeImpression?.Invoke(placementId, b);
            });
        adLoader.OnNativeAdOpening += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.onNativeShow?.Invoke(placementId, NativeAdData);
            });
        adLoader.OnAdFailedToLoad += (a, b) => ReloadAds(b, count);
        adLoader.LoadAd(new AdRequest.Builder().Build());
    }

    private void ReloadAds(AdFailedToLoadEventArgs b, int count)
    {
        count++;
        manager.onNativeFailedToLoad?.Invoke(placementId, null, b.LoadAdError);
        if (count > NUMBER_RELOAD)
        {
            Request(count);
        }
        else
        {
            count = 0;
            isRequesting = false;
        }
    }

    private void OnNativeAdLoaded(object sender, NativeAdEventArgs e)
    {
        NativeAdData = e.nativeAd;
        //Debug.Log("Get Native ads from request: " + placementId);
        manager.onNativeLoaded?.Invoke(placementId, NativeAdData, true);
        NativeAdData.OnPaidEvent += (sender, eventData) =>
        {
            manager.onNativePaid.Invoke(placementId, NativeAdData, eventData.AdValue);
        };
    }

    public NativeAd NativeAdData { get => nativeAdData; set => nativeAdData = value; }
    public bool IsRequesting { get => isRequesting; }
}

