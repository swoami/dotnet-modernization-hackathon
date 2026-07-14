using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContosoInsurance.Data
{
    /// <summary>
    /// Design-time factory used by EF Core tools (dotnet ef migrations add, etc.) when the
    /// startup project cannot supply a configured <see cref="ContosoDbContext"/>.
    /// </summary>
    public class ContosoDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ContosoDbContext>
    {
        public ContosoDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ContosoDbContext>();
            // Placeholder connection string for design-time only — never used at runtime.
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ContosoInsuranceDesignTime;Trusted_Connection=True;");
            return new ContosoDbContext(optionsBuilder.Options);
        }
    }
}
