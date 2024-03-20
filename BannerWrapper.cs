using System;
using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
using UnityEngine;

namespace Omnilatent.AdMob
{
    public class BannerWrapper
    {
        private AdMobManager m_Manager;
        private AdmobBannerAdObject currentBannerAd;

        public BannerWrapper(AdMobManager mManager)
        {
            m_Manager = mManager;
        }

        public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
            AdsManager.InterstitialDelegate onAdLoaded = null)
        {
            string id = CustomMediation.GetAdmobID(placementType);
            //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

            if (currentBannerAd != null && currentBannerAd.AdPlacementType == placementType)
            {
                onAdLoaded?.Invoke(true);
                currentBannerAd.BannerView.Show();
                currentBannerAd.State = AdObjectState.Showing;
                m_Manager.onBannerShow?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
            }
            else
            {
                //.Log(string.Format("destroying current banner({0} {1}), showing new one", AdsManager.bannerId, currentBannerSize));
                DestroyBanner();
                RequestBanner(placementType, bannerTransform, (success, adObject) => { onAdLoaded?.Invoke(success); });
            }
        }

        public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
            BannerLoadDelegate onAdLoaded = null)
        {
            string id = CustomMediation.GetAdmobID(placementType);
            //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

            var adObject = GetCachedBannerObject(placementType);

            if (adObject != null && adObject.State == AdObjectState.Closed)
            {
                onAdLoaded?.Invoke(true, adObject);
                adObject.BannerView.Show();
                adObject.State = AdObjectState.Showing;
                m_Manager.onBannerShow?.Invoke(adObject.AdPlacementType, adObject.BannerView);
            }
            else
            {
                //.Log(string.Format("destroying current banner({0} {1}), showing new one", AdsManager.bannerId, currentBannerSize));
                // DestroyBanner();
                RequestBanner(placementType, bannerTransform, onAdLoaded);
            }
        }

        public void RequestBanner(AdPlacement.Type placementType, BannerTransform bannerTransform,
            BannerLoadDelegate onAdLoaded = null)
        {
            string placementId = CustomMediation.GetAdmobID(placementType);
            var adObject = GetCachedBannerObject(placementType);
            AdMobManager.bannerId = placementId;
            // Create a smart banner at the bottom of the screen.
            GoogleMobileAds.Api.AdPosition adPosition = GoogleMobileAds.Api.AdPosition.Bottom;
            if (bannerTransform.adPosition != Omnilatent.AdsMediation.AdPosition.Unset)
            {
                adPosition = (GoogleMobileAds.Api.AdPosition)bannerTransform.adPosition;
            }

            AdSize adSize = bannerTransform.adSizeData as AdSize;
            if (adSize == null) { adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth); }

            adObject = new AdmobBannerAdObject(placementType, onAdLoaded);
            adObject.BannerView = new BannerView(placementId, adSize, adPosition);

            // Load a banner ad.
            adObject.BannerView.OnBannerAdLoadFailed += OnBannerAdsFailedToLoad;
            adObject.BannerView.OnBannerAdLoaded += OnBannerAdsLoaded;
            adObject.BannerView.OnAdPaid += OnBannerPaidEvent;
            adObject.BannerView.OnAdClicked += () =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    m_Manager.onBannerUserClick?.Invoke(GetCurrentBannerAdObject().AdPlacementType, adObject.BannerView);
                });
            };
            adObject.State = AdObjectState.Loading;

            var adRequest = new AdRequest();
            if (bannerTransform.Collapsible)
            {
                string positionStr = adPosition == GoogleMobileAds.Api.AdPosition.Top ? "top" : "bottom";
                adRequest.Extras.Add("collapsible", positionStr);
            }

            adObject.BannerView.LoadAd(adRequest);
            currentBannerAd = adObject;
            m_Manager.onBannerRequested?.Invoke(placementType);
        }

        void OnBannerAdsFailedToLoad(AdError args)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                m_Manager.ShowError(args);
                GetCurrentBannerAdObject().onAdLoaded?.Invoke(false, GetCurrentBannerAdObject());
                m_Manager.onBannerFailedToLoad?.Invoke(GetCurrentBannerAdObject().AdPlacementType, currentBannerAd.BannerView, args);
                DestroyBanner();
            });
        }

        void OnBannerAdsLoaded()
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                if (this.currentBannerAd != null && currentBannerAd.State != AdObjectState.Closed)
                {
                    currentBannerAd.BannerView.Show();
                    currentBannerAd.State = AdObjectState.Showing;
                    m_Manager.onBannerShow?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
                }

                GetCurrentBannerAdObject().onAdLoaded?.Invoke(true, GetCurrentBannerAdObject());
                m_Manager.onBannerLoaded?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
            });
        }

        void OnBannerPaidEvent(AdValue adValue)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                m_Manager.onBannerPaidEvent?.Invoke(GetCurrentBannerAdObject().AdPlacementType, currentBannerAd.BannerView, adValue);
            });
        }

        public void DestroyBanner()
        {
            if (this.currentBannerAd != null)
            {
                if (currentBannerAd.BannerView != null)
                {
                    currentBannerAd.BannerView.Destroy();
                }

                currentBannerAd.BannerView = null;
                currentBannerAd = null;
            }
        }

        public void DestroyBanner(AdPlacement.Type placementType)
        {
            var adObject = GetCachedBannerObject(placementType);

            if (adObject != null)
            {
                if (adObject.BannerView != null)
                {
                    adObject.BannerView.Destroy();
                    adObject.State = AdObjectState.None;
                }

                adObject.BannerView = null;
                adObject = null;
            }
        }

        private static AdmobBannerAdObject GetCachedBannerObject(AdPlacement.Type placementType)
        {
            AdmobBannerAdObject adObject = AdsManager.GetBannerManager().GetCachedBannerObject(placementType) as AdmobBannerAdObject;
            return adObject;
        }

        public void HideBanner()
        {
            if (currentBannerAd != null && currentBannerAd.BannerView != null)
            {
                currentBannerAd.BannerView.Hide();
                currentBannerAd.State = AdObjectState.Closed;
                m_Manager.onBannerHide?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
            }
        }

        public void HideBanner(AdPlacement.Type placementType)
        {
            var adObject = GetCachedBannerObject(placementType);

            if (adObject != null && adObject.BannerView != null)
            {
                adObject.BannerView.Hide();
                adObject.State = AdObjectState.Closed;
                m_Manager.onBannerHide?.Invoke(adObject.AdPlacementType, adObject.BannerView);
            }
        }

        private AdmobBannerAdObject GetCurrentBannerAdObject(bool makeNewIfNull = true)
        {
            if (currentBannerAd == null)
            {
                Debug.LogError("currentBannerAd is null.");
                if (makeNewIfNull)
                {
                    Debug.Log("New ad will be created");
                    currentBannerAd = new AdmobBannerAdObject();
                }
            }

            return currentBannerAd;
        }
    }
}