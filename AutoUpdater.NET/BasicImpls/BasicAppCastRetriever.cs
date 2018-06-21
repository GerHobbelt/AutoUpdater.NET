using System;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    internal class BasicAppCastRetriever: AppCastRetriever
    {
        public AppCast Retrieve(string appCastUrl, IWebProxy proxy)
        {
            var webRequest = WebRequest.Create(appCastUrl);
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            if (proxy != null)
                webRequest.Proxy = proxy;
            var webResponse = webRequest.GetResponse();

            var appCast = new AppCast();
            try
            {
                appCast.BaseUri = webResponse.ResponseUri;
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    appCast.RemoteData = reader.ReadToEnd();
            }
            finally
            {
                try {
                    webResponse.Close();
                } catch {/*ignored*/}
            }
            return appCast;
        }
    }
#pragma warning restore 1591
}