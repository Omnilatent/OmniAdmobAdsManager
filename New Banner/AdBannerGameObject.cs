using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using UnityEngine;
using AdPosition = GoogleMobileAds.Api.AdPosition;

public class AdBannerGameObject : MonoBehaviour
{
    public bool showBanner;
    [ShowIf("showBanner")]
    public int placementId;
    [ShowIf("showBanner")]
    public AdPosition adPosition;
    [ShowIf("showBanner")]
    public bool collapsible;
    [ShowIf("showBanner")]
    public bool adaptiveSize = true;
    [HideIf("@this.adaptiveSize || !this.showBanner")]
    public Vector2 size;
    private AdSize adSize;

    void Start()
    {
        if (showBanner)
        {
            AdMobManager.instance.onBannerLoaded += OnBannerLoad;
            AdMobManager.instance.InstanceBannerAdWrapper.OnChangeEvent.AddListener(OnChangeEvent);
            if (adaptiveSize)
            {
                adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }
            else
            {
                adSize = new AdSize((int)size.x, (int)size.y);
            }
            AdMobManager.instance.InstanceBannerAdWrapper.LoadAd((int)placementId, collapsible, adPosition, adSize);
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
