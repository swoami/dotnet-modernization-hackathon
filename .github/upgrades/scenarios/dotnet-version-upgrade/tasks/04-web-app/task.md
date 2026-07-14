# 04-web-app: Convert Web project to SDK-style net9.0

## Objective

Convert ContosoInsurance.Web (ASP.NET Web Forms) to SDK-style csproj targeting net9.0. csproj-only scope: no Web Forms rewrite; .aspx/.asax files and code-behinds untouched. Compile errors from System.Web are ACCEPTED known limitations.

## Research Findings

- Legacy web application project (ProjectTypeGuids 349c5851/fae04ec0, WebApplication.targets, IISExpress properties). Compile items: Global.asax.cs, Default/Upload/Login.aspx.cs, AssemblyInfo.cs. Content: .asax/.aspx files + Web.config (kept on disk, picked up as None/Content by SDK globbing).
- Direct package refs (packages.config): log4net 2.0.8, Newtonsoft.Json 11.0.2.
  - log4net MUST align to 3.3.2 - Common (referenced project) brings log4net 3.3.2 transitively; a direct 2.0.8 reference would cause a NuGet downgrade error (NU1605). Strictly required for a successful restore.
  - Newtonsoft.Json kept at 11.0.2 (reduced scope; NU1903 known limitation).
- GAC refs System.Web + System.ServiceModel (client: ChannelFactory/BasicHttpBinding) -> do not exist on net9.0 -> expected compile errors. No client packages added (deferred with the rewrite; project cannot compile regardless due to Web Forms).
- Conversion choice: plain Microsoft.NET.Sdk Library (matching legacy OutputType); Sdk.Web would demand a hosting model that does not exist here (deferred rewrite).
- GenerateAssemblyInfo=false (AssemblyInfo.cs kept).

## Done when

- csproj valid SDK-style net9.0, packages.config removed
- dotnet restore succeeds
- Build failures limited to documented unsupported-API errors (System.Web / Web Forms, System.ServiceModel client)
