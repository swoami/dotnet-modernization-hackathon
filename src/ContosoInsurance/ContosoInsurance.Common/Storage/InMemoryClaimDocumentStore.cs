using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoInsurance.Common.Storage
{
    /// <summary>
    /// In-memory fake implementation of <see cref="IClaimDocumentStore"/> for use in unit tests.
    /// Stores uploaded blobs in memory keyed by "containerName/blobName".
    /// </summary>
    public sealed class InMemoryClaimDocumentStore : IClaimDocumentStore
    {
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns a read-only view of all uploaded blobs keyed by "containerName/blobName".</summary>
        public IReadOnlyDictionary<string, byte[]> Blobs => _blobs;

        /// <inheritdoc />
        public async Task UploadAsync(string containerName, string blobName, Stream content, CancellationToken ct = default)
        {
            var key = containerName + "/" + blobName;
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            _blobs[key] = ms.ToArray();
        }
    }
}
