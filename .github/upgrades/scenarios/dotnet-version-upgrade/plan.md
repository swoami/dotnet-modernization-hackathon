# .NET Version Upgrade Plan — ContosoInsurance (csproj-only scope)

## Overview

**Target**: Convert all 5 ContosoInsurance projects from legacy .NET Framework 4.6.1 csproj format to SDK-style projects targeting `net9.0`.
**Scope**: 5 projects, ~600 LOC solution. **User-confirmed reduced scope**: csproj conversion only — packages.config → PackageReference, no code rewrites (no Web Forms → Razor Pages, no CoreWCF, no BackgroundService migration, no broad package modernization). The solution is expected NOT to fully build afterward; build failures from unsupported .NET Framework APIs (System.Web, WCF hosting, ServiceBase) are documented as known limitations.

### Selected Strategy
**Bottom-Up (Dependency-First)** — Convert from leaf nodes to root applications, tier by tier.
**Rationale**: 5 .NET Framework projects with a 4-tier dependency graph, all legacy non-SDK csproj + packages.config.

```
Tier 4: [Web]
          ↓
Tier 3: [Services] [Worker]
          ↓           ↓
Tier 2: [Data]
          ↓
Tier 1: [Common]
```

Per-tier completion criterion (reduced scope): the tier's csproj files are valid SDK-style projects targeting net9.0 that `dotnet restore` succeeds on; compilation success is required only where no unsupported Framework APIs are involved (Common, Data).

## Tasks

### 01-prerequisites: Verify toolchain and source control state

Verify the installed .NET SDK (10.0.300 per assessment) can target net9.0, confirm no `global.json` pins an incompatible SDK, and confirm the working tree on branch `track/a-web-api` is in a clean, committable state. Inventory the 5 csproj files and their packages.config files to confirm the conversion inputs match the assessment.

**Done when**: `dotnet --version` succeeds, no blocking `global.json`, git status reviewed, and all 5 legacy csproj + packages.config files inventoried.

---

### 02-foundation-libs: Convert Common and Data class libraries to SDK-style net9.0

Convert `ContosoInsurance.Common` (Tier 1, leaf) and `ContosoInsurance.Data` (Tier 2, depends on Common) to SDK-style csproj with `<TargetFramework>net9.0</TargetFramework>`. Convert their packages.config entries (log4net, Newtonsoft.Json in Common; check Data) to PackageReference. Add package shims strictly required by the conversion for GAC references that no longer exist on .NET 9 (e.g., `System.Configuration.ConfigurationManager` for ConfigHelper, `System.Data.SqlClient` reference handling in Data) — only what the csproj conversion itself demands, no API migrations.

These two projects contain no Web Forms/WCF/ServiceBase code, so they are expected to compile on net9.0.

**Done when**: Both csproj files are SDK-style targeting net9.0, packages.config removed, and both projects restore and build (this tier is expected to build cleanly).

---

### 03-service-hosts: Convert Services and Worker projects to SDK-style net9.0

Convert `ContosoInsurance.Services` (WCF service host, Tier 3) and `ContosoInsurance.Worker` (Windows Service exe, Tier 3) to SDK-style csproj targeting net9.0, converting packages.config to PackageReference. Do NOT migrate hosting models: no CoreWCF, no BackgroundService — code files remain untouched. Known limitation: these projects reference System.ServiceModel (server-side) and System.ServiceProcess/System.Configuration.Install, which do not exist on .NET 9 — compile errors are expected and accepted.

**Done when**: Both csproj files are valid SDK-style projects targeting net9.0, packages.config removed, `dotnet restore` succeeds; build failures limited to documented unsupported-API errors.

---

### 04-web-app: Convert Web project to SDK-style net9.0

Convert `ContosoInsurance.Web` (ASP.NET Web Forms, Tier 4) to SDK-style csproj targeting net9.0, converting packages.config to PackageReference. Do NOT rewrite Web Forms; .aspx files and code-behinds remain untouched. Known limitation: System.Web/Web Forms does not exist on .NET 9 — compile errors are expected and accepted.

**Done when**: The csproj is a valid SDK-style project targeting net9.0, packages.config removed, `dotnet restore` succeeds; build failures limited to documented unsupported-API errors.

---

### 05-final-validation: Attempt solution build and document known limitations

Run `dotnet restore` and `dotnet build` on the full solution. Verify Common and Data build cleanly. Catalog the build errors in Services, Worker, and Web, confirming each traces to an unsupported .NET Framework API (System.Web, WCF server hosting, ServiceBase/InstallUtil) rather than a conversion mistake. Produce the known-limitations report for the user.

**Done when**: Full-solution build attempted, Common + Data build without errors or warnings, and every remaining error is classified as a known unsupported-API limitation in the report.
