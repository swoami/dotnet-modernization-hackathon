using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ContosoInsurance.Worker.HealthChecks
{
    /// <summary>
    /// Health check that verifies the Azure Blob Storage container used for exports is reachable.
    /// Returns <see cref="HealthStatus.Healthy"/> when the container exists; <see cref="HealthStatus.Unhealthy"/> otherwise.
    /// </summary>
    public sealed class BlobContainerHealthCheck : IHealthCheck
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly string _containerName;

        public BlobContainerHealthCheck(BlobServiceClient serviceClient, IOptions<ExportOptions> options)
        {
            _serviceClient = serviceClient;
            _containerName = options.Value.ContainerName;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
                var exists = await containerClient.ExistsAsync(cancellationToken);
                return exists.Value
                    ? HealthCheckResult.Healthy($"Blob container '{_containerName}' is accessible.")
                    : HealthCheckResult.Unhealthy($"Blob container '{_containerName}' does not exist.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Blob container check failed: {ex.Message}", ex);
            }
        }
    }
}
