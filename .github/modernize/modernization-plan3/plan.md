# Modernization Plan: modernization-plan3

**Project**: ContosoInsurance.Worker + ContosoInsurance.Common + ContosoInsurance.Data

---

## Technical Framework

- **Language**: C# / .NET 9
- **Framework**: .NET Generic Host (`BackgroundService`) — already migrated from Windows Service in prior plan
- **Build Tool**: SDK-style `.csproj`
- **Database**: SQL Server (EF Core 9 via `ContosoDbContext`)
- **Key Dependencies**: `Microsoft.Extensions.Hosting`, `Microsoft.Data.SqlClient`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.Logging.ApplicationInsights`

---

## Overview

This plan extends the previously modernized `ContosoInsurance.Worker` (BackgroundService + PeriodicTimer) and the shared libraries with cloud-native Azure integrations. The core concerns are:

1. **Azure Blob Storage** replaces `File.WriteAllText` for CSV exports and local-disk document storage. `DefaultAzureCredential` (Managed Identity) is used — no connection strings or storage keys.
2. **IClaimDocumentStore abstraction** is extracted to `ContosoInsurance.Common` so both the Worker (Track B) and the Web upload path (Track A) share the same interface and Blob implementation.
3. **Application Insights** telemetry is wired into the worker host via `AddApplicationInsightsTelemetryWorkerService()`.
4. **ExportLog audit table** records each successful export run (blob name, row count, timestamp) via the shared `ContosoDbContext`.
5. **Health checks** surface blob container availability and database connectivity for readiness/liveness probes.

---

## Migration Impact Summary

| Application                  | Before                                      | After                                                                 | Auth                         | Comments                                                 |
|------------------------------|---------------------------------------------|-----------------------------------------------------------------------|------------------------------|----------------------------------------------------------|
| ContosoInsurance.Worker      | `File.WriteAllText` to local disk           | `BlobContainerClient.UploadBlobAsync` to `claim-exports` container    | `DefaultAzureCredential`     | Container name comes from `ExportOptions.ContainerName`  |
| ContosoInsurance.Worker      | No Application Insights                     | `AddApplicationInsightsTelemetryWorkerService()`                      | N/A                          | Connection string from environment / Key Vault reference |
| ContosoInsurance.Worker      | No audit trail                              | `ExportLog` row persisted after each successful upload                | N/A                          | Via shared `ContosoDbContext`                            |
| ContosoInsurance.Worker      | No health checks                            | Blob container existence + EF Core DB ping health checks              | N/A                          | Exposes `/healthz` readiness endpoint                    |
| ContosoInsurance.Common      | No blob abstraction                         | `IClaimDocumentStore` interface + `BlobClaimDocumentStore` impl       | `DefaultAzureCredential`     | In-memory fake provided for unit tests                   |
| ContosoInsurance.Data        | No `ExportLog` entity                       | `ExportLog` model + `DbSet<ExportLog>` in `ContosoDbContext`          | N/A                          | EF Core migration required                               |
| ContosoInsurance.Web (Track A)| `Upload.aspx.cs` writes to `C:\ClaimsFiles` | Consumes `IClaimDocumentStore` — writes to `claim-docs` Blob container| `DefaultAzureCredential`     | Cross-track coordination; Track A implements the change  |

---

## Tasks

### Task 1 — Add Azure SDK packages to Worker and Common

Add `Azure.Storage.Blobs` and `Azure.Identity` NuGet packages to `ContosoInsurance.Worker.csproj` and `ContosoInsurance.Common.csproj`. Pin to the latest stable versions with no known CVEs. These packages provide `BlobServiceClient`, `BlobContainerClient`, and `DefaultAzureCredential`.

**Files changed**: `ContosoInsurance.Worker.csproj`, `ContosoInsurance.Common.csproj`

### Task 2 — Introduce IClaimDocumentStore in Common

Create the `IClaimDocumentStore` abstraction in `ContosoInsurance.Common` with a single `UploadAsync(string containerName, string blobName, Stream content, CancellationToken ct)` method (or equivalent). Provide:
- `BlobClaimDocumentStore` — production implementation using `BlobServiceClient` + `DefaultAzureCredential`; creates the container if it does not exist.
- `InMemoryClaimDocumentStore` — fake implementation for unit tests; stores blobs in an `IReadOnlyDictionary<string, byte[]>` in memory.

Register `BlobClaimDocumentStore` as `IClaimDocumentStore` in a new `AddClaimDocumentStore(IConfiguration)` extension method so both Worker and Web hosts can use a single call.

**Files added**: `ContosoInsurance.Common/Storage/IClaimDocumentStore.cs`, `ContosoInsurance.Common/Storage/BlobClaimDocumentStore.cs`, `ContosoInsurance.Common/Storage/InMemoryClaimDocumentStore.cs`, `ContosoInsurance.Common/Storage/ClaimDocumentStoreExtensions.cs`

### Task 3 — Replace File.WriteAllText with async Blob upload in ClaimsExporterService

Update `ExportOptions` with a new `ContainerName` property (default `claim-exports`). Inject `IClaimDocumentStore` into `ClaimsExporterService` and replace `Directory.CreateDirectory` + `File.WriteAllText` with `IClaimDocumentStore.UploadAsync`. The blob name is the same timestamped CSV filename that was previously written to disk. Remove the `ExportRoot` usage for the actual write (can retain it for local-dev fallback or remove entirely per team preference). All existing export logic (column selection, `Csv()` helper, row count logging) is preserved verbatim.

**Files changed**: `ExportOptions.cs`, `ClaimsExporterService.cs`

### Task 4 — Add ExportLog entity and audit persistence

Add an `ExportLog` entity to `ContosoInsurance.Data.Models` with columns: `Id` (int, PK, identity), `BlobName` (nvarchar 512), `RowCount` (int), `ExportedAtUtc` (datetime2, default SYSUTCDATETIME()). Add `DbSet<ExportLog> ExportLogs` to `ContosoDbContext` and configure the entity in `OnModelCreating`. Add an EF Core migration. In `ClaimsExporterService.ExportAsync`, after a successful blob upload, resolve `ContosoDbContext` from a scoped DI scope (using `IServiceScopeFactory`) and insert an `ExportLog` row.

**Files added/changed**: `ContosoInsurance.Data/Models/ExportLog.cs`, `ContosoInsurance.Data/ContosoDbContext.cs`, EF Core migration files, `ClaimsExporterService.cs`, `Program.cs`

### Task 5 — Add Application Insights telemetry to Worker host

Add `Microsoft.ApplicationInsights.WorkerService` NuGet package to `ContosoInsurance.Worker.csproj`. Call `builder.Services.AddApplicationInsightsTelemetryWorkerService()` in `Program.cs` before `builder.Build()`. Read the Application Insights connection string from `ApplicationInsights:ConnectionString` in `appsettings.json` (value populated at runtime from Key Vault or environment variable — no hard-coded key).

**Files changed**: `ContosoInsurance.Worker.csproj`, `Program.cs`, `appsettings.json`

### Task 6 — Add health checks

Add `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` and `Azure.Storage.Blobs` health-check extensions to `ContosoInsurance.Worker.csproj`. In `Program.cs`, register:
- `AddDbContextCheck<ContosoDbContext>()` — pings the database via EF Core.
- A custom `BlobContainerHealthCheck` — calls `BlobContainerClient.ExistsAsync()` on the `claim-exports` container.

Expose the health endpoint via `IHostedService` writing results to the console log, or wire a minimal `MapHealthChecks("/healthz")` endpoint if an HTTP port is added to the worker host. Tag both checks as `"ready"`.

**Files added/changed**: `ContosoInsurance.Worker.csproj`, `Program.cs`, `ContosoInsurance.Worker/HealthChecks/BlobContainerHealthCheck.cs`

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? -> A: No — code migration only
- [x] Q: Should the plan include integration testing? -> A: No — unit tests via InMemoryClaimDocumentStore; no integration tests
- [x] Q: Should the plan include a security scan and CVE remediation task? -> A: Covered in prior plan (004); re-run after new packages are added
- [x] Q: Which Azure deployment target should the plan use? -> A: Azure Container Apps via `azd` — Track C owns infra; Worker outputs a Dockerfile
- [x] Q: Target .NET version? -> A: net9.0 (unchanged)
- [x] Q: Cross-track coordination for IClaimDocumentStore? -> A: Track A (Web) consumes the interface from Common; Track B (Worker) owns the implementation; shared contract agreed in Common
