using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data.Models;
using ContosoInsurance.Data.Security;
using Microsoft.EntityFrameworkCore;

namespace ContosoInsurance.Data;

/// <summary>
/// Legacy password verification is retained until its dedicated modernization task completes.
/// </summary>
public class UserRepository
{
    private readonly ContosoDbContext _dbContext;

    public UserRepository(ContosoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User? FindByUsername(string username) =>
        FindByUsernameAsync(username).GetAwaiter().GetResult();

    public Task<User?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

    public bool VerifyPassword(User user, string password) =>
        LegacyPasswordVerifier.Verify(user, password);
}
