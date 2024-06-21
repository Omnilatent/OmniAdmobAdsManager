using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class AdsConfigWraper
{
    public class AdsConfigData
    {
        public bool Is_Using_Custom_ID = false;
        public string Auto_Refresh_Banner_ID;
        public string Manual_Refresh_Banner_ID;
        public int Manual_Banner_Refresh_Rate;
        public string Interstitial_ID;
        public string RewardAd_ID;
        public string OpenAd_ID;

        // Default Value
        public AdsConfigData()
        {
            Is_Using_Custom_ID = false;
#if UNITY_ANDROID
            Auto_Refresh_Banner_ID = "ca-app-pub-8964833758670806/1786835710";
            Manual_Refresh_Banner_ID = "ca-app-pub-8964833758670806/1786835710";
            Manual_Banner_Refresh_Rate = 30;
            Interstitial_ID = "ca-app-pub-8964833758670806/1599133452";
            RewardAd_ID = "ca-app-pub-8964833758670806/2293306017";
            OpenAd_ID = "ca-app-pub-8964833758670806/5904129376";
#elif UNITY_IOS
            Auto_Refresh_Banner_ID = "ca-app-pub-7830655096475746/7162888034";
            Manual_Refresh_Banner_ID = "ca-app-pub-7830655096475746/7162888034";
            Manual_Banner_Refresh_Rate = 30;
            Interstitial_ID = "ca-app-pub-7830655096475746/2161842793";
            RewardAd_ID = "ca-app-pub-7830655096475746/7758031822";
            OpenAd_ID = "ca-app-pub-7830655096475746/1834986847";
#endif
        }
    }

    private static AdsConfigData _defaultConfigData = new AdsConfigData();
    private static AdsConfigData _adsConfigData = new AdsConfigData();
    public static AdsConfigData AdsConfig { get => _adsConfigData; }

    private static bool _isInitialized = false;

    public static void SetUpAdsConfig()
    {
        if (_isInitialized == true)
            return;
        _isInitialized = true;
        string defaultValue = JsonUtility.ToJson(_adsConfigData);
        Debug.Log("Default json Value Adconfig: " + defaultValue);
        string adConfigJson = FirebaseRemoteConfigHelper.GetString("ads_id_config", defaultValue);

        AdsConfigData configData = new AdsConfigData();
        configData = JsonUtility.FromJson<AdsConfigData>(adConfigJson);

        if (configData.Is_Using_Custom_ID == true)
        {
            _adsConfigData = configData;
            if (string.IsNullOrEmpty(configData.Auto_Refresh_Banner_ID) == true )
            {
                _adsConfigData.Auto_Refresh_Banner_ID = _defaultConfigData.Auto_Refresh_Banner_ID;
            }
            if (string.IsNullOrEmpty(configData.Manual_Refresh_Banner_ID) == true)
            {
                _adsConfigData.Manual_Refresh_Banner_ID = configData.Manual_Refresh_Banner_ID;
            }
            if (string.IsNullOrEmpty(configData.Interstitial_ID) == true)
            {
                _adsConfigData.Interstitial_ID = configData.Interstitial_ID;
            }
            if (string.IsNullOrEmpty(configData.RewardAd_ID) == true)
            {
                _adsConfigData.RewardAd_ID = configData.RewardAd_ID;
            }
            if (string.IsNullOrEmpty(configData.OpenAd_ID) == true)
            {
                _adsConfigData.OpenAd_ID = configData.OpenAd_ID;
            }
        }


        Debug.Log("Get Json ID:  " + adConfigJson);
        Debug.Log("Auto_Banner ID: " + _adsConfigData.Auto_Refresh_Banner_ID);
        Debug.Log("Manual_Banner ID: " + _adsConfigData.Manual_Refresh_Banner_ID);
        Debug.Log("Manual_Banner_Refresh_Rate: " + _adsConfigData.Manual_Banner_Refresh_Rate);
        Debug.Log("Inter ID: " + _adsConfigData.Interstitial_ID);
        Debug.Log("Reward ID " + _adsConfigData.RewardAd_ID);
        Debug.Log("OpenAD ID " + _adsConfigData.OpenAd_ID);
    }


    public static string GetBannerID()
    {
        if (AdsConfig.Manual_Banner_Refresh_Rate <=0)
        {
            return AdsConfig.Auto_Refresh_Banner_ID;
        }
        else
        {
            return AdsConfig.Manual_Refresh_Banner_ID;
        }
    }
    public static bool IsUsingManualRefreshBanner()
    {
        if (AdsConfig.Is_Using_Custom_ID == false)
            return false;
        if (AdsConfig.Manual_Banner_Refresh_Rate <= 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public static int GetBannerRefreshRate()
    {
        return AdsConfig.Manual_Banner_Refresh_Rate;
    }
    public static string GetInterstitialID()
    {
        return AdsConfig.Interstitial_ID;
    }
    public static string GetRewardID()
    {
        return AdsConfig.RewardAd_ID;
    }
    public static string GetOpenAdID()
    {
        return AdsConfig.OpenAd_ID;
    }
}
