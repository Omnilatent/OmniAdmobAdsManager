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
    private NativeAdConfig config;
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
        globalAd.SetConfig(NativeAdConfig.Default);
        globalAd.RequestAd();
        nativeAdItems.Add(AdPlacement.Common_Native, globalAd);
    }

    public void GetRMCF()
    {
        var json = FirebaseRemoteConfigHelper.GetString("native_ad_config", null);
        config = JsonUtility.FromJson<NativeAdConfig>(json);
        if (config == null)
        {
            Debug.Log("Use NativeAd Config default!");
            config = NativeAdConfig.Default;
        }
        else
        {
            Debug.Log("Load NativeAd RMCF Success");
        }
        loadNativeConfig = true;
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
            native.SetConfig(config);
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

    public NativeAdItem(AdPlacement.Type placementId, NativeAdWrapper manager)
    {
        this.placementId = placementId;
        this.manager = manager;
    }
    public void SetConfig(NativeAdConfig config)
    {
        this.config = config;
    }

    public void RequestAd()
    {
        if (nativeAdData == null)
        {
            Request(0);
            return;
        }

        //Debug.Log("Get Native ads from cache: " + placementId);
        manager.onNativeLoaded?.Invoke(placementId, NativeAdData, false);
        if (Time.time > nextTimeRefresh && nextTimeRefresh != -1)
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
        nextTimeRefresh = Time.time + config.time_refresh;
        //Debug.Log("Begin request native: " + placementId);
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
        if (count > config.number_reload)
        {
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
        //Debug.Log("Get Native ads from request: " + placementId);
        manager.onNativeLoaded?.Invoke(placementId, NativeAdData, true);
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
        number_reload = 3,
        time_refresh = -1,
        fetch_on_startup = true
    };
}

