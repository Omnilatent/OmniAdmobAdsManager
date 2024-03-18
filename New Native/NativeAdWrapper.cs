using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class NativeAdWrapper
{
    const string rmcf = "time_refresh_ad_native";
    private Dictionary<AdPlacement.Type, NativeAdItem> nativeAdItems;
    public AdMobManager manager;
    private NativeAdItem globalAd;
    public float timeReloadAd;

    public NativeAdWrapper(AdMobManager manager)
    {
        this.manager = manager;
        nativeAdItems = new Dictionary<AdPlacement.Type, NativeAdItem>();
        timeReloadAd = 10;
        GetGlobalData();
        FirebaseRemoteConfigHelper.CheckAndHandleFetchConfig(FetchRMCF);
    }

    private void FetchRMCF(object sender, bool success)
    {
        if (success)
        {
            timeReloadAd = FirebaseRemoteConfigHelper.GetInt(rmcf, 10);
        }
    }

    private void GetGlobalData()
    {
        Debug.Log("Native begin load global!");
        globalAd = new NativeAdItem(AdPlacement.Common_Native, this);
        globalAd.IsRefreshData = true;
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
            if (native.NativeAdData == null)
            {
                globalAd.RequestAd();
            }
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
#if OMNILATENT_NATIVE_ADS
    private NativeAd nativeAdData;
#else
    private object nativeAdData;
#endif
    private NativeAdWrapper manager;
    private AdPlacement.Type placementId;
    private bool isRequesting = false;
    private float nextTimeRefresh = 0;
    private const int NUMBER_RELOAD = 3;

    private float timeRefresh => manager.timeReloadAd;

    public NativeAdItem(AdPlacement.Type placementId, NativeAdWrapper manager)
    {
        this.placementId = placementId;
        this.manager = manager;
    }

    public void RequestAd()
    {
#if OMNILATENT_NATIVE_ADS
        if (nativeAdData == null && !IsRequesting)
        {
            Request(0);
            return;
        }

        Debug.Log("Get Native ads from cache: " + placementId);
        manager.manager.onNativeLoaded?.Invoke(placementId, NativeAdData, false);
#endif
    }


    async void Reload()
    {
        await Task.Delay(1000);
        Request(0);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="count">number call for reload</param>
    private void Request(int count)
    {
#if OMNILATENT_NATIVE_ADS
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
                if (Time.time > nextTimeRefresh && timeRefresh != -1)
                {
                    Reload();
                }
            });
        adLoader.OnNativeAdOpening += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                manager.manager.onNativeShow?.Invoke(placementId, NativeAdData);
            });
        adLoader.OnAdFailedToLoad += (a, b) =>
            QueueMainThreadExecution(() =>
            {
                ReloadAds(b, count);
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

    private void ReloadAds(AdFailedToLoadEventArgs b, int count)
    {
        count++;
        if (count < NUMBER_RELOAD)
        {
            Request(count);
        }
        else
        {
            Debug.LogError("Out of request native ad " + placementId);
            count = 0;
            isRequesting = false;
            manager.manager.onNativeFailedToLoad?.Invoke(placementId, null, b.LoadAdError);
        }
    }

    private void OnNativeAdLoaded(object sender, NativeAdEventArgs e)
    {
        NativeAdData = e.nativeAd;
        Debug.Log("Get Native ads from request: " + placementId);
        manager.manager.onNativeLoaded?.Invoke(placementId, NativeAdData, true);
        NativeAdData.OnPaidEvent += (sender, eventData) =>
        {
            manager.manager.onNativePaid.Invoke(placementId, NativeAdData, eventData.AdValue);
        };
#endif
    }

#if OMNILATENT_NATIVE_ADS
    public NativeAd NativeAdData { get => nativeAdData; set => nativeAdData = value; }
#else
    public object NativeAdData { get => nativeAdData; set => nativeAdData = value; }
#endif
    public bool IsRequesting { get => isRequesting; }

}

