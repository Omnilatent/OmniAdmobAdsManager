## 1.5.3
Changes:
- Add Admob Banner Ad Object to store additional banner value easier. Rewrite banner function to use new Admob Banner Ad Object. Deprecate some banner event and field.
- Add callback in request banner. Check banner state closed before showing when banner is loaded.

Fixes:
- Add using Omnilatent.AdsMediation. Fix ambiguous reference of AdPosition type.
- Fix hide banner: check banner ad object null.
- Fix onBannerPaidEvent to be call in main thread.

## 1.5.2
Fixes:
- Cache admob ad: check ad null before destroying

## 1.5.1
Changes:
- Convert all string message in Reward ad to const variable in AdmobConst to make localize easier.

Fixes:
- Fix cache container not updating app open ad when ad is loaded.
- Fix ad type null exception when ad object is null by adding ad type as a field for cached ad container.
- Add show cached open ad code.
- Fix GetReadyAd: use correct generic PreloadAd instead of PreloadRewardAd.
- CacheAdmobAd: queue main thread execution for callback when App open ad is loaded.

## 1.5.0
New features:
- Cache Admob Ad can cache multiple App Open Ad placements.
- Get cached ad method now support generic types.
Requires AdsManager 2.5.0 to work.

## 1.4.1
New features:
- Add customizable callbacks to App Open Ad.
- Add HandleOpenAdDidRecordImpression to App Open Ad.

Changes:
- Move interstitial related function and field to new file.
- Log error when 'Last Interstitial request failed. No ad to show.'. Log exception when show interstitial failed unexpectedly.
- Delete unneeded callback removal.
- Delete old cache load code.
- Make HandleInterstitialFailedToLoad obsolete.
- Keep time scale unchanged when app open ad is shown to prevent conflict with game time scale (Editor only).

## 1.4.0
New features:
- Cache reward ad: preload ad and set max amount of cached ad. Use Dictionary and Ad Container to manage cached ads.

Changes:
- Custom mediation get id usage: do not pass default ad id anymore. change method work from using same reward video variable to local reward video variable
- Reward ad: on editor always treat reward ad as success because the order of execution in editor is wrong making user never get reward.

## 1.3.1
Changes:
- Move UnityMainThreadDispatcher from Omni AdsManager to Omni Admob Manager.
- Do not queue main thread execution on iOS to avoid crash due to race condition.

## 1.3.0
Require Google Admob 6.1.0 and above.
New features:
- Added support for App Open Ad format.

## 1.2.0
Changes:
- Support Google Admob 6.0.0: Update API from RewardBasedVideoAd to RewardedAd.
- No longer support Google Admob 5.x.x.
- Update from using AdFailedToLoadEventArgs message to AdFailedToLoadEventArgs.LoadAdError. (AdsManager)
- Fix reward timeout coroutine by stopping it before loading new ad & after load successful.
- Update test Ad ID.

## 1.1.1 (2021/6/8)
- Add error message to reward result callback.
- Change timeout variable scope to public static to allow customization.

## 1.1.0 (2021/5/10)
- Implemented new ShowBanner(AdPlacement.Type, BannerTransform, OnAdLoaded) to allow displaying banner at Top of screen

## 1.0.1 (2021/3/11)
- Implemented Interstitial Rewarded format

## 1.0.0
Initial Commit