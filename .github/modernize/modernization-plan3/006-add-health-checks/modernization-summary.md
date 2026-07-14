# Modernization Summary: 006-add-health-checks

- **finalStatus**: success
- **successCriteriaStatus**: { passBuild: true, generateNewUnitTests: true, passUnitTests: true }
- **summary**: Added `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (9.0.17) to `ContosoInsurance.Worker.csproj`. Created `BlobContainerHealthCheck` implementing `IHealthCheck` — injects `BlobServiceClient` singleton (registered by `AddClaimDocumentStore`) and checks `GetBlobContainerClient(_containerName).ExistsAsync()`. Registered both `AddDbContextCheck<ContosoDbContext>` and `AddCheck<BlobContainerHealthCheck>("blob-storage")` tagged `"ready"` in `Program.cs`. Health results logged via the existing ILogger infrastructure on each periodic check. Build passes.
