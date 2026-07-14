using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoInsurance.Worker
{
    public class ClaimsExporterService : BackgroundService
    {
        private readonly ILogger<ClaimsExporterService> _logger;
        private readonly ExportOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;

        public ClaimsExporterService(
            ILogger<ClaimsExporterService> logger,
            IOptions<ExportOptions> options,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _options = options.Value;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ClaimsExporterService starting");

            // Run once on startup
            await ExportSafelyAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.ExportIntervalMinutes));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ExportSafelyAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ClaimsExporterService stopping");
            }
        }

        private async Task ExportSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ExportAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
            }
        }

        private async Task ExportAsync(CancellationToken cancellationToken)
        {
            var root = _options.ExportRoot;
            Directory.CreateDirectory(root);
            var file = Path.Combine(root, "claims-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ClaimsRepository>();
            var claims = await repo.GetRecentAsync(1000, cancellationToken);

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

            await File.WriteAllTextAsync(file, sb.ToString(), Encoding.UTF8, cancellationToken);
            _logger.LogInformation("Wrote export {File} ({Count} rows)", file, claims.Count);
        }

        private static string Csv(string? v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
    }
}
