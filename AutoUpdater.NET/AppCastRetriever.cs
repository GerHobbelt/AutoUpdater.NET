using System;
using System.Net;

namespace AutoUpdaterDotNET
{
#pragma warning disable 1591
    public interface AppCastRetriever
    {
        AppCast Retrieve(string appCastUrl, IWebProxy proxy);
    }

    public class AppCast
    {
        public string RemoteData;
        public Uri BaseUri;
    }
#pragma warning restore 1591
}