using System;

namespace ContosoInsurance.Data.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int PolicyId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? ClaimantName { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; } // Pending, Approved, Rejected, Paid
        public DateTime FiledOn { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string? DocumentPath { get; set; } // e.g. C:\ClaimsFiles\1234\claim.pdf
        public int? Score { get; set; }
        public string? Notes { get; set; }
    }
}
