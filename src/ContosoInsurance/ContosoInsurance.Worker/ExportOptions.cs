using System.ComponentModel.DataAnnotations;

namespace ContosoInsurance.Worker
{
    /// <summary>
    /// Strongly-typed configuration options for the claims export job.
    /// Bound from the "ExportOptions" section of appsettings.json.
    /// </summary>
    public class ExportOptions
    {
        public const string SectionName = "ExportOptions";

        /// <summary>Azure Blob Storage container name for CSV exports.</summary>
        public string ContainerName { get; set; } = "claim-exports";

        /// <summary>How often (in minutes) the export runs after the initial startup run.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "ExportIntervalMinutes must be at least 1.")]
        public int ExportIntervalMinutes { get; set; } = 60;
    }
}
