# 02-foundation-libs - Progress Details

## Files modified

- src/ContosoInsurance/ContosoInsurance.Common/ContosoInsurance.Common.csproj - rewritten as SDK-style, TargetFramework net9.0, GenerateAssemblyInfo=false; PackageReference: log4net 3.3.2, Newtonsoft.Json 11.0.2, System.Configuration.ConfigurationManager 9.0.17
- src/ContosoInsurance/ContosoInsurance.Common/packages.config - removed (migrated to PackageReference)
- src/ContosoInsurance/ContosoInsurance.Data/ContosoInsurance.Data.csproj - rewritten as SDK-style, TargetFramework net9.0, GenerateAssemblyInfo=false; PackageReference: System.Data.SqlClient 4.8.6; ProjectReference to Common preserved

No .cs source files were modified.

## Package changes and rationale

| Package | Before | After | Why |
|---|---|---|---|
| log4net | 2.0.8 (packages.config) | 3.3.2 | 2.0.8 netstandard asset lacks APIs used by AppLogger -> compile errors on net9.0; strictly required |
| Newtonsoft.Json | 11.0.2 (packages.config) | 11.0.2 | Kept identical per reduced scope; NU1903 vulnerability warning documented as known limitation |
| System.Configuration.ConfigurationManager | (GAC ref) | 9.0.17 | ConfigurationManager not in net9.0 box; required by ConfigHelper |
| System.Data.SqlClient | (GAC ref) | 4.8.6 | Assembly not in net9.0 box; 4.8.6 chosen over 4.9.x to avoid [Obsolete] CS0618 warnings without code changes |

## Build results

- ContosoInsurance.Common: build SUCCEEDED, 0 errors, 0 code warnings
- ContosoInsurance.Data: build SUCCEEDED, 0 errors, 0 code warnings
- Remaining warning: NU1903 (Newtonsoft.Json 11.0.2 high-severity vulnerability) - accepted known limitation per user-confirmed scope (no package modernization); recommend 13.x follow-up

## Issues resolved

- CS1503/CS1501 in AppLogger.cs -> fixed by log4net 3.3.2 (package-only fix, no code change)
- 16x CS0618 SqlClient obsolete warnings -> avoided by pinning System.Data.SqlClient 4.8.6
