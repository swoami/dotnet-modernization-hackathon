# Worker Service

> Sources: `ContosoInsurance.Worker/` (`ClaimsExporterService.cs`, `Program.cs`, `ProjectInstaller.cs`, `App.config`).

## Summary

A Windows Service (`ServiceBase`) that periodically exports recent claims to a CSV file on local disk.

## Current state

- `Program.Main` runs the service via `ServiceBase.Run(new ClaimsExporterService())`.
- `ClaimsExporterService` (`ServiceBase`) configures logging on start, reads `ExportIntervalMinutes` (default 60), and uses a `System.Timers.Timer` to trigger exports; it also runs once immediately on start.
- `Export()` reads up to 1000 claims via `ClaimsRepository.GetRecent(1000)` and writes a CSV to `C:\Exports\claims-{yyyyMMdd-HHmmss}.csv` using `File.WriteAllText`.
- `ProjectInstaller.cs` provides Windows Service installation metadata.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Worker/Program.cs` | Service entry point | `ServiceBase.Run` |
| `ContosoInsurance.Worker/ClaimsExporterService.cs` | Timer-driven CSV export | Writes to `C:\Exports` |
| `ContosoInsurance.Worker/ProjectInstaller.cs` | Service install metadata | — |
| `ContosoInsurance.Worker/App.config` | Config + connection string | Plaintext secret |

## Key dependencies

- `System.ServiceProcess` (`ServiceBase`), `System.Timers.Timer`
- `ContosoInsurance.Data` (`ClaimsRepository`)
- `ContosoInsurance.Common` (config, logging)

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| Windows Service hosting | Windows-only, not container-native | `Program.cs`, `ProjectInstaller.cs` |
| `System.Timers.Timer` | No graceful async lifecycle | `ClaimsExporterService.cs` |
| Writes to `C:\Exports` | Won't run in containers | `ClaimsExporterService.Export` |
| `ExportLog` table never written | Audit gap | Table exists in `db/001-schema.sql` |

## Unknowns

- Whether the missing `ExportLog` write is intentional. `Pendiente/Unknown`.

## Related pages
- [[concept-claim-export]]
- [[entity-export-log]]
- [[arch-database]]
