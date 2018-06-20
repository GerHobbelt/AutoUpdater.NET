using System;
using System.Globalization;
using System.Windows.Forms;
using AutoUpdaterDotNET.Properties;

namespace AutoUpdaterDotNET.BasicImpls
{
    internal partial class DownloadUpdateDialog : Form, UpdateDownloadPresenter
    {
        private DateTime _startedAt;

        public AllowCancellationDelegate AllowCancellationDelegate { get; set; }

        public DownloadUpdateDialog()
        {
            InitializeComponent();
        }

        private void DownloadUpdateDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            var allowCancellation = AllowCancellationDelegate;
            if (allowCancellation != null)
                e.Cancel = !allowCancellation();
        }

        void UpdateDownloadPresenter.ShowModal()
        {
            ShowDialog();
        }

        public void DownloadProgressChanged(long bytesReceived, long totalBytesToReceive)
        {
            if (_startedAt == default(DateTime))
            {
                _startedAt = DateTime.Now;
            }
            else
            {
                var timeSpan = DateTime.Now - _startedAt;
                var totalSeconds = (long)timeSpan.TotalSeconds;
                if (totalSeconds > 0)
                {
                    var bytesPerSecond = bytesReceived / totalSeconds;
                    labelInformation.Text = string.Format(Resources.DownloadSpeedMessage, BytesToString(bytesPerSecond));
                }
            }
            labelSize.Text = $@"{BytesToString(bytesReceived)} / {BytesToString(totalBytesToReceive)}";
            progressBar.Value = Convert.ToInt32((decimal)(100.0 * bytesReceived / totalBytesToReceive));
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{(Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture)} {suf[place]}";
        }
    }
}