# Modernization Hackathon Brief — ContosoInsurance

> **Audience:** Legacy Breakers (4-person team; reference model: 3 pairs / 6 — see §10).
> **Source of truth:** the LLM Wiki (`wiki/index.md` and linked pages) plus
> `docs/` (agenda, task-briefs, rubric, reference-solution).
> **Companions:** [plan.md](./plan.md) · [backlog.md](./backlog.md).
> Author: Mary (Business Analyst) · 2026-07-13.

---

## 1. Mission / North Star

Take `ContosoInsurance` — a deliberately-legacy .NET Framework 4.6.1 claims app —
from **Windows/IIS legacy** to **.NET 9 running on Azure Container Apps**, in one
day, driven by **GitHub Copilot app modernization for .NET**, the **Modernization
CLI (appcat)**, and **`azd`**.

> **Success signal:** a new hire can clone `main`, run `azd up`, and have a
> working modernized system in the shared resource group within ~15 minutes.

---

## 2. Current-State Summary

| Area | Current state | Evidence (wiki) |
|---|---|---|
| Framework | .NET Framework 4.6.1 (all 5 projects) | [[overview]] |
| Project style | legacy `.csproj` + `packages.config` | [[arch-current-state]] |
| Frontend | ASP.NET WebForms (`.aspx` + code-behind) | [[arch-webforms]] |
| Scoring service | WCF SOAP (`basicHttpBinding`) | [[arch-wcf-service]] |
| Worker | Windows Service (`ServiceBase` + `System.Timers.Timer`) | [[arch-worker]] |
| Data access | raw ADO.NET (sync), SQL Server | [[arch-database]] |
| Config | `Web.config` / `App.config` via `ConfigurationManager` | [[arch-configuration]] |
| Auth | Forms Auth + SHA1+salt hashing (no logout, no role enforcement) | [[arch-security]] |
| Logging | log4net 2.0.8 + `Trace` | [[arch-logging]] |
| Storage | local `C:\ClaimsFiles`, `C:\Exports`, `C:\` logs | [[arch-worker]], [[concept-document-upload]] |
| Cloud | no Docker / IaC / CI-CD | [[arch-infrastructure]] |

**Baseline build reality** (from `wiki/runbooks/build-and-test.md`):
- NuGet restore **succeeds**; the solution **does not build as-is**.
- Environment prerequisites only (vanish at `net9.0`): missing .NET Fx 4.6.1
  targeting pack (MSB3644), CLI web-targets path (MSB4226).
- **Confirmed code-level gap:** `ContosoInsurance.Web` uses
  `ContosoInsurance.Services.IClaimScoringService` with **no** project/assembly
  reference → `Default.aspx.cs(7,24): error CS0234`. Only the Web project fails;
  Common, Data, Services, Worker compile.

---

## 3. Target-State Summary

| Concern | Before | After |
|---|---|---|
| Framework | .NET Fx 4.6.1 | .NET 9 |
| Projects | `packages.config` + legacy `.csproj` | SDK-style + `PackageReference` |
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

---

## 4. Main User/System Flows to Preserve

Behavior must survive the rewrite (grounded in [[overview]]):

| Flow | What it does today | Preserve as |
|---|---|---|
| **Login** | Verify SHA1+salt hash → Forms Auth cookie | Cookie auth login; PBKDF2 hash |
| **View + auto-score** | List 50 recent claims; score unscored via WCF on page load | Razor Page lists claims; scores via `HttpClient` → minimal API |
| **Upload document** | Save file to `C:\ClaimsFiles\{claimId}\{filename}` | Upload to Blob `claim-docs` |
| **Nightly export** | Timer writes CSV of ≤1000 claims to `C:\Exports` | `BackgroundService` writes CSV to Blob `claim-exports` |
| **Scoring rules** | Deterministic 0–1000 rules (base 500 ± amount/status/age) | Identical rules behind the minimal API |

> **Latent issues to fix, not replicate** ([[arch-security]]): SQL injection in
> `SearchByClaimant` (dead code — safe to fix or delete), path traversal on
> upload, SHA1 hashing, plaintext secrets. Also known gaps you may leave as-is
> unless time allows: no logout flow, `Role` never enforced, `DocumentPath`
> never persisted, `ExportLog` never written.

---

## 5. Success Criteria

**Checkpoint 1 — end of Upgrade sprint:**
- All projects target `net9.0`, SDK-style; `packages.config` gone.
- `dotnet list package --vulnerable` → no High/Critical.
- `appsettings.json` scaffolded for Web, Services, Worker.
- `ContosoDbContext` compiles; at least `GetRecent` on EF Core.
- Platform has `azure.yaml`, `infra/` skeleton, Docker plan.

**Checkpoint 2 — pre-deploy:**
- Branches merged to `integration`; solution builds.
- Managed Identity path in place (no SQL password in config).
- Blob wired for uploads + exports; App Insights CS flows via config.
- Bicep provisions the full stack; Dockerfiles present; Actions workflow drafted.

**Post-deploy smoke test:**
- Web reachable; login works; claims list renders.
- Upload → file in `claim-docs`; worker export → `claim-exports` within one interval.
- Traces/requests visible in App Insights; no secrets in `appsettings`.

---

## 6. Rubric Priorities

Total 100 pts; **aim ≥ 70**. Optimize highest-weight, lowest-risk first.

| Rubric section | Pts | Priority |
|---|---|---|
| Framework & package modernization | 15 | **P0 — do first, unblocks everything** |
| Configuration & data access (EF Core) | 15 | **P0** |
| Hosting & runtime (Kestrel + Worker + containers) | 15 | **P0** |
| Storage & I/O (Blob) | 10 | P0 |
| Identity & secrets (Managed Identity + KV) | 10 | P0 |
| Observability (`ILogger` + App Insights) | 10 | P0 |
| Infrastructure as code (Bicep + `azd`) | 10 | P0 |
| CI/CD (GitHub Actions, OIDC) | 5 | P1 |
| Team artifacts & demo | 10 | P1 (cheap points — do throughout) |
| **Bonus** (Blazor, CoreWCF/gRPC, Entra ID, probes, OTel, cost) | +15 max | P2 — only if core is green |

---

## 7. Anti-Goals / Deductions to Avoid

| Deduction | Rule |
|---|---|
| **−5** | Any committed secret (password, credentialed connection string, storage key) |
| **−5** | `azd up` fails at demo time |
| **−3** | Any Copilot-generated diff accepted without review |
| **−3** | Any part still requires `C:\` paths to work |

Guardrails already in place: `.gitignore` blocks `appsettings.Development/Local.json`,
`*.pfx`, `*.key`, and the BMAD tooling folders. Keep all credentials in Managed
Identity / Key Vault — never in source.

---

## 8. Main Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Shared `Data/` DbContext conflicts between tracks | High | Agree entity/`DbSet` shapes at Checkpoint 1; single owner merges |
| First `azd up` fails (expected) | High | Time-boxed deploy block; diagnose with Copilot + `azd` logs |
| Web→Services `CS0234` lingers | Med | Resolved by design: replace WCF client with `HttpClient` → minimal API (backlog E4-S2) |
| Managed Identity / SQL AAD setup friction | Med | Prep `Authentication=Active Directory Default`; `DefaultAzureCredential` locally |
| Scope creep into stretch goals | Med | Log as follow-up issues; core checklist first |
| Secret accidentally committed | Low/High-impact | `.gitignore` + review discipline; MI/KV only |

---

## 9. Recommended Implementation Strategy

1. **Copilot-first.** Run the appmod **Assess** on each scope, review the plan,
   then **Apply** tasks one at a time — review every diff before accepting.
2. **Two sprints:** *Upgrade* (framework → config → data → hosting) then *Azure
   migration* (Managed Identity → Blob → App Insights → Bicep → CI/CD). Do not
   start Azure work before the code compiles on .NET 9.
3. **Critical path:** E1 (framework/packages) → E2 (config + EF Core) → E3/E4
   (Web + scoring API) ∥ E5 (Worker) ∥ E9/E10 (Docker + `azd init`). See
   [backlog.md](./backlog.md).
4. **Small commits**, one task each, referencing the backlog ID (e.g. `E2-S2`).
5. **Sync at every checkpoint;** rebase on `main`, resolve shared `Data/` first.
6. **Capture learnings** in `docs/learnings.md` as you go (≥6 prompt lessons = rubric points).

---

## 10. Suggested Team Focus

The execution model is a **4-person team** collapsed into three focus areas
(Platform runs solo; the pair sits where the work is densest). The reference
model in `docs/task-briefs.md` is 3 pairs / 6 people:

| Focus | People | Owns | Branch |
|---|---|---|---|
| **Web + API** | 2 | `Web/` (WebForms→Razor), `Services/` (WCF→minimal API), `Data/` DbContext, `Common/` `ILogger` | `track/a-web-api` |
| **Worker + Storage** | 1 | `Worker/` (→`BackgroundService`), Blob wiring for uploads + exports | `track/b-worker-storage` |
| **Platform** | 1 | Dockerfiles, Bicep, `azd`, Managed Identity, Key Vault, App Insights, GitHub Actions | `track/c-platform` |

> `Common` and `Data` are shared — coordinate at every checkpoint. If you have
> the full 6, split back into three pairs per `docs/task-briefs.md`.

---

## 11. First Actions Tomorrow Morning

1. **Tool check:** `dotnet --version` (9.x), `azd version`, `az account show`,
   `docker version`, appmod extensions installed.
2. **Reinstall BMAD if needed:** `npx bmad-method install` (tooling is gitignored).
3. **Cut branches:** `track/a-web-api`, `track/b-worker-storage`, `track/c-platform`.
4. **Run Assess** (appmod) on each track's scope; skim the generated plan.
5. **Web+API:** start E1 (SDK `.csproj` + `net9.0`) → E2 (`ContosoDbContext`).
6. **Worker:** start E5 (`ServiceBase` → `BackgroundService`).
7. **Platform:** `azd init` (existing app) → draft Dockerfiles + `infra/` skeleton.
8. Confirm the **shared `Data/` entity shapes** together before diverging.

---

## 12. Open Questions for the Team

Grounded in wiki `Pendiente/Unknown` items — decide these early:

1. **Scoring service:** separate Container App (minimal API) or an in-process
   module inside Web? (Affects Dockerfile count and `azure.yaml`.)
2. **Test project:** none exists — do we add a token test so CI's `dotnet test`
   gate is meaningful?
3. **Latent legacy gaps:** fix now or leave as follow-ups? — no logout, `Role`
   never enforced, `DocumentPath` never persisted, `ExportLog` never written.
4. **`SearchByClaimant`:** it's dead code with a SQL-injection flaw — delete it
   or keep + parameterize for the rubric point?
5. **Auth scope:** cookie auth for the day, or attempt Entra ID stretch?
6. **Resource group / SQL AAD admin group:** who provisions the shared RG and the
   AAD admin group Bicep needs?
7. **`azd` root:** `src/ContosoInsurance/` or repo root for `azure.yaml`?
