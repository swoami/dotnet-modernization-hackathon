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

        /// <summary>Root directory where CSV export files are written.</summary>
        [Required]
        public string ExportRoot { get; set; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ContosoExports");

        /// <summary>How often (in minutes) the export runs after the initial startup run.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "ExportIntervalMinutes must be at least 1.")]
        public int ExportIntervalMinutes { get; set; } = 60;
    }
}
