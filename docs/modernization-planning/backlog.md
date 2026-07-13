# Modernization Backlog — ContosoInsurance (Legacy Breakers Hackathon)

> Companion to [plan.md](./plan.md). Derived from `docs/task-briefs.md`,
> `docs/reference-solution.md`, `docs/rubric.md`, `docs/agenda.md`.
> **ID scheme:** `E#` epic, `E#-S#` story. Use the story ID in commit messages
> (e.g. `E2-S1: migrate ClaimsRepository.GetRecent to EF Core`).

## Legend

- **Track:** A (Web+API) · B (Worker+Storage) · C (Platform)
- **Sprint:** 1 = Upgrade · 2 = Azure migration
- **Priority:** P0 core (must for ≥70) · P1 important · P2 stretch/bonus
- **Rubric:** section the story scores toward (see `docs/rubric.md`)

## Epic index

| Epic | Title | Track | Sprint | Priority |
|---|---|---|---|---|
| E1 | Framework & package modernization | A, B | 1 | P0 |
| E2 | Configuration & data access (EF Core) | A (+B shared) | 1 | P0 |
| E3 | Web tier: WebForms → ASP.NET Core | A | 1 | P0 |
| E4 | Scoring service: WCF → minimal API | A | 1 | P0 |
| E5 | Worker: Windows Service → BackgroundService | B | 1 | P0 |
| E6 | Storage: local files → Azure Blob | B (+A) | 2 | P0 |
| E7 | Identity & secrets: Managed Identity + Key Vault | A, C | 2 | P0 |
| E8 | Observability: ILogger + App Insights | A, B | 2 | P0 |
| E9 | Containerization | A, B, C | 1–2 | P0 |
| E10 | Infrastructure as Code (Bicep + azd) | C | 2 | P0 |
| E11 | CI/CD (GitHub Actions, OIDC) | C | 2 | P1 |
| E12 | Team artifacts & demo | All | all | P1 |
| E13 | Stretch goals (bonus) | varies | 2+ | P2 |

---

## E1 — Framework & package modernization *(Track A+B · Sprint 1 · P0)*

- **E1-S1** — Convert all five `.csproj` to SDK-style, target `net9.0`.
  *AC:* every project is SDK-style; `dotnet build` resolves; no `TargetFrameworkVersion` v4.6.1 remains. *Rubric: §1 (5pts)*
- **E1-S2** — Remove `packages.config`, migrate to `PackageReference`.
  *AC:* no `packages.config` anywhere; packages listed in `.csproj`. *Rubric: §1 (4pts)*
- **E1-S3** — Remove/replace vulnerable packages: drop `log4net`; `Newtonsoft.Json` → `System.Text.Json` (keep Newtonsoft only if EF requires).
  *AC:* `dotnet list package --vulnerable` → zero High/Critical; no direct `log4net`/`Newtonsoft.Json` refs. *Rubric: §1 (3+3pts)*

## E2 — Configuration & data access *(Track A, DbContext shared with B · Sprint 1 · P0)*

- **E2-S1** — Introduce `ContosoDbContext` with `DbSet<Claim>`, `DbSet<Policy>`, `DbSet<User>`, `DbSet<ExportLog>` (EF Core 9).
  *AC:* context compiles; entities mapped to existing schema. *Rubric: §2 (5pts)*
- **E2-S2** — Migrate `ClaimsRepository` to EF Core, preserving public method signatures (start with `GetRecent`, `GetById`, `UpdateScore`).
  *AC:* callers unchanged; ADO.NET removed from `Data/`. *Rubric: §2 (5pts)*
- **E2-S3** — Fix SQL injection in `ClaimsRepository.SearchByClaimant` (string concat → parameterized LINQ/EF).
  *AC:* no string-concatenated SQL; note the removed injection risk. *Rubric: §2 (2pts)*
- **E2-S4** — Replace `Web.config`/`App.config` with `appsettings.json` + `IConfiguration` + typed options.
  *AC:* no `Web.config`/`App.config` remain; options bound in `Program.cs`. *Rubric: §2 (4+4pts)*

## E3 — Web tier: WebForms → ASP.NET Core *(Track A · Sprint 1 · P0)*

- **E3-S1** — `Program.cs` composes DI, config, logging, cookie auth, health, EF Core.
  *AC:* app boots on Kestrel; DI wires `ClaimsService`, `UserService`. *Rubric: §3 (5pts)*
- **E3-S2** — Convert `Default.aspx` → `Pages/Index.cshtml(.cs)` (claims list).
  *AC:* claims list renders from EF Core. *Rubric: §3*
- **E3-S3** — Convert `Login.aspx` → `Pages/Login.cshtml(.cs)` with ASP.NET Core cookie auth; replace SHA1 hashing with PBKDF2.
  *AC:* login issues auth cookie; SHA1 gone. *Rubric: §5 (SHA1 replacement), Track A checklist*
- **E3-S4** — Convert `Upload.aspx` → `Pages/Upload.cshtml(.cs)` (posts to storage abstraction, see E6).
  *AC:* upload page functional against `IClaimDocumentStore`. *Rubric: §4*

## E4 — Scoring service: WCF → minimal API *(Track A · Sprint 1 · P0)*

- **E4-S1** — Reimplement `IClaimScoringService.ScoreClaim` as ASP.NET Core minimal API `POST /claims/{id}/score`, preserving scoring rules.
  *AC:* endpoint returns JSON score 0–1000; `-1`/404 when claim missing. *Rubric: §3, Track A checklist*
- **E4-S2** — Replace the WCF `ChannelFactory<IClaimScoringService>` call in the Web tier with `HttpClient` + `IHttpClientFactory`.
  *AC:* Web scores claims over HTTP; **CS0234 gap resolved**; no `System.ServiceModel` client. *Rubric: §3*

## E5 — Worker: Windows Service → BackgroundService *(Track B · Sprint 1 · P0)*

- **E5-S1** — Convert `ClaimsExporterService : ServiceBase` → `BackgroundService` on `Host.CreateApplicationBuilder`; drop `System.ServiceProcess` + `ProjectInstaller`.
  *AC:* worker runs on Generic Host; no `ServiceBase`. *Rubric: §3 (5pts)*
- **E5-S2** — Replace `System.Timers.Timer` with `PeriodicTimer`; move `App.config` → typed `ExportOptions`.
  *AC:* interval from options; `App.config` gone. *Rubric: §2, §3*

## E6 — Storage: local files → Azure Blob *(Track B, +A for Upload · Sprint 2 · P0)*

- **E6-S1** — Add `IClaimDocumentStore` in `Common` with `BlobClaimDocumentStore` (`BlobServiceClient` + `DefaultAzureCredential`); provide in-memory fake for tests.
  *AC:* abstraction used by Web + Worker; no connection strings. *Rubric: §4, §5*
- **E6-S2** — Worker export: replace `File.WriteAllText` with blob upload to `claim-exports`; write an `ExportLog` audit row per export.
  *AC:* CSV lands in `claim-exports`; `ExportLog` row inserted. *Rubric: §4 (5pts)*
- **E6-S3** — Web upload writes to `claim-docs` (container from options), not disk.
  *AC:* uploaded file lands in `claim-docs`; no `C:\` paths. *Rubric: §4 (5pts)*

## E7 — Identity & secrets *(Track A app-side, Track C infra · Sprint 2 · P0)*

- **E7-S1** — Azure SQL via Managed Identity: `Authentication=Active Directory Default`; `DefaultAzureCredential` locally. No SQL password anywhere.
  *AC:* no credentials in any config; connects locally (dev AAD) and in-cloud (MI). *Rubric: §5 (5pts)*
- **E7-S2** — Managed Identity for Blob + ACR pull (infra role assignments in Bicep).
  *AC:* MI has Blob Data Contributor + AcrPull. *Rubric: §5 (3pts), §7*
- **E7-S3** — Any remaining secret referenced from Key Vault (or documented that none remain).
  *AC:* Key Vault Secrets User role; no plaintext secrets. *Rubric: §5 (2pts)*

## E8 — Observability *(Track A+B · Sprint 2 · P0)*

- **E8-S1** — Replace `log4net`/`AppLogger` with `ILogger<T>`; remove all `Trace.*` calls.
  *AC:* `ILogger` throughout; log4net + Trace gone. *Rubric: §6 (4pts)*
- **E8-S2** — Wire `AddApplicationInsightsTelemetry()` (Web/Services) and `AddApplicationInsightsTelemetryWorkerService()` (Worker); connection string via config.
  *AC:* AI connection string from config; traces visible after smoke test. *Rubric: §6 (4+2pts)*
- **E8-S3** — Health endpoint `/health` (DbContext + Blob container checks).
  *AC:* `/health` returns Healthy when dependencies reachable. *Rubric: Track A/B checklist, bonus probes*

## E9 — Containerization *(Track A, B, C · Sprint 1–2 · P0)*

- **E9-S1** — Multi-stage Dockerfile for Web (SDK build → aspnet runtime, non-root user).
- **E9-S2** — Multi-stage Dockerfile for Services (skip if merged into Web).
- **E9-S3** — Multi-stage Dockerfile for Worker (SDK → runtime, no ASP.NET base).
  *AC (all):* each service builds + runs as a container locally (`docker run`/`compose`). *Rubric: §3 (5pts)*

## E10 — Infrastructure as Code *(Track C · Sprint 2 · P0)*

- **E10-S1** — `azd init` producing `azure.yaml` (web, scoring, worker) + `infra/` skeleton.
  *AC:* `azure.yaml` at chosen root; services mapped to container apps. *Rubric: §7*
- **E10-S2** — `infra/main.bicep` (+ modules) provisions: Log Analytics, App Insights, ACR, user-assigned MI, Container Apps env + apps, Azure SQL + DB + AAD admin, Key Vault, Storage + `claim-docs`/`claim-exports`.
  *AC:* one `azd up` provisions the full stack. *Rubric: §7 (4pts)*
- **E10-S3** — Parameterize Bicep (no secrets, no hard-coded names); role assignments in Bicep (Blob Contributor, AcrPull, KV Secrets User, SQL AAD user).
  *AC:* no secrets in params; roles in IaC not portal. *Rubric: §7 (3+3pts)*
- **E10-S4** — Wire env vars into Container Apps: App Insights CS, SQL (server-only, MI), Storage account name.
  *AC:* apps read config from env. *Rubric: §5, §6, §7*

## E11 — CI/CD *(Track C · Sprint 2 · P1)*

- **E11-S1** — `.github/workflows/deploy.yml`: OIDC login, `dotnet build`, `azd deploy` on push to `main`.
  *AC:* workflow authenticates via OIDC and deploys. *Rubric: §8 (3pts)*
- **E11-S2** — Gate workflow on `dotnet test` (even with no meaningful tests).
  *AC:* build fails if tests fail. *Rubric: §8 (2pts)*
- **E11-S3** — `docs/README-DEPLOY.md`: deploy commands + post-deploy verification.
  *AC:* documents `azd up`/redeploy/rollback. *Rubric: Track C checklist*

## E12 — Team artifacts & demo *(All · P1)*

- **E12-S1** — `docs/learnings.md` captures ≥6 prompt/response lessons.
  *AC:* ≥6 entries. *Rubric: §9 (3pts)*
- **E12-S2** — Every commit references an appmod task or backlog ID.
  *AC:* commit hygiene spot-check passes. *Rubric: §9 (2pts)*
- **E12-S3** — Team demo covers all three tracks in ≤20 min; stretch goals logged as follow-up issues.
  *AC:* demo done; issues filed. *Rubric: §9 (3+2pts)*

## E13 — Stretch goals *(P2 · bonus, uncapped +15 max)*

- **E13-S1** — WebForms → **Blazor Server** (not just Razor Pages). *+5*
- **E13-S2** — WCF → **CoreWCF or gRPC** with working client. *+5*
- **E13-S3** — **Entra ID** auth (`Microsoft.Identity.Web`) replaces cookie auth. *+5*
- **E13-S4** — Readiness/liveness probes wired in Bicep. *+3*
- **E13-S5** — OpenTelemetry export (in addition to App Insights). *+3*
- **E13-S6** — Cost check: `azd show` cost breakdown captured in `learnings.md`. *+3*

---

## Anti-goals (rubric deductions — avoid)

- −5 — Any committed secret (password, credentialed connection string, storage key).
- −5 — `azd up` fails at demo time.
- −3 — Any Copilot diff accepted without review.
- −3 — Any part still requires `C:\` paths.

## Suggested execution order (critical path to ≥70)

1. **Sprint 1:** E1 → E2 → (E3, E4 on A) ∥ (E5 on B) ∥ (E9 Dockerfiles + E10-S1 on C)
2. **Checkpoint 1:** rebase, resolve shared `Data/` (E2) conflicts.
3. **Sprint 2:** E6 ∥ E7 ∥ E8 ∥ (E10-S2..S4, E11 on C)
4. **Integrate → deploy (E10) → smoke test → harden.**
5. **E12 throughout; E13 only if core is green.**
