using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour
{
    //RewardedAd rewardBasedVideo;

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

        //this.reward = false;
        rewardResult = new RewardResult(RewardResult.Type.Canceled);

        if (this.rewardBasedVideo.IsLoaded())
        {
            this.showingAds = true;
            this.rewardBasedVideo.Show();
            return;
        }
        //Manager.LoadingAnimation(true);

        this.rewardBasedVideo.OnAdLoaded += RewardBasedVideo_OnAdLoaded;
        this.rewardBasedVideo.OnAdFailedToLoad += RewardBasedVideo_OnAdFailedToLoad;
        RequestRewardBasedVideo(rewardVideoAdId);
        StartCoroutine(CoTimeoutLoadReward());
    }

    void RequestRewardBasedVideo(string rewardVideoAdId)
    {
        this.rewardBasedVideo.LoadAd(this.CreateAdRequest(), rewardVideoAdId);
    }

    void RewardBasedVideo_OnAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue((Action)(() =>
        {
            rewardResult.type = RewardResult.Type.LoadFailed;
            rewardResult.message = e.Message;
            CallVideoRewared();
            this.rewardBasedVideo.OnAdLoaded -= this.RewardBasedVideo_OnAdLoaded;
            this.rewardBasedVideo.OnAdFailedToLoad -= this.RewardBasedVideo_OnAdFailedToLoad;
            //Manager.LoadingAnimation(false); //common AdsManager will handle turning off loading
            LogEvent($"Admob_RewardLoadFail_{e.Message}");
            //Manager.Add(PopupController.POPUP_SCENE_NAME, new PopupData(PopupType.OK, "No video available. Please try again later."));
            Debug.Log($"Admob_RewardLoadFail_{e.Message}");
        }));
    }

    void RewardBasedVideo_OnAdLoaded(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue((Action)(() =>
        {
            this.rewardBasedVideo.OnAdLoaded -= this.RewardBasedVideo_OnAdLoaded;
            this.rewardBasedVideo.OnAdFailedToLoad -= this.RewardBasedVideo_OnAdFailedToLoad;
            //Manager.LoadingAnimation(false);

            ShowRewardBasedVideo();
        }));
    }

    void HandleRewardedAdClosed(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
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

    public void DestroyRewardBasedVideo()
    {
        if (this.rewardBasedVideo.IsLoaded())
        {
            this.rewardBasedVideo.LoadAd(this.CreateAdRequest(), string.Empty);
        }
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
        RewardBasedVideo_OnAdFailedToLoad(null, new AdFailedToLoadEventArgs() { Message = "Timeout" });
    }
}
