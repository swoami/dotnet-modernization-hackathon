# PRD — ContosoInsurance Modernization Hackathon (Legacy Breakers)

> **Type:** Lightweight, execution-focused PRD for a **1-day** hackathon.
> **Source of truth:** the LLM Wiki (`wiki/index.md` + linked pages).
> **Companions:** [hackathon-brief.md](./hackathon-brief.md) · [plan.md](./plan.md) · [backlog.md](./backlog.md).
> **Baseline build facts:** [wiki/runbooks/build-and-test.md](../../wiki/runbooks/build-and-test.md).
> Author: John (Product Manager) · 2026-07-13. Optimize for **deployment success** and a **rubric score ≥ 70/100**.

---

## 1. Problem Statement

`ContosoInsurance` is a deliberately-legacy **.NET Framework 4.6.1** claims application
(5 projects: WebForms portal, WCF SOAP scoring service, Windows Service exporter, raw
ADO.NET data layer, shared common library). It is tied to IIS + Windows Service hosting,
`C:\` file paths, plaintext secrets, SHA1 password hashing, and outdated/vulnerable
packages (log4net 2.0.8, Newtonsoft.Json 11.0.2). It cannot run on modern cloud
infrastructure and carries known security debt.

The solution also **does not build as-is** today. Per the verified baseline, this splits
into two distinct categories that must not be conflated (see §10):

- **Environment prerequisites** (not code defects): missing .NET Fx 4.6.1 targeting pack
  (MSB3644) and CLI web-targets path resolution (MSB4226). Both vanish once projects
  target `net9.0`.
- **One real code defect:** the **Web → Services CS0234** gap — `Default.aspx.cs(7,24)`
  uses `ContosoInsurance.Services.IClaimScoringService` with **no** project/assembly
  reference. This is a **confirmed baseline code break**; Common, Data, Services, and
  Worker all compile.

## 2. Mission / North Star

Take `ContosoInsurance` from **Windows/IIS legacy** to **.NET 9 running on Azure Container
Apps** in one day, driven by **GitHub Copilot app modernization for .NET**, the
**Modernization CLI (appcat)**, and **`azd`**.

> **Success signal:** a new hire can clone `main`, run `azd up`, and have a working
> modernized system in the shared resource group within ~15 minutes.

## 3. Current State

| Area | Current state | Wiki evidence |
|---|---|---|
| Framework | .NET Framework 4.6.1 (all 5 projects) | [[overview]], [[arch-current-state]] |
| Project style | legacy `.csproj` + `packages.config` | [[arch-current-state]] |
| Frontend | ASP.NET WebForms (`.aspx` + code-behind) | [[arch-webforms]] |
| Scoring | WCF SOAP (`basicHttpBinding`), rules-based 0–1000 score | [[arch-wcf-service]], [[concept-claim-scoring]] |
| Worker | Windows Service (`ServiceBase` + `System.Timers.Timer`) | [[arch-worker]] |
| Data | raw ADO.NET (sync), SQL Server | [[arch-database]] |
| Config | `Web.config` / `App.config` via `ConfigurationManager` | [[arch-configuration]] |
| Auth | Forms Auth + SHA1+salt (no logout, `Role` not enforced) | [[arch-security]], [[concept-login]] |
| Logging | log4net 2.0.8 + `Trace` | [[arch-logging]] |
| Storage | local `C:\ClaimsFiles`, `C:\Exports`, `C:\` logs | [[concept-document-upload]], [[arch-worker]] |
| Cloud | no Docker / IaC / CI-CD | [[arch-infrastructure]] |
| Tests | none found in repo | [[build-and-test]] |
| **Build** | NuGet restore succeeds; **does not build as-is** (see §1, §10) | [[build-and-test]] |

**Known latent gaps** (documented, not necessarily in scope to fix): no logout flow,
`Role` never enforced, `DocumentPath` never persisted on upload, `ExportLog` never
written, `SearchByClaimant` is dead code with a SQL-injection flaw.

## 4. Target State

| Concern | Before | After |
|---|---|---|
| Framework | .NET Fx 4.6.1 | **.NET 9** |
| Projects | `packages.config` + legacy `.csproj` | SDK-style + `PackageReference` |
| Packages | log4net 2.0.8, Newtonsoft.Json 11.0.2 | up-to-date, zero High/Critical CVEs |
| Frontend | WebForms | ASP.NET Core **Razor Pages** *(stretch: Blazor)* |
| Scoring | WCF SOAP | ASP.NET Core **minimal API** `POST /claims/{id}/score` |
| Worker | Windows Service | Generic Host **`BackgroundService`** + `PeriodicTimer` |
| Data | raw ADO.NET | **EF Core 9** (`ContosoDbContext`) |
| Config | `Web.config`/`App.config` | `appsettings.json` + `IConfiguration` + typed options |
| Database | SQL Server (SQL auth) | **Azure SQL** via Managed Identity |
| Files | `C:\` paths | **Azure Blob Storage** (`claim-docs`, `claim-exports`) |
| Secrets | plaintext in config | **Managed Identity** + **Key Vault** |
| Logging | log4net + `Trace` | **`ILogger<T>`** + **Application Insights** |
| Hosting | IIS + Windows Service | **Azure Container Apps** (Docker) |
| Infra | manual | **Bicep** via **`azd`** |
| CI/CD | none | **GitHub Actions** (OIDC) |
| Auth | Forms Auth + SHA1 | ASP.NET Core cookie auth + PBKDF2 *(stretch: Entra ID)* |

> **The target architecture eliminates the CS0234 gap by design:** the WCF
> `ChannelFactory<IClaimScoringService>` client is replaced with an `HttpClient` /
> `IHttpClientFactory` call to the new minimal-API scoring endpoint (backlog **E4-S2**).
> Modernizing to the target removes the broken reference rather than repairing it.

## 5. In Scope (P0 core — the path to ≥70)

Grounded in [backlog.md](./backlog.md) epics:

- **E1** — Framework & package modernization: SDK-style `.csproj`, `net9.0`, drop
  `packages.config`, remove vulnerable packages (drop `log4net`; `Newtonsoft.Json` →
  `System.Text.Json`).
- **E2** — Configuration & data access: `ContosoDbContext` (EF Core 9), migrate
  `ClaimsRepository` preserving signatures, `appsettings.json` + typed options.
- **E3** — Web tier: WebForms → ASP.NET Core Razor Pages (Index, Login w/ PBKDF2, Upload).
- **E4** — Scoring: WCF → minimal API `POST /claims/{id}/score`; **replace WCF client with
  `HttpClient` (resolves CS0234)**.
- **E5** — Worker: Windows Service → `BackgroundService` + `PeriodicTimer`.
- **E6** — Storage: local files → Azure Blob (`claim-docs`, `claim-exports`).
- **E7** — Identity & secrets: Managed Identity for SQL/Blob/ACR + Key Vault.
- **E8** — Observability: `ILogger<T>` + Application Insights; `/health` endpoint.
- **E9** — Containerization: multi-stage Dockerfiles (non-root) per service.
- **E10** — Infrastructure as Code: `azd init` + Bicep provisioning the full stack.
- **E12** — Team artifacts & demo (artifacts are P1 — cheap, ongoing rubric points —
  but the post-deploy smoke test **E12-S4** is **P0**: it gates final demo readiness and
  closes the P0 execution slice; see `epics-and-stories.md`).

## 6. Out of Scope

- **P1 / P2 items unless core is green:** E11 CI/CD is P1 (nice-to-have for the day);
  E13 stretch/bonus (Blazor, CoreWCF/gRPC, Entra ID, probes, OpenTelemetry, cost check)
  only after the P0 checklist is complete.
- **Latent legacy gaps** may be left as follow-up issues (see §15): logout flow, `Role`
  enforcement, `DocumentPath` persistence, `ExportLog` writes — except where a P0 story
  already covers them (E6-S2 writes `ExportLog`; E6-S3 persists uploads to Blob).
- **`SearchByClaimant`** — dead code; delete or parameterize is an open decision (§15).
- **No production hardening beyond the rubric** (load testing, DR, multi-region, etc.).
- **No changes to scoring business rules** — behavior must be preserved identically (§9).
- Building the legacy app on .NET Fx 4.6.1 is **not required** — the appmod tooling works
  from source ([[build-and-test]]).

## 7. Success Criteria

**Checkpoint 1 — end of Upgrade sprint:**
- All projects target `net9.0`, SDK-style; `packages.config` gone.
- `dotnet list package --vulnerable` → no High/Critical.
- `appsettings.json` scaffolded for Web, Services, Worker.
- `ContosoDbContext` compiles; at least `GetRecent` on EF Core.
- Platform has `azure.yaml`, `infra/` skeleton, Docker plan.

**Checkpoint 2 — pre-deploy:**
- Branches merged to `integration`; solution builds.
- Managed Identity path in place (no SQL password in config).
- Blob wired for uploads + exports; App Insights connection string flows via config.
- Bicep provisions the full stack; Dockerfiles present; Actions workflow drafted.

**Post-deploy smoke test:**
- Web reachable; login works; claims list renders.
- Upload → file in `claim-docs`; worker export → `claim-exports` within one interval.
- Traces/requests visible in App Insights; **no secrets** in `appsettings`.

## 8. Rubric Alignment

Total 100 pts; **target ≥ 70**. Attack highest-weight, lowest-risk first.

| Rubric section | Pts | Priority | Backlog |
|---|---|---|---|
| Framework & package modernization | 15 | **P0 — do first, unblocks everything** | E1 |
| Configuration & data access (EF Core) | 15 | **P0** | E2 |
| Hosting & runtime (Kestrel + Worker + containers) | 15 | **P0** | E3, E5, E9 |
| Storage & I/O (Blob) | 10 | P0 | E6 |
| Identity & secrets (Managed Identity + KV) | 10 | P0 | E7 |
| Observability (`ILogger` + App Insights) | 10 | P0 | E8 |
| Infrastructure as code (Bicep + `azd`) | 10 | P0 | E10 |
| CI/CD (GitHub Actions, OIDC) | 5 | P1 | E11 |
| Team artifacts & demo | 10 | P1 (cheap — do throughout); **E12-S4 smoke test = P0**, gates the demo | E12 |
| **Bonus** (Blazor, CoreWCF/gRPC, Entra ID, probes, OTel, cost) | +15 max | P2 — only if core is green | E13 |

**Deductions to avoid:** −5 committed secret · −5 `azd up` fails at demo · −3 any Copilot
diff accepted without review · −3 any part still requires `C:\` paths.

## 9. User / System Flows That Must Still Work

Behavior must survive the rewrite (grounded in [[overview]]):

| Flow | Today | Preserve as |
|---|---|---|
| **Login** | Verify SHA1+salt hash → Forms Auth cookie | Cookie auth login; **PBKDF2** hash |
| **View + auto-score** | List 50 recent claims; score unscored via WCF on page load | Razor Page lists claims; scores via `HttpClient` → minimal API |
| **Upload document** | Save file to `C:\ClaimsFiles\{claimId}\{filename}` | Upload to Blob `claim-docs` |
| **Nightly export** | Timer writes CSV of ≤1000 claims to `C:\Exports` | `BackgroundService` writes CSV to Blob `claim-exports` |
| **Scoring rules** | Deterministic 0–1000 rules (base 500 ± amount/status/age) | **Identical rules** behind the minimal API |

> Fix, don't replicate ([[arch-security]]): SQL injection in `SearchByClaimant`, path
> traversal on upload, SHA1 hashing, plaintext secrets.

## 10. Technical Constraints

- **Separate environment prerequisites from real code defects** (from [[build-and-test]]):
  - *Environment (not defects):* MSB3644 (.NET Fx 4.6.1 targeting pack missing) and
    MSB4226 (`Microsoft.WebApplication.targets` path resolved wrong by the CLI). Both
    disappear once projects retarget to `net9.0`; neither occurs when building inside
    Visual Studio.
  - *Real code defect:* **Web → Services CS0234** — the one confirmed baseline code break.
    Resolved by the target design (E4-S2 replaces the WCF client with `HttpClient`), not
    by adding a WCF project reference.
- NuGet restore succeeds via `msbuild -t:Restore -p:RestorePackagesConfig=true`.
- **Copilot-first:** use the appmod extension where supported; review **every** diff
  before accept (−3 deduction otherwise).
- Shared `Common` and `Data` projects — coordinate `DbContext`/entity shapes at every
  checkpoint; single owner merges.
- No secrets in source; Managed Identity + Key Vault only. `.gitignore` guardrails already
  block `appsettings.Development/Local.json`, `*.pfx`, `*.key`, and BMAD tooling folders.
- Do not start Azure migration before the code compiles on .NET 9.
- **Do not modify application code as part of this PRD** — this document is planning only.

## 11. Risks and Mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Shared `Data/` `DbContext` conflicts between tracks | High | Agree entity/`DbSet` shapes at Checkpoint 1; single owner merges |
| First `azd up` fails (expected) | High | Time-boxed deploy block; diagnose with Copilot + `azd` logs |
| Web→Services **CS0234** lingers | Med | Resolved by design — replace WCF client with `HttpClient` → minimal API (E4-S2) |
| Managed Identity / SQL AAD setup friction | Med | Prep `Authentication=Active Directory Default`; `DefaultAzureCredential` locally |
| Scope creep into stretch goals | Med | Log as follow-up issues; core checklist first |
| Secret accidentally committed | Low / High-impact | `.gitignore` + review discipline; MI/KV only (−5 anti-goal) |

## 12. Execution Model — 4-Person Team

Reference model is 3 pairs / 6 people; collapsed to three focus areas for a 4-person team
(Platform runs solo; pair where work is densest):

| Focus | People | Owns | Branch |
|---|---|---|---|
| **Web + API** | 2 | `Web/` (WebForms→Razor), `Services/` (WCF→minimal API), `Data/` DbContext, `Common/` `ILogger` | `track/a-web-api` |
| **Worker + Storage** | 1 | `Worker/` (→`BackgroundService`), Blob wiring for uploads + exports | `track/b-worker-storage` |
| **Platform** | 1 | Dockerfiles, Bicep, `azd`, Managed Identity, Key Vault, App Insights, GitHub Actions | `track/c-platform` |

> `Common` and `Data` are shared — coordinate at every checkpoint. With a full 6, split
> back into three pairs per `docs/task-briefs.md`.

## 13. Milestones — Hackathon Day (8-hour, 09:00 start)

| Phase | Window | Focus |
|---|---|---|
| Kickoff + guided `Assess` demo | 09:00–10:00 | Mission, tool check, one task end-to-end |
| Split + branch | 10:00–10:15 | Cut track branches |
| **Sprint 1 — Upgrade** | 10:15–12:30 | Framework, SDK `.csproj`, NuGet/CVE, config, EF Core (A), Worker (B), `azd init` + Docker plan (C) |
| **Checkpoint 1** + rebase | 12:30–13:15 | Report, rebase on `main`, resolve shared-`Data` conflicts |
| **Sprint 2 — Azure migration** | 13:15–15:15 | Managed Identity, App Insights, Blob, Bicep, Dockerfiles, CI/CD |
| **Checkpoint 2** + integrate | 15:15–15:30 | Merge tracks → `integration`, build green |
| Deploy | 15:30–16:15 | `azd up`, smoke test |
| Hardening | 16:15–16:45 | Fix smoke-test gaps, `integration` → `main` |
| Demo + retro | 16:45–17:15 | Per-track demo, `learnings.md` |

## 14. Demo Acceptance Checklist

The demo passes when all of the following are true:

- [ ] `azd up` provisions the full stack and completes without failure.
- [ ] Web app is reachable at its Container Apps URL.
- [ ] Login works (cookie auth, PBKDF2 — no SHA1).
- [ ] Claims list renders 50 recent claims from EF Core.
- [ ] Unscored claims are scored via the minimal API (`POST /claims/{id}/score`), rules
      unchanged (0–1000).
- [ ] Document upload lands in Blob `claim-docs` (no `C:\` paths).
- [ ] Worker export writes CSV to Blob `claim-exports` within one interval; `ExportLog`
      row inserted.
- [ ] Traces/requests visible in Application Insights.
- [ ] No secrets in `appsettings` or source (Managed Identity / Key Vault only).
- [ ] `dotnet list package --vulnerable` → no High/Critical.
- [ ] Team demo covers all three tracks in ≤20 min; stretch goals logged as follow-up issues.
- [ ] `docs/learnings.md` has ≥6 prompt/response lessons.

## 15. Open Decisions for Tomorrow Morning

Decide these before tracks diverge (grounded in wiki `Pendiente/Unknown` + brief §12):

1. **Scoring deployment:** separate Container App (minimal API) or in-process module inside
   Web? (Affects Dockerfile count and `azure.yaml`.)
2. **Test project:** none exists — add a token test so CI's `dotnet test` gate is meaningful?
3. **Latent legacy gaps:** fix now or defer — no logout, `Role` not enforced,
   `DocumentPath` not persisted, `ExportLog` not written (E6-S2 already covers the latter).
4. **`SearchByClaimant`:** delete the dead SQL-injection code, or keep + parameterize for
   the rubric point (E2-S3)?
5. **Auth scope:** cookie auth for the day, or attempt the Entra ID stretch (E13-S3)?
6. **Resource group / SQL AAD admin group:** who provisions the shared RG and the AAD admin
   group Bicep needs?
7. **`azd` root:** `src/ContosoInsurance/` or repo root for `azure.yaml`?
8. **Shared `Data/` entity shapes:** confirm `DbSet` shapes together before diverging.

---

> **Reminder:** the LLM Wiki is the source of truth. If reality diverges during execution,
> update the wiki and this PRD, then log the change in `wiki/log.md`.
