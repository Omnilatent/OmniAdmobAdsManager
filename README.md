# OmniAdmobAdsManager
Module for wrapping Admob Ads

## Dependencies

- Omni Ads Manager.
- Google Mobile Ads SDK 6.1.2.

## USAGE

`AdMobManager.TIMEOUT_LOADAD`: Time waited when loading an interstitial ad before forcing a timeout. Assign a value to change.

`AdMobManager.TIMEOUT_LOADREWARDAD`: Time waited when loading an rewarded ad before forcing a timeout. Assign a value to change.

### Caching Reward Ad (In development):

On first Request Reward Ad, Reward Ad will start loading. Loading will be toggled on. When reward ad finished loading, loading will be toggled off and result will be returned.

On successful load, next reward ad will be preloaded.

`Omnilatent.AdMob.CacheAdmobAd.MaxCacheAdAmount = x;`  
Set max amount of preloaded ad.

`Omnilatent.AdMob.CacheAdmobAd.SameAdTypeShareCache = true/false;`  
Toggling 'Same Ad Type sharing cache':  
If true, all ad placements of same type will share cache with each other. E.g. all rewarded ads will share cache.