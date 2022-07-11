using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
using System.Collections;
using System.Collections.Generic;

namespace Omnilatent.AdMob
{
    public class AdObject
    {
        public AdPlacement.Type placementType;
    }

    public class AdmobBannerAdObject : BannerAdObject
    {
        BannerView bannerView;

        public AdmobBannerAdObject() : base()
        {
        }

        public AdmobBannerAdObject(AdPlacement.Type adPlacementType, AdsManager.InterstitialDelegate onAdLoaded) : base(adPlacementType, onAdLoaded)
        {
        }

        public BannerView BannerView { get => bannerView; set => bannerView = value; }
    }
}