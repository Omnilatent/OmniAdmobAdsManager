﻿using System.Collections;
using System.Collections.Generic;

namespace Omnilatent.AdMob
{
    public class AdMobConst
    {
        #region Message Texts
        public const string rewardAdSelfTimeoutMsg = "Rewarded Ad self timeout.";
        public const string adLoadFailCheckConnectionMsg = "Ad loads failed. Please check internet connection.";
        public const string loadingRewardAdMsg = "Loading Reward Ad.";
        public const string noInternetConnectionMsg = "No internet connection.";
        #endregion

#if DEBUG_ADS
        public const string ADMOB_APP_ID = "ca-app-pub-7830655096475746~1117528011";
        public const string BANNER_ID = "ca-app-pub-3940256099942544/6300978111";
        public const string REWARD_ID = "ca-app-pub-3940256099942544/5224354917";
        public const string REWARD_SKIP_ID = "ca-app-pub-3940256099942544/5224354917";
        public const string REWARD_GET_MORE_HINT_ID = "ca-app-pub-3940256099942544/5224354917";
        public const string INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";
        public const string INTERSTITIAL_SPLASH = "ca-app-pub-3940256099942544/1033173712";
        public const string APP_OPEN_AD = "ca-app-pub-3940256099942544/3419835294";
        public const string REWARDED_INTERSTITIAL = "ca-app-pub-3940256099942544/5354046379";
#else
        public const string ADMOB_APP_ID = "ca-app-pub-7830655096475746~1117528011";
        public const string BANNER_ID = "ca-app-pub-7830655096475746/4370038003";
        public const string REWARD_ID = "ca-app-pub-7830655096475746/8194072251";
        public const string REWARD_SKIP_ID = "ca-app-pub-7830655096475746/8194072251";
        public const string REWARD_GET_MORE_HINT_ID = "ca-app-pub-7830655096475746/3493856440";
        public const string INTERSTITIAL = "ca-app-pub-7830655096475746/5986711319";
        public const string INTERSTITIAL_SPLASH = "ca-app-pub-7830655096475746/4161429062";
        public const string APP_OPEN_AD = "";
        //same ad IDs for cached load
        //public const string Interstitial_Continue = "ca-app-pub-7830655096475746/9238897593";
        //public const string Interstitial_Endgame = "ca-app-pub-7830655096475746/9238897593";

        //Unique Ad IDs

#endif
    }
}