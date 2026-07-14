using System;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContosoInsurance.Data;

public class UserRepository
{
    private readonly ContosoDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserRepository(
        ContosoDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public User? FindByUsername(string username) =>
        FindByUsernameAsync(username).GetAwaiter().GetResult();

    public Task<User?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Username == username, cancellationToken);

    public PasswordVerificationResult VerifyPassword(User? user, string? password)
    {
        if (user is null ||
            string.IsNullOrWhiteSpace(user.PasswordHash) ||
            password is null)
        {
            return PasswordVerificationResult.Failed;
        }

        return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
    }
}
