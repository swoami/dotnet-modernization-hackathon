# Task briefs — per track

Everyone reads their own brief. Every track uses the same loop:

1. Open the appmod extension → run **Assess** on the scoped folder.
2. Review the generated plan.
3. Execute tasks one-by-one, reviewing each diff.
4. Commit after each accepted task; push to your feature branch.
5. Fall back to Copilot Chat / Agent mode for anything the extension can't cover.

The **suggested Copilot prompts** in each brief are starting points — expect to
iterate.

---

## Track A — Web + API tier

**Owners:** 2 people. **Branch:** `track/a-web-api`.

**Scope:**
- `ContosoInsurance.Web/` (WebForms → ASP.NET Core Razor Pages)
- `ContosoInsurance.Services/` (WCF → ASP.NET Core minimal API)
- `ContosoInsurance.Data/` (ADO.NET → EF Core 9) — **shared with Track B**
- `ContosoInsurance.Common/` (log4net → `ILogger`) — **shared with all tracks**

### Sprint 1 — Upgrade

1. **Assess** `ContosoInsurance.Web` and `ContosoInsurance.Services`.
2. Convert `.csproj` files to SDK-style, target `net9.0`.
3. Delete `packages.config`. Migrate to `PackageReference`.
4. Upgrade / replace vulnerable packages:
   - `log4net` → remove entirely (Track A leads the `ILogger` swap in `Common`)
   - `Newtonsoft.Json` → `System.Text.Json` (or latest Newtonsoft if EF Core requires it)
5. Convert `Web.config` → `appsettings.json` + `IConfiguration`. `machineKey`,
   `system.webServer`, `sessionState`, `authentication` sections either drop
   or move into `Program.cs`.
6. **WebForms**: two options — pick one per pair judgement:
   - **Minimum**: convert `Default.aspx`, `Login.aspx`, `Upload.aspx` to Razor Pages.
   - **Stretch**: Blazor Server.
7. **WCF**: replace `IClaimScoringService` SOAP endpoint with an ASP.NET Core
   minimal API (`POST /claims/{id}/score`). Update `Default.aspx`'s replacement
   to call it via `HttpClient` + `IHttpClientFactory` (stretch: CoreWCF).
8. **Data**: introduce `ContosoDbContext` with `DbSet<Claim>`, `DbSet<Policy>`,
   `DbSet<User>`. Coordinate with Track B — the DbContext is shared.
9. **Login flow**: replace Forms Auth with ASP.NET Core cookie auth for now.
   Stretch: Entra ID via `Microsoft.Identity.Web`.

### Sprint 2 — Azure migration

10. Swap SQL connection to **Managed Identity**:
    `Server=<sql>.database.windows.net;Authentication=Active Directory Default;`
    Nothing else in the connection string. `DefaultAzureCredential` locally.
11. Replace `log4net` with `AddApplicationInsightsTelemetry` + `ILogger<T>`
    everywhere in `Common`, `Web`, `Services`.
12. Remove `Trace.*` calls.
13. Health endpoint: `/health` returning `Healthy` if DbContext connects.

### Suggested Copilot prompts (Track A)

- *"Convert this WebForms Default.aspx and code-behind to an ASP.NET Core Razor Page. Reuse the existing repository interface for now."*
- *"Migrate ClaimsRepository to EF Core 9 using a new ContosoDbContext. Keep the current public method signatures so callers don't break."*
- *"Rewrite the WCF ClaimScoringService as an ASP.NET Core minimal API endpoint POST /claims/{id}/score."*
- *"Replace log4net configuration in Common.AppLogger with Microsoft.Extensions.Logging. Provide an ILoggerFactory-based static helper for now to keep call sites compiling."*
- *"This SQL uses string concatenation — convert to LINQ or a parameterized EF query and explain the injection risk you removed."* (target: `ClaimsRepository.SearchByClaimant`)

### Track A "done" checklist

- [x] `Web`, `Services`, `Data`, `Common` target `net9.0`
- [x] No `packages.config`, no `Web.config`
- [x] `Program.cs` composes the app with DI, config, logging, cookie auth
- [ ] `ContosoDbContext` in use; ADO.NET removed from `Data/`
- [ ] Concat-SQL search converted to safe query
- [ ] SHA1 password hashing replaced with a modern KDF (or Entra ID stretch)
- [ ] `ILogger` everywhere; log4net removed
- [x] Scoring endpoint answers over HTTP JSON, no more WCF client

---

## Track B — Worker + Storage

**Owners:** 2 people. **Branch:** `track/b-worker-storage`.

**Scope:**
- `ContosoInsurance.Worker/` (Windows Service → .NET Worker Service)
- Local file I/O in `Web/Upload.aspx.cs` and `Worker/ClaimsExporterService.cs`
  → Azure Blob Storage
- Contributes `DbSet`s / migrations to `Data/` project (shared with Track A)

### Sprint 1 — Upgrade

1. **Assess** `ContosoInsurance.Worker`.
2. Convert `.csproj` to SDK-style, target `net9.0`.
3. Drop `System.ServiceProcess`, `ProjectInstaller`.
4. Convert `ClaimsExporterService : ServiceBase` → a `BackgroundService`
   hosted via `Host.CreateApplicationBuilder`.
5. Replace `System.Timers.Timer` with a `PeriodicTimer` or scheduled loop with
   `IHostApplicationLifetime`.
6. Move `App.config` values into `appsettings.json` + strongly-typed options
   (`ExportOptions`).
7. Replace `log4net` with `ILogger<ClaimsExporterService>` (coordinate with
   Track A on `Common` cleanup).

### Sprint 2 — Azure migration

8. Add `Azure.Storage.Blobs` + `Azure.Identity`. Use `BlobServiceClient` with
   `DefaultAzureCredential` — **no connection strings**.
9. Replace `File.WriteAllText(...)` in the exporter with
   `BlobContainerClient.UploadBlobAsync`. Container: `claim-exports`.
10. Coordinate with **Track A**: `Upload.aspx.cs` (or its Razor Pages
    replacement) also writes to Blob (container: `claim-docs`), not disk.
    Suggested: extract an `IClaimDocumentStore` in `Common` used by both.
11. Add `AddApplicationInsightsTelemetryWorkerService()` in the worker host.
12. Persist an audit row to `ExportLog` after each successful upload (via
    the shared `DbContext`).

### Suggested Copilot prompts (Track B)

- *"Convert this Windows Service (ServiceBase + Timer) to a .NET Worker Service using BackgroundService and PeriodicTimer. Preserve the existing export logic verbatim."*
- *"Introduce IClaimDocumentStore with an Azure Blob Storage implementation using BlobServiceClient + DefaultAzureCredential. Provide a fake in-memory implementation for tests."*
- *"Replace this File.WriteAllText call with an async Blob upload. Container name should come from options."*
- *"Add health checks: a check that the blob container exists and one that pings the database via EF Core."*

### Track B "done" checklist

- [ ] Worker targets `net9.0`, runs as `BackgroundService`
- [ ] No `ServiceBase`, `ProjectInstaller`, `System.Timers.Timer`
- [ ] `App.config` gone; `appsettings.json` + typed options
- [ ] Exports written to Blob container `claim-exports` via Managed Identity
- [ ] Uploads (from Web) written to Blob container `claim-docs`
- [ ] `ExportLog` row inserted per export
- [ ] `ILogger` everywhere; log4net removed

---

## Track C — Platform (containerization, IaC, CI/CD, identity, observability)

**Owners:** 2 people. **Branch:** `track/c-platform`.

**Scope:**
- Repo-level `.azure/`, `azure.yaml`, `infra/` (Bicep), Dockerfiles
- GitHub Actions workflow
- Managed identity + Key Vault + App Insights wiring at the *infra* level (the
  application-side wiring is done by Tracks A and B; Track C owns the
  cloud resources they consume)

### Sprint 1 — Foundation

1. `azd init` at `src/ContosoInsurance/` (or repo root, pair's choice).
   Choose "existing app" template. This produces `azure.yaml` + `infra/` skeleton.
2. In parallel with Tracks A/B, generate **Dockerfiles**:
   - Web (multi-stage: .NET 9 SDK build → aspnet runtime)
   - Services (or, if merged into Web by Track A, skip)
   - Worker (multi-stage: SDK → runtime, no ASP.NET base)
   Use the appmod extension's **containerization plan** first, then refine.
3. Draft `infra/main.bicep` provisioning:
   - Resource group scope (use the shared RG name at deploy time)
   - Log Analytics workspace
   - Application Insights (workspace-based)
   - Azure Container Registry
   - User-assigned managed identity
   - Azure Container Apps environment
   - Container Apps: `web`, `services` (if separate), `worker`
   - Azure SQL server + database + AAD admin group
   - Key Vault
   - Storage account + containers `claim-docs`, `claim-exports`
   - Role assignments:
     - Managed identity → SQL DB user (via post-deploy script or `Microsoft.Sql/servers/administrators`)
     - Managed identity → Storage Blob Data Contributor on the account
     - Managed identity → Key Vault Secrets User
     - Managed identity → ACR Pull

### Sprint 2 — Cloud readiness

4. Wire **App Insights** connection string into all Container Apps as env vars.
5. Wire **SQL connection string** (server-only, MI-based) as env var.
6. Wire **Storage account name** as env var (containers derived).
7. Optional secrets that must remain secrets (e.g., a third-party API key
   for a stretch goal) → Key Vault, referenced via Container Apps `secrets`
   with `keyVaultUrl` + `identity`.
8. **GitHub Actions**: `.github/workflows/deploy.yml`. On push to `main`:
   - Login via OIDC to Azure
   - `dotnet build` and `dotnet test`
   - `azd deploy` (assumes `azd provision` already ran once)
   Or single-shot `azd up` if the workflow provisions on first run.
9. Add a `README-DEPLOY.md` under `docs/` that captures the deploy commands
   and post-deploy verification steps.

### Suggested Copilot prompts (Track C)

- *"Generate a multi-stage Dockerfile for this ASP.NET Core 9 web app. Use `aspnet:9.0-alpine` for runtime and set a non-root user."*
- *"Write Bicep for an Azure Container Apps environment, a user-assigned managed identity, an ACR, Azure SQL, and a storage account with two containers. Assign the managed identity Storage Blob Data Contributor and AcrPull."*
- *"Update the Container App resource so it pulls from our ACR using the user-assigned identity, and injects APPLICATIONINSIGHTS_CONNECTION_STRING and SQL_CONNECTION_STRING as env vars."*
- *"Generate a GitHub Actions workflow that authenticates to Azure via OIDC and runs `azd deploy` on push to main. Assume the federated credential is already configured."*
- *"Show me the exact steps to assign this managed identity as an Azure SQL user with db_datareader and db_datawriter."*

### Track C "done" checklist

- [ ] `azure.yaml` at repo (or `src/ContosoInsurance/`) root
- [ ] `infra/main.bicep` + `infra/main.bicepparam` provisioning full stack
- [ ] Dockerfiles for each container-hosted service
- [ ] User-assigned managed identity attached to all Container Apps
- [ ] Managed identity has: Blob Contributor, AcrPull, Key Vault Secrets User, SQL DB user
- [ ] App Insights + Log Analytics deployed and wired
- [ ] No secrets in Bicep parameters, `azure.yaml`, or GitHub Actions except OIDC creds
- [ ] Workflow `.github/workflows/deploy.yml` builds + deploys on push to main
- [ ] `docs/README-DEPLOY.md` documents `azd up` / rollback / redeploy
