# Concept: Logging

> Sources: `ContosoInsurance.Common/Logging/AppLogger.cs`, `Web/Web.config`, `Worker/App.config`.

## Summary

A static `AppLogger` provides `Info`/`Warn`/`Error` methods that write to log4net and `System.Diagnostics.Trace`.

## Current flow

1. `AppLogger.Configure()` invokes `XmlConfigurator.Configure()` once (idempotent via a flag).
2. Each log call writes to both the log4net logger `"ContosoInsurance"` and a corresponding `Trace` method.
3. log4net `RollingFileAppender` writes to `C:\Logs\*.log` (configured in Web and Worker configs).

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Common/Logging/AppLogger.cs` | Static logger facade |
| `ContosoInsurance.Web/Web.config` | log4net appender (Web) |
| `ContosoInsurance.Worker/App.config` | log4net appender (Worker) |

## Rules / behavior

- Root level INFO; 10MB rolling files, 5 backups.
- `Error` includes exception detail in both sinks.

## Risks / legacy issues

- log4net 2.0.8 (known CVEs).
- Static logger, no DI, no structured logging or App Insights.
- File logging to `C:\Logs` (local dependency).

## Unknowns

- `Services` project has no visible log4net section in its `Web.config`. `Pendiente/Unknown`.

## Related pages
- [[arch-logging]]
- [[arch-configuration]]
