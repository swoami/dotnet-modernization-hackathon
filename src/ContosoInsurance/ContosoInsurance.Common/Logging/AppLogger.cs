using System;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Common.Logging
{
    /// <summary>
    /// Legacy static logger facade. Wraps a <see cref="Microsoft.Extensions.Logging.ILogger"/>
    /// supplied by the Generic Host DI container so all log output flows through the
    /// host's logging pipeline (console, Azure Monitor, etc.).
    ///
    /// <para>Call <see cref="Configure(ILoggerFactory)"/> once at application startup
    /// (after the host is built) to attach the pipeline logger.  Until then, calls
    /// are silently suppressed rather than routed to a low-fidelity trace listener.</para>
    ///
    /// <para>New code should inject <see cref="ILogger{T}"/> directly via constructor DI
    /// instead of calling this class.</para>
    /// </summary>
    [Obsolete("Use Microsoft.Extensions.Logging.ILogger<T> injected via the Generic Host DI container instead.")]
    public static class AppLogger
    {
        private static volatile ILogger? _logger;
        private static bool _configured;

        /// <summary>
        /// Configures the underlying <see cref="ILogger"/> from the Generic Host
        /// <see cref="ILoggerFactory"/>. Should be called once during application startup.
        /// Subsequent calls are ignored.
        /// </summary>
        public static void Configure(ILoggerFactory loggerFactory)
        {
            if (_configured) return;
            _logger = loggerFactory.CreateLogger("ContosoInsurance");
            _configured = true;
        }

        public static void Info(string message)
        {
            _logger?.LogInformation("{Message}", message);
        }

        public static void Warn(string message)
        {
            _logger?.LogWarning("{Message}", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            _logger?.LogError(ex, "{Message}", message);
        }
    }
}
