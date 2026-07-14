# 03-service-hosts - Progress Details

## Files modified

- src/ContosoInsurance/ContosoInsurance.Services/ContosoInsurance.Services.csproj - rewritten as SDK-style (Microsoft.NET.Sdk, Library), TargetFramework net9.0, GenerateAssemblyInfo=false; ProjectReferences to Common + Data. Legacy web-project artifacts dropped: ProjectTypeGuids, UseIISExpress, Microsoft.WebApplication.targets import. ClaimScoringService.svc and Web.config remain in the folder (included as None by SDK globbing).
- src/ContosoInsurance/ContosoInsurance.Services/packages.config - removed (log4net was transitive via Common; no direct reference needed)
- src/ContosoInsurance/ContosoInsurance.Worker/ContosoInsurance.Worker.csproj - rewritten as SDK-style, OutputType Exe, TargetFramework net9.0, GenerateAssemblyInfo=false; ProjectReferences to Common + Data; App.config kept via SDK conventions.
- src/ContosoInsurance/ContosoInsurance.Worker/packages.config - removed (log4net transitive via Common)

No .cs source files were modified (per user-confirmed csproj-only scope).

## Build/restore results

- dotnet restore: SUCCEEDED for both projects.
- ContosoInsurance.Services build: FAILS as expected - CS0246 ServiceContract/OperationContract (System.ServiceModel WCF server hosting does not exist on .NET 9). ACCEPTED known limitation; future fix = CoreWCF migration.
- ContosoInsurance.Worker build: FAILS as expected - CS1069 ServiceBase (System.ServiceProcess) and CS0234/CS0246 Installer (System.Configuration.Install does not exist on .NET 9). ACCEPTED known limitation; future fix = Generic Host BackgroundService + UseWindowsService.
- All build errors classified as unsupported-Framework-API errors; none are conversion mistakes.

## Issues

None beyond the accepted known limitations above.
