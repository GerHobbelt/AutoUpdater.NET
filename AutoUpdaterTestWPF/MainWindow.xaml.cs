using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoUpdaterDotNET;

namespace AutoUpdaterTestWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var assembly = Assembly.GetEntryAssembly();
            LabelVersion.Content = $"Current Version : {assembly.GetName().Version}";
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr");

            AutoUpdater.InitSettings
                .EnableReportErrors()
                .Initialize();

            var timer = new DispatcherTimer {Interval = TimeSpan.FromMinutes(2)};
            timer.Tick += delegate
            {
                AutoUpdater.Start("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/wpf_lastest.xml");
            };
            timer.Start();
        }

        private void ButtonCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdater.Start("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/wpf_lastest.xml");
        }
    }
}
