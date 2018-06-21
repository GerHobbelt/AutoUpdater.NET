using System.Diagnostics;

namespace AutoUpdaterDotNET
{
#pragma warning disable 1591
    public interface UpdateLauncher
    {
        void Launch(string updateFileName, string installArguments, bool runAsAdmin);
    }

    public interface UpdateLauncherFactory
    {
        UpdateLauncher Create();
    }
#pragma warning restore 1591
}