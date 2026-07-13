using System;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Data;

namespace ContosoInsurance.Services
{
    public class ClaimScoringService : IClaimScoringService
    {
        public int ScoreClaim(int claimId)
        {
            var repo = new ClaimsRepository();
            var claim = repo.GetById(claimId);
            if (claim == null)
            {
                AppLogger.Warn("ScoreClaim: not found " + claimId);
                return -1;
            }

            // Legacy "AI": deterministic weighted rules.
            var score = 500;
            score += claim.Amount > 10000m ? -150 : 50;
            score += string.Equals(claim.Status, "Pending", StringComparison.OrdinalIgnoreCase) ? 0 : -25;
            score += (DateTime.UtcNow - claim.FiledOn).TotalDays > 30 ? -75 : 25;
            score = Math.Max(0, Math.Min(1000, score));

            AppLogger.Info("Scored claim " + claimId + " -> " + score);
            return score;
        }

        public string GetModelVersion()
        {
            return ConfigHelper.GetSetting("ScoringModelVersion", "v0");
        }
    }
}
