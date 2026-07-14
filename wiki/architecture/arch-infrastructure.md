# Infrastructure & Deployment

> Sources: `src/ContosoInsurance/README.md`, project files, `Web.config`/`App.config`. Repository searched for Dockerfile / `azure.yaml` / `.bicep` / workflow files (none found).

## Summary

The app is designed to run on Windows: Web + WCF under IIS and the exporter as a Windows Service, against SQL Server and the local filesystem. There is no containerization, IaC, or CI/CD in the repository.

## Current state

- **Web + WCF**: hosted in IIS (WebForms site + `.svc` endpoint).
- **Worker**: installed and run as a Windows Service (`ServiceBase.Run`, `ProjectInstaller`).
- **Database**: SQL Server / LocalDB; initialized with `db/001-schema.sql` then `db/002-seed.sql`.
- **Filesystem**: documents in `C:\ClaimsFiles`, exports in `C:\Exports`, logs in `C:\Logs`.
- **Build**: Visual Studio 2019/2022 with the ASP.NET workload + .NET Fx 4.6.1 dev pack (per README); building is optional for the hackathon.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `src/ContosoInsurance/README.md` | Build/run instructions | VS + IIS + SQL Server |
| `ContosoInsurance.Worker/ProjectInstaller.cs` | Windows Service install | — |
| `db/001-schema.sql`, `db/002-seed.sql` | DB provisioning | Manual |

## Key dependencies

- IIS, Windows Service host, SQL Server, Windows filesystem

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| No containers | Manual, machine-bound deploy | no Dockerfile |
| No IaC | Environments provisioned by hand | no `.bicep`/`azure.yaml` |
| No CI/CD | No automated build/test/deploy | no `.github/workflows` |
| Hard `C:\` path dependencies | Blocks portable/cloud hosting | app settings, log config |
| SQL password auth | Blocks Managed Identity | connection strings |

## Unknowns

- No automated tests found to gate a pipeline. `Pendiente/Unknown`.
- Local build not verified in this environment. `Pendiente/Unknown`.

## Related pages
- [[arch-current-state]]
- [[local-setup]]
- [[build-and-test]]
