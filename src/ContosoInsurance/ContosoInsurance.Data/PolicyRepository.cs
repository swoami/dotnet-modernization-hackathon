using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoInsurance.Data;

public class PolicyRepository
{
    private readonly ContosoDbContext _dbContext;

    public PolicyRepository(ContosoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<Policy> GetAll() =>
        GetAllAsync().GetAwaiter().GetResult();

    public Task<List<Policy>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Policies
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
