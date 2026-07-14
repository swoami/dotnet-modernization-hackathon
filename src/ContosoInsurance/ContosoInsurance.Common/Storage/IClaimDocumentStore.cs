using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoInsurance.Common.Storage
{
    /// <summary>
    /// Abstraction for uploading claim-related documents/exports to blob storage.
    /// </summary>
    public interface IClaimDocumentStore
    {
        /// <summary>
        /// Uploads the given content stream as a blob.
        /// </summary>
        /// <param name="containerName">Target blob container name (e.g. "claim-exports" or "claim-docs").</param>
        /// <param name="blobName">Blob name within the container (e.g. a timestamped CSV filename).</param>
        /// <param name="content">Stream containing the blob data. The stream is read from its current position.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UploadAsync(string containerName, string blobName, Stream content, CancellationToken ct = default);
    }
}
