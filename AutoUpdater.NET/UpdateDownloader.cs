using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;

namespace AutoUpdaterDotNET
{
#pragma warning disable 1591
    public interface UpdateDownloader
    {
        void Download(string fromUrl, string downloadPath, DownloadFinishHandler finishHandler);
    }

    public delegate void DownloadFinishHandler(string updateFileName, Exception error);

    public interface UpdateDownloaderFactory
    {
        UpdateDownloader Create(DownloadPresenterFactory presenterFactory, bool allowCancellation, IWebProxy proxy = null);
    }
#pragma warning restore 1591
}