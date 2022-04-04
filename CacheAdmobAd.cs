using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Omnilatent.AdMob
{
    public static class CacheAdmobAd
    {
        private static Dictionary<AdPlacement.Type, List<CachedAdContainer>> rewardAdsCache;
        public static int maxCacheAdAmount = 1;

        public static Action<AdPlacement.Type, AdFailedToLoadEventArgs> onRewardAdFailedToShow;

        public enum AdStatus { LoadFailed = 0, Finished = 1, Canceled = 2, Loading = 3, LoadSuccess = 4 }

        public class CachedAdContainer
        {
            public AdPlacement.Type placementId;
            public AdStatus status;
            public object ad;

            public CachedAdContainer(AdPlacement.Type placementId, object ad)
            {
                this.placementId = placementId;
                this.status = AdStatus.Loading;
                this.ad = ad;
            }

            public RewardedAd GetRewardedAd() => (RewardedAd)ad;
        }

        static CacheAdmobAd()
        {
            rewardAdsCache = new Dictionary<AdPlacement.Type, List<CachedAdContainer>>();
        }

        public static void PreloadRewardAd(AdPlacement.Type placementType)
        {
            List<CachedAdContainer> adQueue = GetCachedAdContainerList(placementType);
            string id = CustomMediation.GetAdmobID(placementType);
            var newAd = new RewardedAd(id);
            CachedAdContainer cacheContainer = new CachedAdContainer(placementType, newAd);
            AddCallbackToRewardVideo(newAd, cacheContainer);
            adQueue.Add(cacheContainer);
            AdRequest request = new AdRequest.Builder().Build();
            newAd.LoadAd(request);
        }

        static void AddCallbackToRewardVideo(RewardedAd newAd, CachedAdContainer container)
        {
            newAd.OnAdLoaded += (object sender, EventArgs args) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    var containerList = GetCachedAdContainerList(container.placementId);
                    if (containerList.Count < maxCacheAdAmount)
                    {
                        PreloadRewardAd(container.placementId);
                    }
                    container.status = AdStatus.LoadSuccess;
                    Debug.Log($"Ad {newAd} loaded success");
                });
            };
            newAd.OnAdFailedToLoad += (object sender, AdFailedToLoadEventArgs e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    container.status = AdStatus.LoadFailed;
                    container.GetRewardedAd().Destroy();
                    GetCachedAdContainerList(container.placementId).Remove(container);
                    onRewardAdFailedToShow?.Invoke(container.placementId, e);
                    //TODO: log error to firebase
                    /*RewardResult rewardResult = new RewardResult();
                    rewardResult.type = RewardResult.Type.LoadFailed;
                    rewardResult.message = e.LoadAdError.GetMessage();
                    string logMessage = $"Admob_RewardLoadFail_{e.LoadAdError.GetMessage()}";
                    Debug.Log(logMessage);*/
                });
            };
        }

        static List<CachedAdContainer> GetCachedAdContainerList(AdPlacement.Type placementType)
        {
            List<CachedAdContainer> adQueue;
            if (!rewardAdsCache.TryGetValue(placementType, out adQueue))
            {
                rewardAdsCache.Add(placementType, new List<CachedAdContainer>());
                adQueue = rewardAdsCache[placementType];
            }
            return adQueue;
        }

        public static AdStatus GetReadyRewardAd(AdPlacement.Type placementType, out RewardedAd rewardedAd)
        {
            if (!rewardAdsCache.TryGetValue(placementType, out var adQueue) || adQueue.Count == 0)
            {
                Debug.Log($"CacheAdmod: Cached ad list of '{placementType}' not found. Initializing.");
                PreloadRewardAd(placementType);
                rewardedAd = null;
                return AdStatus.Loading;
            }
            else if (adQueue.Count == 0 && AdsManager.HasNoInternet())
            {
                rewardedAd = null;
                return AdStatus.LoadFailed;
            }
            else
            {
                for (int i = adQueue.Count - 1; i >= 0; i--)
                {
                    if (adQueue[i].GetRewardedAd().IsLoaded())
                    {
                        rewardedAd = (RewardedAd)adQueue[i].ad;
                        adQueue.RemoveAt(i);
                        return AdStatus.LoadSuccess;
                    }
                }

                Debug.Log($"CacheAdmod: No ad of '{placementType}' is ready yet.");
                rewardedAd = null;
                return AdStatus.Loading;
            }
        }
    }
}