using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoUpdaterDotNET.BasicImpls
{
    internal partial class BasicUpdateForm : Form, UpdateFormPresenter
    {
        private UpdateFormResult _result;
        private bool _hideReleaseNotes;
        private string _changeLogUrl;

        public RemindLaterFormat RemindLaterTimeSpan { get; set; }
        public int RemindLaterAt { get; set; }
        //public CancellationDelegate CancellationDelegate { get; set; }

        public BasicUpdateForm()
        {
            InitializeComponent();
        }

        public UpdateFormResult ShowModal(string appTitle, Version currentVersion, Version installedVersion, 
            bool showSkipOption, bool showRemindLaterOption, string changeLogUrl)
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(BasicUpdateForm));
            Text = string.Format(resources.GetString("$this.Text", CultureInfo.CurrentCulture), appTitle, currentVersion);
            labelUpdate.Text = string.Format(resources.GetString("labelUpdate.Text", CultureInfo.CurrentCulture), appTitle);
            labelDescription.Text = string.Format(resources.GetString("labelDescription.Text", CultureInfo.CurrentCulture), appTitle, currentVersion, installedVersion);

            buttonSkip.Visible = showSkipOption;
            buttonRemindLater.Visible = showRemindLaterOption;
            UseLatestIE();

            _changeLogUrl = changeLogUrl;
            if (string.IsNullOrEmpty(changeLogUrl))
                HideReleaseNotesBoxAndReduceFormHeight();

            ShowDialog();

            return _result;
        }

        private void HideReleaseNotesBoxAndReduceFormHeight()
        {
            _hideReleaseNotes = true;
            var reduceHeight = labelReleaseNotes.Height + webBrowser.Height;
            labelReleaseNotes.Hide();
            webBrowser.Hide();
            Height -= reduceHeight;
            buttonSkip.Location = new Point(buttonSkip.Location.X, buttonSkip.Location.Y - reduceHeight);
            buttonRemindLater.Location = new Point(buttonRemindLater.Location.X, buttonRemindLater.Location.Y - reduceHeight);
            buttonUpdate.Location = new Point(buttonUpdate.Location.X, buttonUpdate.Location.Y - reduceHeight);
        }

        public sealed override string Text
        {
            get { return  base.Text; }
            set { base.Text = value; }
        }

        private void UseLatestIE()
        {
            int ieValue;
            switch (webBrowser.Version.Major)
            {
                case 11:
                    ieValue = 11001;
                    break;
                case 10:
                    ieValue = 10001;
                    break;
                case 9:
                    ieValue = 9999;
                    break;
                case 8:
                    ieValue = 8888;
                    break;
                case 7:
                    ieValue = 7000;
                    break;
                default:
                    ieValue = 0;
                    break;
            }
            if (ieValue == 0) return;
            using (var registryKey = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
            {
                registryKey?.SetValue(Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName), ieValue, RegistryValueKind.DWord);
            }
        }

        private void UpdateFormLoad(object sender, EventArgs e)
        {
            if (!_hideReleaseNotes)
                webBrowser.Navigate(_changeLogUrl);
        }

        private void ButtonUpdateClick(object sender, EventArgs e)
        {
            _result = UpdateFormResult.Update;
            DialogResult = DialogResult.OK;
        }

        private void ButtonSkipClick(object sender, EventArgs e)
        {
            _result = UpdateFormResult.Skip;
            DialogResult = DialogResult.OK;
        }

        private void ButtonRemindLaterClick(object sender, EventArgs e)
        {
            var remindLaterForm = new RemindLaterForm();
            var dialogResult = remindLaterForm.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                RemindLaterTimeSpan = remindLaterForm.RemindLaterFormat;
                RemindLaterAt = remindLaterForm.RemindLaterAt;
                _result = UpdateFormResult.RemindLater;
                DialogResult = DialogResult.OK;
            }
            else if (dialogResult == DialogResult.Abort)
                ButtonUpdateClick(sender, e);
        }

        private void UpdateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //CancellationDelegate?.Invoke();
            //_result = UpdateFormResult.Cancelled;
            //DialogResult = DialogResult.Cancel;
        }

    }
}
