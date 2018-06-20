namespace AutoUpdaterDotNET
{
    /// <summary>
    ///     Represents the AutoUpdate process' current state. Useful while catching reports in unattended mode.
    /// </summary>
    public enum States
    {
#pragma warning disable 1591
        CheckForUpdateStarted,
        CheckForUpdateEventError,
        CheckForUpdateDelayed,
        UserUpdateFormError,
        UpdateCheckFailed,
        UpdateUnavailable,
        DownloadingUpdate,
        UpdateDownloadCancelled,
        UpdateDownloadCompleted,
        UpdateDownloadError,
        FileIntegrityCheckSumFailed,
        CheckSumHashAlgorithmNotSupported,

        AppCastRetrievalDone,
        AppCastRetrievalError,
        AppCastReadDataError,

        AppCastXmlInfoParseDone,
        AppCastCustomInfoParseEventDone,
        AppCastXmlInfoParseError,
        AppCastCustomInfoParseEventError,
        AppCastDataValidationDone,
        AppCastInvalidDataError,


        ApplicationExitEvent,
        ApplicationExitEventError,
        ApplicationAutoExit
#pragma warning restore 1591
    }
}