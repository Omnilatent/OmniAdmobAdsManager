using GoogleMobileAds.Api;
using Omnilatent.AdMob;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AdMobManager : MonoBehaviour
{
    RewardedAd rewardBasedVideo;
    Coroutine timeoutLoadRewardCoroutine;

    public Action<AdPlacement.Type, EventArgs> onRewardAdLoaded;
    public Action<AdPlacement.Type, EventArgs> onRewardAdOpening;
    public Action<AdPlacement.Type, EventArgs> onRewardAdClosed;
    public Action<AdPlacement.Type, AdErrorEventArgs> onRewardAdFailedToShow;
    public Action<AdPlacement.Type, EventArgs> onRewardAdDidRecordImpression;
    public Action<AdPlacement.Type, AdFailedToLoadEventArgs> onRewardAdFailedToLoad;
    public Action<AdPlacement.Type, AdValueEventArgs> onRewardAdPaidEvent;
    public Action<AdPlacement.Type, Reward> onRewardAdUserEarnReward;

    public static void RewardAdmob(RewardDelegate onFinish, string rewardVideoAdId = AdMobConst.REWARD_ID)
    {
        /*#if UNITY_EDITOR
                onFinish(new RewardResult(RewardResult.Type.Finished));
        #else*/
        if (AdsManager.HasNoInternet()) { onFinish(new RewardResult(RewardResult.Type.LoadFailed, "No internet connection.")); }
        else if (AdMobManager.instance != null)
        {
            AdMobManager.instance.ShowRewardBasedVideo((rewarded) =>
            {
                onFinish(rewarded);
            }, rewardVideoAdId);
        }
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

        this.rewardBasedVideo = RequestRewardBasedVideo(rewardVideoAdId);
        StopCoTimeoutLoadReward();
        timeoutLoadRewardCoroutine = StartCoroutine(CoTimeoutLoadReward(() =>
        {
            LoadAdError loadAdError = new LoadAdError(new Omnilatent.AdMob.CustomLoadAdErrorClient("Self Timeout"));
            RewardBasedVideo_OnAdFailedToLoad(null, new AdFailedToLoadEventArgs() { LoadAdError = loadAdError });
        }));
    }

    public RewardedAd RequestRewardBasedVideo(string rewardVideoAdId)
    {
        var rewardBasedVideo = new RewardedAd(rewardVideoAdId);
        AddCallbackToRewardVideo(rewardBasedVideo);
        AdRequest request = new AdRequest.Builder().Build();
        rewardBasedVideo.LoadAd(request);
        return rewardBasedVideo;
    }

    void AddCallbackToRewardVideo(RewardedAd rewardBasedVideo)
    {
        rewardBasedVideo.OnAdLoaded += RewardBasedVideo_OnAdLoaded;
        rewardBasedVideo.OnAdFailedToLoad += RewardBasedVideo_OnAdFailedToLoad;
        rewardBasedVideo.OnAdClosed += HandleRewardedAdClosed;
        rewardBasedVideo.OnUserEarnedReward += HandleUserEarnedReward;
    }

    #region Callback
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
    #endregion

    IEnumerator CoTimeoutLoadReward(Action onTimeout)
    {
        var delay = new WaitForSeconds(TIMEOUT_LOADAD);
        yield return delay;
        onTimeout.Invoke();
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
        StartCoroutine(CoWaitCachedRewardedAdLoad(placementType, onFinish));
    }

    IEnumerator CoWaitCachedRewardedAdLoad(AdPlacement.Type placementType, RewardDelegate onFinish)
    {
        CacheAdmobAd.AdStatus cacheAdState = CacheAdmobAd.AdStatus.Loading;
        RewardedAd rewardedAd = null;
        bool timedOut = false;

        StopCoTimeoutLoadReward();
        timeoutLoadRewardCoroutine = StartCoroutine(CoTimeoutLoadReward(() =>
        {
            timedOut = true;
            cacheAdState = CacheAdmobAd.AdStatus.LoadFailed;
        }));

        //Continuously check for ready cached ad. If timed out before any ads is ready then break out of checking
        bool loggedLoading = false;
        WaitForSecondsRealtime checkInterval = new WaitForSecondsRealtime(0.1f);
        do
        {
            cacheAdState = CacheAdmobAd.GetReadyRewardAd(placementType, out rewardedAd);
            if (cacheAdState == CacheAdmobAd.AdStatus.LoadSuccess)
            {
                RewardResult rewardResult = new RewardResult(RewardResult.Type.Canceled);

                rewardedAd.OnUserEarnedReward += (sender, reward) =>
                {
                    rewardResult.type = RewardResult.Type.Finished;
                    onRewardAdUserEarnReward?.Invoke(placementType, reward);
                };
                rewardedAd.OnAdClosed += (sender, e) =>
                {
                    QueueMainThreadExecution(() =>
                    {
                        this.showingAds = false;
                        onFinish.Invoke(rewardResult);
                        rewardedAd.Destroy();
                        CacheAdmobAd.CheckAdQueueSizeAndPreload(placementType);
                        onRewardAdClosed?.Invoke(placementType, e);
                    });
                };

                this.showingAds = true;
                rewardedAd.Show();
            }
            else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
            {
                break;
            }
            else
            {
                if(!loggedLoading)
                {
                    Debug.Log($"No ad of '{placementType}' is ready yet. Wating.");
                    loggedLoading = true;
                }
                yield return checkInterval; //TODO: add option to break in case game want to continue instead of waiting for ad ready
            }
        }
        while (cacheAdState == CacheAdmobAd.AdStatus.Loading);

        //No rewardedAd is ready, show message
        if (cacheAdState != CacheAdmobAd.AdStatus.LoadSuccess)
        {
            RewardResult rewardResult;
            if (timedOut)
            {
                rewardResult = new RewardResult(RewardResult.Type.LoadFailed, "Rewarded Ad self timeout.");
            }
            else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
            {
                rewardResult = new RewardResult(RewardResult.Type.LoadFailed, "Ad loads failed. Please check internet connection.");
            }
            else
            {
                rewardResult = new RewardResult(RewardResult.Type.Loading, "Rewarded Ad is loading.");
            }
            onFinish?.Invoke(rewardResult);
        }
        //.Log($"Wait ad load cacheAdState: {cacheAdState}");
    }
}
