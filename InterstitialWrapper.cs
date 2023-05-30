using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using Omnilatent.AdsMediation;
using UnityEngine;

namespace Omnilatent.AdMob
{
    public class InterstitialWrapper
    {
        private AdMobManager m_Manager;
        private InterstitialAd interstitialAd;

        Omnilatent.AdsMediation.InterstitialAdObject loadingInterstitialAdObj = new Omnilatent.AdsMediation.InterstitialAdObject();
        Omnilatent.AdsMediation.InterstitialAdObject currentInterstitialAdObj = new Omnilatent.AdsMediation.InterstitialAdObject();

        //IAdsNetworkHelper function
        public InterstitialWrapper(AdMobManager manager)
        {
            m_Manager = manager;
        }

        public void RequestInterstitialNoShow(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
        {
            string id = CustomMediation.GetAdmobID(placementId);
            loadingInterstitialAdObj.AdPlacementType = placementId;
            loadingInterstitialAdObj.State = AdObjectState.Loading;
            RequestAdmobInterstitialNoShow(id, onAdLoaded, showLoading);
        }

        /// <param name="onAdLoaded">Function to call after the ads is loaded</param>
        public void RequestAdmobInterstitialNoShow(string _adUnitId, AdsManager.InterstitialDelegate onAdLoaded = null, bool showLoading = true)
        {
            // Clean up the old ad before loading a new one.
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            Debug.Log("Loading the interstitial ad.");

            // create our request used to load the ad.
            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");
            // send the request to load the ad.
            InterstitialAd.Load(_adUnitId, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        loadingInterstitialAdObj.State = AdObjectState.LoadFailed;
                        Debug.LogError("interstitial ad failed to load an ad " + "with error : " + error);
                    }
                    else
                    {
                        loadingInterstitialAdObj.State = AdObjectState.Ready;
                        // Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                        interstitialAd = ad;
                    }

                    RegisterEventHandlers(ad, loadingInterstitialAdObj);
                    onAdLoaded?.Invoke(error == null);
                });
        }

        private void RegisterEventHandlers(InterstitialAd ad, InterstitialAdObject adObject)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                m_Manager.onInterstitialPaidEvent?.Invoke(adObject.AdPlacementType, adValue);
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                m_Manager.onInterstitialImpression?.Invoke(adObject.AdPlacementType);
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                m_Manager.onInterstitialClicked?.Invoke(adObject.AdPlacementType);
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                m_Manager.onInterstitialOpening?.Invoke(adObject.AdPlacementType);
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                m_Manager.onInterstitialClosed?.Invoke(adObject.AdPlacementType);
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                m_Manager.onInterstitialFailedToShow?.Invoke(adObject.AdPlacementType, error);
            };
        }

        //IAdsNetworkHelper function
        public void ShowInterstitial(AdPlacement.Type placementId, AdsManager.InterstitialDelegate onAdClosed)
        {
            currentInterstitialAdObj.AdPlacementType = placementId;
            ShowInterstitial(true, onAdClosed, m_Manager.cacheInterstitial, placementId.ToString());
        }
        
        /// <param name="cacheNextInter">Cache next interstitial ads</param>
        /// <param name="logOriginName">For tracking where this interstitial came from</param>
        public void ShowInterstitial(bool showLoading = true, AdsManager.InterstitialDelegate onAdClosed = null, bool cacheNextInter = false, string logOriginName = "")
        {
            if (m_Manager.noAds != null && m_Manager.noAds())
            {
                onAdClosed?.Invoke(false);
                return;
            }

            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                //An ad is loaded and ready to show
                //cacheInterstitial = cacheNextInter;
                m_Manager.LogEvent("InterstitialShow_" + logOriginName);
                m_Manager.showingAds = true;
                m_Manager.interstitialTime = m_Manager.time;
                interstitialAd.Show();
#if UNITY_EDITOR
                onAdClosed?.Invoke(true);
#endif
                return;
            }

            if (!loadingInterstitialAdObj.CanShow)
            {
                //No ad is available
                onAdClosed?.Invoke(false);
                Debug.LogError("Last Interstitial request failed. No ad to show.");
            }
            else
            {
                var e = new Exception($"Last Interstitial request of '{logOriginName}' didn't fail but no ad is ready, this should not happen.");
                Debug.LogException(e);
#if !DISABLE_FIREBASE
                FirebaseManager.LogException(e);
#endif
            }
        }
    }
}