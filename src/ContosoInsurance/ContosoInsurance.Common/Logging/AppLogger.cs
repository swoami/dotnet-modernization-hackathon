using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Common.Logging
{
    /// <summary>
    /// Static logger backed by Microsoft.Extensions.Logging. Also writes to System.Diagnostics.Trace
    /// so ops can hook into ETW listeners.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _syncRoot = new object();
        private static ILoggerFactory _loggerFactory;
        private static ILogger _logger;
        private static bool _configured;

        public static void Configure()
        {
            if (_configured) return;

            lock (_syncRoot)
            {
                if (_configured) return;

                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                });
                _logger = _loggerFactory.CreateLogger("ContosoInsurance");
                _configured = true;
            }
        }

        public static void Info(string message)
        {
            Logger.LogInformation(message);
            Trace.WriteLine("[INFO] " + message);
        }

        public static void Warn(string message)
        {
            Logger.LogWarning(message);
            Trace.TraceWarning(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
                Logger.LogError(ex, message);
            else
                Logger.LogError(message);
            Trace.TraceError(message + " :: " + (ex?.ToString() ?? "<no exception>"));
        }

        private static ILogger Logger
        {
            get
            {
                Configure();
                return _logger;
            }
        }
    }
}
