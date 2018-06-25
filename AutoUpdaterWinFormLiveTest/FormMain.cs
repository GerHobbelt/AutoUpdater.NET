using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using AutoUpdaterWinFormLiveTest.Properties;

namespace AutoUpdaterWinFormLiveTest
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            labelVersion.Text = string.Format(Resources.CurrentVersion, Assembly.GetEntryAssembly().GetName().Version);
        }

        private void FormMain_Load(object sender, System.EventArgs e)
        {
            //Use this to configure the AutoUpdater initialization
            //AutoUpdater.InitSettings

            //Set the url of the file that contains information about latest version of the application. 
            //.SetAppCastURL("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/lastest.xml")

            //Want to show custom application title then uncomment below line.
            //.SetAppTitle("My Custom Application Title")

            //Uncomment below lines to handle parsing logic of non XML AppCast file.
            //.SetAParseUpdateInfoEventHandler(AutoUpdaterOnParseUpdateInfoEvent)

            //Uncomment below line to run update process using non administrator account.
            //.DisableRunUpdateAsAdmin()

            //If you want to open download page when user click on download button uncomment below line.
            //.EnableOpenDownloadPage()

            //Don't want user to select remind later time in AutoUpdater notification window then 
            //uncomment 3 lines below so default remind later time will be set to 1 day.
            //.DisableLetUserSelectRemindLater()

            //Don't want to show Skip option then uncomment below line.
            //.DisableShowSkipOption()

            //Don't want to show Remind Later option then uncomment below line.
            //.DisableShowRemindLaterOption()

            //Want to show errors then uncomment below line.
            //.EnableReportErrors()

            //Want to handle how your application will exit when application finished downloading then uncomment below line.
            //.SetAnApplicationExitEventHandler(AutoUpdater_ApplicationExitEvent)

            //Want to handle update logic yourself then uncomment below line.
            //.SetACustomUpdateCheckEventHandler(AutoUpdaterOnCustomUpdateCheckEvent)

            //Want to use XML and Update file served only through Proxy.
            //.SetProxy(new WebProxy("localproxyIP:8080", true)
            //{
            //    Credentials = new NetworkCredential("domain\\user", "password")
            //})

            // Finally call Initialize for AutoUpdater current instance to use these settings
            //.Initialize();


            //Want to check for updates frequently then uncomment following lines.
            //var timer = new System.Timers.Timer
            //{
            //    Interval = 2 * 60 * 1000,
            //    SynchronizingObject = this
            //};
            //timer.Elapsed += delegate
            //{
            //    AutoUpdater.Start();
            //};
            //timer.Start();

            //Uncomment below line to see russian version
            //Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");

            //AutoUpdater.Start();
        }

        //        private void AutoUpdater_ApplicationExitEvent()
        //        {
        //            Text = @"Closing application...";
        //            Thread.Sleep(5000);
        //            Application.Exit();
        //        }

        //        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        //        {
        //            dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
        //            args.UpdateInfo = new UpdateInfoEventArgs
        //            {
        //                CurrentVersion = json.version,
        //                ChangelogURL = json.changelog,
        //                Mandatory = json.mandatory,
        //                DownloadURL = json.url
        //            };
        //        }
        //
        //        private void AutoUpdaterOnCustomUpdateCheckEvent(UpdateInfoEventArgs args)
        //        {
        //            if (args != null)
        //            {
        //                if (args.IsUpdateAvailable)
        //                {
        //                    DialogResult dialogResult;
        //                    if (args.Mandatory)
        //                    {
        //                        dialogResult =
        //                            MessageBox.Show(
        //                                $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.", @"Update Available",
        //                                MessageBoxButtons.OK,
        //                                MessageBoxIcon.Information);
        //                    }
        //                    else
        //                    {
        //                        dialogResult =
        //                            MessageBox.Show(
        //                                $@"There is new version {args.CurrentVersion} available. You are using version {
        //                                        args.InstalledVersion
        //                                    }. Do you want to update the application now?", @"Update Available",
        //                                MessageBoxButtons.YesNo,
        //                                MessageBoxIcon.Information);
        //                    }
        //
        //
        //                    if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
        //                    {
        //                        try
        //                        {
        //                            //You can use Download Update dialog used by AutoUpdater.NET to download the update.
        //                            AutoUpdater.Current.DownloadAndRunTheUpdate();
        //                        }
        //                        catch (Exception exception)
        //                        {
        //                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
        //                                MessageBoxIcon.Error);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    MessageBox.Show(@"There is no update available. Please try again later.", @"Update Unavailable",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Information);
        //                }
        //            }
        //            else
        //            {
        //                MessageBox.Show(
        //                       @"There is a problem reaching update server. Please check your internet connection and try again later.",
        //                       @"Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            }
        //        }

        private void buttonCheckForUpdate_Click(object sender, System.EventArgs e)
        {
            var myDownloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");

            //Uncomment below lines to select download path where update is saved.

//            var folderBrowserDialog = new FolderBrowserDialog();
//            if (folderBrowserDialog.ShowDialog().Equals(DialogResult.OK))
//                myDownloadPath = folderBrowserDialog.SelectedPath;

            AutoUpdater.InitSettings
                .SetAppCastURL("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/lastest.xml")
                .SetDownloadPath(myDownloadPath)
                .EnableReportAll()
                .Initialize();

            AutoUpdater.Start();
        }
    }
}
