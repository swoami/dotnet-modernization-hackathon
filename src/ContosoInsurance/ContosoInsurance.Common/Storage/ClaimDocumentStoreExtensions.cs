using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoInsurance.Common.Storage
{
    /// <summary>
    /// Extension methods for registering claim document storage services.
    /// </summary>
    public static class ClaimDocumentStoreExtensions
    {
        /// <summary>
        /// Registers a <see cref="BlobServiceClient"/> singleton and <see cref="BlobClaimDocumentStore"/> as
        /// <see cref="IClaimDocumentStore"/> singleton.
        /// Reads the Azure Storage account URI from configuration key <c>AzureStorage:AccountUri</c>.
        /// Managed Identity (<see cref="DefaultAzureCredential"/>) is used — no connection strings.
        /// </summary>
        public static IServiceCollection AddClaimDocumentStore(this IServiceCollection services, IConfiguration configuration)
        {
            var accountUri = configuration["AzureStorage:AccountUri"]
                ?? throw new InvalidOperationException("Configuration key 'AzureStorage:AccountUri' is required.");

            var serviceClient = new BlobServiceClient(new Uri(accountUri), new DefaultAzureCredential());
            services.AddSingleton(serviceClient);
            services.AddSingleton<IClaimDocumentStore>(new BlobClaimDocumentStore(serviceClient));

            return services;
        }
    }
}
