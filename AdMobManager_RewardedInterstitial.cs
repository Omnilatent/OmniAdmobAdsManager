using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour, IAdsNetworkHelper
{
    private RewardedInterstitialAd rewardedInterstitialAd;
    public RewardDelegate adsInterstitialRewardedCallback; //For Interstitial Rewarded

    public void RequestInterstitialRewardedNoShow(AdPlacement.Type placementType, RewardDelegate onFinish = null)
    {
        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the rewarded ad with the request.
        string id = CustomMediation.GetAdmobID(placementType);
        if (rewardedInterstitialAd == null)
        {
            RewardedInterstitialAd.Load(id, request, (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                if (error == null)
                {
                    rewardedInterstitialAd = ad;
                    rewardedInterstitialAd.OnAdFullScreenContentFailed += HandleAdFailedToPresent;
                    rewardedInterstitialAd.OnAdFullScreenContentClosed += HandleAdDidDismiss;
                    rewardedInterstitialAd.OnAdPaid += HandlePaidEvent;
                    onFinish?.Invoke(new RewardResult(RewardResult.Type.Finished));
                }
                else
                {
                    onFinish?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, error.GetMessage()));
                }
            });
        }
        else
        {
            Debug.Log("Admob Reward Inter: An ads has already been loaded");
            onFinish?.Invoke(new RewardResult(RewardResult.Type.Finished));
        }
    }

    public void RequestInterstitialRewardedAndShow(AdPlacement.Type placementType, RewardDelegate onFinish = null)
    {
        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the rewarded ad with the request.
        string id = CustomMediation.GetAdmobID(placementType);
        RewardedInterstitialAd.Load(id, request, (RewardedInterstitialAd ad, LoadAdError error) =>
        {
            if (error == null)
            {
                adsInterstitialRewardedCallback = onFinish;

                rewardedInterstitialAd = ad;
                rewardedInterstitialAd.OnAdFullScreenContentFailed += HandleAdFailedToPresent;
                rewardedInterstitialAd.OnAdFullScreenContentClosed += HandleAdDidDismiss;
                rewardedInterstitialAd.OnAdPaid += HandlePaidEvent;

                ShowInterstitialRewarded(placementType, onFinish);
            }
            else
            {
                onFinish?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, error.GetMessage()));
            }
        });
    }

    private void HandlePaidEvent(AdValue e)
    {
        Debug.Log("Rewarded interstitial ad has received a paid event.");
    }

    private void HandleAdDidDismiss()
    {
        QueueMainThreadExecution(() =>
        {
            Debug.Log("Rewarded Interstitial Ads was dismissed");
#if UNITY_EDITOR
            adsInterstitialRewardedCallback?.Invoke(new RewardResult(RewardResult.Type.Finished, "Rewarded Interstitial Ads was dismissed"));
#else
            adsInterstitialRewardedCallback?.Invoke(new RewardResult(RewardResult.Type.Canceled, "Rewarded Interstitial Ads was dismissed"));
#endif
            adsInterstitialRewardedCallback = null;
        });
    }

    private void HandleAdFailedToPresent(AdError e)
    {
        QueueMainThreadExecution(() =>
        {
            Debug.LogError("Rewarded interstitial ad has failed to present.");
            adsInterstitialRewardedCallback?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, "Rewarded interstitial ad has failed to present."));
            adsInterstitialRewardedCallback = null;
        });
    }

    public void ShowInterstitialRewarded(AdPlacement.Type placementType, RewardDelegate onAdClosed)
    {
        if (rewardedInterstitialAd != null)
        {
            adsInterstitialRewardedCallback = onAdClosed;
            rewardedInterstitialAd.Show((Reward reward) =>
            {
                OnUserEarnedReward(onAdClosed);
            });
            rewardedInterstitialAd = null;
        }
        else
        {
            Debug.LogError("Admob Rewarded Inter Ads: No loaded ads. An ads has to be loaded before being showed");
            onAdClosed?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, "Admob Rewarded Inter Ads: No loaded ads. An ads has to be loaded before being showed"));
        }
    }

    void OnUserEarnedReward(RewardDelegate onAdClosed)
    {
        QueueMainThreadExecution(() =>
        {
            Debug.Log("Rewarded Interstitial show userEarnedReward callback");
            if (adsInterstitialRewardedCallback != null)
            {
                adsInterstitialRewardedCallback.Invoke(new RewardResult(RewardResult.Type.Finished));
                adsInterstitialRewardedCallback = null;
            }
        });
    }
}
