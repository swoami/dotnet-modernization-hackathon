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
└── db/                            SQL Server schema + seed
```

Target framework: **.NET Framework 4.6.1** (all projects).

## Legacy "features" (intentional pain points)

| Concern | Legacy pattern | Where to look |
| --- | --- | --- |
| Framework | .NET Fx 4.6.1, `packages.config` | every `*.csproj` |
| Config | `appsettings.json` + `IConfiguration` (`AppSettings` / `ConnectionStrings`) | `Common/Config/ConfigHelper.cs` |
| Data access | Raw ADO.NET, inline SQL, string-concat parameters in one place | `Data/ClaimsRepository.cs` |
| Logging | `Microsoft.Extensions.Logging` console provider + `Trace.WriteLine` | `Common/Logging/AppLogger.cs` |
| Local file I/O | Writes to `C:\ClaimsFiles\` and `C:\Exports\` | `Web/Upload.aspx.cs`, `Worker/ClaimsExporterService.cs` |
| Secrets | Plain-text SQL user/password in config | `Web/Web.config`, `Worker/App.config` |
| Hosting | IIS (Web + WCF) + Windows Service (Worker) | `*.csproj`, `Worker/ProjectInstaller.cs` |
| Comms | WCF `basicHttpBinding` SOAP | `Services/*` |
| Packages | `Microsoft.Extensions.*` package references in Common | `Common/*.csproj` |

## Building (optional — not required for the hackathon)

Building the legacy app is **not required** to modernize it. If you insist:

- Visual Studio 2019 or 2022 with the *ASP.NET and web development* workload
- .NET Framework 4.6.1 developer pack
- SQL Server (LocalDB is fine); run `db/001-schema.sql` then `db/002-seed.sql`

Update the `<connectionStrings>` in `Web/Web.config`, `Services/Web.config`, and
`Worker/App.config`.

## Do NOT

- Deploy this app anywhere real.
- Reuse its secrets, credential handling, or file I/O patterns.
