using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Data;

public class ClaimsRepository
{
    private readonly ContosoDbContext _dbContext;
    private readonly ILogger<ClaimsRepository> _logger;

    public ClaimsRepository(
        ContosoDbContext dbContext,
        ILogger<ClaimsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public List<Claim> GetRecent(int top = 50) =>
        GetRecentAsync(top).GetAwaiter().GetResult();

    public Task<List<Claim>> GetRecentAsync(
        int top = 50,
        CancellationToken cancellationToken = default) =>
        ClaimsWithPolicies()
            .OrderByDescending(claim => claim.FiledOn)
            .Take(top)
            .ToListAsync(cancellationToken);

    public Claim? GetById(int claimId) =>
        GetByIdAsync(claimId).GetAwaiter().GetResult();

    public Task<Claim?> GetByIdAsync(
        int claimId,
        CancellationToken cancellationToken = default) =>
        ClaimsWithPolicies()
            .SingleOrDefaultAsync(claim => claim.ClaimId == claimId, cancellationToken);

    public List<Claim> SearchByClaimant(string namePart) =>
        SearchByClaimantAsync(namePart).GetAwaiter().GetResult();

    public Task<List<Claim>> SearchByClaimantAsync(
        string namePart,
        CancellationToken cancellationToken = default)
    {
        namePart ??= string.Empty;

        return ClaimsWithPolicies()
            .Where(claim => claim.ClaimantName != null && claim.ClaimantName.Contains(namePart))
            .ToListAsync(cancellationToken);
    }

    public int Insert(Claim claim) =>
        InsertAsync(claim).GetAwaiter().GetResult();

    public async Task<int> InsertAsync(
        Claim claim,
        CancellationToken cancellationToken = default)
    {
        var entity = new Claim
        {
            PolicyId = claim.PolicyId,
            ClaimantName = claim.ClaimantName,
            Amount = claim.Amount,
            Status = claim.Status ?? "Pending",
            FiledOn = claim.FiledOn == default ? DateTime.UtcNow : claim.FiledOn,
            DocumentPath = claim.DocumentPath,
            Notes = claim.Notes
        };

        await _dbContext.Claims.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inserted claim {ClaimId}", entity.ClaimId);
        return entity.ClaimId;
    }

    public void UpdateScore(int claimId, int score) =>
        UpdateScoreAsync(claimId, score).GetAwaiter().GetResult();

    public async Task UpdateScoreAsync(
        int claimId,
        int score,
        CancellationToken cancellationToken = default)
    {
        var claim = await _dbContext.Claims
            .SingleOrDefaultAsync(entity => entity.ClaimId == claimId, cancellationToken);

        if (claim is null)
        {
            return;
        }

        claim.Score = score;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Claim> ClaimsWithPolicies() =>
        from claim in _dbContext.Claims.AsNoTracking()
        join policy in _dbContext.Policies.AsNoTracking()
            on claim.PolicyId equals policy.PolicyId
        select new Claim
        {
            ClaimId = claim.ClaimId,
            PolicyId = claim.PolicyId,
            PolicyNumber = policy.PolicyNumber,
            ClaimantName = claim.ClaimantName,
            Amount = claim.Amount,
            Status = claim.Status,
            FiledOn = claim.FiledOn,
            ClosedOn = claim.ClosedOn,
            DocumentPath = claim.DocumentPath,
            Score = claim.Score,
            Notes = claim.Notes
        };
}
