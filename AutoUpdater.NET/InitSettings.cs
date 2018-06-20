using System.Net;

namespace AutoUpdaterDotNET
{
    ///<summary>
    ///     Initialization settings of AutoUpdater.NET.
    /// </summary>
    public class InitSettings
    {
        internal string AppTitle { get; set; }
        internal string AppCastURL { get; set; }
        internal IWebProxy Proxy { get; set; }
        internal string DownloadPath { get; set; }
        internal bool OpenDownloadPage { get; set; }
        internal InstalledVersionProviderDelegate InstalledVersionProvider { get; set; }
        internal bool ShowSkipOption { get; set; } = true;
        internal bool ShowRemindLaterOption { get; set; } = true;
        internal bool LetUserSelectRemindLater { get; set; } = true;
        internal int RemindLaterAt { get; set; } = 1;
        internal RemindLaterFormat RemindLaterTimeSpan { get; set; } = RemindLaterFormat.Days;
        internal bool RunUpdateAsAdmin { get; set; } = true;
        internal bool Mandatory { get; set; }
        internal bool UnattendedMode { get; set; }
        internal ApplicationExitEventHandler ApplicationExitEvent { get; set; }
        internal CheckForUpdateEventHandler CheckForUpdateEvent { get; set; }
        internal ParseUpdateInfoHandler ParseUpdateInfoEvent { get; set; }

        // just for copy on Init()
        private ILogger SetLogger;
        private bool SetReportInfos;
        private bool SetReportErrors;
        private UpdateFormPresenterFactory SetUpdateFormPresenterFactory;
        private DownloadPresenterFactory SetDownloadPresenterFactory;
        private UpdateDownloaderFactory SetUpdateDownloaderFactory;

        /// <summary>
        ///     Set the Application Title shown in Update dialog. Although AutoUpdater.NET will get it automatically, you can set this property if you like to give custom Title.
        /// </summary>
        public InitSettings SetAppTitle(string t)
        {
            AppTitle = t; return this;
        }

        /// <summary>
        ///     URL of the xml file that contains information about latest version of the application.
        /// </summary>
        public InitSettings SetAppCastURL(string url)
        {
            AppCastURL = url; return this;
        }

        /// <summary>
        ///     Set Proxy server to use for all the web requests in AutoUpdater.NET.
        /// </summary>
        public InitSettings SetProxy(IWebProxy proxy)
        {
            Proxy = proxy; return this;
        }

        /// <summary>
        ///     Set it to folder path where you want to download the update file. If not provided then it defaults to Temp folder.
        /// </summary>
        public InitSettings SetDownloadPath(string path)
        {
            DownloadPath = path; return this;
        }

        /// <summary>
        ///     Will open the download url in default browser. Very usefull if you have portable application.
        /// </summary>
        public InitSettings EnableOpenDownloadPage()
        {
            OpenDownloadPage = true; return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this delegate to get the installed version, if it is not null.
        /// </summary>
        public InitSettings SetInstalledVersionProvider(InstalledVersionProviderDelegate del)
        {
            InstalledVersionProvider = del; return this;
        }

        /// <summary>
        ///     Users will not see the skip option.
        /// </summary>
        public InitSettings DisableShowSkipOption()
        {
            ShowSkipOption = false; return this;
        }

        /// <summary>
        ///     Users will not see the Remind Later option.
        /// </summary>
        public InitSettings DisableShowRemindLaterOption()
        {
            ShowRemindLaterOption = false; return this;
        }

        /// <summary>
        ///     Users will not be able to see the dialog where they can set remind later interval. 
        ///     AutoUpdater.NET will take the interval from RemindLaterAt and RemindLaterTimeSpan settings.
        /// </summary>
        public InitSettings DisableLetUserSelectRemindLater()
        {
            LetUserSelectRemindLater = false; return this;
        }

        /// <summary>
        ///     Remind Later interval after user should be reminded of update. It's default to 1 Day
        /// </summary>
        public InitSettings SetRemindLaterAt(int interval)
        {
            RemindLaterAt = interval; return this;
        }

        /// <summary>
        ///     Set if RemindLaterAt interval should be in Minutes, Hours or Days. It's default to 1 Day
        /// </summary>
        public InitSettings SetRemindLaterTimeSpan(RemindLaterFormat spanFormat)
        {
            RemindLaterTimeSpan = spanFormat; return this;
        }

        /// <summary>
        ///     Use this if your application doesn't need administrator privileges to replace the old version.
        /// </summary>
        public InitSettings DisableRunUpdateAsAdmin()
        {
            RunUpdateAsAdmin = false; return this;
        }

        ///<summary>
        ///     Use this if you want to ignore previously assigned Remind Later and Skip settings. It will also hide Remind Later and Skip options.
        /// </summary>
        public InitSettings EnableMandatory()
        {
            Mandatory = true; return this;
        }

        /// <summary>
        ///     AutoUpdater.NET will run in unattended mode.
        /// </summary>
        public InitSettings EnableUnattendedMode()
        {
            UnattendedMode = true;
            return this;
        }

        /// <summary>
        ///     An event that developers can use to exit the application gracefully.
        /// </summary>
        public InitSettings SetAnApplicationExitEventHandler(ApplicationExitEventHandler handler)
        {
            ApplicationExitEvent = handler;
            return this;
        }

        /// <summary>
        ///     An event that clients can use to be notified whenever the update is checked.
        /// </summary>
        public InitSettings SetACheckForUpdateEventHandler(CheckForUpdateEventHandler handler)
        {
            CheckForUpdateEvent = handler;
            return this;
        }

        /// <summary>
        ///     An event that clients can use to be notified whenever the AppCast file needs parsing.
        /// </summary>
        public InitSettings SetAParseUpdateInfoEventHandler(ParseUpdateInfoHandler handler)
        {
            ParseUpdateInfoEvent = handler;
            return this;
        }

        /// <summary>
        ///     AutoUpdater.NET will use this, if provided. Usefull when running in unattended mode.
        /// </summary>
        public InitSettings SetALogger(ILogger logger)
        {
            SetLogger = logger; return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will report infos. If no Logger provided, it will log infos in the app data directory.
        /// </summary>
        public InitSettings EnableReportInfos()
        {
            SetReportInfos = true;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will report errors. If no Logger provided, it will log infos in the app data directory.
        /// </summary>
        public InitSettings EnableReportErrors()
        {
            SetReportErrors = true;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate an Update Form View to let the user take decisions.
        /// </summary>
        public InitSettings SetAnUpdateFormPresenterFactory(UpdateFormPresenterFactory factory)
        {
            SetUpdateFormPresenterFactory = factory;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate an Update Download View for displaying progress.
        /// </summary>
        public InitSettings SetADownloadPresenterFactory(DownloadPresenterFactory factory)
        {
            SetDownloadPresenterFactory = factory;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate the Update Downloader.
        /// </summary>
        public InitSettings SetAnUpdateDownloaderFactory(UpdateDownloaderFactory factory)
        {
            SetUpdateDownloaderFactory = factory;
            return this;
        }

        ///<summary>
        ///     Returns a new instance of AutoUpdater.NET with these settings applied.
        /// </summary>
        public AutoUpdater Initialize()
        {
            var u = new AutoUpdater
            {
                Settings = this,
                Logger = SetLogger,
                ReportInfos = SetReportInfos,
                ReportErrors = SetReportErrors,
                UpdateFormPresenterFactory = SetUpdateFormPresenterFactory,
                DownloadPresenterFactory = SetDownloadPresenterFactory,
                UpdateDownloaderFactory = SetUpdateDownloaderFactory
            };
            AutoUpdater.Current = u;
            return u;
        }
    }


}