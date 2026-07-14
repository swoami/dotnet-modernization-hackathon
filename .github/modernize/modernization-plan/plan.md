# Modernization Plan: modernization-plan

**Project**: ContosoInsurance.Worker

---

## Technical Framework

- **Language**: C# / .NET Framework 4.6.1
- **Framework**: Windows Service (System.ServiceProcess.ServiceBase)
- **Build Tool**: MSBuild (legacy non-SDK-style .csproj)
- **Database**: SQL Server (ContosoInsurance DB via System.Data.SqlClient)
- **Key Dependencies**: log4net 2.0.8, System.Timers.Timer, System.Configuration (App.config), System.ServiceProcess, System.Configuration.Install (ProjectInstaller)

---

## Overview

This migration modernizes the `ContosoInsurance.Worker` (and its shared `ContosoInsurance.Common` library) from a legacy .NET Framework 4.6.1 Windows Service to a cross-platform .NET 9.0 Generic Host worker service. The application currently runs as a Windows Service (`ClaimsExporterService : ServiceBase`) using a `System.Timers.Timer` for scheduling, log4net for logging, and `App.config` / `ConfigHelper` for configuration. The new architecture will:

- Run as a cross-platform .NET Generic Host `BackgroundService` using `Host.CreateApplicationBuilder`, eliminating the Windows-only `ServiceBase`, `ServiceInstaller`, and `ProjectInstaller` dependencies.
- Use `PeriodicTimer` (or a cancellable async loop with `IHostApplicationLifetime`) for scheduling instead of `System.Timers.Timer`, enabling clean cooperative cancellation.
- Move all `App.config` settings (`ExportRoot`, `ExportIntervalMinutes`) into `appsettings.json` with a strongly-typed `ExportOptions` class bound via `IOptions<ExportOptions>`.
- Replace the static `log4net`-backed `AppLogger` with `ILogger<ClaimsExporterService>` wired through the Generic Host built-in logging pipeline.

The migration follows a phased approach: first upgrade the project format and target framework, then modernize the service hosting and runtime patterns, then replace the logging infrastructure, and finally perform a security/CVE scan.

---

## Migration Impact Summary

| Application             | Original Service              | New Service / Pattern              | Authentication | Comments                                       |
|-------------------------|-------------------------------|------------------------------------|----------------|------------------------------------------------|
| ContosoInsurance.Worker | System.ServiceProcess (WinSvc)| .NET Generic Host BackgroundService| N/A            | Drop ServiceBase, ProjectInstaller             |
| ContosoInsurance.Worker | System.Timers.Timer           | PeriodicTimer / async loop         | N/A            | Cooperative cancellation via CancellationToken |
| ContosoInsurance.Worker | App.config + ConfigHelper     | appsettings.json + ExportOptions   | N/A            | Strongly-typed IOptions<ExportOptions>         |
| ContosoInsurance.Worker | log4net / AppLogger (static)  | ILogger<ClaimsExporterService>     | N/A            | Console + file via Generic Host logging        |
| ContosoInsurance.Common | log4net 2.0.8                 | Microsoft.Extensions.Logging       | N/A            | Remove log4net from shared library             |
| ContosoInsurance.Worker | Legacy .csproj (non-SDK)      | SDK-style .csproj, net9.0          | N/A            | Drop packages.config, AssemblyInfo.cs          |
| ContosoInsurance.Common | Legacy .csproj (non-SDK)      | SDK-style .csproj, net9.0          | N/A            | Drop packages.config, AssemblyInfo.cs          |

---

## Tasks

### Task 1 - Upgrade .NET to net9.0 (SDK-style)

Convert `ContosoInsurance.Worker` and `ContosoInsurance.Common` from legacy non-SDK `.csproj` targeting .NET Framework 4.6.1 to SDK-style `.csproj` files targeting `net9.0`. Remove `packages.config`, `Properties/AssemblyInfo.cs`, and legacy assembly references such as `System.Configuration.Install` and `System.ServiceProcess` that have no equivalent on .NET 9.

### Task 2 - Convert Windows Service to .NET Generic Host BackgroundService

Replace `ClaimsExporterService : ServiceBase` with a `BackgroundService` implementation hosted via `Host.CreateApplicationBuilder` in `Program.cs`. Replace `System.Timers.Timer` with `PeriodicTimer` (or an async loop respecting `IHostApplicationLifetime`). Remove `ProjectInstaller.cs` and all `ServiceInstaller`/`ServiceProcessInstaller` code. Migrate `App.config` `<appSettings>` into `appsettings.json` and bind them to a strongly-typed `ExportOptions` class accessed via `IOptions<ExportOptions>`.

### Task 3 - Replace log4net with ILogger

Remove the log4net dependency from `ContosoInsurance.Common` and replace the static `AppLogger` with `ILogger<ClaimsExporterService>` injected via the Generic Host dependency injection container. Configure console logging through the host built-in logging pipeline. Remove `XmlConfigurator.Configure()` and the `<log4net>` section from configuration.

### Task 4 - Security / CVE Remediation

Scan all project dependencies in the solution for known CVEs and remediate identified vulnerabilities by upgrading to minimum patched versions. Verify the project builds and all tests pass after remediation.

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? -> A: No - focus on code migration only (no deployment target requested)
- [x] Q: Should the plan include integration testing? -> A: No - skip integration testing (not explicitly requested)
- [x] Q: Should the plan include a security scan and CVE remediation task? -> A: Yes - include default security/CVE remediation
- [x] Q: Which Azure deployment target should the plan use? -> A: No deployment - migration/modernization only
- [x] Q: Target .NET version? -> A: net9.0 (user-specified)
