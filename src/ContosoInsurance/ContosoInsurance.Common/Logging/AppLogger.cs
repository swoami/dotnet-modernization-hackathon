using System;
using System.Diagnostics;
using log4net;
using log4net.Config;

namespace ContosoInsurance.Common.Logging
{
    /// <summary>
    /// Legacy static logger wrapping log4net. Also writes to System.Diagnostics.Trace
    /// so ops can hook into ETW listeners.
    /// </summary>
    public static class AppLogger
    {
        private static readonly ILog _log = LogManager.GetLogger("ContosoInsurance");
        private static bool _configured;

        public static void Configure()
        {
            if (_configured) return;
            XmlConfigurator.Configure();
            _configured = true;
        }

        public static void Info(string message)
        {
            _log.Info(message);
            Trace.WriteLine("[INFO] " + message);
        }

        public static void Warn(string message)
        {
            _log.Warn(message);
            Trace.TraceWarning(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
                _log.Error(message, ex);
            else
                _log.Error(message);
            Trace.TraceError(message + " :: " + (ex?.ToString() ?? "<no exception>"));
        }
    }
}
