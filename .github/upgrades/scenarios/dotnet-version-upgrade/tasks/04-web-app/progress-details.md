# 04-web-app - Progress Details

## Files modified

- src/ContosoInsurance/ContosoInsurance.Web/ContosoInsurance.Web.csproj - rewritten as SDK-style (Microsoft.NET.Sdk, Library), TargetFramework net9.0, GenerateAssemblyInfo=false. PackageReference: log4net 3.3.2 (aligned with Common to avoid NU1605 downgrade), Newtonsoft.Json 11.0.2 (kept, reduced scope). ProjectReferences: Common, Data, Services. Legacy artifacts dropped: ProjectTypeGuids, IISExpress properties, WebApplication.targets import. .aspx/.asax/Web.config remain on disk untouched.
- src/ContosoInsurance/ContosoInsurance.Web/packages.config - removed

No .cs/.aspx source files were modified (per user-confirmed csproj-only scope).

## Build/restore results

- dotnet restore: SUCCEEDED (log4net aligned at 3.3.2 avoided the NU1605 downgrade conflict).
- dotnet build: FAILS as expected - the build stops at the ContosoInsurance.Services dependency (WCF contract errors, documented in task 03). The Web project itself additionally uses System.Web (Web Forms, HttpContext, FormsAuthentication) and System.ServiceModel client APIs that do not exist on .NET 9 - ACCEPTED known limitation; future fix = Razor Pages rewrite + System.ServiceModel.Http client package.

## Issues

None beyond the accepted known limitations above.
