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

        public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform, AdsManager.InterstitialDelegate onAdLoaded = null)
        {
            string id = CustomMediation.GetAdmobID(placementType);
            GoogleMobileAds.Api.AdPosition adPosition = GoogleMobileAds.Api.AdPosition.Bottom;
            if (bannerTransform.adPosition != Omnilatent.AdsMediation.AdPosition.Unset)
            {
                adPosition = (GoogleMobileAds.Api.AdPosition)bannerTransform.adPosition;
            }
            //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

            AdSize adSize = bannerTransform.adSizeData as AdSize;
            if (adSize == null) { adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth); }

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
                RequestBanner(placementType, adSize, adPosition, onAdLoaded);
            }
        }

        public void RequestBanner(AdPlacement.Type placementType, AdSize adSize, GoogleMobileAds.Api.AdPosition adPosition, AdsManager.InterstitialDelegate onAdLoaded = null)
        {
            string placementId = CustomMediation.GetAdmobID(placementType);
            if (this.currentBannerAd == null)
            {
                AdMobManager.bannerId = placementId;
                // Create a smart banner at the bottom of the screen.
                currentBannerAd = new AdmobBannerAdObject(placementType, onAdLoaded);
                currentBannerAd.BannerView = new BannerView(placementId, adSize, adPosition);

                // Load a banner ad.
                currentBannerAd.BannerView.OnBannerAdLoadFailed += OnBannerAdsFailedToLoad;
                currentBannerAd.BannerView.OnBannerAdLoaded += OnBannerAdsLoaded;
                currentBannerAd.BannerView.OnAdPaid += OnBannerPaidEvent;
                currentBannerAd.BannerView.OnAdClicked += () =>
                {
                    AdMobManager.QueueMainThreadExecution(() =>
                    {
                        m_Manager.onBannerUserClick?.Invoke(GetCurrentBannerAdObject().AdPlacementType, currentBannerAd.BannerView);
                    });
                };
                currentBannerAd.State = AdObjectState.Loading;

                var adRequest = new AdRequest();
                currentBannerAd.BannerView.LoadAd(adRequest);
                m_Manager.onBannerRequested?.Invoke(placementType);
            }
        }

        void OnBannerAdsFailedToLoad(AdError args)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                m_Manager.ShowError(args);
                GetCurrentBannerAdObject().onAdLoaded?.Invoke(false);
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

                GetCurrentBannerAdObject().onAdLoaded?.Invoke(true);
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

        public void HideBanner()
        {
            if (currentBannerAd != null && currentBannerAd.BannerView != null)
            {
                currentBannerAd.BannerView.Hide();
                currentBannerAd.State = AdObjectState.Closed;
                m_Manager.onBannerHide?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
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