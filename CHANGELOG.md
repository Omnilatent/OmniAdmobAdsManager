===
1.3.0
Require Google Admob 6.1.0 and above.
New features:
- Added support for App Open Ad format.

===
1.2.0
Changes:
- Support Google Admob 6.0.0: Update API from RewardBasedVideoAd to RewardedAd.
- No longer support Google Admob 5.x.x.
- Update from using AdFailedToLoadEventArgs message to AdFailedToLoadEventArgs.LoadAdError. (AdsManager)
- Fix reward timeout coroutine by stopping it before loading new ad & after load successful.
- Update test Ad ID.

===
1.1.1 (2021/6/8)
- Add error message to reward result callback.
- Change timeout variable scope to public static to allow customization.

===
1.1.0 (2021/5/10)
- Implemented new ShowBanner(AdPlacement.Type, BannerTransform, OnAdLoaded) to allow displaying banner at Top of screen

===
1.0.1 (2021/3/11)
- Implemented Interstitial Rewarded format

===
1.0.0
Initial Commit