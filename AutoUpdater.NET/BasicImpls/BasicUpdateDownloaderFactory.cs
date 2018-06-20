using System.Net;

namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    public class BasicUpdateDownloaderFactory: UpdateDownloaderFactory
    {
        public UpdateDownloader Create(DownloadPresenterFactory presenterFactory, bool allowCancellation, IWebProxy proxy = null)
        {
            var presenter = presenterFactory.Create();
            return new BasicUpdateDownloader(presenter, allowCancellation, proxy);
        }
    }
#pragma warning restore 1591
}