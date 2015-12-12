using System;
using System.Net;

class WebClientEx : WebClient
{
    private CookieContainer cookieContainer = new CookieContainer();

    public CookieContainer CookieContainer
    {
        get
        {
            return cookieContainer;
        }
        set
        {
            cookieContainer = value;
        }
    }

    protected override WebRequest GetWebRequest(Uri uri)
    {
        WebRequest webRequest = base.GetWebRequest(uri);

        if (webRequest is HttpWebRequest)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)webRequest;
            httpWebRequest.CookieContainer = this.cookieContainer;
        }

        return webRequest;
    }
}