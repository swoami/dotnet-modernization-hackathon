# 01-prerequisites: Verify toolchain and source control state

## Objective

Verify the environment can perform the csproj-only SDK-style conversion to net9.0 and that source control is in the expected state.

## Research Findings

- **.NET SDK**: `dotnet --version` -> **10.0.300** - can build/target net9.0. OK
- **global.json**: none present at repo root - no SDK pinning conflicts. OK
- **Git**: branch `track/a-web-api` (per user preference - no new branch). Working tree clean except untracked `.github/upgrades/` (scenario artifacts, committed with tasks). OK
- **Project inventory** (all legacy ToolsVersion=15.0 csproj, TargetFrameworkVersion v4.6.1):

| Project | packages.config | Notes |
|---|---|---|
| ContosoInsurance.Common | yes (log4net 2.0.8, Newtonsoft.Json 11.0.2) | GAC ref: System.Configuration |
| ContosoInsurance.Data | no | System.Data / SqlClient usage expected |
| ContosoInsurance.Services | yes | WCF host (Web.config, .svc) |
| ContosoInsurance.Web | yes | Web Forms (Web.config) |
| ContosoInsurance.Worker | yes (App.config) | Windows Service exe |

## Done when

- [x] dotnet --version succeeds (10.0.300)
- [x] No blocking global.json
- [x] Git status reviewed (clean, on track/a-web-api)
- [x] All 5 legacy csproj + packages.config files inventoried
