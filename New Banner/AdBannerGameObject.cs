using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
//using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AdPosition = GoogleMobileAds.Api.AdPosition;

public class AdBannerGameObject : MonoBehaviour
{
    public bool showBanner;
    //[ShowIf("showBanner")]
    public int placementId;
    //[ShowIf("showBanner")]
    public AdPosition adPosition;
    public bool collapsible;
    void Start()
    {
        if (showBanner)
        {
            AdMobManager.instance.onBannerLoaded += OnBannerLoad;
            AdMobManager.instance.InstanceBannerAdWrapper.OnChangeEvent.AddListener(OnChangeEvent);
            AdMobManager.instance.InstanceBannerAdWrapper.LoadAd((int)placementId, collapsible, adPosition);
            return;
        }
        else
        {
            placementId = -1;
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
