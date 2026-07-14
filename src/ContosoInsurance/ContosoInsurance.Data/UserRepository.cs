using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Data.Models;
using ContosoInsurance.Data.Security;

namespace ContosoInsurance.Data
{
    /// <summary>
    /// LEGACY: passwords hashed with SHA1 + per-user salt. Deliberately weak so
    /// Copilot / appmod can recommend a modern KDF (PBKDF2 / Argon2 / Data Protection).
    /// </summary>
    public class UserRepository
    {
        private readonly string _connectionString = ConfigHelper.GetConnectionString("ContosoDb");

        public User FindByUsername(string username)
        {
            const string sql = @"SELECT UserId, Username, PasswordHash, Salt, Role
                                 FROM   dbo.Users
                                 WHERE  Username = @Username";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new User
                    {
                        UserId       = r.GetInt32(0),
                        Username     = r.GetString(1),
                        PasswordHash = r.GetString(2),
                        Salt         = r.GetString(3),
                        Role         = r.GetString(4),
                    };
                }
            }
        }

        public bool VerifyPassword(User user, string password)
        {
            return LegacyPasswordVerifier.Verify(user, password);
        }
    }
}
