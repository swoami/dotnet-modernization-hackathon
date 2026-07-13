# Reference solution — the target state

This is the "answer key". It describes what a well-modernized `ContosoInsurance`
looks like on Monday morning. Facilitators: keep this out of participants'
hands until after the demo, or share as a next-step reference.

The team does not need to hit every bullet. It's a north star.

---

## Solution layout (post-modernization)

```
src/ContosoInsurance/
  ContosoInsurance.sln
  ContosoInsurance.Common/         net9.0, SDK-style
    Logging/LoggingExtensions.cs   AddContosoLogging(this ILoggingBuilder)
    Configuration/ContosoOptions.cs
    Storage/IClaimDocumentStore.cs BlobClaimDocumentStore : IClaimDocumentStore
  ContosoInsurance.Data/           net9.0
    ContosoDbContext.cs            EF Core 9, DbSet<Claim/Policy/User/ExportLog>
    Migrations/                    EF migrations
    Claims/ClaimsService.cs        (was ClaimsRepository)
    Policies/PolicyService.cs
    Users/UserService.cs           PBKDF2 or ASP.NET Core Identity
  ContosoInsurance.Web/            net9.0, ASP.NET Core (Razor Pages)
    Program.cs                     DI + logging + auth + health + AI + EF Core
    Pages/Index.cshtml(.cs)        was Default.aspx
    Pages/Upload.cshtml(.cs)       was Upload.aspx
    Pages/Login.cshtml(.cs)        was Login.aspx (cookie auth or Entra ID)
    appsettings.json
    Dockerfile
  ContosoInsurance.Services/       net9.0, ASP.NET Core minimal API
    Program.cs                     app.MapPost("/claims/{id}/score", …)
    ScoringEndpoints.cs
    Dockerfile
    (Stretch: replaced by an in-process module in Web/)
  ContosoInsurance.Worker/         net9.0, Worker Service
    Program.cs                     Host.CreateApplicationBuilder + Worker
    ClaimsExporterWorker.cs        : BackgroundService, PeriodicTimer
    appsettings.json
    Dockerfile
  db/
    001-schema.sql                 legacy DDL (retained for reference)
infra/
  main.bicep
  main.bicepparam
  modules/
    containerapps.bicep
    sql.bicep
    storage.bicep
    keyvault.bicep
    monitoring.bicep
    identity.bicep
azure.yaml
.github/workflows/deploy.yml
```

---

## Key code shapes

### `ContosoInsurance.Web/Program.cs` (essentials)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddContosoLogging();

builder.Services.AddDbContext<ContosoDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("ContosoDb")));

builder.Services.AddScoped<IClaimDocumentStore, BlobClaimDocumentStore>();
builder.Services.AddScoped<ClaimsService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddHttpClient("Scoring", c =>
    c.BaseAddress = new Uri(builder.Configuration["Scoring:BaseUrl"]!));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Login"; });
builder.Services.AddAuthorization();

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContosoDbContext>()
    .AddAzureBlobStorage(_ => new BlobServiceClient(
        new Uri(builder.Configuration["Storage:BlobEndpoint"]!),
        new DefaultAzureCredential()));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapHealthChecks("/health");
app.Run();
```

### SQL connection string (Managed Identity)

```json
"ConnectionStrings": {
  "ContosoDb": "Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=ContosoInsurance;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
}
```

Both locally (developer AAD) and in Container Apps (user-assigned MI), the
`Microsoft.Data.SqlClient` provider resolves credentials via
`DefaultAzureCredential`.

### `BlobClaimDocumentStore` (essentials)

```csharp
public class BlobClaimDocumentStore(BlobServiceClient client,
                                    IOptions<StorageOptions> options,
                                    ILogger<BlobClaimDocumentStore> log)
    : IClaimDocumentStore
{
    public async Task<Uri> SaveDocumentAsync(int claimId, string filename,
                                             Stream content, CancellationToken ct)
    {
        var container = client.GetBlobContainerClient(options.Value.DocsContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient($"{claimId}/{Path.GetFileName(filename)}");
        await blob.UploadAsync(content, overwrite: true, ct);
        log.LogInformation("Saved claim {ClaimId} doc {Uri}", claimId, blob.Uri);
        return blob.Uri;
    }
}
```

### `ClaimsExporterWorker` (essentials)

```csharp
public sealed class ClaimsExporterWorker(
    IServiceScopeFactory scopes,
    BlobServiceClient blobs,
    IOptions<ExportOptions> options,
    ILogger<ClaimsExporterWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        do
        {
            try { await ExportOnceAsync(stoppingToken); }
            catch (Exception ex) { log.LogError(ex, "Export failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExportOnceAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();

        var claims = await db.Claims.AsNoTracking()
            .OrderByDescending(c => c.FiledOn).Take(1000).ToListAsync(ct);

        var container = blobs.GetBlobContainerClient(options.Value.ExportsContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient($"claims-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");

        await using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, leaveOpen: true);
        await writer.WriteLineAsync("ClaimId,PolicyNumber,ClaimantName,Amount,Status,FiledOn,Score");
        foreach (var c in claims)
            await writer.WriteLineAsync($"{c.ClaimId},{c.Policy?.PolicyNumber},{Csv(c.ClaimantName)},{c.Amount},{c.Status},{c.FiledOn:O},{c.Score}");
        await writer.FlushAsync(ct);
        ms.Position = 0;

        await blob.UploadAsync(ms, overwrite: true, ct);
        db.ExportLog.Add(new ExportLog { FilePath = blob.Uri.ToString(), RowCount = claims.Count });
        await db.SaveChangesAsync(ct);

        log.LogInformation("Exported {Count} rows to {Uri}", claims.Count, blob.Uri);
    }
}
```

---

## Bicep shape (informal)

```bicep
targetScope = 'resourceGroup'

param location string = resourceGroup().location
param environmentName string
param sqlAdminGroupObjectId string
param sqlAdminGroupName string

var tags = { 'azd-env-name': environmentName }

module identity 'modules/identity.bicep'   = { name: 'id',   params: { location: location, tags: tags, name: 'id-contoso-${environmentName}' } }
module monitor  'modules/monitoring.bicep' = { name: 'mon',  params: { location: location, tags: tags, name: 'contoso-${environmentName}' } }
module storage  'modules/storage.bicep'    = { name: 'stg',  params: { location: location, tags: tags, name: 'stcontoso${uniqueString(environmentName)}', principalId: identity.outputs.principalId } }
module sql      'modules/sql.bicep'        = { name: 'sql',  params: { location: location, tags: tags, name: 'sql-contoso-${environmentName}', aadGroupObjectId: sqlAdminGroupObjectId, aadGroupName: sqlAdminGroupName } }
module kv       'modules/keyvault.bicep'   = { name: 'kv',   params: { location: location, tags: tags, name: 'kv-contoso-${environmentName}', principalId: identity.outputs.principalId } }
module apps     'modules/containerapps.bicep' = {
  name: 'apps'
  params: {
    location: location
    tags: tags
    environmentName: environmentName
    identityId: identity.outputs.id
    identityPrincipalId: identity.outputs.principalId
    logAnalyticsId: monitor.outputs.logAnalyticsId
    appInsightsConnectionString: monitor.outputs.appInsightsConnectionString
    sqlServerFqdn: sql.outputs.serverFqdn
    sqlDatabaseName: sql.outputs.databaseName
    storageAccountName: storage.outputs.accountName
    keyVaultUri: kv.outputs.uri
  }
}

output WEB_URL            string = apps.outputs.webUrl
output SCORING_URL        string = apps.outputs.scoringUrl
output APPINSIGHTS_CS     string = monitor.outputs.appInsightsConnectionString
output SQL_SERVER         string = sql.outputs.serverName
output STORAGE_ACCOUNT    string = storage.outputs.accountName
```

Every module receives the managed identity's `principalId` and sets the role
assignments it owns (Storage → Blob Data Contributor; Key Vault → Secrets User;
ACR → AcrPull; SQL → post-deploy AAD user script).

---

## `azure.yaml` (essentials)

```yaml
name: contoso-insurance
services:
  web:
    project: src/ContosoInsurance/ContosoInsurance.Web
    language: dotnet
    host: containerapp
    docker:
      path: ./Dockerfile
  scoring:
    project: src/ContosoInsurance/ContosoInsurance.Services
    language: dotnet
    host: containerapp
    docker:
      path: ./Dockerfile
  worker:
    project: src/ContosoInsurance/ContosoInsurance.Worker
    language: dotnet
    host: containerapp
    docker:
      path: ./Dockerfile
```

---

## What the team almost certainly won't finish in one day

- WebForms → **Blazor Server** (Razor Pages is realistic; Blazor is stretch)
- WCF → **CoreWCF** with real WSDL parity (a minimal-API replacement is realistic)
- **Entra ID** end-to-end (cookie auth is realistic; Entra is stretch)
- Full **integration test suite** running in GitHub Actions
- **Blue/green** or slot-based Container Apps deploy

Log these as follow-up issues at the retro. They are perfect Sprint 2
material for whichever squad continues the work.
