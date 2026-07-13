# Configuration

> Sources: `ContosoInsurance.Common/Config/ConfigHelper.cs`, `Web/Web.config`, `Services/Web.config`, `Worker/App.config`.

## Summary

All configuration lives in `Web.config`/`App.config` files and is read at call-time via `System.Configuration.ConfigurationManager`, wrapped by a static `ConfigHelper`.

## Current state

- `ConfigHelper` exposes `GetSetting`, `GetInt`, and `GetConnectionString`; no caching, no reload token.
- App settings observed:
  - Web: `ClaimDocumentsRoot=C:\ClaimsFiles`, `MaxUploadBytes=10485760`, `ClaimScoringEndpoint=http://localhost:8080/ClaimScoringService.svc`.
  - Services: `ScoringModelVersion=v1.3`.
  - Worker: `ExportRoot=C:\Exports`, `ExportIntervalMinutes=60`.
- Connection string `ContosoDb` (with plaintext password) duplicated across all three config files.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Common/Config/ConfigHelper.cs` | Static config accessor | Wraps `ConfigurationManager` |
| `ContosoInsurance.Web/Web.config` | Web settings, auth, WCF client, log4net | Plaintext secret |
| `ContosoInsurance.Services/Web.config` | WCF service config | Plaintext secret |
| `ContosoInsurance.Worker/App.config` | Worker settings, log4net | Plaintext secret |

## Key dependencies

- `System.Configuration.ConfigurationManager`

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| `ConfigurationManager` static access | No DI, no `IConfiguration`, no options binding | `ConfigHelper.cs` |
| Duplicated connection strings | Drift risk | three config files |
| Secrets in config | Secret leak | connection strings |
| Hard-coded `C:\` paths in settings | Machine-bound | app settings |

## Unknowns

- No `appsettings.json` present (expected for legacy). Confirmed absent.

## Related pages
- [[concept-configuration]]
- [[arch-security]]
- [[arch-logging]]
