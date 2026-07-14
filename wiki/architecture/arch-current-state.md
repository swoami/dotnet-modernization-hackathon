# Current State Architecture

> Sources: `src/ContosoInsurance/README.md`, `ContosoInsurance.sln`, all project folders, `db/`.

## Summary

`ContosoInsurance` is a .NET Framework 4.6.1 multi-process monolith: a WebForms portal, a WCF SOAP scoring service, a Windows Service exporter, and two shared class libraries, all backed by one SQL Server database and the local filesystem.

## Current state

Layered design split across processes:

- **UI/Web** — ASP.NET WebForms portal ([[arch-webforms]]).
- **Service** — WCF SOAP scoring service ([[arch-wcf-service]]).
- **Background** — Windows Service CSV exporter ([[arch-worker]]).
- **Data** — ADO.NET repositories + POCO models ([[arch-database]]).
- **Cross-cutting** — config helper + static logger in `Common` ([[arch-configuration]], [[arch-logging]]).
- **Storage** — SQL Server + local `C:\` filesystem.

Web calls the WCF service via `ChannelFactory<IClaimScoringService>` over `BasicHttpBinding`. Web and Worker both use `ClaimsRepository`.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `src/ContosoInsurance/ContosoInsurance.sln` | Solution (5 projects) | — |
| `ContosoInsurance.Common/` | Config + logging | Shared by all |
| `ContosoInsurance.Data/` | ADO.NET data access | Shared by Web, Services, Worker |
| `ContosoInsurance.Services/` | WCF SOAP host | Scoring |
| `ContosoInsurance.Web/` | WebForms portal | UI |
| `ContosoInsurance.Worker/` | Windows Service | Export |
| `db/` | SQL schema + seed | `001-schema.sql`, `002-seed.sql` |

## Key dependencies

- .NET Framework 4.6.1
- log4net 2.0.8, Newtonsoft.Json 11.0.2 (`packages.config`)
- `System.ServiceModel` (WCF), `System.ServiceProcess` (Windows Service), `System.Data.SqlClient` (ADO.NET)

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| .NET Fx 4.6.1 + `packages.config` | Not container/cloud native | all `.csproj` |
| Multi-process, machine-bound hosting | Hard to deploy off-Windows | IIS + Windows Service |
| Shared static logger + `ConfigurationManager` | No DI, no testability | `Common/` |
| Local `C:\` filesystem dependency | Won't run in containers | Upload, Exporter, log config |
| Web uses `IClaimScoringService` with no reference to `Services` | **Confirmed build break (CS0234)** | `ContosoInsurance.Web.csproj`, `Default.aspx.cs`; see [[build-and-test]], [[arch-wcf-service]] |

## Baseline buildability (2026-07-13)

- **Restore**: ✅ succeeds via `msbuild -t:Restore -p:RestorePackagesConfig=true`.
- **Build (as-is)**: ❌ fails. Machine lacks the **.NET Fx 4.6.1 targeting pack** (MSB3644) and CLI `VSToolsPath` misresolves web targets (MSB4226) — both are environment prerequisites, fixable outside the code.
- **Compile (env worked around)**: only one code error remains — the Web→Services **CS0234** gap. Common, Data, Services, Worker compile cleanly. Details in [[build-and-test]].

## Unknowns

- No test project found. `Pendiente/Unknown`.
- Exact IIS site/binding layout not defined in repo. `Pendiente/Unknown`.

## Related pages
- [[overview]]
- [[arch-webforms]]
- [[arch-wcf-service]]
- [[arch-worker]]
- [[arch-database]]
- [[arch-infrastructure]]
