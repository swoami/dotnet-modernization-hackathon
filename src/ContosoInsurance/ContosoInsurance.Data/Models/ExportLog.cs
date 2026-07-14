using System;

namespace ContosoInsurance.Data.Models
{
    /// <summary>Audit record of a successful claims export run.</summary>
    public class ExportLog
    {
        /// <summary>Primary key (identity).</summary>
        public int Id { get; set; }

        /// <summary>Name of the blob written to Azure Blob Storage (e.g. "claims-20260714-120000.csv").</summary>
        public string BlobName { get; set; } = string.Empty;

        /// <summary>Number of claim rows included in this export.</summary>
        public int RowCount { get; set; }

        /// <summary>UTC timestamp when the export completed successfully. Default: SYSUTCDATETIME().</summary>
        public DateTime ExportedAtUtc { get; set; }
    }
}
