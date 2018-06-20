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

        private readonly BitArray _reportLevels = new BitArray(2);
        private readonly InnerLogger _innerLogger;
        private UpdateFormPresenterFactory _updateFormPresenterFactory = new BasicUpdateFormPresenterFactory();
        private DownloadPresenterFactory _downloadPresenterFactory = new BasicDownloadPresenterFactory();
        private UpdateDownloaderFactory _updateDownloaderFactory = new BasicUpdateDownloaderFactory();
        private System.Timers.Timer _remindLaterTimer;

        internal InitSettings Settings { get; set; } = new InitSettings();

        internal ILogger Logger
        {
            get { return _innerLogger; }
            set { _innerLogger.OutterLogger = value; }
        }
        internal bool ReportInfos
        {
            get { return _reportLevels.Get((int)ReportLevel.Info); }
            set { _reportLevels.Set((int)ReportLevel.Info, value); }
        }
        internal bool ReportErrors
        {
            get { return _reportLevels.Get((int)ReportLevel.Error); }
            set { _reportLevels.Set((int)ReportLevel.Error, value); }
        }
        internal UpdateFormPresenterFactory UpdateFormPresenterFactory
        {
            get { return _updateFormPresenterFactory; }
            set { _updateFormPresenterFactory = value ?? new BasicUpdateFormPresenterFactory(); }
        }
        internal DownloadPresenterFactory DownloadPresenterFactory
        {
            get { return _downloadPresenterFactory; }
            set { _downloadPresenterFactory = value ?? new BasicDownloadPresenterFactory(); }
        }
        internal UpdateDownloaderFactory UpdateDownloaderFactory
        {
            get { return _updateDownloaderFactory; }
            set { _updateDownloaderFactory = value ?? new BasicUpdateDownloaderFactory(); }
        }

        internal bool Running;
        internal bool IsWinFormsApplication;
        internal Version CurrentVersion;
        internal string DownloadURL;
        internal string ChangelogURL;
        internal string Checksum;
        internal string HashingAlgorithm;
        internal string InstallerArgs;
        internal string RegistryLocation;
        internal Version InstalledVersion;

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
        /// <param name="myAssembly">Assembly to use for version checking.</param>
        public static void Start(Assembly myAssembly = null)
        {
            Current.StartChecking(myAssembly: myAssembly);
        }

        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="appCast">URL of the xml file that contains information about latest version of the application.</param>
        /// <param name="myAssembly">Assembly to use for version checking.</param>
        public static void Start(string appCast, Assembly myAssembly = null)
        {
            Current.StartChecking(appCast, myAssembly);
        }

        internal AutoUpdater()
        {
            var reportLevels = _reportLevels;
            _innerLogger = new InnerLogger(rl => reportLevels.Get((int) rl));
        }

        internal void StartChecking(string appCast = null, Assembly myAssembly = null)
        {
            lock (_initLocker)
            {
                if (Running) return;
                Running = true;
            }
            if (Settings.Mandatory && _remindLaterTimer != null)
            {
                _remindLaterTimer.Stop();
                _remindLaterTimer.Close();
                _remindLaterTimer = null;
            }
            if (_remindLaterTimer != null) return;

            Logger.Info(States.CheckForUpdateStarted);

            if(appCast != null)
                Settings.AppCastURL = appCast;

            IsWinFormsApplication = Application.MessageLoop;

            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorkerDoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;

            backgroundWorker.RunWorkerAsync(myAssembly ?? Assembly.GetEntryAssembly());
        }

        private void ReportToUser(ReportLevel level, string caption, string message)
        {
            if (Settings.UnattendedMode) return;
            if (_reportLevels.Get((int) level))
                MessageBox.Show(message, caption, MessageBoxButtons.OK, 
                                level == ReportLevel.Error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
        }

        private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            e.Cancel = false;
            var installedAppInfo = LoadInstalledAppInformation((Assembly)e.Argument, Settings.AppTitle, Settings.InstalledVersionProvider);
            Settings.AppTitle = installedAppInfo.AppTitle;
            InstalledVersion = installedAppInfo.InstalledVersion;
            RegistryLocation = installedAppInfo.RegistryAppInfoLocation;

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

        private class InstalledAppInformation
        {
            public string AppTitle { get; set; }
            public Version InstalledVersion { get; set; }
            public string RegistryAppInfoLocation { get; set; }
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
            if (appCast == null) return false;

            Logger.Info(States.AppCastRetrievalDone, $"baseUri:[{appCast.BaseUri}]\nremoteData:{appCast.RemoteData}");

            if (parseUpdateInfoHandler != null)
                ExecuteUpdateCustomInfoParseEven(appCast, parseUpdateInfoHandler, out info);
            else
                ExecuteUpdateXmlInfoParse(appCast, out info);
            if (info == null) return false;

            if (info.CurrentVersion == null || string.IsNullOrEmpty(info.DownloadURL))
            {
                Logger.Error(States.AppCastInvalidDataError);
                return false;
            }
            Logger.Info(States.AppCastDataValidationDone);

            info.DownloadURL = GetURL(appCast.BaseUri, info.DownloadURL);
            info.ChangelogURL = GetURL(appCast.BaseUri, info.ChangelogURL);
            return true;
        }

        private class RegisteredAppInformation
        {
            public bool SkipVersions { get; set; }
            public Version MinimalVersion { get; set; } = new Version();
            public DateTime RemindLaterTime { get; set; } = DateTime.MinValue;
        }

        private static RegisteredAppInformation GetRegisteredAppInformation(string regAppInfoKeyName)
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

        private void ResetRegSkipVersion(Version minimalVersion)
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

        private class AppCast
        {
            public string RemoteData;
            public Uri BaseUri;
        }

        private AppCast RetrieveTheAppCast(string appCastUrl, IWebProxy proxy)
        {
            WebResponse webResponse;
            try
            {
                var webRequest = WebRequest.Create(appCastUrl);
                webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                if (proxy != null)
                    webRequest.Proxy = proxy;
                webResponse = webRequest.GetResponse();
            }
            catch (Exception e)
            {
                Logger.Error(States.AppCastRetrievalError, exception: e);
                return null;
            }
            var appCast = new AppCast();
            try
            {
                appCast.BaseUri = webResponse.ResponseUri;
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    appCast.RemoteData = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Logger.Error(States.AppCastReadDataError, exception: e);
                return null;
            }
            finally
            {
                try
                {
                    webResponse.Close();
                }
                catch {/*ignored*/}
            }
            return appCast;
        }

        private void ExecuteUpdateCustomInfoParseEven(AppCast appCast, ParseUpdateInfoHandler parseUpdateInfoHandler, out UpdateInfoEventArgs info)
        {
            info = null;
            try
            {
                var parseArgs = new ParseUpdateInfoEventArgs(appCast.RemoteData);
                parseUpdateInfoHandler(parseArgs);
                info = parseArgs.UpdateInfo;
                Logger.Info(States.AppCastCustomInfoParseEventDone);
            }
            catch (Exception e)
            {
                Logger.Error(States.AppCastCustomInfoParseEventError, exception: e);
            }
        }

        private void ExecuteUpdateXmlInfoParse(AppCast appCast, out UpdateInfoEventArgs info)
        {
            info = null;
            try
            {
                var appCastXmlDoc = new XmlDocument();
                appCastXmlDoc.Load(appCast.RemoteData);
                var appCastItems = appCastXmlDoc.SelectNodes("item");
                if (appCastItems == null) return;
                info = new UpdateInfoEventArgs();
                foreach (XmlNode item in appCastItems)
                    ParseAppCastXmlInfo(item, info);
                Logger.Info(States.AppCastXmlInfoParseDone);
            }
            catch (Exception exc)
            {
                Logger.Error(States.AppCastXmlInfoParseError, exception: exc);
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

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            try
            {
                if (runWorkerCompletedEventArgs.Cancelled) return;
                if (runWorkerCompletedEventArgs.Result is DateTime)
                {
                    SetTimer((DateTime)runWorkerCompletedEventArgs.Result);
                }
                else
                {
                    var updateInfo = runWorkerCompletedEventArgs.Result as UpdateInfoEventArgs;
                    if (Settings.CheckForUpdateEvent != null)
                    {
                        try
                        {
                            Settings.CheckForUpdateEvent(updateInfo);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(States.CheckForUpdateEventError, exception: e);
                        }
                    }
                    else
                    {
                        if (updateInfo == null)
                        {
                            Logger.Error(States.UpdateCheckFailed, Resources.UpdateCheckFailedMessage);
                            ReportToUser(ReportLevel.Error, Resources.UpdateCheckFailedCaption, Resources.UpdateCheckFailedMessage);
                            return;
                        }
                        if (updateInfo.IsUpdateAvailable)
                        {
                            if (Settings.UnattendedMode)
                                AutoProcessTheUpdate();
                            else
                                LetUserToProcessTheUpdate();
                            return;
                        }
                        Logger.Info(States.UpdateUnavailable, Resources.UpdateUnavailableMessage);
                        ReportToUser(ReportLevel.Info, Resources.UpdateUnavailableCaption, Resources.UpdateUnavailableMessage);
                    }
                }
            }
            finally
            {
                Running = false;
            }
        }

        internal void SetTimer(DateTime remindLater)
        {
            var timeSpan = remindLater - DateTime.Now;

            var context = SynchronizationContext.Current;

            _remindLaterTimer = new System.Timers.Timer
            {
                Interval = (int)timeSpan.TotalMilliseconds,
                AutoReset = false
            };

            _remindLaterTimer.Elapsed += delegate
            {
                _remindLaterTimer = null;
                if (context != null)
                {
                    try
                    {
                        context.Send(state => Start(), null);
                    }
                    catch (InvalidAsynchronousStateException)
                    {
                        Start();
                    }
                }
                else
                {
                    Start();
                }
            };

            _remindLaterTimer.Start();
            Logger.Info(States.CheckForUpdateDelayed, $"Start delayed by: {timeSpan.TotalSeconds:F3} seconds");
        }

        private void AutoProcessTheUpdate()
        {
            try
            {
                Logger.Info(States.DownloadingUpdate, Resources.DownloadingMessage);
                DownloadAndRunTheUpdate();
            }
            catch (Exception e)
            {
                Logger.Error(States.UpdateDownloadError, Resources.DownloadErrorMessage, e);
            }
        }

        private void LetUserToProcessTheUpdate()
        {
            if (!IsWinFormsApplication)
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
                var formPresenter = UpdateFormPresenterFactory.Create();
                var r = formPresenter.ShowModal(Settings.AppTitle, CurrentVersion, InstalledVersion,
                    Settings.ShowSkipOption, Settings.LetUserSelectRemindLater && Settings.ShowRemindLaterOption, ChangelogURL);
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

        private void SetupVersionSkipping()
        {
            using (var updateKey = Registry.CurrentUser.CreateSubKey(RegistryLocation))
                if (updateKey != null)
                {
                    updateKey.SetValue("version", CurrentVersion.ToString());
                    updateKey.SetValue("skip", 1);
                }
        }

        private void SetupLaterReminder(RemindLaterFormat remindLaterFormat, int remindLaterAt)
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

        /// <summary>
        ///     Download the update and execute the installer when download completes.
        /// </summary>
        public void DownloadAndRunTheUpdate()
        {
            UpdateDownloaderFactory
                .Create(DownloadPresenterFactory, !Settings.Mandatory, Settings.Proxy)
                .Download(DownloadURL, Settings.DownloadPath, UpdateDownloadFinished);
        }

        private void UpdateDownloadFinished(string updateFileName, Exception error)
        {
            if (!DownloadResultOK(updateFileName, error)) return;
            if (!ValidateFileIntegrity(updateFileName)) return;
            Logger.Info(States.UpdateDownloadCompleted, Resources.DownloadCompletedMessage);
            ExecuteUpdate(updateFileName);
            Exit();
        }

        private bool DownloadResultOK(string fileName, Exception error)
        {
            if (error != null)
            {
                Logger.Error(States.UpdateDownloadError, Resources.DownloadErrorMessage, error);
                ReportToUser(ReportLevel.Error, Resources.UpdateCheckFailedCaption, Resources.UpdateCheckFailedMessage);
                return false;
            }
            if (fileName != null) return true;
            Logger.Info(States.UpdateDownloadCancelled);
            return false;
        }

        private bool ValidateFileIntegrity(string fileName)
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

        private void ExecuteUpdate(string updateFileName)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = updateFileName,
                UseShellExecute = true,
                Arguments = InstallerArgs.Replace("%path%", Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
            };

            var extension = Path.GetExtension(updateFileName);
            if (".zip".Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                var zipExtractor = Path.Combine(Path.GetDirectoryName(updateFileName), "ZipExtractor.exe");
                File.WriteAllBytes(zipExtractor, Resources.ZipExtractor);
                var arguments = new StringBuilder($"\"{updateFileName}\" \"{Process.GetCurrentProcess().MainModule.FileName}\"");
                IncludeCommandLineArguments(arguments);
                processStartInfo = new ProcessStartInfo
                {
                    FileName = zipExtractor,
                    UseShellExecute = true,
                    Arguments = arguments.ToString()
                };
            }
            else if (".msi".Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{updateFileName}\""
                };
            }

            if (Settings.RunUpdateAsAdmin)
                processStartInfo.Verb = "runas";

            try
            {
                Process.Start(processStartInfo);
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode != 1223)
                    throw;
            }
        }

        private static void IncludeCommandLineArguments(StringBuilder arguments)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length; i++)
            {
                if (i.Equals(1))
                    arguments.Append(" \"");
                arguments.Append(args[i]);
                arguments.Append(i.Equals(args.Length - 1) ? "\"" : " ");
            }
        }

        /// <summary>
        /// Detects and exits all instances of running assembly, including current.
        /// </summary>
        private void Exit()
        {
            if (Settings.ApplicationExitEvent == null)
                DoAutoExit();
            else
                try
                {
                    Logger.Info(States.ApplicationExitEvent);
                    Settings.ApplicationExitEvent();
                }
                catch (Exception e)
                {
                    Logger.Error(States.ApplicationExitEventError, exception: e);
                    DoAutoExit();
                }
        }

        private void DoAutoExit()
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

            if (IsWinFormsApplication)
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

#pragma warning disable 1591
    public delegate Version InstalledVersionProviderDelegate();

    public delegate void ApplicationExitEventHandler();
    public delegate void CheckForUpdateEventHandler(UpdateInfoEventArgs args);
    public delegate void ParseUpdateInfoHandler(ParseUpdateInfoEventArgs args);
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
