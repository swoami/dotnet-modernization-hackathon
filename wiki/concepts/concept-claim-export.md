# Concept: Claim Export

> Sources: `ContosoInsurance.Worker/ClaimsExporterService.cs`, `ContosoInsurance.Worker/App.config`.

## Summary

A Windows Service periodically exports recent claims to a timestamped CSV file on local disk.

## Current flow

1. On start, `ClaimsExporterService` configures logging and reads `ExportIntervalMinutes` (default 60).
2. A `System.Timers.Timer` triggers `ExportSafely` on interval; an export also runs once immediately at start.
3. `Export()` reads up to 1000 claims via `ClaimsRepository.GetRecent(1000)` and writes CSV to `C:\Exports\claims-{yyyyMMdd-HHmmss}.csv` with `File.WriteAllText`.
4. Values are CSV-escaped via a local `Csv` helper.

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Worker/ClaimsExporterService.cs` | Timer + export logic |
| `ContosoInsurance.Worker/App.config` | `ExportRoot`, `ExportIntervalMinutes` |

## Rules / behavior

- Header row: `ClaimId,PolicyNumber,ClaimantName,Amount,Status,FiledOn,Score`.
- Exceptions during export are caught and logged; the timer continues.

## Risks / legacy issues

- Writes to `C:\Exports` (local dependency).
- Does **not** write an `ExportLog` audit row despite the table existing.
- Windows Service + `System.Timers.Timer` (legacy hosting). See [[arch-worker]].

## Resolved (2026-07-13)

- **`ExportLog` is never written** by the exporter. `Export()` only writes the CSV file (`File.WriteAllText`); there is no `ExportLog` model or repository, and no INSERT into `dbo.ExportLog`. Confirmed. See [[entity-export-log]].

## Related pages
- [[entity-claim]]
- [[entity-export-log]]
- [[arch-worker]]
