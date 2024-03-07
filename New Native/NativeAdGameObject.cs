using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using Omnilatent.AdMob;
using UnityEngine;
using Image = UnityEngine.UI.RawImage;
using Text = TMPro.TMP_Text;

[RequireComponent(typeof(CanvasGroup))]
public class NativeAdGameObject : MonoBehaviour
{
    private static NativeAdGameObject instance;
    public static NativeAdGameObject Instance => instance;
    [SerializeField] private Image icon;
    [SerializeField] private Image choiceIcon;
    [SerializeField] private Text headLineText;
    [SerializeField] private Text advertiserText;
    [SerializeField] private Text buttonText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private NativeAdPlacement _placement;
    AdPlacement.Type currentPlacement;

    private void Awake()
    {
        _canvasGroup.alpha = 0f;
    }

    private void Start()
    {
#if UNITY_EDITOR
        ShowAdsFake();
        return;
#endif
        AdMobManager.instance.onNativeLoaded += ShowAd;
        gameObject.SetActive(false);
        AdMobManager.instance.InstanceNativeAdWrapper.LoadNativeAd((int)_placement);
    }

    private void ShowAdsFake()
    {
        string id = CustomMediation.GetAdmobID((int)_placement);
        if (AdsManager.Instance.DoNotShowAds((int)_placement))
        {
            return;
        }
        gameObject.SetActive(true);
        icon.color = Color.cyan;
        choiceIcon.color = Color.cyan;
        headLineText.text = "Native ads demo";
        advertiserText.text = "Native ads demo";
        buttonText.text = "Native ads demo";
        image.color = Color.cyan;
        _canvasGroup.alpha = 1;
    }

    private void ShowAd(AdPlacement.Type placementId, NativeAd nativeAd, bool tracking)
    {
        currentPlacement = placementId;
        if ((int)_placement != placementId && placementId != AdPlacement.Common_Native)
            return;
        if (nativeAd == null) return;
        instance = this;
        RegisterGameobject(nativeAd);
        AdMobManager.instance.onNativeLoaded -= ShowAd;
        AdMobManager.instance.onNativeShow?.Invoke((int)placementId, nativeAd);
        gameObject.SetActive(true);
        FillValue(nativeAd);
        _canvasGroup.alpha = 1;
    }

    private void FillValue(NativeAd nativeAd)
    {
        try
        {
            icon.texture = nativeAd.GetIconTexture();
        }
        catch
        {
            Debug.LogError("Exception Get Icon!");
        }
        try
        {
            choiceIcon.texture = nativeAd.GetAdChoicesLogoTexture();
        }
        catch
        {
            Debug.LogError("Exception Get choiceIcon!");
        }
        try
        {
            headLineText.text = nativeAd.GetHeadlineText();
        }
        catch
        {
            Debug.LogError("Exception Get headline!");
        }
        try
        {
            advertiserText.text = nativeAd.GetAdvertiserText();
        }
        catch
        {
            Debug.LogError("Exception Get advertiserText!");
        }
        try
        {
            buttonText.text = nativeAd.GetCallToActionText();
        }
        catch
        {
            Debug.LogError("Exception Get advertiserText!");
        }
        try
        {
            image.texture = nativeAd.GetImageTextures()[0];
        }
        catch
        {
            Debug.LogError("Exception Get image!");
        }
        try
        {
            bodyText?.SetText(nativeAd.GetBodyText());
        }
        catch
        {
            Debug.LogError("Exception Get bodyText!");
        }

        icon.color = Color.white;
        image.color = Color.white;
        choiceIcon.color = Color.white;
    }

    private void RegisterGameobject(NativeAd nativeAd)
    {
        if (choiceIcon != null && !nativeAd.RegisterAdChoicesLogoGameObject(choiceIcon.gameObject))
        {
            Debug.LogError($"Error to register {nameof(choiceIcon)}!");
        }
        if (advertiserText != null && !nativeAd.RegisterAdvertiserTextGameObject(advertiserText.gameObject))
        {
            Debug.LogError($"Error to register {nameof(advertiserText)}!");
        }
        if (bodyText != null && !nativeAd.RegisterBodyTextGameObject(bodyText.gameObject))
        {
            Debug.LogError($"Error to register {nameof(bodyText)}!");
        }
        if (buttonText != null && !nativeAd.RegisterCallToActionGameObject(buttonText.gameObject))
        {
            Debug.LogError($"Error to register {nameof(buttonText)}!");
        }
        if (headLineText != null && !nativeAd.RegisterHeadlineTextGameObject(headLineText.gameObject))
        {
            Debug.LogError($"Error to register {nameof(headLineText)}!");
        }
        if (icon != null && !nativeAd.RegisterIconImageGameObject(icon.gameObject))
        {
            Debug.LogError("Error to register icon!");
        }
        //if (!nativeAd.RegisterPriceGameObject(image.gameObject))
        //{
        //    Debug.LogError("Error to register icon!");
        //}
        //if (!nativeAd.RegisterStoreGameObject(image.gameObject))
        //{
        //    Debug.LogError("Error to register icon!");
        //}
    }

    private void OnDestroy()
    {
        AdMobManager.instance.onNativeLoaded -= ShowAd;
    }

    public void ToggleShowNative(bool show)
    {
        if (instance == null) return;
        gameObject.SetActive(show);
        _canvasGroup.alpha = show ? 1 : 0;
    }
}

[System.Serializable]
public enum NativeAdPlacement
{
    Native_Default = 500,
    Native_OnBoarding = 501,
    Native_Theme = 502,
    Native_RecordList = 503,
    Native_HomeSetting = 504,
    Native_Language = 505
}