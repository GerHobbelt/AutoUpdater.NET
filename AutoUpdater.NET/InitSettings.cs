using System;
using System.Collections;
using System.Net;
using System.Reflection;

namespace AutoUpdaterDotNET
{
    ///<summary>
    ///     Initialization settings of AutoUpdater.NET.
    /// </summary>
    public class InitSettings
    {
#pragma warning disable 1591
        public Assembly MainAssembly { get; internal set; }
        public string AppTitle { get; internal set; }
        public string AppCastURL { get; internal set; }
        public IWebProxy Proxy { get; internal set; }
        public string DownloadPath { get; internal set; }
        public bool OpenDownloadPage { get; internal set; }
        public InstalledVersionProviderDelegate InstalledVersionProvider { get; internal set; }
        public bool ShowSkipOption { get; internal set; } = true;
        public bool ShowRemindLaterOption { get; internal set; } = true;
        public bool LetUserSelectRemindLater { get; internal set; } = true;
        public int RemindLaterAt { get; internal set; } = 1;
        public RemindLaterFormat RemindLaterTimeSpan { get; internal set; } = RemindLaterFormat.Days;
        public bool RunUpdateAsAdmin { get; internal set; } = true;
        public bool Mandatory { get; internal set; }
        public bool UnattendedMode { get; internal set; }
        public bool ReportInfos
        {
            get { return ReportLevels.Get((int)ReportLevel.Info); }
            set { ReportLevels.Set((int)ReportLevel.Info, value); }
        }
        public bool ReportErrors
        {
            get { return ReportLevels.Get((int)ReportLevel.Error); }
            set { ReportLevels.Set((int)ReportLevel.Error, value); }
        }
        public UpdateFormPresenterFactory UpdateFormPresenterFactory { get; internal set; }
        public DownloadPresenterFactory DownloadPresenterFactory { get; internal set; }
        public FileDownloaderFactory FileDownloaderFactory { get; internal set; }
        public UpdateLauncherFactory UpdateLauncherFactory { get; internal set; }
        public ApplicationExitEventHandler ApplicationExitEvent { get; internal set; }
        public UpdateCheckEventHandler CustomUpdateCheckEvent { get; internal set; }
        public ParseUpdateInfoHandler ParseUpdateInfoEvent { get; internal set; }
#pragma warning restore 1591

        internal readonly BitArray ReportLevels = new BitArray(2);

        // just for copy on Init()
        internal string SetDefaultLogFolder;
        internal ILogger SetLogger;

        /// <summary>
        ///     AutoUpdater.NET will use this for version checking, instead of default that is the entry assembly.
        /// </summary>
        public InitSettings SetTheMainAssembly(Assembly assembly)
        {
            MainAssembly = assembly; return this;
        }

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
            ReportInfos = true;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will report errors. If no Logger provided, it will log infos in the app data directory.
        /// </summary>
        public InitSettings EnableReportErrors()
        {
            ReportErrors = true;
            return this;
        }

        /// <summary>
        ///     AutoUpdater.NET will use this, if provided. Usefull when running in unattended mode.
        /// </summary>
        public InitSettings SetTheDefaultLogFolder(string path)
        {
            SetDefaultLogFolder = path; return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will report infos and errors. If no Logger provided, it will log infos in the app data directory.
        /// </summary>
        public InitSettings EnableReportAll()
        {
            ReportInfos = true;
            ReportErrors = true;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate an Update Form View to let the user take decisions.
        /// </summary>
        public InitSettings SetAnUpdateFormPresenterFactory(UpdateFormPresenterFactory factory)
        {
            UpdateFormPresenterFactory = factory;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate an Update Download View for displaying progress.
        /// </summary>
        public InitSettings SetADownloadPresenterFactory(DownloadPresenterFactory factory)
        {
            DownloadPresenterFactory = factory;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate the Update Downloader.
        /// </summary>
        public InitSettings SetAnUpdateDownloaderFactory(FileDownloaderFactory factory)
        {
            FileDownloaderFactory = factory;
            return this;
        }

        ///<summary>
        ///     AutoUpdater.NET will use this factory to instantiate the Update Launcher.
        /// </summary>
        public InitSettings SetAnUpdateLauncherFactory(UpdateLauncherFactory factory)
        {
            UpdateLauncherFactory = factory;
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
        public InitSettings SetACustomUpdateCheckEventHandler(UpdateCheckEventHandler handler)
        {
            CustomUpdateCheckEvent = handler;
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


        ///<summary>
        ///     Returns a new instance of AutoUpdater.NET with these settings applied.
        /// </summary>
        public AutoUpdater Initialize()
        {
            return AutoUpdater.Current = new AutoUpdater(this);
        }
    }


#pragma warning disable 1591
    public delegate Version InstalledVersionProviderDelegate();
    public delegate void ApplicationExitEventHandler();
    public delegate void UpdateCheckEventHandler(UpdateInfoEventArgs args);
    public delegate void ParseUpdateInfoHandler(ParseUpdateInfoEventArgs args);
    public delegate void DoneDelegate();
#pragma warning restore 1591

    /// <summary>
    ///     Enum representing the remind later time span.
    /// </summary>
    public enum RemindLaterFormat
    {
        /// <summary>
        ///     Represents the time span in minutes.
        /// </summary>
        Minutes,

        /// <summary>
        ///     Represents the time span in hours.
        /// </summary>
        Hours,

        /// <summary>
        ///     Represents the time span in days.
        /// </summary>
        Days
    }
}