using System;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Data;

namespace ContosoInsurance.Worker
{
    [SupportedOSPlatform("windows")]
    public class ClaimsExporterService : ServiceBase
    {
        private Timer? _timer;

        public ClaimsExporterService()
        {
            ServiceName = "ContosoClaimsExporter";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            AppLogger.Configure();
            AppLogger.Info("ClaimsExporterService starting");

            var minutes = ConfigHelper.GetInt("ExportIntervalMinutes", 60);
            _timer = new Timer(TimeSpan.FromMinutes(minutes).TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += (s, e) => ExportSafely();
            _timer.Start();

            // Run once on startup
            ExportSafely();
        }

        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            AppLogger.Info("ClaimsExporterService stopped");
        }

        private void ExportSafely()
        {
            try
            {
                Export();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Export failed", ex);
            }
        }

        private static void Export()
        {
            var root = ConfigHelper.GetSetting("ExportRoot", @"C:\Exports") ?? @"C:\Exports";
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, "claims-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv");

            var repo = new ClaimsRepository();
            var claims = repo.GetRecent(1000);

            var sb = new StringBuilder();
            sb.AppendLine("ClaimId,PolicyNumber,ClaimantName,Amount,Status,FiledOn,Score");
            foreach (var c in claims)
            {
                sb.AppendLine(string.Join(",",
                    c.ClaimId,
                    Csv(c.PolicyNumber),
                    Csv(c.ClaimantName),
                    c.Amount.ToString(CultureInfo.InvariantCulture),
                    Csv(c.Status),
                    c.FiledOn.ToString("O"),
                    c.Score?.ToString(CultureInfo.InvariantCulture) ?? ""));
            }

            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            AppLogger.Info("Wrote export " + file + " (" + claims.Count + " rows)");
        }

        private static string Csv(string? v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
    }
}
