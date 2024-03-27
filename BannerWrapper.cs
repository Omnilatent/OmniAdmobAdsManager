using System;
using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
using UnityEngine;

namespace Omnilatent.AdMob
{
    public class BannerWrapper
    {
        private AdMobManager m_Manager;
        // private AdmobBannerAdObject currentBannerAd;

        public BannerWrapper(AdMobManager mManager)
        {
            m_Manager = mManager;
        }

        public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
            AdsManager.InterstitialDelegate onAdLoaded = null)
        {
            BannerAdObject adObject = GetCachedBannerObject(placementType);
            ShowBanner(placementType, bannerTransform, ref adObject, (success, loadedAdObject) => onAdLoaded?.Invoke(success));
        }
        
        /*public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
            AdsManager.InterstitialDelegate onAdLoaded = null)
        {
            string id = CustomMediation.GetAdmobID(placementType);
            //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

            AdmobBannerAdObject adObject = GetCachedBannerObject(placementType);
            if (adObject != null && adObject.CanShow)
            {
                onAdLoaded?.Invoke(true);
                adObject.BannerView.Show();
                // currentBannerAd.State = AdObjectState.Showing;
                m_Manager.onBannerShow?.Invoke(adObject.AdPlacementType, adObject.BannerView);
            }
            else
            {
                //.Log(string.Format("destroying current banner({0} {1}), showing new one", AdsManager.bannerId, currentBannerSize));
                DestroyBanner(placementType);
                AdmobBannerAdObject newAdObject = new AdmobBannerAdObject(placementType, null);
                AdsManager.GetBannerManager().SetCachedBannerObject(placementType, newAdObject);
                BannerAdObject bannerAdObject = newAdObject;
                RequestBanner(placementType, bannerTransform, ref bannerAdObject, (success, adObject) => { onAdLoaded?.Invoke(success); });
            }
        }*/

        public void ShowBanner(AdPlacement.Type placementType, Omnilatent.AdsMediation.BannerTransform bannerTransform,
            ref BannerAdObject bannerAdObject,
            BannerLoadDelegate onAdLoaded = null)
        {
            // string id = CustomMediation.GetAdmobID(placementType);
            //ShowBanner(id, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), adPosition, 0f, onAdLoaded);

            // var bannerAdObject = GetCachedBannerObject(placementType);
            AdmobBannerAdObject adObject = bannerAdObject as AdmobBannerAdObject;

            if (adObject != null)
            {
                if (adObject.CanShow)
                {
                    onAdLoaded?.Invoke(true, adObject);
                    adObject.BannerView.Show();
                    // adObject.State = AdObjectState.Showing;
                    m_Manager.onBannerShow?.Invoke(adObject.AdPlacementType, adObject.BannerView);
                }
                else if (AdsMediation.AdObject.NeedReload(adObject.State))
                {
                    RequestBannerThenShow(ref bannerAdObject);
                }
            }
            else
            {
                RequestBannerThenShow(ref bannerAdObject);
            }

            void RequestBannerThenShow(ref BannerAdObject refBannerAdObject)
            {
                var admobBannerObj = refBannerAdObject as AdmobBannerAdObject;
                RequestBanner(placementType, bannerTransform, ref refBannerAdObject, (success, loadedAdObject) =>
                {
                    if (admobBannerObj != null && admobBannerObj.State != AdObjectState.Closed)
                    {
                        admobBannerObj.BannerView.Show();
                        // admobBannerObj.State = AdObjectState.Showing;
                        m_Manager.onBannerShow?.Invoke(admobBannerObj.AdPlacementType, admobBannerObj.BannerView);
                    }

                    onAdLoaded?.Invoke(success, loadedAdObject);
                });
            }
        }

        public void RequestBanner(AdPlacement.Type placementType, BannerTransform bannerTransform, ref BannerAdObject bannerAdObject,
            BannerLoadDelegate onAdLoaded = null)
        {
            string placementId = CustomMediation.GetAdmobID(placementType);
            AdMobManager.bannerId = placementId;
            // Create a smart banner at the bottom of the screen.
            GoogleMobileAds.Api.AdPosition adPosition = GoogleMobileAds.Api.AdPosition.Bottom;
            if (bannerTransform.adPosition != Omnilatent.AdsMediation.AdPosition.Unset)
            {
                adPosition = (GoogleMobileAds.Api.AdPosition)bannerTransform.adPosition;
            }

            AdSize adSize = bannerTransform.adSizeData as AdSize;
            if (adSize == null) { adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth); }

            AdmobBannerAdObject adObject = bannerAdObject as AdmobBannerAdObject;
            if (adObject == null)
            {
                // bannerAdObject is not of type AdmobBannerAdObject, creating new AdmobBannerAdObject and overwrite original bannerAdObject
                adObject = new AdmobBannerAdObject(placementType, onAdLoaded);
                bannerAdObject = adObject;
            }
            else
            {
                adObject.onAdLoaded = onAdLoaded;
            }

            // AdsManager.GetBannerManager().SetCachedBannerObject(placementType, adObject);
            adObject.BannerView = new BannerView(placementId, adSize, adPosition);

            // Load a banner ad.
            adObject.BannerView.OnBannerAdLoadFailed += error => OnBannerAdsFailedToLoad(adObject, error);
            adObject.BannerView.OnBannerAdLoaded += () => OnBannerAdsLoaded(adObject);
            adObject.BannerView.OnAdPaid += value => OnBannerPaidEvent(adObject, value);
            adObject.BannerView.OnAdClicked += () =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    m_Manager.onBannerUserClick?.Invoke(adObject.AdPlacementType, adObject.BannerView);
                });
            };
            // adObject.State = AdObjectState.Loading;

            var adRequest = new AdRequest();
            if (bannerTransform.Collapsible)
            {
                string positionStr = adPosition == GoogleMobileAds.Api.AdPosition.Top ? "top" : "bottom";
                adRequest.Extras.Add("collapsible", positionStr);
            }

            adObject.BannerView.LoadAd(adRequest);
            // currentBannerAd = adObject;
            m_Manager.onBannerRequested?.Invoke(placementType);
        }

        void OnBannerAdsFailedToLoad(AdmobBannerAdObject sender, AdError args)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                m_Manager.ShowError(args);
                sender.onAdLoaded?.Invoke(false, sender);
                m_Manager.onBannerFailedToLoad?.Invoke(sender.AdPlacementType, sender.BannerView, args);
                DestroyBanner(sender.AdPlacementType);
            });
        }

        void OnBannerAdsLoaded(AdmobBannerAdObject sender)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                /*if (this.currentBannerAd != null && currentBannerAd.State != AdObjectState.Closed)
                {
                    currentBannerAd.BannerView.Show();
                    currentBannerAd.State = AdObjectState.Showing;
                    m_Manager.onBannerShow?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
                }*/

                sender.onAdLoaded?.Invoke(true, sender);
                m_Manager.onBannerLoaded?.Invoke(sender.AdPlacementType, sender.BannerView);
            });
        }

        void OnBannerPaidEvent(AdmobBannerAdObject sender, AdValue adValue)
        {
            AdMobManager.QueueMainThreadExecution(() =>
            {
                m_Manager.onBannerPaidEvent?.Invoke(sender.AdPlacementType, sender.BannerView, adValue);
            });
        }

        /*public void DestroyBanner()
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
        }*/

        public void DestroyBanner(AdPlacement.Type placementType)
        {
            var adObject = GetCachedBannerObject(placementType);

            if (adObject != null)
            {
                if (adObject.BannerView != null)
                {
                    adObject.BannerView.Destroy();
                    // adObject.State = AdObjectState.None;
                }

                adObject.BannerView = null;
                adObject = null;
            }
        }

        private static AdmobBannerAdObject GetCachedBannerObject(AdPlacement.Type placementType)
        {
            AdmobBannerAdObject adObject =
                AdsManager.GetBannerManager().GetCachedBannerObject(placementType, CustomMediation.AD_NETWORK.GoogleAdmob) as
                    AdmobBannerAdObject;
            return adObject;
        }

        /*public void HideBanner()
        {
            if (currentBannerAd != null && currentBannerAd.BannerView != null)
            {
                currentBannerAd.BannerView.Hide();
                // currentBannerAd.State = AdObjectState.Closed;
                m_Manager.onBannerHide?.Invoke(currentBannerAd.AdPlacementType, currentBannerAd.BannerView);
            }
        }*/

        public void HideBanner(AdPlacement.Type placementType)
        {
            var adObject = GetCachedBannerObject(placementType);

            if (adObject != null && adObject.BannerView != null)
            {
                adObject.BannerView.Hide();
                // adObject.State = AdObjectState.Closed;
                m_Manager.onBannerHide?.Invoke(adObject.AdPlacementType, adObject.BannerView);
            }
        }

        /*private AdmobBannerAdObject GetCurrentBannerAdObject(bool makeNewIfNull = true)
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
        }*/
    }
}