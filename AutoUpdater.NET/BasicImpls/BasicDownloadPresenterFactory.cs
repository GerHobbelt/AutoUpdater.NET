namespace AutoUpdaterDotNET.BasicImpls
{
    internal class BasicDownloadPresenterFactory : DownloadPresenterFactory
    {
        public DownloadPresenter Create() => new BasicDownloadUpdateDialog();
    }
}