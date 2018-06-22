using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AutoUpdaterDotNET;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task _01_CheckedAndFoundUnavailableUpdate()
        {
            var done = new TaskCompletionSource<object>();
            var logger = new TestLogger();

            var settings = AutoUpdater.InitSettings
                                        .SetTheMainAssembly(Assembly.GetCallingAssembly())
                                        .SetAppCastURL("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/lastest.xml")
                                        .SetALogger(logger)
                                        .EnableReportAll()
                                        .EnableUnattendedMode();
                                        //.Initialize();
            var updater = new TestableUpdater(settings);

            updater.StartChecking(done: () => done.SetResult(null));
            await done.Task;

            Assert.AreEqual(0, logger.ErrorCount, "No errors were expected");
            Assert.AreEqual(new Version(4,0,0,0), updater.InstalledVersion);
            Assert.AreEqual(new Version(2,0,0,0), updater.CurrentVersion);
            Assert.AreEqual("setup.exe", updater.InstallerArgs);
            Assert.IsTrue(logger.LoggedStates.Contains(States.UnavailableUpdate), "Expected unavailable update state");
        }

        [TestMethod]
        public async Task _02_FoundNewUpdateThenDownloadItAndLaunchIt()
        {
            var done = new TaskCompletionSource<object>();
            var dir = AppDomain.CurrentDomain.BaseDirectory + "\\test2";
            var logger = new TestLogger();
            var launcherMock = new UpdateLauncherMock();

            var settings = AutoUpdater.InitSettings
                                        .SetTheMainAssembly(Assembly.GetCallingAssembly())
                                        .SetAppCastURL("https://raw.githubusercontent.com/asarmiento13315/AutoUpdater.NET/master/UnitTests/DownloadSamples/lastest.xml")
                                        .SetDownloadPath(dir)
                                        .SetALogger(logger)
                                        .EnableReportAll()
                                        .EnableUnattendedMode()
                                        .SetInstalledVersionProvider(() => new Version(1,0,0,0))
                                        .SetAnUpdateLauncherFactory(launcherMock);
                                        //.Initialize();
            var updater = new TestableUpdater(settings);

            updater.StartChecking(done: () => done.SetResult(null));
            await done.Task;

            Assert.AreEqual(0, logger.ErrorCount, "No errors were expected");
            Assert.AreEqual(new Version(1, 0, 0, 0), updater.InstalledVersion);
            Assert.AreEqual(new Version(2, 0, 0, 0), updater.CurrentVersion);
            Assert.AreEqual("https://github.com/asarmiento13315/AutoUpdater.NET/blob/master/UnitTests/DownloadSamples/update.zip?raw=true", updater.DownloadURL);
            Assert.AreEqual(dir + "\\update.zip", launcherMock.updateFileName);
            Assert.IsTrue(File.Exists(dir + "\\update.zip"));
            Assert.IsTrue(updater.DidExit);
        }
    }


    public class TestableUpdater : AutoUpdater
    {
        public bool DidExit;

        public TestableUpdater(InitSettings settings): base(settings) { }

        protected override void DoAutoExit() { DidExit = true; } // No Exit
    }

    public class TestLogger : ILogger
    {
        public int ErrorCount = 0;
        public List<States> LoggedStates = new List<States>();

        public void Info(States state, string message = null)
        {
            LoggedStates.Add(state);
            Console.WriteLine($"Info: [{state}]");
            if (message != null)
                Console.WriteLine($" - {message}");
            Console.WriteLine();
        }

        public void Error(States state, string message = null, Exception exception = null)
        {
            LoggedStates.Add(state);
            ErrorCount++;
            Console.WriteLine($"Err#{ErrorCount}: [{state}]");
            if(message != null)
                Console.WriteLine($" - {message}");
            if (exception != null)
                Console.WriteLine($"\tExc: {exception.Message}\n\t{exception.StackTrace}");
            Console.WriteLine();
        }
    }

    public class UpdateLauncherMock: UpdateLauncherFactory, UpdateLauncher
    {
        public string updateFileName;

        public UpdateLauncher Create() => this;

        public void Launch(string f, string args, bool ra, bool u)
        {
            this.updateFileName = f;
            Console.WriteLine($"Launching update\nfile: {f}\nargs: {args}");
        }
    }
}
