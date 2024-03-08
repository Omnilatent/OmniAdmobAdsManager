using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AdPosition = GoogleMobileAds.Api.AdPosition;

[System.Serializable]
public enum BannerType
{
    None = -1, Common = 100, Collapsiable = 101
}

public class AdBannerGameObject : MonoBehaviour
{
    public bool showBanner;
    [ShowIf("showBanner")]
    public BannerType placementId;
    [ShowIf("showBanner")]
    public AdPosition adPosition;
    private bool collapsible => placementId == BannerType.Collapsiable;
    void Start()
    {
        if (showBanner)
        {
            AdMobManager.instance.InstanceBannerAdWrapper.LoadAd((int)placementId, collapsible, adPosition);
            AdMobManager.instance.onBannerLoaded += OnBannerLoad;
            AdMobManager.instance.InstanceBannerAdWrapper.OnChangeEvent.AddListener(OnChangeEvent);
            return;
        }
        else
        {
            placementId = BannerType.None;
            AdMobManager.instance.InstanceBannerAdWrapper.HideBannerObj();
        }
    }

    private void OnChangeEvent()
    {
        AdMobManager.instance.InstanceBannerAdWrapper.ShowBanner((int)placementId);
    }

    private void OnBannerLoad(AdPlacement.Type placementId, BannerView view)
    {
        if (placementId == (int)this.placementId)
        {
            AdMobManager.instance.InstanceBannerAdWrapper.ShowBanner(placementId);
        }
    }

    private void OnDestroy()
    {
        if (showBanner)
            AdMobManager.instance.onBannerLoaded -= OnBannerLoad;
        else
            AdMobManager.instance.InstanceBannerAdWrapper.ShowBannerObj();
    }
}
