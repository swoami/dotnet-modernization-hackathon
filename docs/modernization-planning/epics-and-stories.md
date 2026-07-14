---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - wiki/index.md
  - docs/modernization-planning/hackathon-brief.md
  - docs/modernization-planning/plan.md
  - docs/modernization-planning/backlog.md
  - docs/modernization-planning/prd.md
---

# ContosoInsurance Modernization — Epics & Stories

> **Type:** Execution-focused epics & stories for a **1-day** hackathon, 4-person team.
> **Source of truth:** the PRD ([prd.md](./prd.md)) and the LLM Wiki (`wiki/index.md`).
> **ID scheme:** reuses the backlog `E#` / `E#-S#` IDs ([backlog.md](./backlog.md)) so
> commit messages stay consistent (e.g. `E2-S2: migrate ClaimsRepository to EF Core`).
> New day-zero stories are numbered `E0-S#`.
> Author: John (Product Manager) · 2026-07-13.

---

## P0 Hackathon Execution Slice

> The minimum path to a deployed system and ≥70/100. **15 slice items covering the core P0
> stories** (grouped where they always ship together); the full 42-story backlog below remains
> the traceable source. Lanes **A** (Web+API pair), **B** (Worker+Storage dev), **C** (Platform
> engineer) run **in parallel** from item 2 onward; items inside a lane run in the order shown.

| # | Stories | Focus | Lane / owner | Runs in parallel with | Rubric pts protected | Demo dependency |
|---|---|---|---|---|---|---|
| 1 | E0-S3 | Morning decisions (topology, azd root, shared `Data/` shapes, auth scope) | Whole team | — (blocks divergence) | Protects all 100 | Everything downstream |
| 2 | E1-S1 (+E1-S2) | SDK-style `.csproj` + `net9.0` + `PackageReference` | A+B split by project | 12 (azd init can start) | §1 (9) | Nothing builds without it |
| 3 | E1-S3 | Drop `log4net`; `Newtonsoft.Json` → `System.Text.Json` | A | 10, 12 | §1 (6) | `--vulnerable` check clean |
| 4 | E2-S1 + E2-S2 | `ContosoDbContext` + `ClaimsRepository` on EF Core | A | 10, 11, 12 | §2 (10) | Claims list renders |
| 5 | E2-S4 | `appsettings.json` + typed options (Web/Services) | A | 10, 11, 12 | §2 (8) | Config chain to cloud |
| 6 | E3-S1 + E3-S2 | `Program.cs` on Kestrel + Index page (claims list) | A | 10, 11, 12 | §3 (part of 15) | Web reachable; list renders |
| 7 | E3-S3 | Login page — cookie auth + PBKDF2 (SHA1 gone) | A | 10, 11, 12 | §3, §5 | Login works |
| 8 | E4-S1 | Minimal API `POST /claims/{id}/score`, rules identical | A | 10, 11, 12 | §3 | Auto-scoring works |
| 9 | E4-S2 | WCF client → `HttpClient` — **eliminates CS0234 by design** | A | 10, 11, 12 | §3 | Scoring from the page |
| 10 | E5-S1 + E5-S2 | Worker → `BackgroundService` + `PeriodicTimer` + `ExportOptions` | B | 4–9, 12 | §3 (5) | Export runs in-cloud |
| 11 | E6-S1 → E6-S2 + E6-S3 (+E3-S4) | `IClaimDocumentStore` (interface **early** — A needs it), export → `claim-exports` + `ExportLog`, upload → `claim-docs` | B (+A for Upload page) | 4–9, 12–13 | §4 (10) | Upload + export land in Blob |
| 12 | E9-S1/S3 (+S2 per topology) + E10-S1 | Dockerfiles (non-root) + `azd init` (`azure.yaml`, `infra/` skeleton) | C | 2–11 | §3 (5), §7 | `azd up` has something to build |
| 13 | E10-S2 → E10-S4 (+E10-S3) | Bicep full stack + env-var wiring (params/roles in IaC) | C | 4–11 | §7 (10) | `azd up` succeeds (−5 if not) |
| 14 | E7-S1 + E8-S1/S2 | SQL via Managed Identity (no passwords) + `ILogger<T>` + App Insights | A+B app-side, C infra | Within Sprint 2 | §5 (10), §6 (10) | No secrets; traces visible |
| 15 | E12-S4 | Post-deploy smoke test vs. PRD §14 checklist + harden | Whole team | — (convergence) | Defends ≥70 and the −5 | The demo itself |

**Parallelism in one line:** after the morning decisions (1), lane A runs 2→9 sequentially, lane B runs 2→10→11, lane C runs 12→13 — three lanes fully parallel until Sprint 2 convergence at 14, then everyone converges on 15.

### Time-Boxed Day Plan

| Timebox | Focus | Expected output |
|---|---|---|
| Morning (09:00–12:30) | Baseline + appmod Assess + architecture decisions (E0), then Sprint 1 starts: E1 → E2 (A), E5 (B), E9/E10-S1 (C) | Shared direction: decisions recorded, branches cut, all projects on `net9.0` |
| Midday (12:30–15:15) | Core modernization tracks: E3/E4 (A) ∥ E6 (B) ∥ E10-S2 (C); Checkpoint 1 rebase resolves shared `Data/` | Buildable modernized slices: Razor Pages + minimal API + Worker compile; Blob wired |
| Afternoon (15:15–16:15) | Azure/deployment integration: E7 + E8 + E10-S4, merge to `integration`, `azd up` | `azd up` path: full stack provisioned, apps running on Container Apps |
| Final hour (16:15–17:15) | Smoke test (E12-S4) + hardening + learnings (E12-S1) + demo prep (E12-S3) | Demo-ready system: PRD §14 checklist green, ≥6 learnings, ≤20-min demo rehearsed |

---

## Requirements Inventory

Derived from PRD §5 (scope), §9 (flows that must survive), §10 (constraints), §14 (demo checklist).

### Functional Requirements

- **FR1:** Login works with ASP.NET Core cookie auth and PBKDF2 password hashing (SHA1 gone).
- **FR2:** Claims list renders 50 recent claims from EF Core (`ContosoDbContext`).
- **FR3:** Unscored claims are scored on page load via `HttpClient`/`IHttpClientFactory` → minimal API `POST /claims/{id}/score`.
- **FR4:** Scoring rules are preserved identically — deterministic 0–1000 (base 500 ± amount/status/age); `-1`/404 when claim missing.
- **FR5:** Document upload lands in Blob container `claim-docs` (no `C:\` paths anywhere).
- **FR6:** Worker export writes CSV (≤1000 claims) to Blob `claim-exports` within one interval and inserts an `ExportLog` row.
- **FR7:** `/health` endpoint returns Healthy when DbContext + Blob container are reachable.

### Non-Functional Requirements

- **NFR1:** All 5 projects target `net9.0`, SDK-style `.csproj`; no `packages.config` remains.
- **NFR2:** `dotnet list package --vulnerable` reports zero High/Critical (drop `log4net`; `Newtonsoft.Json` → `System.Text.Json`).
- **NFR3:** No secrets in source or config — Managed Identity + Key Vault only (**−5 deduction** if violated).
- **NFR4:** `ILogger<T>` + Application Insights throughout; traces/requests visible after the smoke test.
- **NFR5:** Each service builds and runs as a non-root container.
- **NFR6:** One `azd up` provisions the full stack from Bicep (~15-min new-hire clone-to-running experience).
- **NFR7:** Fix, don't replicate: SQL injection in `SearchByClaimant`, path traversal on upload, SHA1 hashing, plaintext secrets.
- **NFR8:** Every Copilot/agent diff is reviewed before accept (**−3**); commits reference backlog IDs.

### Baseline Issues — Environment Prerequisites vs. Real Code Defects

Per the verified baseline ([[build-and-test]], PRD §1/§10) these **must not be conflated**:

| Category | Issue | Resolution path |
|---|---|---|
| **Environment prerequisite** (not a code defect) | MSB3644 — .NET Fx 4.6.1 targeting pack missing | Vanishes when projects retarget `net9.0` (E1-S1). No fix work needed. |
| **Environment prerequisite** (not a code defect) | MSB4226 — CLI resolves `Microsoft.WebApplication.targets` path wrong | Vanishes at `net9.0` (E1-S1); doesn't occur inside Visual Studio. |
| **Real code defect** (confirmed baseline break) | **Web → Services CS0234** — `Default.aspx.cs(7,24)` uses `ContosoInsurance.Services.IClaimScoringService` with no project/assembly reference. Only Web fails; Common, Data, Services, Worker compile. | **Eliminated by design, not repaired:** E4-S2 replaces the WCF `ChannelFactory` client with `HttpClient` → minimal API. Do **not** add a WCF project reference. |

### Requirements Coverage Map

| Requirement | Covered by |
|---|---|
| FR1 | E3-S1, E3-S3 |
| FR2 | E2-S1, E2-S2, E3-S2 |
| FR3 | E4-S1, E4-S2 |
| FR4 | E4-S1 |
| FR5 | E6-S1, E6-S3, E3-S4 |
| FR6 | E5-S1, E5-S2, E6-S2 |
| FR7 | E8-S3 |
| NFR1 | E1-S1, E1-S2 |
| NFR2 | E1-S3 |
| NFR3 | E7-S1, E7-S2, E7-S3, E10-S3 |
| NFR4 | E8-S1, E8-S2 |
| NFR5 | E9-S1, E9-S2, E9-S3 |
| NFR6 | E10-S1, E10-S2, E10-S3, E10-S4 |
| NFR7 | E2-S3, E3-S3, E3-S4, E7-S3 |
| NFR8 | E12-S1, E12-S2 |
| CS0234 baseline defect | E4-S2 (by design) |

---

## Reading This Document

- **Priority:** **P0** = required for the ≥70/100 path (P0 workstreams carry **85 rubric pts**; you need nearly all of them green). **P1** = important, do if on track. **P2** = stretch only after core is green.
- **Owner profiles** map to the 4-person model (PRD §12): **Web+API pair** (2 people, `track/a-web-api`) · **Worker+Storage dev** (1, `track/b-worker-storage`) · **Platform engineer** (1, `track/c-platform`) · **Whole team**.
- **Sprint 1 (Upgrade)** = code compiles on .NET 9 first; **Sprint 2 (Azure migration)** = cloud wiring. Do not start Sprint 2 work before Sprint 1 compiles (PRD §10).
- Every story is sized to be executed or validated within the hackathon day; commit one story per commit, ID in the message.

### Epic Index by Workstream

| Workstream | Epics | Sprint | Priority |
|---|---|---|---|
| 1. Baseline / Buildability | E0 (day-zero), E1 (framework/packages) | 0–1 | P0 |
| 2. Web + API + Data | E2 (config/EF Core), E3 (Razor Pages), E4 (minimal API) | 1 | P0 |
| 3. Worker + Storage | E5 (BackgroundService), E6 (Blob) | 1–2 | P0 |
| 4. Platform + Deployment | E9 (containers), E10 (Bicep/azd), E11 (CI/CD) | 1–2 | P0 (E11 = P1) |
| 5. Security + Observability | E7 (MI + Key Vault), E8 (ILogger + App Insights) | 2 | P0 |
| 6. Demo Readiness + Learnings | E12 (artifacts, smoke test, demo) | all day | P1 (E12-S4 = P0) |

---

## Workstream 1 — Baseline / Buildability

### Epic E0 — Day-Zero Baseline & Setup

- **Epic goal:** Everyone starts from a verified, shared understanding of the baseline; tracks diverge only after tools work, branches exist, and the eight open decisions (PRD §15) are made.
- **Scope:** Tool checks, branch cuts, appmod Assess runs, morning decisions. No application code changes.
- **Dependencies:** None — this is the front door for everything else.
- **Rubric impact:** No direct points, but protects all 100: prevents the shared-`Data/` conflict risk (High likelihood, PRD §11) and the −3 unreviewed-diff deduction.
- **Main risks:** Skipping decisions to "save time" and paying for it at Checkpoint 1 merge; a missing tool (azd/docker/appmod) discovered mid-sprint.

#### E0-S1 — Tool check & environment readiness

- **Goal:** Every machine can build, containerize, and deploy before the first sprint starts.
- **Acceptance criteria:**
  - [ ] `dotnet --version` reports 9.x on all 4 machines.
  - [ ] `azd version`, `az account show`, `docker version` all succeed.
  - [ ] GitHub Copilot app modernization extension + Modernization CLI (appcat) installed where used.
  - [ ] BMAD tooling reinstalled where needed (`npx bmad-method install` — it's gitignored).
- **Suggested owner profile:** Whole team (each member self-verifies; Platform engineer sweeps).
- **Dependencies:** None.
- **Rubric impact:** None directly; unblocks everything.
- **Priority:** **P0**

#### E0-S2 — Cut track branches & confirm baseline build facts

- **Goal:** Three track branches exist and the team agrees on what is broken vs. what is merely missing from the environment.
- **Acceptance criteria:**
  - [ ] Branches `track/a-web-api`, `track/b-worker-storage`, `track/c-platform` cut from `main`.
  - [ ] Team explicitly acknowledges: MSB3644 + MSB4226 are **environment prerequisites** (vanish at `net9.0`, no fix work); **CS0234 Web→Services is the one real code defect**, to be eliminated by E4-S2's design — nobody spends time adding a WCF reference or installing the 4.6.1 targeting pack.
  - [ ] `msbuild -t:Restore -p:RestorePackagesConfig=true` confirmed green (restore already works).
- **Suggested owner profile:** Platform engineer (branches) + whole team (acknowledgment).
- **Dependencies:** E0-S1.
- **Rubric impact:** None directly; avoids wasted P0 time.
- **Priority:** **P0**

#### E0-S3 — Resolve the eight morning decisions

- **Goal:** The open decisions in PRD §15 are answered before tracks diverge, so no track blocks on another mid-sprint.
- **Acceptance criteria:**
  - [ ] Decided and written down (in this doc or `wiki/log.md`): (1) scoring as separate Container App vs. in-process module; (2) add a token test project or not; (3) latent legacy gaps fix-now vs. defer (logout, `Role`, `DocumentPath`, `ExportLog` — E6-S2/E6-S3 already cover the latter two paths); (4) `SearchByClaimant` delete vs. parameterize; (5) cookie auth vs. Entra ID stretch; (6) who provisions the shared RG + SQL AAD admin group; (7) `azure.yaml` root; (8) shared `Data/` entity/`DbSet` shapes agreed.
- **Suggested owner profile:** Whole team, 15 minutes, timeboxed; PM/lead records outcomes.
- **Dependencies:** None (run during kickoff).
- **Rubric impact:** Indirect — decision (1) changes Dockerfile count (E9-S2) and `azure.yaml` (E10-S1); decision (8) defuses the highest-likelihood risk in the PRD.
- **Priority:** **P0**

#### E0-S4 — Run appmod Assess per track

- **Goal:** Each track has a Copilot appmod assessment plan to drive its Apply tasks (Copilot-first ground rule).
- **Acceptance criteria:**
  - [ ] Assess run on each track's scope; generated plans skimmed by the track owner.
  - [ ] Each track knows which of its stories appmod can Apply vs. which need Copilot Chat/manual work.
- **Suggested owner profile:** Each track owner for their own scope.
- **Dependencies:** E0-S1, E0-S2.
- **Rubric impact:** §9 team artifacts (feeds `learnings.md`); protects the −3 unreviewed-diff deduction discipline.
- **Priority:** **P0**

### Epic E1 — Framework & Package Modernization

- **Epic goal:** All five projects compile on `net9.0` with SDK-style projects and zero High/Critical package vulnerabilities — the single biggest unblock of the day.
- **Scope:** `.csproj` conversion, `packages.config` → `PackageReference`, drop `log4net`, `Newtonsoft.Json` → `System.Text.Json`. Tracks A+B share this across their projects.
- **Dependencies:** E0. Note: after retargeting, `Web` **still won't compile** until the CS0234 gap is eliminated (E4-S2) and WebForms pages are replaced (E3) — expected and fine; other projects go green first.
- **Rubric impact:** §1 Framework & package modernization — **15 pts**, highest-weight/lowest-risk section; PRD says "do first, unblocks everything."
- **Main risks:** Time sunk trying to make legacy WebForms `.aspx` compile on .NET 9 (don't — E3 replaces them); an EF Core dependency silently pulling Newtonsoft back in.

#### E1-S1 — Convert all five `.csproj` to SDK-style targeting `net9.0`

- **Goal:** Legacy project files are gone; `dotnet build` resolves the solution on .NET 9 (this alone makes MSB3644/MSB4226 disappear).
- **Acceptance criteria:**
  - [ ] Every project (Common, Data, Services, Web, Worker) is SDK-style.
  - [ ] No `TargetFrameworkVersion` v4.6.1 remains anywhere.
  - [ ] `dotnet build` resolves; Common, Data, Services, Worker compile (Web pending E3/E4-S2 — tracked, not a surprise).
- **Suggested owner profile:** Web+API pair (Web/Services/Data/Common) + Worker+Storage dev (Worker), via appmod Apply.
- **Dependencies:** E0-S2, E0-S4.
- **Rubric impact:** §1 (5 pts).
- **Priority:** **P0**

#### E1-S2 — Migrate `packages.config` → `PackageReference`

- **Goal:** Package management is declarative in the `.csproj`, ready for `dotnet` CLI tooling.
- **Acceptance criteria:**
  - [ ] No `packages.config` anywhere in the repo.
  - [ ] All packages listed as `PackageReference` in their `.csproj`.
- **Suggested owner profile:** Same owners as E1-S1 (usually falls out of the SDK conversion).
- **Dependencies:** E1-S1.
- **Rubric impact:** §1 (4 pts).
- **Priority:** **P0**

#### E1-S3 — Remove/replace vulnerable packages

- **Goal:** Zero High/Critical CVEs: `log4net` 2.0.8 dropped (superseded by `ILogger<T>`, E8-S1), `Newtonsoft.Json` 11.0.2 → `System.Text.Json`.
- **Acceptance criteria:**
  - [ ] `dotnet list package --vulnerable` → zero High/Critical.
  - [ ] No direct `log4net` or `Newtonsoft.Json` references (keep Newtonsoft only if EF tooling genuinely requires it — document if so).
- **Suggested owner profile:** Web+API pair (owns `Common/`, where both packages live).
- **Dependencies:** E1-S1, E1-S2.
- **Rubric impact:** §1 (3+3 pts).
- **Priority:** **P0**

---

## Workstream 2 — Web + API + Data

### Epic E2 — Configuration & Data Access (EF Core)

- **Epic goal:** Raw sync ADO.NET and `ConfigurationManager` are replaced with EF Core 9 (`ContosoDbContext`) and `appsettings.json` + typed options, preserving repository signatures so callers don't churn.
- **Scope:** `Data/` (shared with Track B!) and config plumbing for Web, Services, Worker.
- **Dependencies:** E1 (must compile on `net9.0` first). `DbSet`/entity shapes agreed in E0-S3(8) before anyone codes.
- **Rubric impact:** §2 Configuration & data access — **15 pts**.
- **Main risks:** **Highest-likelihood risk of the day:** Tracks A and B both touch `Data/` — mitigate with the agreed shapes + single owner merging at checkpoints. Schema mismatch between EF mappings and `db/001-schema.sql`.

#### E2-S1 — Introduce `ContosoDbContext` (EF Core 9)

- **Goal:** One DbContext with `DbSet<Claim>`, `DbSet<Policy>`, `DbSet<User>`, `DbSet<ExportLog>` mapped to the existing schema.
- **Acceptance criteria:**
  - [ ] Context compiles; entities map to the existing 4-table schema (`db/001-schema.sql`) — no schema changes.
  - [ ] Shapes match what was agreed in E0-S3; Track B consumes the same context.
- **Suggested owner profile:** Web+API pair (one person owns `Data/` merges).
- **Dependencies:** E1-S1, E0-S3(8).
- **Rubric impact:** §2 (5 pts).
- **Priority:** **P0**

#### E2-S2 — Migrate `ClaimsRepository` to EF Core, preserving signatures

- **Goal:** `GetRecent`, `GetById`, `UpdateScore` run on EF Core; callers unchanged.
- **Acceptance criteria:**
  - [ ] Public method signatures preserved; callers compile without edits.
  - [ ] ADO.NET removed from `Data/`.
  - [ ] `GetRecent` returns the 50 recent claims (feeds FR2).
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E2-S1.
- **Rubric impact:** §2 (5 pts).
- **Priority:** **P0**

#### E2-S3 — Fix or delete `SearchByClaimant` (SQL injection)

- **Goal:** The string-concatenated SQL injection flaw is gone — either parameterized LINQ/EF or deleted outright (it's confirmed dead code, no caller).
- **Acceptance criteria:**
  - [ ] No string-concatenated SQL remains in `Data/`.
  - [ ] The chosen path (E0-S3 decision 4) is noted in the commit message + `wiki/log.md`.
- **Suggested owner profile:** Web+API pair (5-minute story either way).
- **Dependencies:** E2-S2, E0-S3(4).
- **Rubric impact:** §2 (2 pts) if parameterized; hygiene either way (NFR7).
- **Priority:** **P1** — decision-gated; cheap points, don't let it block P0 flow.

#### E2-S4 — `Web.config`/`App.config` → `appsettings.json` + typed options

- **Goal:** All configuration flows through `IConfiguration` with typed options bound in `Program.cs`; legacy config files gone.
- **Acceptance criteria:**
  - [ ] No `Web.config`/`App.config` remain (Worker's `App.config` handled by E5-S2 — coordinate).
  - [ ] `appsettings.json` scaffolded for Web, Services, Worker; options bound in each `Program.cs`.
  - [ ] No secrets in any `appsettings` (NFR3 — connection info is server-only + Managed Identity, see E7-S1).
- **Suggested owner profile:** Web+API pair (Web/Services) — Worker's half belongs to E5-S2.
- **Dependencies:** E1-S1.
- **Rubric impact:** §2 (4+4 pts).
- **Priority:** **P0**

### Epic E3 — Web Tier: WebForms → ASP.NET Core Razor Pages

- **Epic goal:** The WebForms portal is replaced by Razor Pages on Kestrel: login (cookie auth + PBKDF2), claims list, upload — the three flows that must survive (FR1, FR2, FR5).
- **Scope:** New ASP.NET Core `Web` project surface: `Program.cs`, `Pages/Index`, `Pages/Login`, `Pages/Upload`. Fix-don't-replicate: SHA1 → PBKDF2, path traversal on upload.
- **Dependencies:** E1, E2. Upload's storage backend depends on E6-S1 abstraction (page can be built against the interface first).
- **Rubric impact:** §3 Hosting & runtime — part of **15 pts** (with E5, E9); §5 for the SHA1 replacement.
- **Main risks:** WebForms conversion is the densest work of Sprint 1 — that's why the pair sits here; scope creep into Blazor (P2 stretch, log it and move on).

#### E3-S1 — `Program.cs` composition root

- **Goal:** The web app boots on Kestrel with DI, config, logging, cookie auth, health checks, and EF Core wired.
- **Acceptance criteria:**
  - [ ] App boots on Kestrel; DI resolves `ClaimsService`, `UserService`, `ContosoDbContext`.
  - [ ] Cookie auth + health-check plumbing registered (endpoints land in E3-S3/E8-S3).
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E1-S1, E2-S1, E2-S4.
- **Rubric impact:** §3 (5 pts).
- **Priority:** **P0**

#### E3-S2 — `Default.aspx` → `Pages/Index.cshtml(.cs)` (claims list)

- **Goal:** The claims list renders 50 recent claims from EF Core (FR2), with unscored claims auto-scored via the new API client (hook for E4-S2).
- **Acceptance criteria:**
  - [ ] Claims list renders from EF Core (`GetRecent`).
  - [ ] Page structure ready for the E4-S2 scoring call (no `System.ServiceModel` anywhere).
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E3-S1, E2-S2.
- **Rubric impact:** §3.
- **Priority:** **P0**

#### E3-S3 — `Login.aspx` → `Pages/Login.cshtml(.cs)` with cookie auth + PBKDF2

- **Goal:** Login issues an ASP.NET Core auth cookie; SHA1+salt hashing is replaced with PBKDF2 (FR1, NFR7).
- **Acceptance criteria:**
  - [ ] Login issues auth cookie; protected pages require it.
  - [ ] SHA1 hashing gone; PBKDF2 in place (seed-user compatibility path decided and noted).
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E3-S1.
- **Rubric impact:** §3; §5 SHA1 replacement.
- **Priority:** **P0**

#### E3-S4 — `Upload.aspx` → `Pages/Upload.cshtml(.cs)` against storage abstraction

- **Goal:** Upload posts through `IClaimDocumentStore` (E6-S1) — no direct disk writes, no path traversal.
- **Acceptance criteria:**
  - [ ] Upload page functional against `IClaimDocumentStore` (in-memory fake acceptable until E6-S3 wires Blob).
  - [ ] File name sanitized (path traversal fixed, NFR7); no `C:\` paths.
- **Suggested owner profile:** Web+API pair (coordinates with Worker+Storage dev on the interface).
- **Dependencies:** E3-S1, E6-S1 (interface only).
- **Rubric impact:** §4.
- **Priority:** **P0**

### Epic E4 — Scoring Service: WCF → Minimal API

- **Epic goal:** The WCF SOAP scoring service becomes an ASP.NET Core minimal API with **identical** scoring rules, and the Web tier calls it over HTTP — **which eliminates the CS0234 baseline defect by design** (the broken WCF client reference is removed, not repaired).
- **Scope:** `Services/` reimplementation + Web-side client swap. Deployment topology (separate Container App vs. in-process) per E0-S3 decision 1.
- **Dependencies:** E1, E2 (score persistence via `UpdateScore`).
- **Rubric impact:** §3 Hosting & runtime; resolves the Med-likelihood "CS0234 lingers" risk from PRD §11.
- **Main risks:** Accidental behavior drift in scoring rules (PRD §6: **no changes to scoring business rules**) — diff the rule logic line-by-line at review.

#### E4-S1 — Reimplement scoring as minimal API `POST /claims/{id}/score`

- **Goal:** The deterministic 0–1000 rules (base 500 ± amount/status/age) run behind a minimal API endpoint (FR3, FR4).
- **Acceptance criteria:**
  - [ ] Endpoint returns JSON score 0–1000; scoring rules byte-for-byte equivalent in behavior.
  - [ ] `-1`/404 when claim missing.
  - [ ] Score persisted via `ClaimsRepository.UpdateScore`.
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E1-S1, E2-S2, E0-S3(1).
- **Rubric impact:** §3; Track A checklist.
- **Priority:** **P0**

#### E4-S2 — Replace the WCF client with `HttpClient` + `IHttpClientFactory` (resolves CS0234)

- **Goal:** The Web tier scores claims over HTTP instead of the WCF `ChannelFactory<IClaimScoringService>` — this **removes the one confirmed baseline code defect** without ever adding a Services reference.
- **Acceptance criteria:**
  - [ ] Web scores unscored claims via `HttpClient`/`IHttpClientFactory` → `POST /claims/{id}/score` on page load (FR3).
  - [ ] **CS0234 gap resolved**; no `System.ServiceModel` client code remains.
  - [ ] Scoring endpoint base URL comes from config (E2-S4), not hard-coded.
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E4-S1, E3-S2.
- **Rubric impact:** §3; closes the baseline-defect risk (PRD §11).
- **Priority:** **P0**

---

## Workstream 3 — Worker + Storage

### Epic E5 — Worker: Windows Service → BackgroundService

- **Epic goal:** The nightly exporter runs as a Generic Host `BackgroundService` with `PeriodicTimer` — containerizable, no Windows Service machinery.
- **Scope:** `Worker/` only (plus its `App.config` → options). Blob output is E6-S2, kept separate so this lands in Sprint 1.
- **Dependencies:** E1 (compiles on `net9.0`); shared `Data/` shapes from E2-S1.
- **Rubric impact:** §3 Hosting & runtime — part of **15 pts**; §2 for the options binding.
- **Main risks:** Only one person on Track B — if E5 slips, E6 (10 pts) slips with it; keep E5 lean and defer polish.

#### E5-S1 — `ClaimsExporterService : ServiceBase` → `BackgroundService`

- **Goal:** Worker runs on `Host.CreateApplicationBuilder`; `System.ServiceProcess` and `ProjectInstaller` deleted.
- **Acceptance criteria:**
  - [ ] Worker runs on Generic Host; no `ServiceBase` remains.
  - [ ] Export loop still selects ≤1000 claims per run (behavior preserved, FR6).
- **Suggested owner profile:** Worker+Storage dev, via appmod Apply.
- **Dependencies:** E1-S1.
- **Rubric impact:** §3 (5 pts).
- **Priority:** **P0**

#### E5-S2 — `System.Timers.Timer` → `PeriodicTimer`; `App.config` → typed `ExportOptions`

- **Goal:** Interval and export settings come from `appsettings.json`-bound options; the legacy timer and `App.config` are gone.
- **Acceptance criteria:**
  - [ ] `PeriodicTimer` drives the loop; interval read from `ExportOptions`.
  - [ ] Worker `App.config` gone (completes NFR1/E2-S4's "no legacy config" goal).
- **Suggested owner profile:** Worker+Storage dev.
- **Dependencies:** E5-S1.
- **Rubric impact:** §2, §3.
- **Priority:** **P0**

### Epic E6 — Storage: Local Files → Azure Blob

- **Epic goal:** All `C:\` file I/O is replaced by Azure Blob Storage: uploads → `claim-docs`, exports → `claim-exports` — killing the −3 "`C:\` paths" deduction and 10 rubric pts of §4.
- **Scope:** `IClaimDocumentStore` abstraction in `Common/`, Blob implementation, Worker + Web wiring. Also closes two latent legacy gaps as a side effect: `ExportLog` finally written (E6-S2) and upload actually persisted (E6-S3).
- **Dependencies:** E5 (worker shape), E3-S4 (upload page), E7-S2/E10-S2 for the real Azure containers + MI (local dev via `DefaultAzureCredential`/Azurite meanwhile).
- **Rubric impact:** §4 Storage & I/O — **10 pts**; §5 (credential-free access).
- **Main risks:** Sprint 2 story on a solo track — the interface (E6-S1) must land early so Track A isn't blocked; Blob auth friction before infra exists (mitigate: fake store until `azd up`).

#### E6-S1 — `IClaimDocumentStore` abstraction + `BlobClaimDocumentStore`

- **Goal:** One storage abstraction in `Common/` used by both Web and Worker; Blob implementation uses `BlobServiceClient` + `DefaultAzureCredential` — zero connection strings.
- **Acceptance criteria:**
  - [ ] Interface consumed by Web (E3-S4) and Worker (E6-S2).
  - [ ] In-memory fake available for local/dev/test use.
  - [ ] No connection strings anywhere (NFR3).
- **Suggested owner profile:** Worker+Storage dev (interface agreed with Web+API pair early in Sprint 1).
- **Dependencies:** E1-S1; interface shape needed by E3-S4.
- **Rubric impact:** §4, §5.
- **Priority:** **P0**

#### E6-S2 — Worker export → Blob `claim-exports` + `ExportLog` audit row

- **Goal:** `File.WriteAllText` is gone; CSV lands in `claim-exports` and — fixing a documented latent gap — an `ExportLog` row is inserted per export (FR6).
- **Acceptance criteria:**
  - [ ] CSV lands in `claim-exports` within one timer interval.
  - [ ] `ExportLog` row inserted per export (first time this table is ever written).
  - [ ] No `C:\Exports` path remains.
- **Suggested owner profile:** Worker+Storage dev.
- **Dependencies:** E5-S2, E6-S1, E2-S1 (`DbSet<ExportLog>`).
- **Rubric impact:** §4 (5 pts).
- **Priority:** **P0**

#### E6-S3 — Web upload → Blob `claim-docs`

- **Goal:** Uploaded documents land in `claim-docs` (container name from options), not disk (FR5).
- **Acceptance criteria:**
  - [ ] Uploaded file lands in `claim-docs`; no `C:\ClaimsFiles` path remains.
  - [ ] Container name from typed options (E2-S4).
- **Suggested owner profile:** Worker+Storage dev + Web+API pair (joint — page is A's, store is B's).
- **Dependencies:** E3-S4, E6-S1.
- **Rubric impact:** §4 (5 pts).
- **Priority:** **P0**

---

## Workstream 4 — Platform + Deployment

### Epic E9 — Containerization

- **Epic goal:** Every deployable service builds and runs as a non-root container locally — the precondition for Container Apps.
- **Scope:** Multi-stage Dockerfiles (SDK build → runtime) for Web, Services (unless merged in-process per E0-S3 decision 1), Worker.
- **Dependencies:** E1 (Sprint 1 draft possible immediately after SDK conversion); final images need compiling code.
- **Rubric impact:** §3 Hosting & runtime (5 pts of the 15).
- **Main risks:** Deferring Dockerfiles to Sprint 2 and discovering build-context issues at deploy time — draft in Sprint 1 per the plan.

#### E9-S1 — Multi-stage Dockerfile for Web

- **Goal:** Web builds into an `aspnet` runtime image, running as non-root.
- **Acceptance criteria:**
  - [ ] `docker build` + `docker run` serve the app locally.
  - [ ] Non-root user in the final stage.
- **Suggested owner profile:** Platform engineer (with Web+API pair for build quirks).
- **Dependencies:** E1-S1 (draftable), E3-S1 (final).
- **Rubric impact:** §3 (shared 5 pts).
- **Priority:** **P0**

#### E9-S2 — Multi-stage Dockerfile for Services

- **Goal:** The scoring API gets its own container — **skip entirely if E0-S3 decided in-process hosting inside Web**.
- **Acceptance criteria:**
  - [ ] If separate: builds + runs locally, non-root. If merged: story closed as N/A with the decision referenced.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E0-S3(1), E4-S1.
- **Rubric impact:** §3 (shared 5 pts).
- **Priority:** **P0** (or N/A by decision)

#### E9-S3 — Multi-stage Dockerfile for Worker

- **Goal:** Worker builds on the plain `runtime` base (no ASP.NET), non-root.
- **Acceptance criteria:**
  - [ ] Container runs the export loop locally (fake or real store).
  - [ ] No ASP.NET base image; non-root user.
- **Suggested owner profile:** Platform engineer (with Worker+Storage dev).
- **Dependencies:** E5-S1.
- **Rubric impact:** §3 (shared 5 pts).
- **Priority:** **P0**

### Epic E10 — Infrastructure as Code (Bicep + azd)

- **Epic goal:** One `azd up` provisions the entire stack — the north-star success signal and 10 rubric pts.
- **Scope:** `azure.yaml`, `infra/main.bicep` + modules: Log Analytics, App Insights, ACR, user-assigned MI, Container Apps env + apps, Azure SQL + DB + AAD admin, Key Vault, Storage + `claim-docs`/`claim-exports` containers; env-var wiring.
- **Dependencies:** E0-S3 decisions 1 (topology), 6 (RG/AAD admin group), 7 (azd root); E9 Dockerfiles for `azd up` to build.
- **Rubric impact:** §7 IaC — **10 pts**; enables §5 and §6 points; failing `azd up` at demo is **−5**.
- **Main risks:** "First `azd up` fails" is expected (High likelihood, PRD §11) — start `azd init` in Sprint 1, keep the deploy block timeboxed, diagnose with Copilot + azd logs.

#### E10-S1 — `azd init`: `azure.yaml` + `infra/` skeleton

- **Goal:** azd knows the services (web, scoring, worker) and where infra lives — drafted in Sprint 1 so Sprint 2 is provisioning, not scaffolding.
- **Acceptance criteria:**
  - [ ] `azure.yaml` at the root chosen in E0-S3(7); services mapped to container apps.
  - [ ] `infra/` skeleton committed.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E0-S3(1)(7).
- **Rubric impact:** §7.
- **Priority:** **P0**

#### E10-S2 — `infra/main.bicep` provisions the full stack

- **Goal:** One `azd up` creates everything the app needs (NFR6).
- **Acceptance criteria:**
  - [ ] Provisions: Log Analytics, App Insights, ACR, user-assigned MI, Container Apps env + apps, Azure SQL + DB + AAD admin, Key Vault, Storage with `claim-docs` + `claim-exports`.
  - [ ] `azd up` completes without manual portal steps.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E10-S1, E0-S3(6).
- **Rubric impact:** §7 (4 pts).
- **Priority:** **P0**

#### E10-S3 — Parameterize Bicep; all role assignments in IaC

- **Goal:** No secrets or hard-coded names in parameters; every role assignment (Blob Data Contributor, AcrPull, KV Secrets User, SQL AAD user) lives in Bicep, not the portal.
- **Acceptance criteria:**
  - [ ] No secrets in params (NFR3); names parameterized.
  - [ ] Role assignments in Bicep only.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E10-S2, E7-S2.
- **Rubric impact:** §7 (3+3 pts).
- **Priority:** **P0**

#### E10-S4 — Wire env vars into Container Apps

- **Goal:** Apps read App Insights connection string, SQL server (server-only, MI auth), and Storage account name from environment — completing the config chain from Bicep to `IConfiguration`.
- **Acceptance criteria:**
  - [ ] Container Apps receive: AI connection string, SQL server name (no password), Storage account name.
  - [ ] Apps boot in-cloud reading config from env (verified at smoke test).
- **Suggested owner profile:** Platform engineer (with both tracks for key names).
- **Dependencies:** E10-S2, E2-S4.
- **Rubric impact:** §5, §6, §7.
- **Priority:** **P0**

### Epic E11 — CI/CD (GitHub Actions, OIDC)

- **Epic goal:** Push to `main` builds, tests, and deploys via OIDC — 5 pts of polish once the core is green.
- **Scope:** `deploy.yml` workflow, `dotnet test` gate, deploy runbook doc.
- **Dependencies:** E10 working end-to-end; E0-S3 decision 2 (token test project) for the test gate to be meaningful.
- **Rubric impact:** §8 CI/CD — **5 pts** (P1: nice-to-have for the day, per PRD §6).
- **Main risks:** OIDC federation setup eating deploy-window time — do it only after `azd up` works manually.

#### E11-S1 — `.github/workflows/deploy.yml` (OIDC → build → deploy)

- **Goal:** Workflow authenticates to Azure via OIDC, builds, and runs `azd deploy` on push to `main`.
- **Acceptance criteria:**
  - [ ] OIDC login succeeds (no stored credentials).
  - [ ] `dotnet build` + `azd deploy` run on push to `main`.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E10-S2.
- **Rubric impact:** §8 (3 pts).
- **Priority:** **P1**

#### E11-S2 — Gate the workflow on `dotnet test`

- **Goal:** The pipeline fails when tests fail (token test project per E0-S3 decision 2 makes the gate real).
- **Acceptance criteria:**
  - [ ] Build fails if tests fail.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E11-S1, E0-S3(2).
- **Rubric impact:** §8 (2 pts).
- **Priority:** **P1**

#### E11-S3 — `docs/README-DEPLOY.md`

- **Goal:** Deploy, redeploy, rollback, and post-deploy verification are documented for the "new hire" success signal.
- **Acceptance criteria:**
  - [ ] Documents `azd up`, redeploy, rollback, and the smoke-test steps.
- **Suggested owner profile:** Platform engineer (15-minute write-up, can be done while `azd up` runs).
- **Dependencies:** E10-S2.
- **Rubric impact:** Track C checklist.
- **Priority:** **P1**

---

## Workstream 5 — Security + Observability

### Epic E7 — Identity & Secrets (Managed Identity + Key Vault)

- **Epic goal:** Zero credentials anywhere: SQL, Blob, and ACR access via Managed Identity; anything left goes through Key Vault. Protects 10 rubric pts **and** the −5 committed-secret deduction.
- **Scope:** App-side connection changes (Track A) + infra role assignments (Track C, lands via E10-S3).
- **Dependencies:** E2-S4 (config plumbing), E10-S2 (MI + roles exist). Local dev path: `DefaultAzureCredential` with dev AAD.
- **Rubric impact:** §5 Identity & secrets — **10 pts**.
- **Main risks:** SQL AAD/MI friction (Med, PRD §11) — prep `Authentication=Active Directory Default` connection-string form early; who owns the AAD admin group is E0-S3 decision 6.

#### E7-S1 — Azure SQL via Managed Identity

- **Goal:** Connection string is server-only with `Authentication=Active Directory Default` — no SQL password exists anywhere.
- **Acceptance criteria:**
  - [ ] No credentials in any config or source (NFR3).
  - [ ] Connects locally (dev AAD via `DefaultAzureCredential`) and in-cloud (MI).
- **Suggested owner profile:** Web+API pair (app side) + Platform engineer (server AAD admin).
- **Dependencies:** E2-S4, E10-S2.
- **Rubric impact:** §5 (5 pts).
- **Priority:** **P0**

#### E7-S2 — Managed Identity for Blob + ACR pull

- **Goal:** The workload identity can read/write blobs and pull images — no storage keys, no registry passwords.
- **Acceptance criteria:**
  - [ ] MI holds Storage Blob Data Contributor + AcrPull (assigned in Bicep, E10-S3).
  - [ ] Blob access works via `DefaultAzureCredential` (proven by E6-S2/E6-S3 at smoke test).
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E10-S2, E6-S1.
- **Rubric impact:** §5 (3 pts), §7.
- **Priority:** **P0**

#### E7-S3 — Remaining secrets → Key Vault (or documented none remain)

- **Goal:** Any secret that survives the MI sweep is referenced from Key Vault; ideally the finding is "none remain" — documented.
- **Acceptance criteria:**
  - [ ] Key Vault Secrets User role assigned; no plaintext secrets anywhere.
  - [ ] A note (README-DEPLOY or `wiki/log.md`) states which secrets live in KV — or that none do.
- **Suggested owner profile:** Platform engineer.
- **Dependencies:** E7-S1, E7-S2, E10-S2.
- **Rubric impact:** §5 (2 pts).
- **Priority:** **P0**

### Epic E8 — Observability (ILogger + Application Insights)

- **Epic goal:** `log4net`/`AppLogger`/`Trace` are gone; structured `ILogger<T>` telemetry flows to Application Insights, and a `/health` endpoint proves dependencies are alive.
- **Scope:** `Common/` logger swap (Track A owns it), AI wiring per service, health endpoint.
- **Dependencies:** E1-S3 (log4net already dropped), E2-S4 (AI connection string via config), E10-S4 (string flows in-cloud).
- **Rubric impact:** §6 Observability — **10 pts**.
- **Main risks:** Traces not visible until deploy — verify at smoke test, not before; static `AppLogger` call sites scattered across all projects (appmod/find-replace, then review).

#### E8-S1 — Replace `log4net`/`AppLogger` with `ILogger<T>`

- **Goal:** Every log call goes through injected `ILogger<T>`; the static wrapper and all `Trace.*` calls are deleted.
- **Acceptance criteria:**
  - [ ] `ILogger<T>` throughout Web, Services, Worker, Common, Data.
  - [ ] `AppLogger`, log4net config, and `Trace.*` calls gone; no `C:\` log paths.
- **Suggested owner profile:** Web+API pair (owns `Common/`); Worker+Storage dev converts Worker call sites.
- **Dependencies:** E1-S3, E3-S1/E5-S1 (DI hosts exist).
- **Rubric impact:** §6 (4 pts).
- **Priority:** **P0**

#### E8-S2 — Wire Application Insights

- **Goal:** `AddApplicationInsightsTelemetry()` (Web/Services) + `AddApplicationInsightsTelemetryWorkerService()` (Worker), connection string via config only.
- **Acceptance criteria:**
  - [ ] AI connection string from config/env (never hard-coded).
  - [ ] Traces/requests visible in App Insights after the smoke test (FR-level proof, PRD §14).
- **Suggested owner profile:** Web+API pair + Worker+Storage dev (own services); Platform engineer supplies the connection string path (E10-S4).
- **Dependencies:** E8-S1, E2-S4, E10-S4 (in-cloud).
- **Rubric impact:** §6 (4+2 pts).
- **Priority:** **P0**

#### E8-S3 — `/health` endpoint

- **Goal:** `/health` returns Healthy when the DbContext and Blob container are reachable (FR7) — also the hook for the E13-S4 probes bonus.
- **Acceptance criteria:**
  - [ ] `/health` wired with DbContext + Blob checks; returns Healthy when both reachable.
- **Suggested owner profile:** Web+API pair.
- **Dependencies:** E3-S1, E2-S1, E6-S1.
- **Rubric impact:** Track A/B checklist; feeds bonus probes (E13-S4).
- **Priority:** **P0**

---

## Workstream 6 — Demo Readiness + Learnings

### Epic E12 — Team Artifacts, Smoke Test & Demo

- **Epic goal:** The cheap, continuous 10 rubric pts (§9) plus the P0 gate that protects everything: the post-deploy smoke test against the demo acceptance checklist.
- **Scope:** `docs/learnings.md`, commit hygiene, smoke test + hardening, ≤20-min demo. Runs **all day**, not at the end.
- **Dependencies:** E12-S4 needs a completed `azd up` (E10); everything else is continuous.
- **Rubric impact:** §9 Team artifacts & demo — **10 pts**; E12-S4 defends the −5 "azd up fails at demo" deduction.
- **Main risks:** Leaving learnings/demo prep to 16:45 — then the cheapest points on the board are the ones you drop.

#### E12-S1 — `docs/learnings.md` with ≥6 prompt/response lessons

- **Goal:** Capture Copilot/appmod lessons as they happen — each track contributes.
- **Acceptance criteria:**
  - [ ] ≥6 entries by demo time (PRD §14); each names the prompt/task and what was learned.
- **Suggested owner profile:** Whole team (add an entry per surprising diff); one person curates.
- **Dependencies:** None — starts at kickoff.
- **Rubric impact:** §9 (3 pts).
- **Priority:** **P1**

#### E12-S2 — Commit hygiene: every commit references a story/appmod ID

- **Goal:** A spot-check of the git log shows one story per commit, ID in the message (e.g. `E4-S2: replace WCF client with HttpClient`).
- **Acceptance criteria:**
  - [ ] Spot-check passes at Checkpoint 1 and 2.
- **Suggested owner profile:** Whole team; track owners enforce on their branch.
- **Dependencies:** None — continuous.
- **Rubric impact:** §9 (2 pts); supports NFR8.
- **Priority:** **P1**

#### E12-S3 — Team demo ≤20 min covering all three tracks

- **Goal:** The demo walks the preserved flows (login → claims list → auto-score → upload → export → App Insights) across all three tracks; stretch ideas are filed as follow-up issues, not chased.
- **Acceptance criteria:**
  - [ ] Demo done in ≤20 min covering all tracks.
  - [ ] Stretch goals + deferred latent gaps (logout, `Role` enforcement, `DocumentPath` history) logged as follow-up issues.
- **Suggested owner profile:** Whole team; one narrator, dry-run during hardening window.
- **Dependencies:** E12-S4.
- **Rubric impact:** §9 (3+2 pts).
- **Priority:** **P1**

#### E12-S4 — Post-deploy smoke test against the demo acceptance checklist

- **Goal:** Immediately after `azd up`, run PRD §14 end-to-end and fix gaps inside the hardening window — so nothing fails live at the demo (−5).
- **Acceptance criteria:**
  - [ ] Web reachable at its Container Apps URL; login works (cookie + PBKDF2).
  - [ ] Claims list renders 50 recent claims; unscored claims scored via `POST /claims/{id}/score` with unchanged rules.
  - [ ] Upload lands in `claim-docs`; worker export lands in `claim-exports` within one interval with `ExportLog` row.
  - [ ] Traces visible in App Insights; `/health` Healthy.
  - [ ] No secrets in `appsettings`/source; `dotnet list package --vulnerable` → no High/Critical.
  - [ ] Gaps found are fixed in the 16:15–16:45 hardening window or consciously accepted.
- **Suggested owner profile:** Whole team (each track verifies its own flows; Platform engineer drives).
- **Dependencies:** E10-S2/S4, all P0 workstreams merged to `integration`.
- **Rubric impact:** Defends the full ≥70 result and the −5 deploy deduction.
- **Priority:** **P0**

---

## P0 Critical Path to ≥70/100

P0 stories above cover the seven 10–15-pt rubric sections = **85 available points**; reaching ≥70 means essentially every P0 workstream lands. E11 + E12 (P1, 15 pts) are cheap insurance if any P0 section drops points.

```
E0 (setup + decisions)
 └─ E1 (net9.0, SDK, CVEs)  ← unblocks everything
     ├─ Track A: E2 → E3 ∥ E4 (E4-S2 eliminates CS0234 by design)
     ├─ Track B: E5 → E6 (E6-S1 interface early — Track A needs it)
     └─ Track C: E9 + E10-S1 (Sprint 1) → E10-S2..S4 (Sprint 2)
         └─ Sprint 2 convergence: E7 ∥ E8 (need infra + config plumbing)
             └─ integrate → azd up → E12-S4 smoke test → harden → demo
```

**Watch items (from PRD §11):** shared `Data/` is the highest-likelihood conflict — one owner merges; first `azd up` failing is *expected* — timebox it; nobody spends Sprint 1 time "fixing" MSB3644/MSB4226 or adding a WCF reference for CS0234.

> **Reminder:** the LLM Wiki is the source of truth. If reality diverges during execution,
> update the wiki and this document, then log the change in `wiki/log.md`.
