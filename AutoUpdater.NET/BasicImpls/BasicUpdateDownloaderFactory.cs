using System.Net;

namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    public class BasicFileDownloaderFactory: FileDownloaderFactory
    {
        public FileDownloader Create(DownloadPresenter presenter, bool allowCancellation, IWebProxy proxy = null)
        {
            return new BasicFileDownloader(presenter, allowCancellation, proxy);
        }
    }
#pragma warning restore 1591
}