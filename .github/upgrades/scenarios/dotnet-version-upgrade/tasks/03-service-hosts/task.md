# 03-service-hosts: Convert Services and Worker projects to SDK-style net9.0

## Objective

Convert ContosoInsurance.Services (WCF host, web project) and ContosoInsurance.Worker (Windows Service exe) to SDK-style csproj targeting net9.0. csproj-only scope: no hosting migration (no CoreWCF, no BackgroundService), code untouched. Compile errors from unsupported Framework APIs are ACCEPTED known limitations.

## Research Findings

- **Services**: legacy web project (ProjectTypeGuids 3D9AD99F/fae04ec0, Microsoft.WebApplication.targets import, UseIISExpress). Compile items: IClaimScoringService.cs, ClaimScoringService.svc.cs, AssemblyInfo.cs. Content: ClaimScoringService.svc, Web.config. GAC refs System.ServiceModel + System.Web -> DO NOT EXIST on net9.0 -> expected compile errors ([ServiceContract] etc.). packages.config: log4net 2.0.8 only, with NO direct assembly Reference in csproj -> consumed transitively via Common (3.3.2); no direct PackageReference needed.
- Conversion choice for Services: plain Microsoft.NET.Sdk (Library) - Sdk.Web would require an entry point / hosting model change, out of scope. WebApplication.targets import dropped (legacy-only).
- **Worker**: Exe; compile items ClaimsExporterService.cs (ServiceBase), Program.cs, ProjectInstaller.cs, AssemblyInfo.cs; App.config kept. GAC refs System.ServiceProcess + System.Configuration.Install -> not on net9.0 -> expected compile errors. packages.config: log4net 2.0.8 only, transitive via Common.
- System.Configuration usage covered transitively by Common's System.Configuration.ConfigurationManager 9.0.17.
- GenerateAssemblyInfo=false in both (AssemblyInfo.cs kept).

## Done when

- Both csproj valid SDK-style net9.0, packages.config removed
- dotnet restore succeeds for both
- Build failures limited to documented unsupported-API errors (System.ServiceModel/System.Web in Services; ServiceBase/Installer in Worker)
