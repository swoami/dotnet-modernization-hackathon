using ContosoInsurance.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoInsurance.Data;

public class ContosoDbContext : DbContext
{
    public ContosoDbContext(DbContextOptions<ContosoDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ExportLog> ExportLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("Claims", "dbo");
            entity.HasKey(claim => claim.ClaimId);

            entity.Property(claim => claim.ClaimantName)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(claim => claim.Amount)
                .HasPrecision(18, 2);
            entity.Property(claim => claim.Status)
                .HasMaxLength(32)
                .HasDefaultValue("Pending")
                .IsRequired();
            entity.Property(claim => claim.DocumentPath)
                .HasMaxLength(512);
            entity.Property(claim => claim.Notes)
                .HasMaxLength(1024);
            entity.Property(claim => claim.FiledOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Ignore(claim => claim.PolicyNumber);

            entity.HasOne(claim => claim.Policy)
                .WithMany()
                .HasForeignKey(claim => claim.PolicyId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies", "dbo");
            entity.HasKey(policy => policy.PolicyId);

            entity.Property(policy => policy.PolicyNumber)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(policy => policy.HolderName)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(policy => policy.ProductLine)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(policy => policy.CoverageAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(user => user.UserId);

            entity.Property(user => user.Username)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(user => user.PasswordHash)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(user => user.Role)
                .HasMaxLength(32)
                .HasDefaultValue("Agent")
                .IsRequired();
        });

        modelBuilder.Entity<ExportLog>(entity =>
        {
            entity.ToTable("ExportLogs", "dbo");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BlobName)
                .HasMaxLength(512)
                .IsRequired();
            entity.Property(e => e.ExportedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
