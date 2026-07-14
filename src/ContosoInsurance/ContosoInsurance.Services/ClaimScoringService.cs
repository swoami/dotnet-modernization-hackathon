using System;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Services;

public sealed class ClaimScoringService
{
    private readonly ContosoDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaimScoringService> _logger;

    public ClaimScoringService(
        ContosoDbContext dbContext,
        IConfiguration configuration,
        ILogger<ClaimScoringService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ClaimScoreResult> ScoreClaimAsync(int claimId, CancellationToken cancellationToken = default)
    {
        var claim = await _dbContext.Claims.FindAsync([claimId], cancellationToken);
        if (claim is null)
        {
            _logger.LogWarning("ScoreClaim: not found {ClaimId}", claimId);
            return null;
        }

        var score = 500;
        score += claim.Amount > 10000m ? -150 : 50;
        score += string.Equals(claim.Status, "Pending", StringComparison.OrdinalIgnoreCase) ? 0 : -25;
        score += (DateTime.UtcNow - claim.FiledOn).TotalDays > 30 ? -75 : 25;
        score = Math.Max(0, Math.Min(1000, score));

        claim.Score = score;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scored claim {ClaimId} -> {Score}", claimId, score);
        return new ClaimScoreResult(claimId, score, GetModelVersion());
    }

    public string GetModelVersion()
    {
        return _configuration["AppSettings:ScoringModelVersion"] ?? "v0";
    }
}

public sealed record ClaimScoreResult(int ClaimId, int Score, string ModelVersion);
