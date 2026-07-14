using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ContosoInsurance.Common.Storage
{
    /// <summary>
    /// Production implementation of <see cref="IClaimDocumentStore"/> that writes blobs to
    /// Azure Blob Storage using <see cref="Azure.Identity.DefaultAzureCredential"/> (Managed Identity).
    /// No connection strings or storage keys are used.
    /// </summary>
    public sealed class BlobClaimDocumentStore : IClaimDocumentStore
    {
        private readonly BlobServiceClient _serviceClient;

        public BlobClaimDocumentStore(BlobServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        /// <inheritdoc />
        public async Task UploadAsync(string containerName, string blobName, Stream content, CancellationToken ct = default)
        {
            var containerClient = _serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            // Re-uploading a document for the same claim/blob name is a normal, expected
            // scenario (e.g. replacing a photo/estimate). Without overwrite: true this throws
            // a 409 BlobAlreadyExists RequestFailedException, which crashes the Blazor circuit.
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true, cancellationToken: ct);
        }
    }
}
