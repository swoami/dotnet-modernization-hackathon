using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Data.Models;

namespace ContosoInsurance.Data
{
    public class PolicyRepository
    {
        private readonly string _connectionString = ConfigHelper.GetConnectionString("ContosoDb");

        public List<Policy> GetAll()
        {
            var results = new List<Policy>();
            const string sql = @"SELECT PolicyId, PolicyNumber, HolderName, ProductLine,
                                        CoverageAmount, EffectiveDate, ExpirationDate
                                 FROM   dbo.Policies";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        results.Add(new Policy
                        {
                            PolicyId         = r.GetInt32(0),
                            PolicyNumber     = r.GetString(1),
                            HolderName       = r.GetString(2),
                            ProductLine      = r.GetString(3),
                            CoverageAmount   = r.GetDecimal(4),
                            EffectiveDate    = r.GetDateTime(5),
                            ExpirationDate   = r.GetDateTime(6),
                        });
                    }
                }
            }
            return results;
        }
    }
}
