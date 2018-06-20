namespace AutoUpdaterDotNET.BasicImpls
{
    internal class BasicDownloadPresenterFactory : DownloadPresenterFactory
    {
        public UpdateDownloadPresenter Create() => new DownloadUpdateDialog();
    }
}