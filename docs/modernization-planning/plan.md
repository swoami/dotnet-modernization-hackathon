# Modernization Plan — ContosoInsurance (Legacy Breakers Hackathon)

> Source of truth for this plan: `docs/pre-read.md`, `docs/task-briefs.md`,
> `docs/reference-solution.md`, `docs/rubric.md`, `docs/agenda.md`, and the
> baseline build findings in `wiki/runbooks/build-and-test.md`.
> Companion: [backlog.md](./backlog.md) (epics + stories).

## 1. Goal

Modernize `ContosoInsurance` from **.NET Framework 4.6.1** (WebForms + WCF +
Windows Service + ADO.NET + SQL Server, IIS-hosted, `C:\` file paths, plaintext
secrets) to **.NET 9 on Azure Container Apps**, driven by **GitHub Copilot app
modernization for .NET**, the **Modernization CLI (appcat)**, and **`azd`**.

Target success: a new hire can clone `main`, run `azd up`, and have a working
modernized system in the shared resource group within ~15 minutes.

## 2. Target architecture (Before → After)

| Concern | Before | After |
|---|---|---|
| Framework | .NET Fx 4.6.1 | .NET 9 |
| Project files | `packages.config` + legacy `.csproj` | SDK-style `.csproj` + `PackageReference` |
| Packages / CVEs | log4net 2.0.8, Newtonsoft.Json 11.0.2 | up-to-date, zero High/Critical CVEs |
| Config | `Web.config` / `App.config` | `appsettings.json` + `IConfiguration` + typed options |
| Data access | Raw ADO.NET | EF Core 9 (`ContosoDbContext`) |
| Logging | log4net + `Trace` | `ILogger<T>` + Application Insights |
| Local files | `C:\ClaimsFiles`, `C:\Exports` | Azure Blob Storage (`claim-docs`, `claim-exports`) |
| Secrets | Plaintext SQL user/password | Managed Identity + Key Vault |
| Web hosting | IIS + WebForms | Kestrel + ASP.NET Core Razor Pages *(stretch: Blazor)* |
| Service hosting | Windows Service (`ServiceBase`) | Generic Host Worker Service (`BackgroundService`) |
| Scoring service | WCF SOAP | ASP.NET Core minimal API `POST /claims/{id}/score` *(stretch: CoreWCF/gRPC)* |
| Container | none | Dockerfile per service |
| Infra | manual | Bicep via `azd` |
| CI/CD | none | GitHub Actions (OIDC) |
| Auth | Forms Auth + SHA1 | ASP.NET Core cookie auth + PBKDF2 *(stretch: Entra ID)* |

## 3. Tracks (ownership)

One team of 4 across three tracks, each on its own feature branch — Track A runs
as a pair, Tracks B and C run solo (reference model: 3 pairs / 6, per `docs/task-briefs.md`).

| Track | Branch | Owns |
|---|---|---|
| **A — Web + API tier** | `track/a-web-api` | `Web/`, `Services/`, `Data/` (DbContext shared with B), `Common/` (`ILogger` swap) |
| **B — Worker + Storage** | `track/b-worker-storage` | `Worker/`, Blob wiring, shared `Data/` |
| **C — Platform** | `track/c-platform` | Containerization, Bicep, `azd`, Managed Identity, Key Vault, App Insights, GitHub Actions |

`Common` and `Data` are shared — coordinate at every checkpoint.

## 4. Sprints & timeline (8-hour day, 09:00 start)

| Phase | Window | Focus |
|---|---|---|
| Kickoff + guided `Assess` demo | 09:00–10:00 | Mission, tool check, one task end-to-end |
| Split + branch | 10:00–10:15 | Cut track branches |
| **Sprint 1 — Upgrade** | 10:15–12:30 | Framework, SDK `.csproj`, NuGet/CVE, config, EF Core (A), Worker Service (B), `azd init` + Docker plan (C) |
| Checkpoint 1 + rebase | 12:30–13:15 | Report, rebase on `main`, resolve shared-`Data` conflicts |
| **Sprint 2 — Azure migration** | 13:15–15:15 | Managed Identity, App Insights, Blob, Bicep, Dockerfiles, CI/CD |
| Checkpoint 2 + integrate | 15:15–15:30 | Merge tracks → `integration`, build green |
| Deploy | 15:30–16:15 | `azd up`, smoke test |
| Hardening | 16:15–16:45 | Fix smoke-test gaps, `integration` → `main` |
| Demo + retro | 16:45–17:15 | Per-track demo, `learnings.md` |

## 5. Definition of Done (per phase)

**Checkpoint 1 (end of Sprint 1):**
- All projects target `net9.0`, SDK-style; `packages.config` gone
- `dotnet list package --vulnerable` reports no High/Critical
- `appsettings.json` scaffolded for Web, Services, Worker
- `ContosoDbContext` compiles; at least `GetRecent` migrated
- Track C has `azure.yaml`, `infra/` skeleton, Docker plan drafted

**Checkpoint 2 (pre-deploy):**
- All three branches merged to `integration`; solution builds
- Managed Identity path in place (no SQL password in config)
- Blob client wired for uploads + exports (behind config flag)
- App Insights connection string flows via config; `ILogger` writes to it
- Bicep provisions full stack; Dockerfiles present; Actions workflow drafted

**Post-deploy:**
- Web reachable, login works, claims list renders
- Upload → file lands in `claim-docs`; worker export → `claim-exports` within one interval
- Traces/requests visible in App Insights; no secrets in `appsettings`

## 6. Baseline reality (from build verification)

- NuGet restore succeeds; the solution **does not build as-is**.
- Environment prerequisites (not code defects): missing .NET Fx 4.6.1 targeting
  pack (MSB3644) and CLI web-targets path (MSB4226) — both vanish once projects
  target `net9.0`.
- One real code break: **Web → Services CS0234** (`Default.aspx.cs` uses
  `IClaimScoringService` with no reference to `Services`). The target design
  removes this by replacing the WCF client with an `HttpClient` call to the new
  minimal-API scoring endpoint. See `wiki/runbooks/build-and-test.md`.

## 7. Ground rules

- **Copilot-first**: use the appmod extension for anything it supports; fall
  back to Copilot Chat/Agent for freeform edits; hand-editing is last resort and
  logged in `docs/learnings.md`.
- **Every agent diff is reviewed** before accept.
- **Small commits**: one task = one commit; each commit references its appmod
  task or a concrete concern (see backlog IDs).
- **Sync at every checkpoint**; rebase/merge `main`.
- **Ask before rewriting** another track's project.
- **Time-box stretch goals**; only after the core checklist is green.

## 8. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Shared `Data/` DbContext conflicts (A & B) | Agree entity/`DbSet` shapes at Checkpoint 1; one owner merges |
| First `azd up` fails (expected) | Time-boxed deploy block; diagnose with Copilot + `azd` logs |
| Scope creep into stretch goals | Log as follow-up issues; keep core checklist first |
| Secret committed accidentally | `.gitignore` guardrails + rubric −5 anti-goal; MI/Key Vault only |
| BMAD tooling noise (489 files) | `.gitignore` excludes `.agents/`, `.claude/`, `.github/agents/`, `_bmad/`, `_bmad-output/`; reinstall via `npx bmad-method install` |

## 9. Scoring targets (rubric, ≥70/100 = success)

Framework/packages 15 · Config & EF Core 15 · Hosting/containers 15 · Blob 10 ·
Identity/secrets 10 · Observability 10 · Bicep IaC 10 · CI/CD 5 · Team artifacts 10.
Bonus (+15 max): Blazor, CoreWCF/gRPC, Entra ID, probes, OpenTelemetry, cost check.
