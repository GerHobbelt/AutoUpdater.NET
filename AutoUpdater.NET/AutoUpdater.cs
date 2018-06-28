using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using AutoUpdaterDotNET.Properties;
using Microsoft.Win32;
using System.Text;
using AutoUpdaterDotNET.BasicImpls;

namespace AutoUpdaterDotNET
{
    /// <summary>
    ///     Main class that lets you auto update applications by setting some static fields and executing its Start method.
    /// </summary>
    public class AutoUpdater
    {
        private static readonly object _initLocker = new object();
        private static volatile AutoUpdater _current = null;

        private readonly SynchronizationContext _context;
        private InnerLogger _innerLogger;
        private bool _initialized;
        private DoneDelegate _done;
        private bool _isWinFormsApplication;
        private bool _downloadAndRunWasCall;
        private System.Timers.Timer _remindLaterTimer;

        internal AppCastRetriever AppCastRetriever;
        internal bool DontExit;

#pragma warning disable 1591
        public InitSettings Settings { get; internal set; }
        public ILogger Logger
        {
            get { return _innerLogger.OutterLogger; }
            set { _innerLogger.OutterLogger = value; }
        }

        public bool Running { get; internal set; }
        public string RegistryLocation { get; internal set; }
        public Version InstalledVersion { get; internal set; }
        public Version CurrentVersion { get; internal set; }
        public string DownloadURL { get; internal set; }
        public string ChangelogURL { get; internal set; }
        public string Checksum { get; internal set; }
        public string HashingAlgorithm { get; internal set; }
        public string InstallerArgs { get; internal set; }
#pragma warning restore 1591

        /// <summary>
        ///     Current active AutoUpdater.NET instance.
        /// </summary>
        public static AutoUpdater Current
        {
            get
            {
                //if (_current != null) return _current;
                lock (_initLocker)
                    return _current ?? (_current = new AutoUpdater());
            }
            set
            {
                lock (_initLocker)
                    if (_current == null || !_current.Running)
                        _current = value;
            }
        }

        /// <summary>
        ///     AutoUpdater.NET custom initialization. Will not be applied if current instance is running.
        /// </summary>
        public static InitSettings InitSettings => new InitSettings();

        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="done">Call when it is done.</param>
        public static void Start(DoneDelegate done = null)
        {
            Current.StartChecking(done: done);
        }

        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="appCast">URL of the xml file that contains information about latest version of the application.</param>
        /// <param name="done">Call when it is done.</param>
        public static void Start(string appCast, DoneDelegate done = null)
        {
            Current.StartChecking(appCast, done);
        }

#pragma warning disable 1591
        internal AutoUpdater(InitSettings settings = null)
        {
            Settings = settings ?? new InitSettings();
            Settings.UpdateFormPresenterFactory = settings?.UpdateFormPresenterFactory ?? new BasicUpdateFormPresenterFactory();
            Settings.DownloadPresenterFactory = settings?.DownloadPresenterFactory ?? new BasicDownloadPresenterFactory();
            Settings.FileDownloaderFactory = settings?.FileDownloaderFactory ?? new BasicFileDownloaderFactory();
            Settings.UpdateLauncherFactory = settings?.UpdateLauncherFactory ?? new BasicUpdateLauncherFactory();
            AppCastRetriever = new BasicAppCastRetriever();
            _context = SynchronizationContext.Current;

            Initialize();
        }

        internal void Initialize()
        {
            _initialized = false;
            var reportLevels = Settings.ReportLevels;
            _innerLogger = new InnerLogger(rl => reportLevels.Get((int) rl), Settings.SetDefaultLogFolder);
            if(Settings.SetLogger != null)
                _innerLogger.OutterLogger = Settings.SetLogger;

            if (Settings.MainAssembly == null)
                Settings.MainAssembly = Assembly.GetEntryAssembly();
            if (Settings.MainAssembly == null)
            {
                Logger.Error(States.NullEntryAssemblyError);
                return;
            }

            var installedAppInfo = LoadInstalledAppInformation(Settings.MainAssembly, Settings.AppTitle, Settings.InstalledVersionProvider);
            Settings.AppTitle = installedAppInfo.AppTitle;
            InstalledVersion = installedAppInfo.InstalledVersion;
            RegistryLocation = installedAppInfo.RegistryAppInfoLocation;

            _isWinFormsApplication = Application.MessageLoop;
            _initialized = true;
        }

        internal void StartChecking(string appCast = null, DoneDelegate done = null)
        {
            if (!_initialized) { done?.Invoke(); return; }
            _done = () =>
            {
                Running = false;
                done?.Invoke();
            };
            lock (_initLocker)
            {
                if (Running) {
                    Logger.Info(States.StartCheckIgnoredWhileRunning);
                    _done?.Invoke(); return;
                }
                Running = true;
            }

            if (Settings.Mandatory && _remindLaterTimer != null)
            {
                _remindLaterTimer.Stop();
                _remindLaterTimer.Close();
                _remindLaterTimer = null;
            }
            if (_remindLaterTimer != null)
            {
                Logger.Info(States.StartCheckIgnoredForRemindLater);
                _done?.Invoke(); return;
            }

            Logger.Info(States.CheckForUpdateStarted);
            _downloadAndRunWasCall = false;

            if (appCast != null)
                Settings.AppCastURL = appCast;

            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorkerDoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;

            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        ///     Download and execute the update file.
        /// </summary>
        public void DownloadAndRunTheUpdate()
        {
            try
            {
                Logger.Info(States.DownloadingUpdate, Resources.DownloadingMessage);
                var presenter = Settings.DownloadPresenterFactory.Create();
                Settings.FileDownloaderFactory
                    .Create(presenter, !Settings.Mandatory, Settings.Proxy)
                    .Download(DownloadURL, Settings.DownloadPath, UpdateDownloadFinished);
                _downloadAndRunWasCall = true;
            }
            catch (Exception e)
            {
                Logger.Error(States.UpdateDownloadError, Resources.DownloadErrorMessage, e);
            }
        }

        protected virtual void ReportToUser(ReportLevel level, string caption, string message)
        {
            CallSync(s =>
            {
                if (Settings.UnattendedMode) return;
                if (Settings.ReportLevels.Get((int)level))
                    MessageBox.Show(message, caption, MessageBoxButtons.OK,
                        level == ReportLevel.Error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            });
        }

        private void CallSync(SendOrPostCallback action, object s = null)
        {
            if (_context == null)
                action(s);
            else
                try {
                    _context.Send(action, s);
                } catch (InvalidAsynchronousStateException) {
                    action(s);
                }
        }

        private void CallAsync(SendOrPostCallback action, object s = null)
        {
            if (_context == null)
                action(s);
            else
                try {
                    _context.Post(action, s);
                } catch (InvalidAsynchronousStateException) {
                    action(s);
                }
        }

        private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            e.Cancel = false;

            UpdateInfoEventArgs appCastInfo;
            if(!GetTheAppCastInformation(Settings.AppCastURL, Settings.Proxy, Settings.ParseUpdateInfoEvent, out appCastInfo)) return;

            CurrentVersion = appCastInfo.CurrentVersion;
            DownloadURL = appCastInfo.DownloadURL;
            ChangelogURL = appCastInfo.ChangelogURL;
            Settings.Mandatory = Settings.Mandatory || appCastInfo.Mandatory;
            InstallerArgs = appCastInfo.InstallerArgs ?? string.Empty;
            HashingAlgorithm = appCastInfo.HashingAlgorithm ?? "MD5";
            Checksum = appCastInfo.Checksum ?? string.Empty;

            if (Settings.Mandatory)
            {
                Settings.ShowRemindLaterOption = false;
                Settings.ShowSkipOption = false;
            }
            else
            {
                var regAppInfo = GetRegisteredAppInformation(RegistryLocation);
                if (regAppInfo.SkipVersions)
                {
                    if (CurrentVersion <= regAppInfo.MinimalVersion)
                    {
                        e.Cancel = true;
                        return;
                    }
                    ResetRegSkipVersion(CurrentVersion);
                }
                if (DateTime.Now.CompareTo(regAppInfo.RemindLaterTime) < 0)
                {
                    e.Result = regAppInfo.RemindLaterTime;
                    return;
                }
            }

            appCastInfo.InstalledVersion = InstalledVersion;
            appCastInfo.IsUpdateAvailable = CurrentVersion > InstalledVersion;

            e.Result = appCastInfo;
        }

        private static InstalledAppInformation LoadInstalledAppInformation(Assembly mainAssembly, string appTitle, InstalledVersionProviderDelegate installedVersionProvider)
        {
            var info = new InstalledAppInformation();
            var companyAttribute = (AssemblyCompanyAttribute)GetAttribute(mainAssembly, typeof(AssemblyCompanyAttribute));
            var appCompany = companyAttribute != null ? companyAttribute.Company : "";
            info.AppTitle = appTitle;
            if (string.IsNullOrEmpty(appTitle))
            {
                var titleAttribute = (AssemblyTitleAttribute)GetAttribute(mainAssembly, typeof(AssemblyTitleAttribute));
                info.AppTitle = titleAttribute != null ? titleAttribute.Title : mainAssembly.GetName().Name;
            }

            info.RegistryAppInfoLocation = !string.IsNullOrEmpty(appCompany)
                ? $@"Software\{appCompany}\{info.AppTitle}\AutoUpdater"
                : $@"Software\{info.AppTitle}\AutoUpdater";

            info.InstalledVersion = installedVersionProvider?.Invoke() ?? mainAssembly.GetName().Version;
            return info;
        }

        private bool GetTheAppCastInformation(string appCastUrl, IWebProxy proxy, 
            ParseUpdateInfoHandler parseUpdateInfoHandler, out UpdateInfoEventArgs info)
        {
            info = null;
            var appCast = RetrieveTheAppCast(appCastUrl, proxy);

            Logger.Info(States.AppCastRetrievalDone, $"baseUri:[{appCast.BaseUri}]\nremoteData:{appCast.RemoteData}");

            try
            {
                if (parseUpdateInfoHandler != null)
                    ExecuteUpdateCustomInfoParseEven(appCast, parseUpdateInfoHandler, out info);
                else
                    ExecuteUpdateXmlInfoParse(appCast, out info);
            }
            catch (Exception e)
            {
                throw new UpdaterException(States.AppCastInvalidDataError, exception: e);
            }

            if (info.CurrentVersion == null || string.IsNullOrEmpty(info.DownloadURL))
                throw new UpdaterException(States.AppCastInvalidDataError);

            Logger.Info(States.AppCastDataValidationDone);

            info.DownloadURL = GetURL(appCast.BaseUri, info.DownloadURL);
            info.ChangelogURL = GetURL(appCast.BaseUri, info.ChangelogURL);
            return true;
        }

        protected virtual RegisteredAppInformation GetRegisteredAppInformation(string regAppInfoKeyName)
        {
            var ri = new RegisteredAppInformation();
            using (var regAppInfoKey = Registry.CurrentUser.OpenSubKey(regAppInfoKeyName))
            {
                if (regAppInfoKey == null) return ri;
                var skipValue = regAppInfoKey.GetValue("skip");
                var minVersionValue = regAppInfoKey.GetValue("version");
                if (skipValue != null && minVersionValue != null)
                {
                    ri.SkipVersions = skipValue.ToString().Equals("1");
                    try {
                        ri.MinimalVersion = new Version(minVersionValue.ToString());
                    } catch {/*ignored*/}
                }
                var remindLaterTimeValue = regAppInfoKey.GetValue("remindlater");
                if (remindLaterTimeValue != null)
                    ri.RemindLaterTime = Convert.ToDateTime(remindLaterTimeValue.ToString(),
                                                            CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat);
            }
            return ri;
        }

        protected virtual void ResetRegSkipVersion(Version minimalVersion)
        {
            using (var updateKeyWrite = Registry.CurrentUser.CreateSubKey(RegistryLocation))
            {
                if (updateKeyWrite == null) return;
                updateKeyWrite.SetValue("version", minimalVersion.ToString());
                updateKeyWrite.SetValue("skip", 0);
            }
        }

        private static Attribute GetAttribute(Assembly assembly, Type attributeType)
        {
            var attributes = assembly.GetCustomAttributes(attributeType, false);
            if (attributes.Length == 0)
            {
                return null;
            }
            return (Attribute)attributes[0];
        }

        private AppCast RetrieveTheAppCast(string appCastUrl, IWebProxy proxy)
        {
            try {
                return AppCastRetriever.Retrieve(appCastUrl, proxy);
            }
            catch (Exception e)
            {
                throw new UpdaterException(States.AppCastRetrievalError, exception: e);
            }
        }

        private void ExecuteUpdateCustomInfoParseEven(AppCast appCast, ParseUpdateInfoHandler parseUpdateInfoHandler, out UpdateInfoEventArgs info)
        {
            info = null;
            try
            {
                var parseArgs = new ParseUpdateInfoEventArgs(appCast.RemoteData);
                CallSync(s => parseUpdateInfoHandler((ParseUpdateInfoEventArgs)s), parseArgs);
                info = parseArgs.UpdateInfo;
                Logger.Info(States.AppCastCustomInfoParseEventDone);
            }
            catch (Exception e)
            {
                throw new UpdaterException(States.AppCastCustomInfoParseEventError, exception: e);
            }
        }

        private void ExecuteUpdateXmlInfoParse(AppCast appCast, out UpdateInfoEventArgs info)
        {
            info = null;
            try
            {
                var appCastXmlDoc = new XmlDocument();
                appCastXmlDoc.LoadXml(appCast.RemoteData);
                var appCastItems = appCastXmlDoc.SelectNodes("item");
                if (appCastItems == null) return;
                info = new UpdateInfoEventArgs();
                foreach (XmlNode item in appCastItems)
                    ParseAppCastXmlInfo(item, info);
                Logger.Info(States.AppCastXmlInfoParseDone);
            }
            catch (Exception exc)
            {
                throw new UpdaterException(States.AppCastXmlInfoParseError, exception: exc);
            }
        }

        private static void ParseAppCastXmlInfo(XmlNode item, UpdateInfoEventArgs info)
        {
            try
            {
                var versionNode = item.SelectSingleNode("version");
                info.CurrentVersion = new Version(versionNode?.InnerText);
            }
            catch { info.CurrentVersion = null; }

            var appCastChangeLogNode = item.SelectSingleNode("changelog");
            info.ChangelogURL = appCastChangeLogNode?.InnerText;

            var appCastUrlNode = item.SelectSingleNode("url");
            info.DownloadURL = appCastUrlNode?.InnerText;

            var mandatoryNode = item.SelectSingleNode("mandatory");
            bool mandatory;
            info.Mandatory = bool.TryParse(mandatoryNode?.InnerText, out mandatory) && mandatory;

            var appArgsNode = item.SelectSingleNode("args");
            info.InstallerArgs = appArgsNode?.InnerText;

            var checksumNode = item.SelectSingleNode("checksum");
            info.HashingAlgorithm = checksumNode?.Attributes?["algorithm"]?.InnerText;
            info.Checksum = checksumNode?.InnerText;
        }

        private static string GetURL(Uri baseUri, string url)
        {
            if (baseUri != null && !string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Relative))
            {
                var uri = new Uri(baseUri, url);
                if (uri.IsAbsoluteUri)
                    url = uri.AbsoluteUri;
            }
            return url;
        }

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs workArgs)
        {
            try
            {
                if (workArgs.Cancelled || workArgs.Error != null)
                {
                    if (workArgs.Error == null) return;
                    var myExc = workArgs.Error as UpdaterException;
                    if (myExc != null)
                    {
                        Logger.Error(myExc.State, myExc.Message, myExc.InnerException);
                        if (myExc.State == States.AppCastRetrievalError)
                            ReportToUser(ReportLevel.Error, Resources.UpdateCheckFailedCaption, Resources.UpdateCheckFailedMessage);
                        else if (myExc.State == States.AppCastInvalidDataError)
                            ReportToUser(ReportLevel.Error, Resources.UpdateManifestInvalidCaption, Resources.UpdateManifestInvalidMessage);
                    }
                    else
                        Logger.Error(States.UnexpectedCheckProcessError, exception: workArgs.Error);
                }
                else if (workArgs.Result is DateTime)
                {
                    SetTimer((DateTime) workArgs.Result);
                }
                else
                {
                    var updateInfo = workArgs.Result as UpdateInfoEventArgs;
                    if (Settings.CustomUpdateCheckEvent != null)
                        CallSync(s =>
                        {
                            try
                            {
                                Settings.CustomUpdateCheckEvent(updateInfo);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(States.CustomUpdateCheckEventError, exception: e);
                            }
                        });
                    else
                    {
                        if (updateInfo == null)
                        {
                            Logger.Error(States.UpdateCheckFailed, Resources.UpdateCheckFailedMessage);
                            ReportToUser(ReportLevel.Error, Resources.UpdateCheckFailedCaption,
                                Resources.UpdateCheckFailedMessage);
                        }
                        else if (!updateInfo.IsUpdateAvailable)
                        {
                            Logger.Info(States.UnavailableUpdate, Resources.UpdateUnavailableMessage);
                            ReportToUser(ReportLevel.Info, Resources.UpdateUnavailableCaption,
                                Resources.UpdateUnavailableMessage);
                        }
                        else
                        {
                            if (Settings.UnattendedMode)
                                DownloadAndRunTheUpdate();
                            else
                                LetUserToProcessTheUpdate();
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Error(States.UnexpectedCheckProcessError, exception: exc);
            }
            finally
            {
                if(_downloadAndRunWasCall == false)
                    _done?.Invoke();
            }
        }

        private void SetTimer(DateTime remindLater)
        {
            var timeSpan = remindLater - DateTime.Now;
            _remindLaterTimer = new System.Timers.Timer
            {
                Interval = (int)timeSpan.TotalMilliseconds,
                AutoReset = false
            };
            _remindLaterTimer.Elapsed += delegate
            {
                _remindLaterTimer = null;
                CallSync(s => Start());
            };
            _remindLaterTimer.Start();
            Logger.Info(States.CheckForUpdateDelayed, $"Start delayed by: {timeSpan.TotalSeconds:F3} seconds");
        }

        private void LetUserToProcessTheUpdate()
        {
            if (!_isWinFormsApplication)
                Application.EnableVisualStyles();
            if (Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA))
                ShowUpdateForm();
            else
            {
                var thread = new Thread(ShowUpdateForm);
                thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.CurrentCulture;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }

        private void ShowUpdateForm()
        {
            try
            {
                Logger.Info(States.LettingUserToProcessTheUpdate);
                var formPresenter = Settings.UpdateFormPresenterFactory.Create();
                var r = formPresenter.ShowModal(Settings.AppTitle, CurrentVersion, InstalledVersion,
                                                Settings.ShowSkipOption, 
                                                Settings.LetUserSelectRemindLater && Settings.ShowRemindLaterOption, 
                                                ChangelogURL);
                Logger.Info(States.UserUpdateFormReturned, $"result: {r}");
                switch (r)
                {
                    case UpdateFormResult.Skip:
                        SetupVersionSkipping();
                        break;
                    case UpdateFormResult.RemindLater:
                        SetupLaterReminder(formPresenter.RemindLaterTimeSpan, formPresenter.RemindLaterAt);
                        break;
                    case UpdateFormResult.Update:
                        if (!Settings.OpenDownloadPage)
                            DownloadAndRunTheUpdate();
                        else {
                            Process.Start(new ProcessStartInfo(DownloadURL));
                            Exit();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(States.UserUpdateFormError, exception: e);
            }
        }

        protected virtual void SetupVersionSkipping()
        {
            using (var updateKey = Registry.CurrentUser.CreateSubKey(RegistryLocation))
                if (updateKey != null)
                {
                    updateKey.SetValue("version", CurrentVersion.ToString());
                    updateKey.SetValue("skip", 1);
                }
        }

        protected virtual void SetupLaterReminder(RemindLaterFormat remindLaterFormat, int remindLaterAt)
        {
            Settings.RemindLaterTimeSpan = remindLaterFormat;
            Settings.RemindLaterAt = remindLaterAt;
            using (var updateKey = Registry.CurrentUser.CreateSubKey(RegistryLocation))
            {
                if (updateKey == null) return;
                updateKey.SetValue("version", CurrentVersion);
                updateKey.SetValue("skip", 0);
                var remindLaterDateTime = DateTime.Now;
                switch (Settings.RemindLaterTimeSpan)
                {
                    case RemindLaterFormat.Days:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromDays(Settings.RemindLaterAt);
                        break;
                    case RemindLaterFormat.Hours:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromHours(Settings.RemindLaterAt);
                        break;
                    case RemindLaterFormat.Minutes:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromMinutes(Settings.RemindLaterAt);
                        break;
                }
                updateKey.SetValue("remindlater", remindLaterDateTime.ToString(CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
                SetTimer(remindLaterDateTime);
            }
        }

        private void UpdateDownloadFinished(string updateFileName, Exception error)
        {
            try
            {
                if (!DownloadResultOK(updateFileName, error)) return;
                if (!ValidateFileIntegrity(updateFileName)) return;
                Logger.Info(States.UpdateDownloadCompleted, Resources.DownloadCompletedMessage);

                LaunchUpdate(updateFileName);
                Exit();
            }
            catch (Exception e)
            {
                var myExc = e as UpdaterException;
                if (myExc != null)
                    Logger.Error(myExc.State, myExc.Message, myExc.InnerException);
                else
                    Logger.Error(States.UnexpectedUpdateProcessError, exception: e);
            }
            finally
            {
                _done?.Invoke();
            }
        }

        private bool DownloadResultOK(string fileName, Exception error)
        {
            if (error != null)
            {
                Logger.Error(States.UpdateDownloadError, Resources.DownloadErrorMessage, error);
                ReportToUser(ReportLevel.Error, Resources.UpdateCheckFailedCaption, Resources.UpdateCheckFailedMessage);
                return false;
            }
            if (!string.IsNullOrEmpty(fileName)) return true;
            Logger.Info(States.UpdateDownloadCancelled);
            return false;
        }

        protected virtual bool ValidateFileIntegrity(string fileName)
        {
            if (string.IsNullOrEmpty(Checksum)) return true;
            using (var hashAlgorithm = HashAlgorithm.Create(HashingAlgorithm))
            {
                if (hashAlgorithm != null)
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        var hash = hashAlgorithm.ComputeHash(stream);
                        var fileChecksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                        if (Checksum.ToLowerInvariant().Equals(fileChecksum))
                            return true;
                    }
                    Logger.Error(States.FileIntegrityCheckSumFailed, Resources.FileIntegrityCheckFailedMessage);
                    ReportToUser(ReportLevel.Error, Resources.FileIntegrityCheckFailedCaption, Resources.FileIntegrityCheckFailedMessage);
                }
                else
                {
                    Logger.Error(States.CheckSumHashAlgorithmNotSupported, Resources.HashAlgorithmNotSupportedMessage);
                    ReportToUser(ReportLevel.Error, Resources.HashAlgorithmNotSupportedCaption, Resources.HashAlgorithmNotSupportedMessage);
                }
                return false;
            }
        }

        private void LaunchUpdate(string updateFileName)
        {
            try
            {
                Settings.UpdateLauncherFactory
                    .Create()
                    .Launch(updateFileName, InstallerArgs, Settings.RunUpdateAsAdmin, Settings.UnattendedMode);
                Logger.Info(States.LaunchUpdateDone);
            }
            catch (Exception e)
            {
                throw new UpdaterException(States.LaunchUpdateError, exception: e);
            }
        }

        /// <summary>
        /// Detects and exits all instances of running assembly, including current.
        /// </summary>
        private void Exit()
        {
            CallSync(s =>
            {
                if (Settings.ApplicationExitEvent != null)
                    try
                    {
                        Logger.Info(States.ApplicationExitEvent);
                        var exitHandled = false;
                        Settings.ApplicationExitEvent(ref exitHandled);
                        if (exitHandled) return;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(States.ApplicationExitEventError, exception: e);
                    }   
                DoAutoExit();         
            });
        }

        protected virtual void DoAutoExit()
        {
            Logger.Info(States.ApplicationAutoExit);
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                string processPath;
                try
                {
                    processPath = process.MainModule.FileName;
                }
                catch (Win32Exception)
                {
                    // Current process should be same as processes created by other instances of the application so it should be able to access modules of other instances. 
                    // This means this is not the process we are looking for so we can safely skip this.
                    continue;
                }

                if (process.Id == currentProcess.Id || processPath != currentProcess.MainModule.FileName) continue;
                if (process.CloseMainWindow())
                    process.WaitForExit((int) TimeSpan.FromSeconds(10).TotalMilliseconds); //give some time to process message
                if (!process.HasExited)
                    process.Kill(); //TODO show UI message asking user to close program himself instead of silently killing it
            }

            if (_isWinFormsApplication)
            {
                MethodInvoker methodInvoker = Application.Exit;
                methodInvoker.Invoke();
            }
#if NETWPF
                else if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        System.Windows.Application.Current.Shutdown()));
                }
#endif
            else
            {
                Environment.Exit(0);
            }
        }
    }

    internal class InstalledAppInformation
    {
        public string AppTitle { get; set; }
        public Version InstalledVersion { get; set; }
        public string RegistryAppInfoLocation { get; set; }
    }

    public class RegisteredAppInformation
    {
        public bool SkipVersions { get; set; }
        public Version MinimalVersion { get; set; } = new Version();
        public DateTime RemindLaterTime { get; set; } = DateTime.MinValue;
    }

    internal class UpdaterException : Exception
    {
        public States State { get; }

        public UpdaterException(States state, string message = null, Exception exception = null)
            : base(message, exception)
        {
            State = state;
        }
    }
#pragma warning restore 1591


    /// <summary>
    ///     Object of this class gives you all the details about the update useful in handling the update logic yourself.
    /// </summary>
    public class UpdateInfoEventArgs : EventArgs
    {
        /// <summary>
        ///     If new update is available then returns true otherwise false.
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        ///     Download URL of the update file.
        /// </summary>
        public string DownloadURL { get; set; }

        /// <summary>
        ///     URL of the webpage specifying changes in the new update.
        /// </summary>
        public string ChangelogURL { get; set; }

        /// <summary>
        ///     Returns newest version of the application available to download.
        /// </summary>
        public Version CurrentVersion { get; set; }

        /// <summary>
        ///     Returns version of the application currently installed on the user's PC.
        /// </summary>
        public Version InstalledVersion { get; set; }

        /// <summary>
        ///     Shows if the update is required or optional.
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        ///     Command line arguments used by Installer.
        /// </summary>
        public string InstallerArgs { get; set; }

        /// <summary>
        ///     Checksum of the update file.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        ///     Hash algorithm that generated the checksum provided in the XML file.
        /// </summary>
        public string HashingAlgorithm { get; set; }
    }


    /// <summary>
    ///     An object of this class contains the AppCast file received from server..
    /// </summary>
    public class ParseUpdateInfoEventArgs : EventArgs
    {
        /// <summary>
        ///     Remote data received from the AppCast file.
        /// </summary>
        public string RemoteData { get; }

        /// <summary>
        ///      Set this object with values received from the AppCast file.
        /// </summary>
        public UpdateInfoEventArgs UpdateInfo { get; set; }

        /// <summary>
        ///     An object containing the AppCast file received from server.
        /// </summary>
        /// <param name="remoteData">A string containing remote data received from the AppCast file.</param>
        public ParseUpdateInfoEventArgs(string remoteData)
        {
            RemoteData = remoteData;
        }
    }
}
