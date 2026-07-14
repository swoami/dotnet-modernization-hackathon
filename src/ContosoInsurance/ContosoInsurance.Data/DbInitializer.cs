using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContosoInsurance.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Data;

/// <summary>
/// Applies EF Core migrations at startup and seeds baseline data.
/// Replaces the legacy hand-run db/001-schema.sql and db/002-seed.sql scripts.
/// </summary>
public static class DbInitializer
{
    /// <summary>Password used for the seeded demo accounts. Rotate or remove these accounts outside local demos.</summary>
    public const string DemoPassword = "Password1";

    private static readonly (string Username, string Role)[] DemoUsers =
    {
        ("agent1", "Agent"),
        ("adjuster", "Adjuster"),
        ("admin", "Admin"),
    };

    public static async Task InitializeAsync(
        ContosoDbContext context,
        IPasswordHasher<User> passwordHasher,
        ILogger logger,
        bool seedSampleData = true,
        CancellationToken cancellationToken = default)
    {
        await MigrateAsync(context, logger, cancellationToken);
        await SeedUsersAsync(context, passwordHasher, logger, cancellationToken);

        if (seedSampleData)
        {
            await SeedSampleDataAsync(context, logger, cancellationToken);
        }
    }

    private static async Task MigrateAsync(ContosoDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date; no migrations to apply");
            return;
        }

        await BaselineLegacySchemaIfNeededAsync(context, pending, logger, cancellationToken);

        pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
            await context.Database.MigrateAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Databases created by the legacy SQL scripts already contain the tables but have no
    /// __EFMigrationsHistory. Mark the initial migration as applied so MigrateAsync does not
    /// fail trying to re-create existing tables.
    /// </summary>
    private static async Task BaselineLegacySchemaIfNeededAsync(
        ContosoDbContext context,
        IReadOnlyList<string> pendingMigrations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return; // database does not exist yet; MigrateAsync will create it
        }

        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (applied.Any())
        {
            return;
        }

        var usersTableId = await context.Database
            .SqlQueryRaw<int?>("SELECT OBJECT_ID(N'dbo.Users', N'U') AS [Value]")
            .SingleAsync(cancellationToken);
        if (usersTableId is null)
        {
            return; // fresh database; nothing to baseline
        }

        var initialMigration = pendingMigrations[0];
        logger.LogWarning(
            "Existing legacy schema detected without migration history; baselining initial migration {Migration}",
            initialMigration);

        var productVersion = ProductInfo.GetVersion();
        await context.Database.ExecuteSqlAsync($"""
            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {initialMigration})
            BEGIN
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ({initialMigration}, {productVersion});
            END;
            """, cancellationToken);
    }

    private static async Task SeedUsersAsync(
        ContosoDbContext context,
        IPasswordHasher<User> passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var demoUsernames = DemoUsers.Select(demo => demo.Username).ToArray();
        var existing = await context.Users
            .Where(user => demoUsernames.Contains(user.Username))
            .Select(user => user.Username)
            .ToListAsync(cancellationToken);

        var added = 0;
        foreach (var (username, role) in DemoUsers)
        {
            if (existing.Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var user = new User
            {
                Username = username,
                PasswordHash = string.Empty,
                Role = role,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
            context.Users.Add(user);
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} demo user account(s)", added);
        }
    }

    private static async Task SeedSampleDataAsync(ContosoDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.Policies.AnyAsync(cancellationToken))
        {
            return;
        }

        var policies = new[]
        {
            new Policy { PolicyNumber = "POL-1001", HolderName = "Alice Johnson", ProductLine = "Auto", CoverageAmount = 25000.00m, EffectiveDate = new DateTime(2024, 1, 1), ExpirationDate = new DateTime(2025, 12, 31) },
            new Policy { PolicyNumber = "POL-1002", HolderName = "Bob Smith", ProductLine = "Home", CoverageAmount = 350000.00m, EffectiveDate = new DateTime(2023, 6, 1), ExpirationDate = new DateTime(2026, 5, 31) },
            new Policy { PolicyNumber = "POL-1003", HolderName = "Carol Diaz", ProductLine = "Auto", CoverageAmount = 30000.00m, EffectiveDate = new DateTime(2024, 8, 15), ExpirationDate = new DateTime(2025, 8, 14) },
            new Policy { PolicyNumber = "POL-1004", HolderName = "David Nguyen", ProductLine = "Life", CoverageAmount = 500000.00m, EffectiveDate = new DateTime(2022, 1, 10), ExpirationDate = new DateTime(2032, 1, 9) },
            new Policy { PolicyNumber = "POL-1005", HolderName = "Eve Patel", ProductLine = "Home", CoverageAmount = 275000.00m, EffectiveDate = new DateTime(2024, 11, 1), ExpirationDate = new DateTime(2027, 10, 31) },
        };
        context.Policies.AddRange(policies);

        if (!await context.Claims.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            context.Claims.AddRange(
                new Claim { Policy = policies[0], ClaimantName = "Alice Johnson", Amount = 4200.00m, Status = "Pending", FiledOn = now.AddDays(-3), DocumentPath = @"C:\ClaimsFiles\1\photo.jpg", Notes = "Rear-end collision" },
                new Claim { Policy = policies[1], ClaimantName = "Bob Smith", Amount = 18500.00m, Status = "Pending", FiledOn = now.AddDays(-7), DocumentPath = @"C:\ClaimsFiles\2\estimate.pdf", Notes = "Water damage" },
                new Claim { Policy = policies[2], ClaimantName = "Carol Diaz", Amount = 1200.00m, Status = "Approved", FiledOn = now.AddDays(-40), Notes = "Windshield replacement" },
                new Claim { Policy = policies[3], ClaimantName = "David Nguyen", Amount = 25000.00m, Status = "Pending", FiledOn = now.AddDays(-1) },
                new Claim { Policy = policies[4], ClaimantName = "Eve Patel", Amount = 9800.00m, Status = "Rejected", FiledOn = now.AddDays(-60), Notes = "Out of policy period" });
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded sample policies and claims");
    }
}
