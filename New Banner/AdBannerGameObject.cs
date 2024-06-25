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
    [ShowIf("showBanner")]
    public bool manualRefreshBanner = false;
    [HideIf("@this.adaptiveSize || !this.showBanner")]
    public Vector2 size;
    private AdSize adSize;


    void Start()
    {
        if (showBanner)
        {
            if (adaptiveSize)
            {
                adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }
            else
            {
                adSize = new AdSize((int)size.x, (int)size.y);
            }
            // - Banner Type check
            if (manualRefreshBanner == false)
            {
                ShowNormalBanner();
            }
            else
            {
                if (AdsConfigWraper.IsUsingManualRefreshBanner() == true)
                {
                    ShowManualRefreshBanner();
                }
                else
                {
                    ShowNormalBanner();
                    Debug.Log("Show Auto Banner");
                }
            }
        }
        else
        {
            StopShowBanner();
        }
        
    }

    void ShowNormalBanner()
    {
        AdsUltsMono.Instance.DestroyBanner();
        AdMobManager.instance.onBannerLoaded += OnBannerLoad;
        AdMobManager.instance.InstanceBannerAdWrapper.OnChangeEvent.AddListener(OnChangeEvent);
        AdMobManager.instance.InstanceBannerAdWrapper.LoadAd((int)placementId, collapsible, adPosition, adSize);
        manualRefreshBanner = false;
    }
    void ShowManualRefreshBanner()
    {
        if (AdsManager.Instance.DoNotShowAds(placementId) == false)
        {
            AdsUltsMono.Instance.ShowManualRefreshBanner(placementId, new BannerTransform(Omnilatent.AdsMediation.AdPosition.Bottom) { adSizeData = adSize });
            AdMobManager.instance.InstanceBannerAdWrapper.HideBannerObj();
            showBanner = false;
            placementId = -1;
            Debug.Log("Show Manual Banner");
        }
    }
    void StopShowBanner()
    {
        placementId = -1;
        AdMobManager.instance.InstanceBannerAdWrapper.HideBannerObj();
        AdsUltsMono.Instance.DestroyBanner();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("Destroy Banner");
            AdMobManager.instance.InstanceBannerAdWrapper.HideBannerFocus();
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
        {
            if (manualRefreshBanner == false)
            {
                AdMobManager.instance.onBannerLoaded -= OnBannerLoad;
            }
           
        }
        else
            AdMobManager.instance.InstanceBannerAdWrapper.ShowBannerObj();
    }
}
