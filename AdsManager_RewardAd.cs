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

    public Action<AdPlacement.Type> onRewardAdLoaded;
    public Action<AdPlacement.Type> onRewardAdOpening;
    public Action<AdPlacement.Type> onRewardAdClosed;
    public Action<AdPlacement.Type, AdError> onRewardAdFailedToShow;
    public Action<AdPlacement.Type> onRewardAdDidRecordImpression;
    public Action<AdPlacement.Type, LoadAdError> onRewardAdFailedToLoad;
    public Action<AdPlacement.Type, AdValue> onRewardAdPaidEvent;
    public Action<AdPlacement.Type, Reward> onRewardAdUserEarnReward;

    #region Callback

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
        QueueMainThreadExecution(() =>
        {
            //this.reward = true;
            rewardResult.type = RewardResult.Type.Finished;
        });
    }

    void HandleUserEarnedReward(object sender, Reward e)
    {
        QueueMainThreadExecution(() =>
        {
            //this.reward = true;
            rewardResult.type = RewardResult.Type.Finished;
        });
    }

    #endregion

    IEnumerator CoTimeoutLoadReward(Action onTimeout)
    {
        if (TIMEOUT_LOADREWARDAD > 0f)
        {
            var delay = new WaitForSeconds(TIMEOUT_LOADREWARDAD);
            yield return delay;
        }

        onTimeout.Invoke();
        timeoutLoadRewardCoroutine = null;
    }

    void StopCoTimeoutLoadReward()
    {
        if (timeoutLoadRewardCoroutine != null)
        {
            StopCoroutine(timeoutLoadRewardCoroutine);
            timeoutLoadRewardCoroutine = null;
        }
    }

    public void ShowCachedRewardedAd(AdPlacement.Type placementType, RewardDelegate onFinish)
    {
        StartCoroutine(CoWaitShowCachedRewardedAd(placementType, onFinish));
    }

    IEnumerator CoWaitShowCachedRewardedAd(AdPlacement.Type placementType, RewardDelegate onFinish)
    {
        CacheAdmobAd.AdStatus cacheAdState = CacheAdmobAd.AdStatus.Loading;
        RewardedAd rewardedAd = null;
        bool timedOut = false;

        StopCoTimeoutLoadReward();
        timeoutLoadRewardCoroutine = StartCoroutine(CoTimeoutLoadReward(() =>
        {
            timedOut = true;
            // cacheAdState = CacheAdmobAd.AdStatus.Loading;
        }));

        //Continuously check for ready cached ad. If timed out before any ads is ready then break out of checking
        bool loggedLoading = false;
        WaitForSecondsRealtime checkInterval = new WaitForSecondsRealtime(0.1f);
        do
        {
            cacheAdState = CacheAdmobAd.GetReadyAd<RewardedAd>(placementType, out rewardedAd, true);
            if (cacheAdState == CacheAdmobAd.AdStatus.LoadSuccess)
            {
                RewardResult rewardResult = new RewardResult(RewardResult.Type.Canceled);

                #if UNITY_EDITOR
                rewardResult.type = RewardResult.Type.Finished;
                #endif
                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    QueueMainThreadExecution(() =>
                    {
                        this.showingAds = false;
                        onFinish.Invoke(rewardResult);
                        rewardedAd.Destroy();
                        CacheAdmobAd.CheckAdQueueSizeAndPreload<RewardedAd>(placementType);
                        onRewardAdClosed?.Invoke(placementType);
                    });
                };

                this.showingAds = true;
                rewardedAd.Show((Reward reward) =>
                {
                    rewardResult.type = RewardResult.Type.Finished;
                    QueueMainThreadExecution(() =>
                    {
                        onRewardAdUserEarnReward?.Invoke(placementType, reward);
                    });
                });
                break;
            }
            else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
            {
                break;
            }
            else if (!timedOut)
            {
                if (!loggedLoading)
                {
                    Debug.Log($"No ad of '{placementType}' is ready yet. Wating.");
                    loggedLoading = true;
                }

                yield return checkInterval; //TODO: add option to break in case game want to continue instead of waiting for ad ready
            }
        } while (cacheAdState == CacheAdmobAd.AdStatus.Loading && !timedOut);

        StopCoTimeoutLoadReward();

        //No rewardedAd is ready, show message
        if (cacheAdState != CacheAdmobAd.AdStatus.LoadSuccess)
        {
            RewardResult rewardResult = CreateLoadFailedMessage(timedOut, cacheAdState);
            onFinish?.Invoke(rewardResult);
        }
        //.Log($"Wait ad load cacheAdState: {cacheAdState}");
    }

    public void RequestRewardAd(AdPlacement.Type placementId, RewardDelegate onFinish)
    {
        StartCoroutine(CoWaitLoadCachedRewardedAd(placementId, onFinish));
    }

    IEnumerator CoWaitLoadCachedRewardedAd(AdPlacement.Type placementType, RewardDelegate onFinish)
    {
        CacheAdmobAd.AdStatus cacheAdState = CacheAdmobAd.AdStatus.Loading;
        RewardedAd rewardedAd = null;
        bool timedOut = false;

        StopCoTimeoutLoadReward();
        timeoutLoadRewardCoroutine = StartCoroutine(CoTimeoutLoadReward(() =>
        {
            timedOut = true;
            // cacheAdState = CacheAdmobAd.AdStatus.Loading;
        }));

        //Continuously check for ready cached ad. If timed out before any ads is ready then break out of checking
        bool loggedLoading = false;
        WaitForSecondsRealtime checkInterval = new WaitForSecondsRealtime(0.1f);
        do
        {
            cacheAdState = CacheAdmobAd.GetReadyAd<RewardedAd>(placementType, out rewardedAd, false);
            if (cacheAdState == CacheAdmobAd.AdStatus.LoadSuccess)
            {
                RewardResult rewardResult = new RewardResult(RewardResult.Type.Loaded);
                #if UNITY_EDITOR
                rewardResult.type = RewardResult.Type.Loaded;
                #endif
                onFinish?.Invoke(rewardResult);
                break;
            }
            else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
            {
                break;
            }
            else if (!timedOut)
            {
                if (!loggedLoading)
                {
                    Debug.Log($"No ad of '{placementType}' is ready yet. Wating.");
                    loggedLoading = true;
                }

                yield return checkInterval; //TODO: add option to break in case game want to continue instead of waiting for ad ready
            }
        } while (cacheAdState == CacheAdmobAd.AdStatus.Loading && !timedOut);

        StopCoTimeoutLoadReward();

        //No rewardedAd is ready, show message
        if (cacheAdState != CacheAdmobAd.AdStatus.LoadSuccess)
        {
            RewardResult rewardResult = CreateLoadFailedMessage(timedOut, cacheAdState);
            onFinish?.Invoke(rewardResult);
        }
        //.Log($"Wait ad load cacheAdState: {cacheAdState}");
    }

    RewardResult CreateLoadFailedMessage(bool timedOut, CacheAdmobAd.AdStatus cacheAdState)
    {
        RewardResult rewardResult;
        if (timedOut)
        {
            if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
            {
                rewardResult = new RewardResult(RewardResult.Type.LoadFailed, AdMobConst.rewardAdSelfTimeoutMsg);
            }
            else
            {
                rewardResult = new RewardResult(RewardResult.Type.Loading, AdMobConst.loadingRewardAdMsg);
            }
        }
        else if (cacheAdState == CacheAdmobAd.AdStatus.LoadFailed)
        {
            rewardResult = new RewardResult(RewardResult.Type.LoadFailed, AdMobConst.adLoadFailCheckConnectionMsg);
        }
        else
        {
            rewardResult = new RewardResult(RewardResult.Type.Loading, AdMobConst.loadingRewardAdMsg);
        }

        return rewardResult;
    }
}