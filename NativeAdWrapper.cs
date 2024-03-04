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
    public Action<AdPlacement.Type, NativeAd, AdValue> onNativePaidEvent;
    public Action<AdPlacement.Type, NativeAd> onNativeLoaded;
    public Action<AdPlacement.Type, NativeAd> onNativeShow;
    public Action<AdPlacement.Type> onNativeClosed;
    public Action<AdPlacement.Type, NativeAd> onNativeUserClick;
    public Action<AdPlacement.Type> onNativeRequested;
    public Action<AdPlacement.Type> OnNativeImpression;


    private Dictionary<AdPlacement.Type, NativeAdItem> nativeAdItems;
    private AdMobManager manager;
    private NativeAdItem globalAd;
    private NativeAdConfig config;

    public NativeAdWrapper(AdMobManager manager)
    {
        this.manager = manager;
        nativeAdItems = new Dictionary<AdPlacement.Type, NativeAdItem>();
        GetRMCF();
    }

    async void GetRMCF()
    {
        await Task.Delay(50);
        var json = FirebaseRemoteConfigHelper.GetString("native_ad_config", null);
        try
        {
            config = JsonUtility.FromJson<NativeAdConfig>(json);
        }
        catch (Exception es)
        {
            Debug.LogError("Error when get <color=yellow>NativeAd Config</color> from RMCF!!!\n Reason: " + es.Message);
            Debug.Log("Use NativeAd Config default!");
            config = NativeAdConfig.Default;
        }
        globalAd = new NativeAdItem(AdPlacement.Common_Native);
        globalAd.RequestAd();
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
            var native = new NativeAdItem(placementId);
            native.RequestAd();
            nativeAdItems.Add(placementId, native);
        }
    }
}

[System.Serializable]
public class NativeAdItem
{
    private NativeAd nativeAdData;
    private NativeAdWrapper manager;
    private AdPlacement.Type placementId;
    private NativeAdConfig config;
    private bool isRequesting = false;
    private float nextTimeRefresh = 0;

    public NativeAdItem(AdPlacement.Type placementId)
    {
        this.placementId = placementId;
    }
    public void SetConfig(NativeAdConfig config)
    {
        this.config = config;
    }

    public void RequestAd()
    {
        if (nativeAdData != null)
        {
            manager.onNativeLoaded.Invoke(placementId, NativeAdData);
        }
        if (Time.time > nextTimeRefresh && nextTimeRefresh != -1)
        {
            Request(0);
        }
    }

    private void Request(int count)
    {
        isRequesting = true;
        nextTimeRefresh = Time.time + config.time_refresh;
        string unitId = CustomMediation.GetAdmobID(placementId);
        AdLoader adLoader = new AdLoader.Builder(unitId).ForNativeAd().Build();
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
                manager.onNativeClosed.Invoke(placementId);
            });
        adLoader.OnNativeAdClicked += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.onNativeUserClick.Invoke(placementId, NativeAdData);
            });
        adLoader.OnNativeAdImpression += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.OnNativeImpression.Invoke(placementId);
            });
        adLoader.OnNativeAdOpening += (a, b) =>
            AdMobManager.QueueMainThreadExecution(() =>
            {
                manager.onNativeShow.Invoke(placementId, NativeAdData);
            });
        adLoader.OnAdFailedToLoad += (a, b) => ReloadAds(b, count);
        adLoader.LoadAd(new AdRequest.Builder().Build());

    }

    private void ReloadAds(AdFailedToLoadEventArgs b, int count)
    {
        count++;
        if (count > config.number_reload)
        {
            manager.onNativeFailedToLoad.Invoke(placementId, null, b.LoadAdError);
            Request(count);
        }
        else
        {
            isRequesting = false;
        }
    }

    private void OnNativeAdLoaded(object sender, NativeAdEventArgs e)
    {
        NativeAdData = e.nativeAd;
        manager.onNativeLoaded.Invoke(placementId, NativeAdData);
    }

    public NativeAd NativeAdData { get => nativeAdData; set => nativeAdData = value; }

    public bool IsRequesting { get => isRequesting; }
}

[System.Serializable]
public class NativeAdConfig
{
    public bool fetch_on_startup;
    public int number_reload;
    public float time_refresh;

    public static readonly NativeAdConfig Default = new NativeAdConfig()
    {
        use_global = false,
        number_reload = 3,
        time_refresh = 180,
        fetch_on_startup = true
    };
}

