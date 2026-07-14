# Logging & Observability

> Sources: `ContosoInsurance.Common/Logging/AppLogger.cs`, `Web/Web.config`, `Worker/App.config`, `packages.config`.

## Summary

Logging is done through a static `AppLogger` that wraps log4net 2.0.8 and also writes to `System.Diagnostics.Trace`. Output goes to local log files.

## Current state

- `AppLogger.Configure()` calls `XmlConfigurator.Configure()` once; `Info`/`Warn`/`Error` write to both log4net and `Trace`.
- log4net `RollingFileAppender` configured in `Web/Web.config` (`C:\Logs\ContosoInsurance.Web.log`) and `Worker/App.config` (`C:\Logs\ContosoInsurance.Worker.log`), 10MB rolling, 5 backups, level INFO.
- The **`Services` project has no log4net configuration**: `Services/Web.config` has no `log4net` `configSection` or `<log4net>` block, `Services.csproj` has no direct `log4net` reference (only transitive via `Common`), and `ClaimScoringService` never calls `AppLogger.Configure()`. Its log4net calls are therefore unconfigured (dropped by log4net); only the `Trace` sink in `AppLogger` still emits.
- No structured logging, correlation IDs, metrics, distributed tracing, or Application Insights.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Common/Logging/AppLogger.cs` | Static logger | log4net + `Trace` |
| `ContosoInsurance.Web/Web.config` | log4net appender config | `C:\Logs\...Web.log` |
| `ContosoInsurance.Worker/App.config` | log4net appender config | `C:\Logs\...Worker.log` |
| `ContosoInsurance.Common/packages.config` | log4net 2.0.8 reference | Known CVEs |

## Key dependencies

- log4net 2.0.8
- `System.Diagnostics.Trace`

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| log4net 2.0.8 (outdated) | Known CVEs | `packages.config` |
| Static logger | No DI, hard to test | `AppLogger.cs` |
| File logging to `C:\Logs` | Won't run in containers | config appenders |
| `Trace.*` alongside log4net | Duplicate/legacy sinks | `AppLogger.cs` |
| No metrics/tracing/App Insights | Poor observability | absent |

## Resolved (2026-07-13)

- The **`Services` project has no log4net configuration** and never calls `AppLogger.Configure()`; its log4net output is effectively disabled (no appender), leaving only the `Trace` sink. Confirmed. Only `Web` and `Worker` configure log4net (via their config files + `AppLogger.Configure()` at startup).

## Related pages
- [[concept-logging]]
- [[arch-configuration]]
