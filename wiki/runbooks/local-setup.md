# Runbook: Local Setup

> Sources: `src/ContosoInsurance/README.md`, project files, `db/001-schema.sql`, `db/002-seed.sql`. Building is **optional** for the modernization hackathon.

## Summary

The legacy app targets .NET Framework 4.6.1 and is designed to run on Windows with IIS, a Windows Service, and SQL Server. Running it locally is not required to modernize it.

## Prerequisites (per README)

| Item | Notes |
|---|---|
| Visual Studio 2019/2022 | ASP.NET and web development workload |
| .NET Framework 4.6.1 developer pack | Required target framework |
| SQL Server (LocalDB is fine) | For schema/seed |
| Windows | IIS + Windows Service hosting |

## Database setup

1. Run `db/001-schema.sql` (creates DB `ContosoInsurance` + tables).
2. Run `db/002-seed.sql` (seeds users, policies, claims).

## Configuration

Update the `<connectionStrings>` named `ContosoDb` in:

- `ContosoInsurance.Web/Web.config`
- `ContosoInsurance.Services/Web.config`
- `ContosoInsurance.Worker/App.config`

Default local paths in use: `C:\ClaimsFiles`, `C:\Exports`, `C:\Logs`.

## Run model

- Web + WCF: host under IIS.
- Worker: install/run as a Windows Service (`ProjectInstaller`), or run the executable.

## Warnings

- Do not deploy this app anywhere real (per README).
- Contains intentional insecure patterns; do not reuse its secrets or file I/O.

## Unknowns

- Exact IIS site/binding configuration not defined in the repo. `Pendiente/Unknown`.
- Local build not verified in this environment. `Pendiente/Unknown`.

## Related pages
- [[build-and-test]]
- [[arch-infrastructure]]
- [[arch-configuration]]
