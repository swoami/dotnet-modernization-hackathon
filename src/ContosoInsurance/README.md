# ContosoInsurance (Legacy)

A deliberately-legacy claims processing sample for the Legacy Breakers hackathon.

## Solution layout

```
ContosoInsurance.sln
├── ContosoInsurance.Common        Class library (Microsoft.Extensions.Logging, IConfiguration helpers)
├── ContosoInsurance.Data          Class library (ADO.NET, SqlCommand/SqlDataReader)
├── ContosoInsurance.Services      WCF service host (SOAP claim scoring)
├── ContosoInsurance.Web           ASP.NET WebForms agent portal
├── ContosoInsurance.Worker        Windows Service (nightly claims export)
└── ContosoInsurance.Data/Migrations   EF Core migrations (schema + applied at startup)
```

Target framework: **.NET Framework 4.6.1** (all projects).

## Legacy "features" (intentional pain points)

| Concern | Legacy pattern | Where to look |
| --- | --- | --- |
| Framework | .NET Fx 4.6.1, `packages.config` | every `*.csproj` |
| Config | `appsettings.json` + host-provided `IConfiguration` (`AppSettings` / `ConnectionStrings`) | each application's `Program.cs` |
| Data access | Raw ADO.NET, inline SQL, string-concat parameters in one place | `Data/ClaimsRepository.cs` |
| Logging | `ILogger` through the shared host logging pipeline | `Common/Logging/ContosoLoggingExtensions.cs` |
| Local file I/O | Writes to `C:\ClaimsFiles\` and `C:\Exports\` | `Web/Upload.aspx.cs`, `Worker/ClaimsExporterService.cs` |
| Secrets | Plain-text SQL user/password in config | `Web/Web.config`, `Worker/App.config` |
| Hosting | IIS (Web + WCF) + Windows Service (Worker) | `*.csproj`, `Worker/ProjectInstaller.cs` |
| Comms | WCF `basicHttpBinding` SOAP | `Services/*` |
| Packages | `Microsoft.Extensions.*` package references in Common | `Common/*.csproj` |

## Building (optional — not required for the hackathon)

Building the legacy app is **not required** to modernize it. If you insist:

- Visual Studio 2019 or 2022 with the *ASP.NET and web development* workload
- .NET Framework 4.6.1 developer pack
- SQL Server (LocalDB is fine); the Web app applies EF Core migrations and
  seeds data automatically at startup (see *Database initialization* below)

Update the `<connectionStrings>` in `Web/Web.config`, `Services/Web.config`, and
`Worker/App.config`.

## Database initialization

`ContosoInsurance.Web` initializes the database at startup (`DbInitializer` in
`ContosoInsurance.Data`):

1. Applies pending EF Core migrations (creates the database if missing).
   Databases created by the old `db/*.sql` scripts are detected and baselined
   automatically.
2. Seeds one demo login account per role (skipped if they already exist).
3. Seeds sample policies/claims when the tables are empty.

Configuration switches (`appsettings.json` or environment variables):

| Key | Default | Effect |
| --- | --- | --- |
| `Database:AutoMigrate` | `true` | Apply migrations + seed at startup |
| `Database:SeedSampleData` | `true` | Seed sample policies/claims |

To add a new migration:

```
dotnet ef migrations add <Name> --project ContosoInsurance.Data --startup-project ContosoInsurance.Web
```

## Local development login accounts

The startup seeder creates exactly one account per role; all use the
documented demo password `Password1`.

| Role | Username |
| --- | --- |
| Agent | `agent1` |
| Adjuster | `adjuster` |
| Admin | `admin` |

These accounts are only for local development. Set `Database:AutoMigrate` to
`false` (or rotate the accounts) in any environment that must not have the
demo password.

## Do NOT

- Deploy this app anywhere real.
- Reuse its secrets, credential handling, or file I/O patterns.
