# Runbook: Build and Test

> Sources: `src/ContosoInsurance/ContosoInsurance.sln`, project files, `src/ContosoInsurance/README.md`. Repository searched for test projects (none found).

The solution is a classic .NET Framework 4.6.1 solution built with MSBuild/Visual Studio. No automated test project was found.

## Verified baseline (2026-07-13)

Baseline restore/build attempted on this machine with **VS 2022 Professional MSBuild 17.8.3** (`.../2022/Professional/MSBuild/Current/Bin/MSBuild.exe`) and .NET SDK 9.0.205.

| Step | Command | Result |
|---|---|---|
| Restore | `msbuild ContosoInsurance.sln -t:Restore -p:RestorePackagesConfig=true` | ✅ Success (log4net 2.0.8, Newtonsoft.Json 11.0.2 → `src/ContosoInsurance/packages/`) |
| Build (as-is) | `msbuild ContosoInsurance.sln -p:Configuration=Debug` | ❌ Fails — env prerequisites missing (see below) |
| Build (env workaround) | add `-p:VisualStudioVersion=17.0 -p:VSToolsPath=<VS>\MSBuild\Microsoft\VisualStudio\v17.0 -p:TargetFrameworkVersion=v4.8` | ❌ Fails — **only** the Web→Services code gap remains |

### Blocking issues found

1. **Environment — MSB3644** (Common, Data, Worker, and transitively all): *"reference assemblies for .NETFramework,Version=v4.6.1 were not found"*. The **.NET Framework 4.6.1 Targeting/Developer Pack is not installed** — the `v4.6.1` reference-assembly folder holds only IntelliSense XMLs (0 DLLs). Fix (not applied): install the .NET Fx 4.6.1 Developer Pack, or retarget.
2. **Environment — MSB4226** (Web, Services): *`Microsoft.WebApplication.targets` not found*. The file **does exist** in the VS install (`...\Professional\MSBuild\Microsoft\VisualStudio\v17.0\WebApplications\`); the CLI build just resolves `VSToolsPath` to the wrong default. Fix: pass `-p:VisualStudioVersion=17.0` and `-p:VSToolsPath=<that folder>`. Not an issue when building inside Visual Studio.
3. **Code — CS0234 (real build break)**: `Default.aspx.cs(7,24): error CS0234: The type or namespace name 'Services' does not exist in the namespace 'ContosoInsurance'`. See [[arch-wcf-service]]. After the two env issues are worked around, this is the **only remaining error**; Common, Data, Services, and Worker all compile.

> The `-p:TargetFrameworkVersion=v4.8` override above was used **only** to run the compiler for baseline observation (v4.8 ref assemblies are present); no project files were modified.

- Per the README, building the legacy app is **not required** to modernize it — the appmod tooling works from source.

## Build (in Visual Studio)

- Open `src/ContosoInsurance/ContosoInsurance.sln` in Visual Studio 2022 (ASP.NET and web development workload + .NET Fx 4.6.1 targeting pack) and build. Inside VS the two environment issues above do not occur; the CS0234 Web→Services gap still does.

## Test

- No test project exists in the repository — there is nothing to run today. `Pendiente/Unknown` whether tests are expected.
- The evaluation rubric (`docs/rubric.md`) notes CI should gate on `dotnet test` "even if there are no meaningful tests".

## Dependency notes

- Packages via `packages.config`: log4net 2.0.8, Newtonsoft.Json 11.0.2 (both outdated/vulnerable).
- Restore works via `msbuild -t:Restore -p:RestorePackagesConfig=true` (no `nuget.exe` required); creates `src/ContosoInsurance/packages/`.

## Unknowns

- Whether a test project should be introduced. `Pendiente/Unknown`.

## Related pages
- [[local-setup]]
- [[arch-infrastructure]]
- [[overview]]
