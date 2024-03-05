using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class NativeAdWrapper
{
    private Dictionary<AdPlacement.Type, NativeAdItem> nativeAdItems;
    public AdMobManager manager;
    private NativeAdItem globalAd;

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
        manager.manager.onNativeLoaded?.Invoke(placementId, NativeAdData, false);
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
        manager.manager.onNativeRequested?.Invoke(placementId);
        adLoader.LoadAd(new AdRequest.Builder().Build());
        adLoader.OnNativeAdLoaded += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                isRequesting = false;
                OnNativeAdLoaded(a, b);
            });
        adLoader.OnNativeAdClosed += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                manager.manager.onNativeClosed?.Invoke(placementId);
            });
        adLoader.OnNativeAdClicked += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                manager.manager.onNativeUserClick?.Invoke(placementId, NativeAdData);
            });
        adLoader.OnNativeAdImpression += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                manager.manager.OnNativeImpression?.Invoke(placementId, b);
            });
        adLoader.OnNativeAdOpening += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                manager.manager.onNativeShow?.Invoke(placementId, NativeAdData);
            });
        adLoader.OnAdFailedToLoad += (a, b) => ReloadAds(b, count);
    }

    public static void QueueMainThreadExecution(Action action)
    {
#if UNITY_ANDROID
        UnityMainThreadDispatcher.Instance().Enqueue(() => { action.Invoke(); });
#else
        action.Invoke();
#endif
    }

    private void ReloadAds(AdFailedToLoadEventArgs b, int count)
    {
        count++;
        manager.manager.onNativeFailedToLoad?.Invoke(placementId, null, b.LoadAdError);
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
        manager.manager.onNativeLoaded?.Invoke(placementId, NativeAdData, true);
        NativeAdData.OnPaidEvent += (sender, eventData) =>
        {
            manager.manager.onNativePaid.Invoke(placementId, NativeAdData, eventData.AdValue);
        };
    }

    public NativeAd NativeAdData { get => nativeAdData; set => nativeAdData = value; }
    public bool IsRequesting { get => isRequesting; }
}

