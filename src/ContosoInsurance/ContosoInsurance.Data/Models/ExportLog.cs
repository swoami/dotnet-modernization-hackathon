using System;

namespace ContosoInsurance.Data.Models
{
    public class ExportLog
    {
        public int ExportId { get; set; }
        public DateTime ExportedAt { get; set; }
        public required string FilePath { get; set; }
        public int RowCount { get; set; }
    }
}
