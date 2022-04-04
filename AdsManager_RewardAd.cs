using GoogleMobileAds.Api;
using Omnilatent.AdMob;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour
{
    //RewardedAd rewardBasedVideo;
    Coroutine timeoutLoadRewardCoroutine;

    public static void RewardAdmob(RewardDelegate onFinish, string rewardVideoAdId = AdMobConst.REWARD_ID)
    {
#if UNITY_EDITOR
        onFinish(new RewardResult(RewardResult.Type.Finished));
#else
        if (AdsManager.HasNoInternet()) { onFinish(new RewardResult(RewardResult.Type.LoadFailed, "No internet connection.")); }
        else if (AdMobManager.instance != null)
        {
            AdMobManager.instance.ShowRewardBasedVideo((rewarded) =>
            {
                onFinish(rewarded);
            }, rewardVideoAdId);
        }
#endif
    }

    public void ShowRewardBasedVideo(RewardDelegate onVideoCompleted = null, string rewardVideoAdId = AdMobConst.REWARD_ID)
    {
        if (onVideoCompleted != null)
        {
            this.adsVideoRewardedCallback = onVideoCompleted;
        }

        //Start loading reward
        //this.reward = false;
        rewardResult = new RewardResult(RewardResult.Type.Canceled);

        if (this.rewardBasedVideo != null && this.rewardBasedVideo.IsLoaded())
        {
            this.showingAds = true;
            this.rewardBasedVideo.Show();
            return;
        }

        RequestRewardBasedVideo(rewardVideoAdId);
        StopCoTimeoutLoadReward();
        timeoutLoadRewardCoroutine = StartCoroutine(CoTimeoutLoadReward());
    }

    void RequestRewardBasedVideo(string rewardVideoAdId)
    {
        this.rewardBasedVideo = new RewardedAd(rewardVideoAdId);
        AddCallbackToRewardVideo();
        AdRequest request = new AdRequest.Builder().Build();
        this.rewardBasedVideo.LoadAd(request);
    }

    void AddCallbackToRewardVideo()
    {
        this.rewardBasedVideo.OnAdLoaded += RewardBasedVideo_OnAdLoaded;
        this.rewardBasedVideo.OnAdFailedToLoad += RewardBasedVideo_OnAdFailedToLoad;
        this.rewardBasedVideo.OnAdClosed += HandleRewardedAdClosed;
        this.rewardBasedVideo.OnUserEarnedReward += HandleUserEarnedReward;
    }

    void RewardBasedVideo_OnAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
    {
        QueueMainThreadExecution((Action)(() =>
        {
            //Finish loading, return load failed

            rewardResult.type = RewardResult.Type.LoadFailed;
            rewardResult.message = e.LoadAdError.GetMessage();
            CallVideoRewared();
            this.rewardBasedVideo.OnAdLoaded -= this.RewardBasedVideo_OnAdLoaded;
            this.rewardBasedVideo.OnAdFailedToLoad -= this.RewardBasedVideo_OnAdFailedToLoad;
            this.rewardBasedVideo.OnAdClosed -= HandleRewardedAdClosed;
            this.rewardBasedVideo.OnUserEarnedReward -= HandleUserEarnedReward;
            this.rewardBasedVideo.Destroy();
            this.rewardBasedVideo = null;
            string logMessage = $"Admob_RewardLoadFail_{e.LoadAdError.GetMessage()}";
            LogEvent(logMessage);
            Debug.Log(logMessage);
        }));
    }

    void RewardBasedVideo_OnAdLoaded(object sender, EventArgs e)
    {
        QueueMainThreadExecution((Action)(() =>
        {
            this.rewardBasedVideo.OnAdLoaded -= this.RewardBasedVideo_OnAdLoaded;
            this.rewardBasedVideo.OnAdFailedToLoad -= this.RewardBasedVideo_OnAdFailedToLoad;
            StopCoTimeoutLoadReward();
            //Manager.LoadingAnimation(false);

            ShowRewardBasedVideo();
        }));
    }

    void HandleRewardedAdClosed(object sender, EventArgs e)
    {
        QueueMainThreadExecution(() =>
        {
            this.showingAds = false;

            //if (this.reward)
            //{
            CallVideoRewared();
            //}
            //else
            //{
            //    Manager.LoadingAnimation(true);
            //    Invoke("CallVideoRewared", 2f);
            //}
        });
    }

    void CallVideoRewared()
    {
        //Manager.LoadingAnimation(false); //let common adsmanager handle

        if (adsVideoRewardedCallback != null)
        {
            adsVideoRewardedCallback(rewardResult);
            adsVideoRewardedCallback = null;
        }

        //if (Application.platform == RuntimePlatform.Android)
        //{
        //    this.RequestRewardBasedVideo(videoId);
        //}
    }

    void HandleRewardedAdLoaded(object sender, EventArgs args)
    {

    }

    void HandleVideoCompleted(object sender, EventArgs args)
    {
        //this.reward = true;
        rewardResult.type = RewardResult.Type.Finished;
    }

    void HandleUserEarnedReward(object sender, Reward e)
    {
        //this.reward = true;
        rewardResult.type = RewardResult.Type.Finished;
    }

    IEnumerator CoTimeoutLoadReward()
    {
        var delay = new WaitForSeconds(TIMEOUT_LOADAD);
        yield return delay;
        LoadAdError loadAdError = new LoadAdError(new Omnilatent.AdMob.CustomLoadAdErrorClient("Self Timeout"));
        RewardBasedVideo_OnAdFailedToLoad(null, new AdFailedToLoadEventArgs() { LoadAdError = loadAdError });
    }

    void StopCoTimeoutLoadReward()
    {
        if (timeoutLoadRewardCoroutine != null)
        {
            StopCoroutine(timeoutLoadRewardCoroutine);
            timeoutLoadRewardCoroutine = null;
        }
    }

    public void RequestRewardAd(AdPlacement.Type placementType, RewardDelegate onLoaded)
    {
        string id = CustomMediation.GetAdmobID(placementType);
        RequestRewardBasedVideo(id);
    }

    public void ShowCachedRewardedAd(AdPlacement.Type placementType, RewardDelegate onFinish)
    {
        var cacheAdState = CacheAdmobAd.GetReadyRewardAd(placementType, out var rewardedAd);
        if (cacheAdState == CacheAdmobAd.AdStatus.LoadSuccess)
        {
            RewardResult rewardResult = new RewardResult();

            rewardedAd.OnUserEarnedReward += (sender, reward) =>
            {
                rewardResult.type = RewardResult.Type.Finished;
            };
            rewardedAd.OnAdClosed += (sender, e) =>
            {
                QueueMainThreadExecution(() =>
                {
                    this.showingAds = false;
                    onFinish.Invoke(rewardResult);
                    rewardedAd.Destroy();
                    CacheAdmobAd.PreloadRewardAd(placementType);
                });
            };

            this.showingAds = true;
            rewardedAd.Show();
        }
        else if (cacheAdState == CacheAdmobAd.AdStatus.Loading)
        {
            RewardResult rewardResult = new RewardResult(RewardResult.Type.Loading, "Rewarded Ad is loading. Please wait.");
            onFinish?.Invoke(rewardResult);
        }
        else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
        {
            RewardResult rewardResult = new RewardResult(RewardResult.Type.LoadFailed, "Ad loads failed. Please check internet connection.");
            onFinish?.Invoke(rewardResult);
        }
    }
}
