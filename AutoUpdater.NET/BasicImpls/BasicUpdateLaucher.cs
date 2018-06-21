using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using AutoUpdaterDotNET.Properties;

namespace AutoUpdaterDotNET.BasicImpls
{
#pragma warning disable 1591
    public class BasicUpdateLaucher: UpdateLauncher
    {
        public void Launch(string updateFileName, string installArguments, bool runAsAdmin)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = updateFileName,
                UseShellExecute = true,
                Arguments = installArguments.Replace("%path%", Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
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

            if (runAsAdmin)
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
    }
#pragma warning restore 1591
}