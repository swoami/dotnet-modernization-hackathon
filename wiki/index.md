# LLM Wiki Index

> Agents: read this file first, then load ONLY the relevant pages.
> Last updated: 2026-07-13
> Sources used: `src/ContosoInsurance/` (all 5 projects + `db/`), `src/ContosoInsurance/README.md`, root `README.md`, `docs/` (pre-read, task-briefs, rubric, reference-solution), all `.aspx`/`.svc` markup, `Global.asax.cs`, every `.csproj`, `Web.config`/`App.config`, `packages.config`, `db/001-schema.sql`, `db/002-seed.sql`.

## Principles

- Do not change legacy behavior without identifying the current behavior first.
- Do not invent facts; mark unclear items as `Pendiente/Unknown`.
- Keep the app buildable after every change.
- Prefer small, evidence-based updates.
- Keep wiki pages compact and linked from this index.

## Project Overview

- [[overview]]

## Concepts

| Page | One-line summary |
|---|---|
| [[concept-login]] | Forms-auth login verifying SHA1+salt password hashes. |
| [[concept-claim-listing]] | `Default.aspx` lists 50 recent claims in a grid. |
| [[concept-claim-scoring]] | WCF SOAP service returns a rules-based 0–1000 score. |
| [[concept-document-upload]] | `Upload.aspx` saves claim files to local `C:\ClaimsFiles`. |
| [[concept-claim-export]] | Windows Service exports claims to CSV on a timer. |
| [[concept-configuration]] | `ConfigurationManager` reads `Web.config`/`App.config`. |
| [[concept-logging]] | Static `AppLogger` wraps log4net + `Trace`. |

## Entities

| Page | Key fields |
|---|---|
| [[entity-user]] | UserId, Username, PasswordHash, Salt, Role |
| [[entity-policy]] | PolicyId, PolicyNumber, HolderName, ProductLine, CoverageAmount, EffectiveDate, ExpirationDate |
| [[entity-claim]] | ClaimId, PolicyId, ClaimantName, Amount, Status, FiledOn, DocumentPath, Score, Notes |
| [[entity-export-log]] | ExportId, ExportedAt, FilePath, RowCount |

## Architecture

| Page | Summary |
|---|---|
| [[arch-current-state]] | Overall .NET Fx 4.6.1 multi-process monolith. |
| [[arch-webforms]] | ASP.NET WebForms agent portal. |
| [[arch-wcf-service]] | WCF SOAP claim scoring service. |
| [[arch-worker]] | Windows Service nightly CSV exporter. |
| [[arch-database]] | SQL Server schema + ADO.NET access. |
| [[arch-security]] | Forms Auth, SHA1 hashing, plaintext secrets. |
| [[arch-configuration]] | `Web.config`/`App.config` + `ConfigHelper`. |
| [[arch-logging]] | log4net 2.0.8 + `Trace` to local files. |
| [[arch-infrastructure]] | IIS + Windows Service; no containers/IaC/CI-CD. |

## Runbooks

| Page | Purpose |
|---|---|
| [[local-setup]] | Prerequisites and local run of the legacy app. |
| [[build-and-test]] | How to build; current test situation. |

## Current Build Status

| Area | Status | Notes |
|---|---|---|
| Solution structure | ✅ Confirmed | 5 projects in `ContosoInsurance.sln`. |
| Target framework | ✅ Confirmed | .NET Framework 4.6.1 across all projects. |
| Database schema | ✅ Confirmed | `db/001-schema.sql` (4 tables). |
| Data access | ✅ Confirmed | Raw ADO.NET repositories. |
| WCF service | ✅ Confirmed | `IClaimScoringService` SOAP. |
| Worker service | ✅ Confirmed | `ServiceBase` + `System.Timers.Timer`. |
| Automated tests | ❓ Pendiente/Unknown | No test project found. |
| Docker/IaC/CI-CD | ❓ Pendiente/Unknown | None present in repo. |
| `Web → Services` reference | ❌ Confirmed break | CS0234 at compile: `Default.aspx.cs` uses `IClaimScoringService` with no reference to `Services`. |
| NuGet restore | ✅ Confirmed | `msbuild -t:Restore -p:RestorePackagesConfig=true` succeeds. |
| Local buildability | ❌ Fails as-is | Env: missing .NET Fx 4.6.1 targeting pack (MSB3644) + CLI web-targets path (MSB4226); code: Web→Services CS0234. Only Web fails to compile. See [[build-and-test]]. |

## Project Reference Graph

```
Common  -> (NuGet) log4net 2.0.8, Newtonsoft.Json 11.0.2
Data    -> Common
Services-> Common, Data
Web     -> Common, Data   (uses Services contract WITHOUT a ProjectReference)
Worker  -> Common, Data
```
See [[overview]] for the diagram and the `Web → Services` gap.

## Latest Log Entries

- **[2026-07-13] wiki:update** — Verified baseline buildability. NuGet restore succeeds; as-is build fails on env prerequisites (.NET Fx 4.6.1 targeting pack MSB3644, CLI web-targets path MSB4226). With those worked around, the **only** code error is the Web→Services **CS0234** gap — confirmed real build break; Common/Data/Services/Worker compile. See [[log]].
- **[2026-07-13] wiki:update** — Resolved initial unknowns after inspecting `.aspx`/`.svc` markup, `Global.asax.cs`, and all `.csproj` references: no logout flow, no role-based authz, upload never persists `DocumentPath`, `ExportLog` never written, `SearchByClaimant` has no caller, `Services` has no log4net config. Documented the project reference graph and a `Web → Services` reference gap. See [[log]].
- **[2026-07-13] wiki:init** — Bootstrapped the LLM Wiki: overview, 9 architecture pages, 2 runbooks, 7 concepts, 4 entities, all linked from this index. Unclear items marked `Pendiente/Unknown`. See [[log]].
