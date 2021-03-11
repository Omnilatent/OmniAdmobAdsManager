using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour
{
    private RewardedInterstitialAd rewardedInterstitialAd;
    public RewardDelegate adsInterstitialRewardedCallback; //For Interstitial Rewarded

    public void RequestInterstitialRewardedNoShow(AdPlacement.Type placementType, RewardDelegate onFinish = null)
    {
        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the rewarded ad with the request.
        string id = CustomMediation.GetAdmobID(placementType, "");
        RewardedInterstitialAd.LoadAd(id, request, (RewardedInterstitialAd ad, string error) =>
        {
            if (error == null)
            {
                adsInterstitialRewardedCallback = onFinish;

                rewardedInterstitialAd = ad;
                rewardedInterstitialAd.OnAdFailedToPresentFullScreenContent += HandleAdFailedToPresent;
                rewardedInterstitialAd.OnAdDidDismissFullScreenContent += HandleAdDidDismiss;
                rewardedInterstitialAd.OnPaidEvent += HandlePaidEvent;

                ShowRewardedInterstitialAd();
            }
            else
            {
                onFinish?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, error));
            }
        });
    }

    private void HandlePaidEvent(object sender, AdValueEventArgs e)
    {
        Debug.Log("Rewarded interstitial ad has received a paid event.");
    }

    private void HandleAdDidDismiss(object sender, EventArgs e)
    {
        adsInterstitialRewardedCallback?.Invoke(new RewardResult(RewardResult.Type.Canceled, "User has canceled"));
    }

    private void HandleAdFailedToPresent(object sender, AdErrorEventArgs e)
    {
        Debug.LogError("Rewarded interstitial ad has failed to present.");
        adsInterstitialRewardedCallback?.Invoke(new RewardResult(RewardResult.Type.LoadFailed, "Rewarded interstitial ad has failed to present."));
    }

    public void ShowRewardedInterstitialAd()
    {
        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Show((Reward reward) =>
            {
                if (adsInterstitialRewardedCallback != null)
                {
                    adsInterstitialRewardedCallback.Invoke(new RewardResult(RewardResult.Type.Finished));
                }
            });
        }
    }
}
