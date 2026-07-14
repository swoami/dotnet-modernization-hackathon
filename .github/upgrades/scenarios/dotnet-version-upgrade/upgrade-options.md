# Upgrade Options — ContosoInsurance

Assessment: 5 projects, all .NET Framework 4.6.1 legacy csproj + packages.config; WCF host, Web Forms app, Windows Service; 2 vulnerable packages; target net9.0.

> ## ⚠️ SCOPE CHANGE (user-confirmed 2026-07-14)
>
> User: *"let's only upgrade csproj for now."*
>
> **Confirmed scope**: Convert the 5 `.csproj` files to SDK-style format with `TargetFramework net9.0`, converting `packages.config` → `PackageReference` where present. **No code rewrites** (no Web Forms → Razor Pages, no CoreWCF, no Windows Service → BackgroundService, no broad package modernization) — only changes strictly required for the csproj conversion itself.
>
> **Accepted limitation**: the solution will NOT fully build afterward (Web Forms, WCF hosting, ServiceBase APIs don't exist on .NET 9). Build failures from unsupported Framework APIs are reported as known limitations, not fixed.
>
> The "Unsupported API Handling → Rewrite to modern equivalents" selection below is **deferred** to a future scenario run. Bottom-Up strategy and PackageReference-per-project remain in effect.

## Strategy

### Upgrade Strategy
Multiple (5) .NET Framework projects with a clear dependency chain — Bottom-Up is mandatory for multi-project .NET Framework solutions.

| Value | Description |
|-------|-------------|
| **Bottom-Up** (selected) | Upgrade leaf libraries first (Common → Data), then hosts (Services, Worker, Web); validate each tier before the next. |
| All-at-Once | Upgrade every project in a single atomic pass; only viable for trivial solutions. |

## Project Structure

### Project Approach
The Web project is ASP.NET Web Forms and the user asked for all projects to be converted and retargeted to net9.0 in-place; the app is tiny (3 pages).

| Value | Description |
|-------|-------------|
| **In-place** (selected) | Rewrite Web as ASP.NET Core Razor Pages in the same project; Services rehosted on CoreWCF in-place; Worker becomes a Generic Host worker in-place. |
| Side-by-side | Keep the old Framework web project running and build a parallel ASP.NET Core project behind a YARP proxy; old project excluded from conversion. |

### Package Management
5 projects share only 2 packages (log4net, Newtonsoft.Json); no CPM present.

| Value | Description |
|-------|-------------|
| **PackageReference per project** (selected) | Convert packages.config to per-project PackageReference during SDK-style conversion; lowest churn. |
| Central Package Management | Add Directory.Packages.props and centralize all versions; more structure than this solution needs. |

## Compatibility

### Unsupported API Handling
System.Web (Web Forms, FormsAuthentication), WCF server hosting, and ServiceBase/InstallUtil do not exist on .NET 9.

| Value | Description |
|-------|-------------|
| **Rewrite to modern equivalents** (selected) | Web Forms → Razor Pages + cookie auth; WCF host → CoreWCF (contract unchanged, WCF client kept via System.ServiceModel.Http); Windows Service → BackgroundService + UseWindowsService (ProjectInstaller removed, register via `sc.exe create`). |
| Stub and defer | Compile with stubs/exclusions and fix functionality later; leaves the solution non-functional. |

## Modernization

### Configuration Migration
ConfigHelper (System.Configuration.ConfigurationManager) is shared by all projects via Common.

| Value | Description |
|-------|-------------|
| **Keep System.Configuration** (selected) | Add the System.Configuration.ConfigurationManager package; keep App.config-based settings for Worker/Services; Web gets equivalent settings; minimal code change. |
| Migrate to Microsoft.Extensions.Configuration | Replace ConfigHelper with IConfiguration/appsettings.json everywhere; touches every project. |

### Logging Framework
log4net 2.0.8 in use (vulnerable, net45-only).

| Value | Description |
|-------|-------------|
| **Keep log4net (update to 3.x)** (selected) | Update to latest log4net 3.x (netstandard2.0, fixes CVE-2018-1285); AppLogger API unchanged. |
| Migrate to Microsoft.Extensions.Logging | Replace AppLogger/log4net with ILogger; touches all call sites. |

### Nullable Reference Types

| Value | Description |
|-------|-------------|
| **Do not enable** (selected) | Keep nullable disabled to avoid warning churn in legacy code (warning-free build is a hard goal). |
| Enable solution-wide | Turn on `<Nullable>enable</Nullable>`; would generate many warnings requiring annotation work. |
