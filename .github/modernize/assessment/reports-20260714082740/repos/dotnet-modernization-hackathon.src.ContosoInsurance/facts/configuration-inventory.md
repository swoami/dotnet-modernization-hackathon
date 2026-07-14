# Configuration & Externalized Settings Inventory

The solution uses three static XML configuration files (`Web.config` × 2, `App.config` × 1) with no environment-specific overrides, no secret store integration, and no runtime profile system — all settings, including plaintext database credentials, are baked directly into source-controlled files.

## Configuration Sources

| Source | Type | Path / Location | Notes |
|--------|------|-----------------|-------|
| `Web.config` | XML / ASP.NET | `ContosoInsurance.Web/Web.config` | App settings, connection string, Forms auth, log4net, machineKey |
| `Web.config` | XML / WCF | `ContosoInsurance.Services/Web.config` | App settings, connection string, WCF service model |
| `App.config` | XML / .NET Console/Service | `ContosoInsurance.Worker/App.config` | App settings, connection string, log4net, supported runtime |
| `packages.config` × 4 | NuGet package manifest | `*/packages.config` | NuGet package pinning for Web, Services, Worker, Common |
| `*.csproj` | MSBuild project files | `*/ContosoInsurance.*.csproj` | Build configurations (Debug / Release), target framework declaration |

> Note: No `appsettings.json`, `launchSettings.json`, Docker Compose, Kubernetes ConfigMaps, cloud config server, or secret store references were found.

---

## Build Profiles

| Profile | Activation | Purpose | Key Symbols / Settings |
|---------|------------|---------|------------------------|
| **Debug** | Default (`$(Configuration) == ''`) | Development — full debug info, no optimization | `DEBUG;TRACE`, `<DebugType>full</DebugType>`, `<Optimize>false</Optimize>` |
| **Release** | Manual `-p:Configuration=Release` | Production deployment — optimized, PDB only | `TRACE`, `<DebugType>pdbonly</DebugType>`, `<Optimize>true</Optimize>` |

All five projects share the same two-profile model. No Maven/Gradle profiles, npm build scripts, webpack environments, or conditional compilation plugins are present.

---

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---------|-------------------|--------------|---------------|
| *(single static profile)* | N/A — no environment-specific transforms | `Web.config` / `App.config` | None — all environments share identical configuration |

> Note: No `Web.{Environment}.config` transforms, `ASPNETCORE_ENVIRONMENT` variable, or `Web.Debug.config` / `Web.Release.config` XDT transforms were found. The solution predates the ASP.NET Core multi-environment model and relies on manual file edits per deployment target.

---

## Properties Inventory

### ContosoInsurance.Web (`Web.config`)

| Property Key | Default Value | Profile / Source | Notes |
|--------------|--------------|------------------|-------|
| `ClaimDocumentsRoot` | `C:\ClaimsFiles` | Static — `<appSettings>` | Hard-coded UNC-style local path |
| `MaxUploadBytes` | `10485760` (10 MB) | Static — `<appSettings>` | Also enforced via `<requestLimits maxAllowedContentLength>` |
| `ClaimScoringEndpoint` | `http://localhost:8080/ClaimScoringService.svc` | Static — `<appSettings>` | WCF SOAP endpoint; localhost-only |
| `ContosoDb` (connection string) | `Server=.;Database=ContosoInsurance;User Id=contoso_app;Password=[MASKED];MultipleActiveResultSets=True;` | Static — `<connectionStrings>` | SQL auth with plaintext password |
| `compilation/@debug` | `true` | Static — `<system.web>` | Should be `false` in production |
| `compilation/@targetFramework` | `4.6.1` | Static — `<system.web>` | |
| `httpRuntime/@targetFramework` | `4.6.1` | Static — `<system.web>` | |
| `httpRuntime/@maxRequestLength` | `10240` (KB = 10 MB) | Static — `<system.web>` | Paired with `MaxUploadBytes` app setting |
| `authentication/@mode` | `Forms` | Static — `<system.web>` | |
| `forms/@loginUrl` | `~/Login.aspx` | Static | |
| `forms/@timeout` | `60` (minutes) | Static | Session idle timeout |
| `forms/@defaultUrl` | `~/Default.aspx` | Static | |
| `sessionState/@mode` | `InProc` | Static — `<system.web>` | Not suitable for multi-instance deployments |
| `sessionState/@timeout` | `60` (minutes) | Static | |
| `customErrors/@mode` | `Off` | Static — `<system.web>` | Exposes error details; unsafe for production |
| `machineKey/@validationKey` | `AutoGenerate,IsolateApps` | Static | SHA1 validation — weak algorithm |
| `machineKey/@decryptionKey` | `AutoGenerate,IsolateApps` | Static | |
| `machineKey/@validation` | `SHA1` | Static | |
| `log4net/FileAppender/file` | `C:\Logs\ContosoInsurance.Web.log` | Static — `<log4net>` | Hard-coded OS path |
| `log4net/FileAppender/maximumFileSize` | `10MB` | Static | |
| `log4net/FileAppender/maxSizeRollBackups` | `5` | Static | |
| `log4net/root/level` | `INFO` | Static | |

### ContosoInsurance.Services (`Web.config`)

| Property Key | Default Value | Profile / Source | Notes |
|--------------|--------------|------------------|-------|
| `ScoringModelVersion` | `v1.3` | Static — `<appSettings>` | Scoring algorithm version |
| `ContosoDb` (connection string) | `Server=.;Database=ContosoInsurance;User Id=contoso_app;Password=[MASKED];` | Static — `<connectionStrings>` | SQL auth with plaintext password |
| `compilation/@debug` | `true` | Static — `<system.web>` | Should be `false` in production |
| `compilation/@targetFramework` | `4.6.1` | Static | |
| `serviceDebug/@includeExceptionDetailInFaults` | `true` | Static — `<system.serviceModel>` | Exposes internal fault details; unsafe for production |
| `serviceMetadata/@httpGetEnabled` | `true` | Static | Enables WSDL at `?wsdl` URL |
| `basicHttpBinding/ClaimScoringSoap/@maxReceivedMessageSize` | `1048576` (1 MB) | Static — `<bindings>` | WCF message size limit |

### ContosoInsurance.Worker (`App.config`)

| Property Key | Default Value | Profile / Source | Notes |
|--------------|--------------|------------------|-------|
| `ExportRoot` | `C:\Exports` | Static — `<appSettings>` | Output directory for CSV exports |
| `ExportIntervalMinutes` | `60` | Static — `<appSettings>` | Timer interval for export runs; read via `ConfigHelper.GetInt` |
| `ContosoDb` (connection string) | `Server=.;Database=ContosoInsurance;User Id=contoso_app;Password=[MASKED];` | Static — `<connectionStrings>` | SQL auth with plaintext password |
| `startup/supportedRuntime/@version` | `v4.0` | Static — `<startup>` | |
| `startup/supportedRuntime/@sku` | `.NETFramework,Version=v4.6.1` | Static | |
| `log4net/FileAppender/file` | `C:\Logs\ContosoInsurance.Worker.log` | Static — `<log4net>` | Hard-coded OS path |
| `log4net/FileAppender/maximumFileSize` | `10MB` | Static | |
| `log4net/FileAppender/maxSizeRollBackups` | `5` | Static | |
| `log4net/root/level` | `INFO` | Static | |

---

## Startup Parameters & Resource Requirements

| Service | Runtime Options | Memory / CPU | Instance Notes |
|---------|----------------|--------------|----------------|
| **ContosoInsurance.Web** | Hosted by IIS / IIS Express; no explicit startup parameters | IIS application pool defaults (typically 1–4 GB virtual) | Single IIS site; no load-balancer config present |
| **ContosoInsurance.Services** | Hosted by IIS / IIS Express (WCF `.svc`); `aspNetCompatibilityEnabled=true` | IIS application pool defaults | Can share app pool with Web or run separately |
| **ContosoInsurance.Worker** | Windows Service (`ServiceBase.Run`); `supportedRuntime v4.0 / .NETFramework 4.6.1` | OS process defaults; no explicit limits | Single instance; timer-driven (60-min interval) |

> Note: No JVM heap settings, Docker resource limits, Kubernetes resource requests/limits, or cloud deployment sizing configurations were found.

---

## Startup Dependency Chain

```
SQL Server (ContosoInsurance DB)
  └─► ContosoInsurance.Services  (WCF — must be reachable before Web processes claims)
        └─► ContosoInsurance.Web   (ASP.NET WebForms — calls ClaimScoringEndpoint on form submit)

SQL Server (ContosoInsurance DB)
  └─► ContosoInsurance.Worker    (Windows Service — reads claims on startup and every 60 minutes)
```

| Dependency | Mechanism | Timeout / Retry |
|------------|-----------|-----------------|
| SQL Server availability | ADO.NET connection at first repository call | Default SqlClient connection timeout (30 s); no retry logic |
| WCF `ClaimScoringService` | Hard-coded URL in `ClaimScoringEndpoint`; called synchronously from Web | Default WCF send timeout (1 min); no retry or circuit breaker |

> No `dockerize`, Kubernetes readiness probes, Spring Cloud Config retry, or `depends_on` health-check mechanisms are present.

---

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage | Location |
|-----------------|------|---------|----------|
| `ContosoDb` Password | SQL Server credential | **Plaintext in source-controlled XML** | `ContosoInsurance.Web/Web.config` |
| `ContosoDb` Password | SQL Server credential | **Plaintext in source-controlled XML** | `ContosoInsurance.Services/Web.config` |
| `ContosoDb` Password | SQL Server credential | **Plaintext in source-controlled XML** | `ContosoInsurance.Worker/App.config` |
| `machineKey validationKey` | ASP.NET forms-auth / view-state signing key | `AutoGenerate` per machine — not rotatable across multiple servers | `ContosoInsurance.Web/Web.config` |
| `machineKey decryptionKey` | ASP.NET forms-auth ticket encryption key | `AutoGenerate` per machine | `ContosoInsurance.Web/Web.config` |

### Secrets Provisioning Workflow

There is **no automated secrets provisioning workflow**. Secrets are managed manually:

1. A developer or operator edits `Web.config` / `App.config` directly on the deployment machine.
2. The SQL password (`P@ssw0rd!`) is embedded in plaintext in all three config files and committed to source control.
3. There is no integration with Azure Key Vault, HashiCorp Vault, AWS Secrets Manager, DPAPI-encrypted config sections, or Jasypt.
4. The `machineKey` uses `AutoGenerate,IsolateApps` — this generates a per-machine key at runtime, which breaks forms-authentication tickets and view-state when deploying to more than one server (web farm scenarios).

> **Modernization note**: All three connection strings should be migrated to Managed Identity (Azure SQL) or pulled from Azure Key Vault / environment variables injected at deploy time, removing the plaintext password from source control entirely.

---

## Feature Flags

No feature flag framework is present in the solution. No LaunchDarkly, Unleash, `Microsoft.FeatureManagement`, Spring Feature Flags, `@ConditionalOnProperty`, or custom toggle mechanisms were found.

| Flag Name | Default | Controlled By |
|-----------|---------|---------------|
| *(none)* | — | — |

---

## Framework & Runtime Versions

| Component | Version | Source |
|-----------|---------|--------|
| .NET Framework (target) | 4.6.1 | All `*.csproj` — `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` |
| ASP.NET Web Forms | 4.6.1 | `ContosoInsurance.Web.csproj` — `ProjectTypeGuids` includes Web Application GUID |
| WCF (Windows Communication Foundation) | 4.6.1 (inbox) | `ContosoInsurance.Services.csproj` — `System.ServiceModel` reference |
| Windows Service | 4.6.1 (inbox) | `ContosoInsurance.Worker.csproj` — `System.ServiceProcess` reference |
| MSBuild | ToolsVersion 15.0 | All `*.csproj` |
| log4net | 2.0.8 | `packages.config` — `ContosoInsurance.Web`, `Services`, `Worker`, `Common` |
| Newtonsoft.Json | 11.0.2 | `packages.config` — `ContosoInsurance.Web`, `Common` |
| System.Configuration | 4.6.1 (inbox) | Referenced in Web, Services, Worker, Common `.csproj` files |
| System.Data (ADO.NET) | 4.6.1 (inbox) | Referenced in `ContosoInsurance.Data.csproj` |
| NuGet (package management) | `packages.config` format | Pre-SDK-style projects; no `PackageReference` format |
