using System;
using System.Collections;

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

    internal class NullLogger : ILogger
    {
        public void Info(States state, string message) { }
        public void Error(States state, string message, Exception exception) { }
    }

    internal delegate bool ReportLevelAllowedDelegate(ReportLevel rl);

    internal class InnerLogger : ILogger
    {
        private readonly ReportLevelAllowedDelegate _reportLevelAllowed;
        private ILogger _outterLogger = new NullLogger();
        public ILogger OutterLogger
        {
            get { return _outterLogger; }
            set { _outterLogger = value ?? new NullLogger(); }
        }

        public InnerLogger(ReportLevelAllowedDelegate d)
        {
            _reportLevelAllowed = d;
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
}