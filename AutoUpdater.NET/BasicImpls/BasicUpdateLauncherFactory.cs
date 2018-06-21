namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    public class BasicUpdateLauncherFactory: UpdateLauncherFactory
    {
        public UpdateLauncher Create()
        {
            return new BasicUpdateLaucher();
        }
    }
#pragma warning restore 1591
}