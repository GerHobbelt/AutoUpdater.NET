using System;

namespace AutoUpdaterDotNET
{
#pragma warning disable 1591
    public interface UpdateFormPresenter
    {
        RemindLaterFormat RemindLaterTimeSpan { get; set; }
        int RemindLaterAt { get; set; }
        CancellationDelegate CancellationDelegate { get; set; }

        UpdateFormResult ShowModal(string appTitle, Version currentVersion, Version installedVersion, 
                                    bool showSkipOption, bool showRemindLaterOption, string changeLogUrl);
        void Close();
    }

    public delegate void CancellationDelegate();

    public enum UpdateFormResult
    {
        Cancelled,
        Skip,
        RemindLater,
        Update
    }

    public interface UpdateFormPresenterFactory
    {
        UpdateFormPresenter Create();
    }
#pragma warning restore 1591
}