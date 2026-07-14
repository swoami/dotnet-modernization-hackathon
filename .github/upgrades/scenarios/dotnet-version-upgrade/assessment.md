# Upgrade Assessment — ContosoInsurance → net9.0

**Date**: 2026-07-14
**Solution**: `src/ContosoInsurance/ContosoInsurance.sln`
**Current TFM**: .NET Framework 4.6.1 (`v4.6.1`, legacy non-SDK csproj + packages.config)
**Target TFM**: `net9.0` (STS — support ends **2026-11-10**; user explicitly chose this target)
**SDKs installed**: 10.0.300 (can build net9.0)

## Project Inventory & Dependency Graph

| # | Project | Type | Output | Depends on | Upgrade complexity |
|---|---------|------|--------|-----------|--------------------|
| 1 | ContosoInsurance.Common | Class library | dll | — | **Low** |
| 2 | ContosoInsurance.Data | Class library (ADO.NET) | dll | Common | **Low-Medium** |
| 3 | ContosoInsurance.Services | **WCF service host** (.svc, IIS) | dll | Common, Data | **High** — WCF server hosting not supported on .NET 9; requires CoreWCF |
| 4 | ContosoInsurance.Web | **ASP.NET Web Forms** (.aspx) | dll | Common, Data, Services | **High** — Web Forms does not exist on .NET 9; requires rewrite to ASP.NET Core (Razor Pages) |
| 5 | ContosoInsurance.Worker | **Windows Service** (ServiceBase + ProjectInstaller) | exe | Common, Data | **Medium** — migrate to Generic Host Worker Service with `UseWindowsService()` |

Upgrade order (leaf → root): **Common → Data → Services → Worker → Web**

## Package Analysis (packages.config → PackageReference)

| Package | Current | Issue | Action |
|---------|---------|-------|--------|
| Newtonsoft.Json | 11.0.2 | ⚠️ **Vulnerable** — GHSA-5crp-9r3c-p9vr (High, DoS, fixed in 13.0.1) | Update to latest 13.x |
| log4net | 2.0.8 | ⚠️ **Vulnerable** — CVE-2018-1285 (XXE in XmlConfigurator, fixed in 2.0.10); net45-only lib, incompatible with net9.0 | Update to latest 3.x (netstandard2.0) |
| *(implicit)* System.Configuration | GAC ref | `ConfigurationManager` not in net9.0 box | Add `System.Configuration.ConfigurationManager` |
| *(implicit)* System.Data.SqlClient | GAC ref | Not in net9.0 box; NuGet package is deprecated | Migrate to `Microsoft.Data.SqlClient` |
| *(implicit)* System.ServiceModel (server) | GAC ref | WCF server not on .NET 9 | `CoreWCF.Http` (server), `System.ServiceModel.Http` (client) |
| *(implicit)* System.ServiceProcess / Configuration.Install | GAC ref | `ProjectInstaller`/InstallUtil not on .NET 9 | `Microsoft.Extensions.Hosting.WindowsServices` |
| *(implicit)* System.Web (Web Forms, FormsAuthentication) | GAC ref | Does not exist on .NET 9 | ASP.NET Core Razor Pages + cookie authentication |

## API/Behavioral Risks

1. **Web Forms → Razor Pages rewrite** (Default.aspx claims grid, Upload.aspx, Login.aspx): biggest functional change; FormsAuthentication → ASP.NET Core cookie auth; `Web.config` → `appsettings.json` + Program.cs.
2. **WCF server → CoreWCF**: contract (`IClaimScoringService`) and implementation stay as-is; hosting moves from .svc/IIS to ASP.NET Core + CoreWCF. The Web project's `ChannelFactory<T>`/`BasicHttpBinding` client keeps working on net9.0 via `System.ServiceModel.Http`.
3. **Windows Service → Worker Service**: `ServiceBase`/`Timer` → `BackgroundService` + `PeriodicTimer`; `ProjectInstaller` removed (register with `sc.exe create` instead of InstallUtil).
4. **Microsoft.Data.SqlClient defaults**: `Encrypt=true` by default — connection strings may need `Encrypt=False`/`TrustServerCertificate=True` for local dev.
5. **Configuration**: `ConfigurationManager` continues to work via NuGet shim reading `App.config`; Web project moves to `appsettings.json`.
6. **log4net 2→3**: `XmlConfigurator.Configure()` needs explicit repository/config file on .NET (no web.config section auto-load).

## Pre-existing Code Security Findings (out of scope, flagged only)

- `ClaimsRepository.SearchByClaimant` — intentional **SQL injection** (string-concatenated SQL); comment says it is kept deliberately for app-modernization exercises. Not changed by this upgrade.
- `Upload.aspx.cs` — **path traversal** (client-controlled filename). The Razor Pages rewrite will sanitize the filename as a side effect of the rewrite.

## Code Metrics

- ~20 C# files, ~600 LOC total — small solution, single-phase upgrade feasible.
- No unit tests present in the repository (validation = build + warning-free).

## Conclusion

All 5 projects can be converted to SDK-style and retargeted to net9.0, but **Services, Web, and Worker require code/hosting migration**, not just csproj conversion. Package vulnerabilities (Newtonsoft.Json, log4net) will be fixed as part of the upgrade.
