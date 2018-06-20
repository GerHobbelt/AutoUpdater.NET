using AutoUpdaterDotNET.BasicImpls;

namespace AutoUpdaterDotNET
{
#pragma warning disable 1591
    public interface UpdateDownloadPresenter
    {
        AllowCancellationDelegate AllowCancellationDelegate { get; set; }

        void ShowModal();
        void DownloadProgressChanged(long bytesReceived, long totalBytesToReceive);
        void Close();
    }

    public delegate bool AllowCancellationDelegate();

    public interface DownloadPresenterFactory
    {
        UpdateDownloadPresenter Create();
    }
#pragma warning restore 1591
}