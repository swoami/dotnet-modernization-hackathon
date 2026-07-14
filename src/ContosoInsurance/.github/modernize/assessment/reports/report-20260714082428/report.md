# ContosoInsurance.Common

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 1 |
| Mandatory Blockers | 0 |
| Potential Issues | 0 |

## Component Information

| Property | Value |
|----------|-------|
| Language | C# |
| Frameworks | .NETFramework,Version=v4.6.1 |
| Build tools | MSBuild |

## Cloud Readiness Issues

| Issue Name | Criticality | Story Points | Occurrences |
|------------|-------------|--------------|-------------|
| Upgrade to newer target framework to get better cloud experience | Optional | 3 | [1](#Upgrade_to_newer_target_framework_to_get_better_cloud_experience) |

### Issue Details

<details id="Upgrade_to_newer_target_framework_to_get_better_cloud_experience">
<summary><b>Upgrade to newer target framework to get better cloud experience</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`

</details>

## DotNET Upgrade Issues [View Details](scenarios/dotnet-version-upgrade/assessment.md)

| Issue Category | Criticality | Story Points |
|----------------|-------------|--------------|
| Binary incompatible for selected .NET version | Mandatory | 1 |
| Project file needs to be converted to SDK-style | Mandatory | 1 |
| Project's target framework(s) needs to be changed | Mandatory | 1 |
| Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs | Mandatory | 1 |
| Legacy Configuration System | Mandatory | 2 |
| WCF Client APIs | Mandatory | 1 |
| ASP.NET Framework (System.Web) | Mandatory | 4 |
| Configuration Installation Components | Mandatory | 3 |
| Source incompatible for selected .NET version | Potential | 1 |
| AutoGenerateBindingRedirects not set and no manual redirects | Potential | 1 |
| NuGet package upgrade is recommended | Potential | 1 |
| NuGet package contains security vulnerability | Optional | 1 |

### Issue Details

<details>
<summary><b>Binary incompatible for selected .NET version</b> — affected files</summary>

- `ContosoInsurance.Web\Login.aspx.cs (line 22, col 12)`
- `ContosoInsurance.Web\Login.aspx.cs (line 21, col 12)`
- `ContosoInsurance.Web\Login.aspx.cs (line 8, col 37)`
- `ContosoInsurance.Web\Upload.aspx.cs (line 8, col 34)`
- `ContosoInsurance.Web\Default.aspx.cs (line 10, col 35)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 25, col 12)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 24, col 12)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 16, col 12)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 11, col 12)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 9, col 8)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 7, col 36)`

</details>

<details>
<summary><b>Project file needs to be converted to SDK-style</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`
- `ContosoInsurance.Data\ContosoInsurance.Data.csproj`
- `ContosoInsurance.Services\ContosoInsurance.Services.csproj`
- `ContosoInsurance.Web\ContosoInsurance.Web.csproj`
- `ContosoInsurance.Worker\ContosoInsurance.Worker.csproj`

</details>

<details>
<summary><b>Project's target framework(s) needs to be changed</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`
- `ContosoInsurance.Data\ContosoInsurance.Data.csproj`
- `ContosoInsurance.Services\ContosoInsurance.Services.csproj`
- `ContosoInsurance.Web\ContosoInsurance.Web.csproj`
- `ContosoInsurance.Worker\ContosoInsurance.Worker.csproj`

</details>

<details>
<summary><b>Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs</b> — affected files</summary>

- `ContosoInsurance.Web\Global.asax.cs`

</details>

<details>
<summary><b>Source incompatible for selected .NET version</b> — affected files</summary>

- `ContosoInsurance.Common\Config\ConfigHelper.cs (line 22, col 12)`
- `ContosoInsurance.Common\Config\ConfigHelper.cs (line 20, col 16)`
- `ContosoInsurance.Common\Config\ConfigHelper.cs (line 18, col 12)`
- `ContosoInsurance.Common\Config\ConfigHelper.cs (line 12, col 12)`
- `ContosoInsurance.Data\UserRepository.cs (line 30, col 20)`
- `ContosoInsurance.Data\UserRepository.cs (line 29, col 20)`
- `ContosoInsurance.Data\UserRepository.cs (line 27, col 16)`
- `ContosoInsurance.Data\UserRepository.cs (line 26, col 16)`
- `ContosoInsurance.Data\UserRepository.cs (line 25, col 16)`
- `ContosoInsurance.Data\UserRepository.cs (line 23, col 12)`
- `ContosoInsurance.Data\UserRepository.cs (line 22, col 12)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 25, col 24)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 23, col 20)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 21, col 16)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 20, col 16)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 18, col 12)`
- `ContosoInsurance.Data\PolicyRepository.cs (line 17, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 123, col 8)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 135, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 134, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 133, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 132, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 131, col 42)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 131, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 130, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 129, col 41)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 129, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 128, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 127, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 126, col 39)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 126, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 125, col 39)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 125, col 28)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 119, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 118, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 117, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 116, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 114, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 113, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 104, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 103, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 102, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 101, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 100, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 99, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 98, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 97, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 96, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 94, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 93, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 80, col 20)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 78, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 77, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 75, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 74, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 60, col 20)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 58, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 57, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 56, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 54, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 53, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 43, col 20)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 41, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 40, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 39, col 16)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 37, col 12)`
- `ContosoInsurance.Data\ClaimsRepository.cs (line 36, col 12)`
- `ContosoInsurance.Services\IClaimScoringService.cs (line 4, col 5)`
- `ContosoInsurance.Services\IClaimScoringService.cs (line 10, col 9)`
- `ContosoInsurance.Services\IClaimScoringService.cs (line 7, col 9)`
- `ContosoInsurance.Web\Login.aspx.cs (line 22, col 12)`
- `ContosoInsurance.Web\Default.aspx.cs (line 51, col 16)`
- `ContosoInsurance.Web\Default.aspx.cs (line 50, col 16)`
- `ContosoInsurance.Web\Default.aspx.cs (line 41, col 12)`
- `ContosoInsurance.Web\Default.aspx.cs (line 40, col 12)`
- `ContosoInsurance.Web\Global.asax.cs (line 16, col 12)`
- `ContosoInsurance.Web\Global.asax.cs (line 6, col 26)`
- `ContosoInsurance.Worker\ProjectInstaller.cs (line 16, col 12)`
- `ContosoInsurance.Worker\Program.cs (line 8, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 30, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 21, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 20, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 19, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 18, col 12)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 16, col 8)`
- `ContosoInsurance.Worker\ClaimsExporterService.cs (line 12, col 41)`

</details>

<details>
<summary><b>AutoGenerateBindingRedirects not set and no manual redirects</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`
- `ContosoInsurance.Data\ContosoInsurance.Data.csproj`
- `ContosoInsurance.Services\ContosoInsurance.Services.csproj`
- `ContosoInsurance.Web\ContosoInsurance.Web.csproj`
- `ContosoInsurance.Worker\ContosoInsurance.Worker.csproj`

</details>

<details>
<summary><b>NuGet package upgrade is recommended</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`
- `ContosoInsurance.Web\ContosoInsurance.Web.csproj`

</details>

<details>
<summary><b>NuGet package contains security vulnerability</b> — affected files</summary>

- `ContosoInsurance.Common\ContosoInsurance.Common.csproj`
- `ContosoInsurance.Web\ContosoInsurance.Web.csproj`

</details>

---

## Codebase Insights

> **Note:** These documents are generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

1. **[Architecture Diagram](facts/architecture-diagram.md)** — Understand the big picture: system layers and component relationships
2. **[Dependency Map](facts/dependency-map.md)** — Know what the project depends on and where the risks are
3. **[API & Service Contracts](facts/api-service-contracts.md)** — See how services communicate and what contracts they expose
4. **[Data Architecture](facts/data-architecture.md)** — Explore data models, storage, and data flow patterns
5. **[Configuration Inventory](facts/configuration-inventory.md)** — Review how the application is configured across environments
6. **[Business Workflows](facts/business-workflows.md)** — Trace end-to-end business processes and domain logic

[Share feedback](https://aka.ms/ghcp-appmod/feedback)
