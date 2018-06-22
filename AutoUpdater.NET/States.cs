namespace AutoUpdaterDotNET
{
    /// <summary>
    ///     Represents the AutoUpdate process' current state. Useful while catching reports in unattended mode.
    /// </summary>
    public enum States
    {
#pragma warning disable 1591
        NullEntryAssemblyError,
        StartCheckIgnoredWhileRunning,
        StartCheckIgnoredForRemindLater,
        CheckForUpdateStarted,
        CheckForUpdateDelayed,

        AppCastRetrievalDone,
        AppCastRetrievalError,
        AppCastXmlInfoParseDone,
        AppCastXmlInfoParseError,
        AppCastCustomInfoParseEventDone,
        AppCastCustomInfoParseEventError,
        AppCastDataValidationDone,
        AppCastInvalidDataError,

        UpdateCheckFailed,
        UnavailableUpdate,
        CustomUpdateCheckEventError,
        UnexpectedCheckProcessError,

        UserUpdateFormError,
        DownloadingUpdate,
        UpdateDownloadCompleted,
        UpdateDownloadCancelled,
        UpdateDownloadError,
        CheckSumHashAlgorithmNotSupported,
        FileIntegrityCheckSumFailed,
        UnexpectedUpdateProcessError,

        LaunchUpdateDone,
        LaunchUpdateError,

        ApplicationExitEvent,
        ApplicationExitEventError,
        ApplicationAutoExit,
#pragma warning restore 1591
    }
}