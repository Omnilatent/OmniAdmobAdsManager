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
        private static Dictionary<AdPlacement.Type, List<CachedAdContainer>> appOpenAdsCache;

        /// <summary>
        /// Max amount of ad to be preloaded per placement
        /// </summary>
        public static int MaxCacheAdAmount { get => maxCacheAdAmount; set => maxCacheAdAmount = value; }
        static int maxCacheAdAmount = 1;

        /// <summary>
        /// If true, all ad placements of same type will share cache with each other. E.g. all rewarded ads will share cache.
        /// </summary>
        public static bool SameAdTypeShareCache { get => sameAdTypeShareCache; set => sameAdTypeShareCache = value; }
        static bool sameAdTypeShareCache = true;

        public enum AdStatus { LoadFailed = 0, Finished = 1, Canceled = 2, Loading = 3, LoadSuccess = 4 }

        public class CachedAdContainer
        {
            public AdPlacement.Type placementId;
            public AdStatus status;
            public object ad;
            Type admobType; //value can be RewardedAd or AppOpenAd

            public CachedAdContainer(AdPlacement.Type placementId, object ad, Type admobType)
            {
                this.placementId = placementId;
                this.status = AdStatus.Loading;
                this.ad = ad;
                this.admobType = admobType;
            }

            public RewardedAd GetRewardedAd() => (RewardedAd)ad;
            public AppOpenAd GetAppOpenAd() => (AppOpenAd)ad;
            public bool IsAdLoaded()
            {
                if (TypeIsAppOpenAd(admobType)) { return ad != null; }
                else if (TypeIsRewardedAd(admobType)) { return GetRewardedAd().IsLoaded(); }
                throw new Exception("Unhandled type of ad. Only App Open and Rewarded Ad is supported.");
            }
        }

        static CacheAdmobAd()
        {
            rewardAdsCache = new Dictionary<AdPlacement.Type, List<CachedAdContainer>>();
            appOpenAdsCache = new Dictionary<AdPlacement.Type, List<CachedAdContainer>>();
        }

        #region Rewarded Ad
        public static void PreloadRewardAd(AdPlacement.Type placementType)
        {
            List<CachedAdContainer> adQueue = GetCachedAdContainerList<RewardedAd>(placementType, true);
            string id = CustomMediation.GetAdmobID(placementType);
            var newAd = new RewardedAd(id);
            CachedAdContainer cacheContainer = new CachedAdContainer(placementType, newAd, typeof(RewardedAd));
            AddCallbackToRewardVideo(newAd, cacheContainer);
            adQueue.Add(cacheContainer);
            AdRequest request = new AdRequest.Builder().Build();
            newAd.LoadAd(request);
            Debug.Log($"Preload {placementType}. adQueue size {adQueue.Count}");
        }

        static void AddCallbackToRewardVideo(RewardedAd newAd, CachedAdContainer container)
        {
            newAd.OnAdLoaded += (object sender, EventArgs args) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    CheckAdQueueSizeAndPreload<RewardedAd>(container.placementId);
                    container.status = AdStatus.LoadSuccess;
                    Debug.Log($"Ad {container.placementId} loaded success");
                    AdMobManager.instance.onRewardAdLoaded?.Invoke(container.placementId, args);
                });
            };
            newAd.OnAdFailedToLoad += (object sender, AdFailedToLoadEventArgs e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    container.status = AdStatus.LoadFailed;
                    container.GetRewardedAd().Destroy();
                    //GetCachedAdContainerList(container.placementId, false).Remove(container);
                    Debug.Log($"Ad {container.placementId} loaded failed");
                    AdMobManager.instance.onRewardAdFailedToLoad?.Invoke(container.placementId, e);
                });
            };
            newAd.OnAdFailedToShow += (sender, e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    AdMobManager.instance.onRewardAdFailedToShow?.Invoke(container.placementId, e);
                });
            };
            newAd.OnAdDidRecordImpression += (sender, e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    AdMobManager.instance.onRewardAdDidRecordImpression?.Invoke(container.placementId, e);
                });
            };
            newAd.OnAdOpening += (sender, e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    AdMobManager.instance.onRewardAdOpening?.Invoke(container.placementId, e);
                });
            };
            newAd.OnPaidEvent += (sender, e) =>
            {
                AdMobManager.QueueMainThreadExecution(() =>
                {
                    AdMobManager.instance.onRewardAdPaidEvent?.Invoke(container.placementId, e);
                });
            };
        }
        #endregion

        public static void CheckAdQueueSizeAndPreload<T>(AdPlacement.Type placementId)
        {
            var containerList = GetCachedAdContainerList<T>(placementId, false);
            if (containerList.Count < maxCacheAdAmount)
            {
                PreloadAd<T>(placementId);
            }
        }

        static List<CachedAdContainer> GetCachedAdContainerList<T>(AdPlacement.Type placementType, bool initListIfNotExist)
        {
            List<CachedAdContainer> adQueue;
            if (sameAdTypeShareCache)
            {
                if (TypeIsRewardedAd(typeof(T)))
                    placementType = AdPlacement.Reward;
                else if (TypeIsAppOpenAd(typeof(T)))
                    placementType = AdPlacement.App_Open_Ad;
            }
            if (!rewardAdsCache.TryGetValue(placementType, out adQueue) && initListIfNotExist)
            {
                rewardAdsCache.Add(placementType, new List<CachedAdContainer>());
                adQueue = rewardAdsCache[placementType];
            }
            return adQueue;
        }

        [Obsolete]
        public static AdStatus GetReadyRewardAd(AdPlacement.Type placementType, out RewardedAd rewardedAd)
        {
            var adQueue = GetCachedAdContainerList<RewardedAd>(placementType, false);
            if (adQueue == null || adQueue.Count == 0)
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
                int failedCount = 0, adQueueSizeBeforeCheck = adQueue.Count;
                for (int i = adQueue.Count - 1; i >= 0; i--)
                {
                    if (adQueue[i].GetRewardedAd().IsLoaded())
                    {
                        rewardedAd = (RewardedAd)adQueue[i].ad;
                        adQueue.RemoveAt(i);
                        return AdStatus.LoadSuccess;
                    }
                    else if (adQueue[i].status == AdStatus.LoadFailed)
                    {
                        adQueue.RemoveAt(i);
                        failedCount++;
                    }
                }

                if (adQueueSizeBeforeCheck == failedCount)
                {
                    Debug.Log("All ads in queue load failed.");
                    rewardedAd = null;
                    return AdStatus.LoadFailed;
                }

                //.Log($"CacheAdmod: No ad of '{placementType}' is ready yet.");
                rewardedAd = null;
                return AdStatus.Loading;
            }
        }

        //App Open Ad
        public static void PreloadAppOpenAd(AdPlacement.Type placementType, RewardDelegate onAdLoaded)
        {
            List<CachedAdContainer> adQueue = GetCachedAdContainerList<AppOpenAd>(placementType, true);
            string id = CustomMediation.GetAdmobID(placementType);

            AppOpenAd newAd = null;
            CachedAdContainer cacheContainer = new CachedAdContainer(placementType, newAd, typeof(AppOpenAd));
            adQueue.Add(cacheContainer);

            AdRequest request = new AdRequest.Builder().Build();
            // Load an app open ad for portrait orientation
            AppOpenAd.LoadAd(id, Screen.orientation, request, (newAppOpenAd, error) =>
            {
                if (error != null)
                {
                    // Handle the error.
                    Debug.LogFormat("Failed to load the ad {1}. (reason: {0})", error.LoadAdError.GetMessage(), cacheContainer.placementId);
                    onAdLoaded?.Invoke(new RewardResult(RewardResult.Type.LoadFailed));
                    AdsManager.LogError($"[{cacheContainer.placementId}]-id:'{id}' load failed.{error.LoadAdError.GetMessage()}", cacheContainer.placementId.ToString());

                    cacheContainer.status = AdStatus.LoadFailed;
                    cacheContainer.GetAppOpenAd().Destroy();
                    //GetCachedAdContainerList(container.placementId, false).Remove(container);
                    return;
                }
                else
                {
                    cacheContainer.ad = newAppOpenAd;
                    onAdLoaded?.Invoke(new RewardResult(RewardResult.Type.Finished));
                }
            });
            Debug.Log($"Preload {placementType}. adQueue size {adQueue.Count}");
        }

        //Generic Ad Load
        static bool TypeIsRewardedAd(Type t) { return t == typeof(RewardedAd); }
        static bool TypeIsAppOpenAd(Type t) { return t == typeof(AppOpenAd); }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Type of Ad</typeparam>
        /// <param name="placementType"></param>
        /// <param name="onAdLoaded">Only implemented for App Open Ad</param>
        public static void PreloadAd<T>(AdPlacement.Type placementType, RewardDelegate onAdLoaded = null)
        {
            if (TypeIsRewardedAd(typeof(T)))
            {
                PreloadRewardAd(placementType);
            }
            else if (TypeIsAppOpenAd(typeof(T)))
            {
                PreloadAppOpenAd(placementType, onAdLoaded);
            }
        }

        public static AdStatus GetReadyAd<T>(AdPlacement.Type placementType, out T adReady) where T : class
        {
            var adQueue = GetCachedAdContainerList<T>(placementType, false);
            if (adQueue == null || adQueue.Count == 0)
            {
                Debug.Log($"CacheAdmod: Cached ad list of '{placementType}' not found. Initializing.");
                PreloadRewardAd(placementType);
                adReady = null;
                return AdStatus.Loading;
            }
            else if (adQueue.Count == 0 && AdsManager.HasNoInternet())
            {
                adReady = null;
                return AdStatus.LoadFailed;
            }
            else
            {
                int failedCount = 0, adQueueSizeBeforeCheck = adQueue.Count;
                for (int i = adQueue.Count - 1; i >= 0; i--)
                {
                    if (adQueue[i].IsAdLoaded())
                    {
                        adReady = (T)adQueue[i].ad;
                        adQueue.RemoveAt(i);
                        return AdStatus.LoadSuccess;
                    }
                    else if (adQueue[i].status == AdStatus.LoadFailed)
                    {
                        adQueue.RemoveAt(i);
                        failedCount++;
                    }
                }

                if (adQueueSizeBeforeCheck == failedCount)
                {
                    Debug.Log("All ads in queue load failed.");
                    adReady = null;
                    return AdStatus.LoadFailed;
                }

                //.Log($"CacheAdmod: No ad of '{placementType}' is ready yet.");
                adReady = null;
                return AdStatus.Loading;
            }
        }
    }
}