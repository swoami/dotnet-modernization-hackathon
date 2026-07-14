using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Common.Storage;
using ContosoInsurance.Data;
using ContosoInsurance.Data.Models;
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
        private readonly IClaimDocumentStore _store;
        private readonly IServiceScopeFactory _scopeFactory;

        public ClaimsExporterService(
            ILogger<ClaimsExporterService> logger,
            IOptions<ExportOptions> options,
            IClaimDocumentStore store,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _options = options.Value;
            _store = store;
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

        internal async Task ExportAsync(CancellationToken cancellationToken)
        {
            var blobName = "claims-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv";

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

            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            using var stream = new MemoryStream(csvBytes);
            await _store.UploadAsync(_options.ContainerName, blobName, stream, cancellationToken);

            _logger.LogInformation("Uploaded export blob {BlobName} ({Count} rows) to container {Container}",
                blobName, claims.Count, _options.ContainerName);

            // Persist audit record
            var db = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();
            db.ExportLogs.Add(new ExportLog
            {
                BlobName = blobName,
                RowCount = claims.Count,
                ExportedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        private static string Csv(string? v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
    }
}
