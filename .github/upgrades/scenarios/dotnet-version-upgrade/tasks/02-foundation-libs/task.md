# 02-foundation-libs: Convert Common and Data class libraries to SDK-style net9.0

## Objective

Convert ContosoInsurance.Common (Tier 1) and ContosoInsurance.Data (Tier 2) to SDK-style csproj targeting net9.0. csproj-only scope: no code changes, no package version bumps beyond what the conversion strictly requires.

## Research Findings

- **Tooling deviation**: the dedicated convert_project_to_sdk_style tool is NOT available in this CLI environment (verified via tool search). Conversion is done by hand-writing SDK-style csproj. Solution is tiny (3 + 7 compile items), risk is low.
- **Common** (leaf): compile items Config/ConfigHelper.cs, Logging/AppLogger.cs, Properties/AssemblyInfo.cs (covered by SDK globbing). packages.config: log4net 2.0.8, Newtonsoft.Json 11.0.2 - both have netstandard assets compatible with net9.0, versions KEPT IDENTICAL per skill rule + reduced scope. GAC ref System.Configuration -> requires System.Configuration.ConfigurationManager 9.0.17 package (ConfigHelper uses ConfigurationManager.AppSettings/ConnectionStrings) - strictly required by conversion.
- **Data**: no packages.config; refs System.Data + uses System.Data.SqlClient (ClaimsRepository, PolicyRepository, UserRepository) -> requires System.Data.SqlClient 4.9.1 package (assembly not in net9.0 box; no code change). ProjectReference to Common (GUID metadata dropped in SDK-style).
- **AssemblyInfo.cs**: kept in both projects; set GenerateAssemblyInfo=false to avoid duplicate-attribute CS0579.
- **Nullable/ImplicitUsings**: left disabled (user preference: nullable off).
- **Known risk**: NuGet audit may emit NU1902/NU1903 vulnerability warnings for log4net 2.0.8 / Newtonsoft.Json 11.0.2 - version bumps are out of the user-confirmed scope; document as known limitation if they appear.

## Done when

- Both csproj are SDK-style targeting net9.0
- Common packages.config removed
- Both projects restore and build (expected clean - no unsupported APIs)

## Execution Deviations (discovered during build)

- log4net had to be updated 2.0.8 -> 3.3.2: the netstandard1.3 asset of 2.0.8 lacks LogManager.GetLogger(string) and parameterless XmlConfigurator.Configure() -> CS1503/CS1501 compile errors. Update is strictly required for net9.0 compilation and matches the previously user-approved option "Keep log4net (update to 3.x)". No code changes needed.
- System.Data.SqlClient pinned to 4.8.6 instead of 4.9.1: 4.9.x marks all types [Obsolete] -> 16x CS0618 warnings that could only be fixed by code changes (out of scope) or suppression (forbidden). 4.8.6 compiles warning-free.
- Newtonsoft.Json kept at 11.0.2 (scope: no package modernization). NuGet audit emits NU1903 (high severity, GHSA-5crp-9r3c-p9vr) on restore - documented as known limitation; recommend updating to 13.x in a follow-up.
