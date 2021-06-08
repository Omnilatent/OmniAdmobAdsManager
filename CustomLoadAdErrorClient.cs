using GoogleMobileAds.Common;
using System.Collections;
using System.Collections.Generic;

namespace Omnilatent.AdMob
{

    //For custom ad load error call
    public class CustomLoadAdErrorClient : ILoadAdErrorClient
    {
        int code = -1;
        string message;

        public CustomLoadAdErrorClient(string message)
        {
            this.message = message;
        }

        public IAdErrorClient GetCause()
        {
            return null;
        }

        public int GetCode()
        {
            return code;
        }

        public string GetDomain()
        {
            return "Omnilatent Admob Manager";
        }

        public string GetMessage()
        {
            return message;
        }

        public IResponseInfoClient GetResponseInfoClient()
        {
            return null;
        }
    }
}