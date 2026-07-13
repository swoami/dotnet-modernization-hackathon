using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Data.Models;

namespace ContosoInsurance.Data
{
    /// <summary>
    /// Raw ADO.NET data access. No async. Some methods use parameterized SQL,
    /// one uses string concatenation on purpose (see <see cref="SearchByClaimant"/>)
    /// so the appmod flow can flag it as SQL injection.
    /// </summary>
    public class ClaimsRepository
    {
        private readonly string _connectionString;

        public ClaimsRepository()
        {
            _connectionString = ConfigHelper.GetConnectionString("ContosoDb");
        }

        public List<Claim> GetRecent(int top = 50)
        {
            var results = new List<Claim>();
            const string sql = @"SELECT TOP (@Top) c.ClaimId, c.PolicyId, p.PolicyNumber,
                                        c.ClaimantName, c.Amount, c.Status, c.FiledOn,
                                        c.ClosedOn, c.DocumentPath, c.Score, c.Notes
                                 FROM   dbo.Claims c
                                 JOIN   dbo.Policies p ON p.PolicyId = c.PolicyId
                                 ORDER BY c.FiledOn DESC";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Top", top);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) results.Add(Map(r));
                }
            }
            return results;
        }

        public Claim GetById(int claimId)
        {
            const string sql = @"SELECT c.ClaimId, c.PolicyId, p.PolicyNumber,
                                        c.ClaimantName, c.Amount, c.Status, c.FiledOn,
                                        c.ClosedOn, c.DocumentPath, c.Score, c.Notes
                                 FROM   dbo.Claims c
                                 JOIN   dbo.Policies p ON p.PolicyId = c.PolicyId
                                 WHERE  c.ClaimId = @Id";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", claimId);
                conn.Open();
                using (var r = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return r.Read() ? Map(r) : null;
                }
            }
        }

        /// <summary>
        /// LEGACY: concatenated SQL — vulnerable to injection.
        /// Kept intentionally so Copilot / appmod can flag and fix it.
        /// </summary>
        public List<Claim> SearchByClaimant(string namePart)
        {
            var results = new List<Claim>();
            var sql = "SELECT c.ClaimId, c.PolicyId, p.PolicyNumber, c.ClaimantName, " +
                      "c.Amount, c.Status, c.FiledOn, c.ClosedOn, c.DocumentPath, c.Score, c.Notes " +
                      "FROM dbo.Claims c JOIN dbo.Policies p ON p.PolicyId = c.PolicyId " +
                      "WHERE c.ClaimantName LIKE '%" + namePart + "%'"; // <-- BAD
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) results.Add(Map(r));
                }
            }
            return results;
        }

        public int Insert(Claim claim)
        {
            const string sql = @"INSERT INTO dbo.Claims
                                    (PolicyId, ClaimantName, Amount, Status, FiledOn, DocumentPath, Notes)
                                 VALUES
                                    (@PolicyId, @ClaimantName, @Amount, @Status, @FiledOn, @DocumentPath, @Notes);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PolicyId", claim.PolicyId);
                cmd.Parameters.AddWithValue("@ClaimantName", claim.ClaimantName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", claim.Amount);
                cmd.Parameters.AddWithValue("@Status", claim.Status ?? "Pending");
                cmd.Parameters.AddWithValue("@FiledOn", claim.FiledOn == default(DateTime) ? DateTime.UtcNow : claim.FiledOn);
                cmd.Parameters.AddWithValue("@DocumentPath", (object)claim.DocumentPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", (object)claim.Notes ?? DBNull.Value);
                conn.Open();
                var newId = (int)cmd.ExecuteScalar();
                AppLogger.Info("Inserted claim " + newId);
                return newId;
            }
        }

        public void UpdateScore(int claimId, int score)
        {
            const string sql = "UPDATE dbo.Claims SET Score = @Score WHERE ClaimId = @Id";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Score", score);
                cmd.Parameters.AddWithValue("@Id", claimId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static Claim Map(SqlDataReader r) => new Claim
        {
            ClaimId       = r.GetInt32(r.GetOrdinal("ClaimId")),
            PolicyId      = r.GetInt32(r.GetOrdinal("PolicyId")),
            PolicyNumber  = r["PolicyNumber"] as string,
            ClaimantName  = r["ClaimantName"] as string,
            Amount        = r.GetDecimal(r.GetOrdinal("Amount")),
            Status        = r["Status"] as string,
            FiledOn       = r.GetDateTime(r.GetOrdinal("FiledOn")),
            ClosedOn      = r["ClosedOn"] as DateTime?,
            DocumentPath  = r["DocumentPath"] as string,
            Score         = r["Score"] as int?,
            Notes         = r["Notes"] as string
        };
    }
}
