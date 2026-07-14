# 05-final-validation - Progress Details

## What was done

Full-solution validation only; no files modified.

- dotnet restore ContosoInsurance.sln: SUCCEEDED for all 5 projects.
- dotnet build ContosoInsurance.sln: attempted; results below.

## Per-project build status

| Project | Restore | Build | Classification |
|---|---|---|---|
| ContosoInsurance.Common | OK | SUCCEEDED (0 errors, 0 code warnings) | Fully converted |
| ContosoInsurance.Data | OK | SUCCEEDED (0 errors, 0 code warnings) | Fully converted |
| ContosoInsurance.Services | OK | FAILS - CS0246 ServiceContract/OperationContract (IClaimScoringService.cs) | KNOWN LIMITATION: WCF server hosting (System.ServiceModel) does not exist on .NET 9; future fix = CoreWCF |
| ContosoInsurance.Worker | OK | FAILS - CS1069 ServiceBase, CS0234/CS0246 Installer (ClaimsExporterService.cs, ProjectInstaller.cs) | KNOWN LIMITATION: System.ServiceProcess/System.Configuration.Install not on .NET 9; future fix = BackgroundService + UseWindowsService |
| ContosoInsurance.Web | OK | FAILS - CS0234/CS0246 System.Web.UI.Page, HttpApplication, System.Web.Security (all .aspx.cs + Global.asax.cs); plus cascading CS0234 for ContosoInsurance.* namespaces caused by the failed Services dependency | KNOWN LIMITATION: ASP.NET Web Forms (System.Web) does not exist on .NET 9; future fix = Razor Pages rewrite |

Every build error maps to an unsupported .NET Framework API family or is a cascade of the Services failure - no conversion mistakes found.

## Remaining warnings

- NU1903: Newtonsoft.Json 11.0.2 high-severity vulnerability (GHSA-5crp-9r3c-p9vr), reported for Common and Web. Version bump excluded by user-confirmed scope (no package modernization); recommend updating to 13.x in a follow-up.

## Done-when verification

- [x] Full-solution build attempted
- [x] Common + Data build without errors or warnings (code warnings: 0; only the NU1903 audit warning from the kept Newtonsoft.Json version)
- [x] Every remaining error classified as known unsupported-API limitation
