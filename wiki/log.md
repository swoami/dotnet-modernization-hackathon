# Wiki Log

## [2026-07-13] wiki:update | Verify baseline buildability
- Checked restore/build baseline for the legacy solution (VS 2022 Professional MSBuild 17.8.3; .NET SDK 9.0.205).
- NuGet restore: ✅ `msbuild ContosoInsurance.sln -t:Restore -p:RestorePackagesConfig=true` (log4net 2.0.8, Newtonsoft.Json 11.0.2).
- Build as-is: ❌ fails on two environment prerequisites — MSB3644 (.NET Fx 4.6.1 targeting pack not installed; v4.6.1 ref folder has 0 DLLs) and MSB4226 (`Microsoft.WebApplication.targets` unresolved from CLI `VSToolsPath`; file exists in the VS install).
- Confirmed the `Web → Services` reference gap is a **real** compile-time break: after working around the env issues (`-p:VisualStudioVersion=17.0`, `-p:VSToolsPath=<VS v17.0>`, `-p:TargetFrameworkVersion=v4.8`), the only remaining error is `Default.aspx.cs(7,24): error CS0234: The type or namespace name 'Services' does not exist in the namespace 'ContosoInsurance'`.
- Isolation: Common, Data, Services, Worker compile cleanly; only Web fails. No out-of-repo proxy / Service Reference exists in `ContosoInsurance.Web/`.
- No application code or project files modified; the v4.8 override was used only to run the compiler for observation.
- Pages updated: `index.md`, `log.md`, `runbooks/build-and-test.md`, `architecture/arch-current-state.md`, `architecture/arch-wcf-service.md`.

## [2026-07-13] wiki:update | Resolve initial unknowns
- Updated pages after inspecting markup, service host, Global.asax, and project references.
- Resolved or confirmed remaining `Pendiente/Unknown` items.
- Confirmed: no logout flow (no `FormsAuthentication.SignOut`); no role-based authorization (`Role` stored but never enforced).
- Confirmed: upload never persists `Claims.DocumentPath`; `ExportLog` never written; `SearchByClaimant` has no caller (dead code).
- Confirmed: `Services` project has no log4net configuration and never calls `AppLogger.Configure()`.
- `.aspx`/`.svc` markup inspected — no changes to existing findings.
- New finding: `ContosoInsurance.Web.csproj` lacks a `ProjectReference` to `ContosoInsurance.Services` despite using `IClaimScoringService` (probable build gap).
- Documented the project reference graph in [[overview]] and [[index]].
- Pages updated: `index.md`, `overview.md`, `concept-login.md`, `concept-document-upload.md`, `concept-claim-export.md`, `concept-claim-listing.md`, `arch-security.md`, `arch-database.md`, `arch-logging.md`, `arch-wcf-service.md`, `entity-claim.md`, `entity-export-log.md`, `entity-user.md`.

## [2026-07-13] wiki:init | Bootstrap LLM Wiki
- Created initial LLM Wiki structure for `ContosoInsurance`.
- Added factual overview, architecture pages, runbooks, concepts, and entity pages.
- Linked all created pages from `wiki/index.md`.
- Marked unclear items as `Pendiente/Unknown`.
