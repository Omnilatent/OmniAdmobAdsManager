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
            AdNetwork = CustomMediation.AD_NETWORK.GoogleAdmob;
        }

        public AdmobBannerAdObject(AdPlacement.Type adPlacementType, BannerLoadDelegate onAdLoaded) : base(adPlacementType, onAdLoaded)
        {
            AdNetwork = CustomMediation.AD_NETWORK.GoogleAdmob;
        }

        public BannerView BannerView { get => bannerView; set => bannerView = value; }
    }
}