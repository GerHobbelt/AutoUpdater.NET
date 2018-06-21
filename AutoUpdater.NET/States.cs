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
        UnexpectedCheckProcessError,
        CheckForUpdateEventError,
        CheckForUpdateDelayed,
        UserUpdateFormError,
        UpdateCheckFailed,
        UnavailableUpdate,
        DownloadingUpdate,
        UpdateDownloadCancelled,
        UpdateDownloadCompleted,
        UpdateDownloadError,
        FileIntegrityCheckSumFailed,
        CheckSumHashAlgorithmNotSupported,

        AppCastRetrievalDone,
        AppCastRetrievalError,

        AppCastXmlInfoParseDone,
        AppCastCustomInfoParseEventDone,
        AppCastXmlInfoParseError,
        AppCastCustomInfoParseEventError,
        AppCastDataValidationDone,
        AppCastInvalidDataError,


        ApplicationExitEvent,
        ApplicationExitEventError,
        ApplicationAutoExit,
#pragma warning restore 1591
    }
}