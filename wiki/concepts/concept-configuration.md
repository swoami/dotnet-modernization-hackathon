# Concept: Configuration

> Sources: `ContosoInsurance.Common/Config/ConfigHelper.cs`, `Web/Web.config`, `Services/Web.config`, `Worker/App.config`.

## Summary

Configuration and connection strings are read from `Web.config`/`App.config` via `ConfigurationManager`, wrapped by a static `ConfigHelper`.

## Current flow

1. Code calls `ConfigHelper.GetSetting`, `GetInt`, or `GetConnectionString`.
2. `ConfigHelper` reads from `ConfigurationManager.AppSettings` / `ConnectionStrings` at call time (no caching).
3. Missing connection strings throw `ConfigurationErrorsException`.

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Common/Config/ConfigHelper.cs` | Static accessor |
| `ContosoInsurance.Web/Web.config` | Web app settings + secret |
| `ContosoInsurance.Services/Web.config` | Service config + secret |
| `ContosoInsurance.Worker/App.config` | Worker settings + secret |

## Rules / behavior

- Settings resolved per call, no reload token or caching.
- Connection string `ContosoDb` is duplicated across three config files.

## Risks / legacy issues

- Static `ConfigurationManager` access (no DI / `IConfiguration`).
- Plaintext SQL credentials in config.
- Hard-coded `C:\` paths.

## Unknowns

- No `appsettings.json` present (expected for legacy). Confirmed absent.

## Related pages
- [[arch-configuration]]
- [[arch-security]]
