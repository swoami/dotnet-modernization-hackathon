using System;

namespace ContosoInsurance.Data.Models
{
    public class Policy
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; }
        public string HolderName { get; set; }
        public string ProductLine { get; set; } // Auto, Home, Life
        public decimal CoverageAmount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
