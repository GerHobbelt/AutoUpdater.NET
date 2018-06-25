using System;
using System.Collections;
using System.IO;
using System.Text;

namespace AutoUpdaterDotNET
{
    /// <summary>
    ///     Logger abstraction.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Log an Info level message.
        /// </summary>
        /// <param name="state">Process' state.</param>
        /// <param name="message">Message to log.</param>
        void Info(States state, string message = null);
        /// <summary>
        ///     Log an Error level message.
        /// </summary>
        /// <param name="state">Process' state.</param>
        /// <param name="message">Message to log.</param>
        /// <param name="exception">Exception associated with the error.</param>
        void Error(States state, string message = null, Exception exception = null);
    }

#pragma warning disable 1591
    public enum ReportLevel
    {
        Info,
        Error
    }
#pragma warning restore 1591

    internal delegate bool ReportLevelAllowedDelegate(ReportLevel rl);

    internal class InnerLogger : ILogger
    {
        private readonly string _logFolder;
        private readonly ReportLevelAllowedDelegate _reportLevelAllowed;
        private ILogger _outterLogger;
        public ILogger OutterLogger
        {
            get { return _outterLogger; }
            set { _outterLogger = value ?? new BasicLogger(_logFolder); }
        }

        public InnerLogger(ReportLevelAllowedDelegate d, string f)
        {
            _reportLevelAllowed = d;
            _logFolder = f;
            _outterLogger = new BasicLogger(_logFolder);
        }

        public void Info(States state, string message)
        {
            if (_reportLevelAllowed == null) return;
            if (_reportLevelAllowed(ReportLevel.Info))
                OutterLogger.Info(state, message);
        }
        public void Error(States state, string message, Exception exception)
        {
            if (_reportLevelAllowed == null) return;
            if (_reportLevelAllowed(ReportLevel.Error))
                OutterLogger.Error(state, message, exception);
        }
    }

    internal class BasicLogger : ILogger
    {
        private readonly string _logFolder;

        public BasicLogger(string logFolder = null)
        {
            _logFolder = logFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logFolder);
        }

        public void Info(States state, string message = null)
        {
            WriteLog($"{DateTime.Now:s} INFO [{state}]\n");
            if (!string.IsNullOrEmpty(message))
                WriteLog($"msg: {message}]\n");
            WriteLog("----------\n");
        }

        public void Error(States state, string message = null, Exception exception = null)
        {
            var txtBuilder = new StringBuilder();
            txtBuilder.Append($"{DateTime.Now:s} ERROR [{state}]\n");
            if (!string.IsNullOrEmpty(message))
                txtBuilder.Append($"msg: {message}\n");
            if (exception != null)
            {
                txtBuilder.Append($"exc:\n{exception.Message}\n");
                txtBuilder.Append($"{exception.StackTrace}\n");
                IncludeInnerException(txtBuilder, exception.InnerException);
            }
            txtBuilder.Append("----------\n");
            WriteLog(txtBuilder.ToString());
        }

        private static void IncludeInnerException(StringBuilder txtBuilder, Exception innerException)
        {
            while (true)
            {
                if (innerException != null)
                {
                    txtBuilder.Append($"--inner exc --:\n{innerException.Message}\n");
                    txtBuilder.Append($"{innerException.StackTrace}\n");
                    innerException = innerException.InnerException;
                    continue;
                }
                break;
            }
        }

        private void WriteLog(string txt)
        {
            var f = Path.Combine(_logFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(f, txt);
        }
    }
}