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
            return;
        }
        else
        {
            placementId =  BannerType.None;
            AdMobManager.instance.InstanceBannerAdWrapper.HideAll();
        }
    }

    private void OnBannerLoad(AdPlacement.Type placementId, BannerView view)
    {
        if (placementId == (int)this.placementId)
        {
            AdMobManager.instance.InstanceBannerAdWrapper.ShowBanner(placementId);
        }
    }

    private void OnDisable()
    {
        AdMobManager.instance.onBannerLoaded -= OnBannerLoad;
#if UNITY_IOS
        AdMobManager.instance.onInterstitialClosed -= (a, b) => Start();
        AdMobManager.instance.onAOAdDidPresentFullScreenContent -= (a, b) => Start();
#endif
    }

    private void OnEnable()
    {
        AdMobManager.instance.onBannerLoaded += OnBannerLoad;
#if UNITY_IOS
        AdMobManager.instance.onInterstitialClosed += (a, b) => Start();
        AdMobManager.instance.onAOAdDidPresentFullScreenContent += (a, b) => Start();
#endif
    }

    private void OnDestroy()
    {
        AdMobManager.instance.onBannerLoaded -= OnBannerLoad;
#if UNITY_IOS
        AdMobManager.instance.onInterstitialClosed -= (a, b) => Start();
        AdMobManager.instance.onAOAdDidPresentFullScreenContent -= (a, b) => Start();
#endif
    }


    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            Start();
        }
        else
        {
            AdMobManager.instance.InstanceBannerAdWrapper.HideAll();
        }
    }
}
