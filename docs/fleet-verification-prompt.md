# Fleet prompt — modernization requirements audit

Use this prompt to launch a **fleet of agents** that (1) mine every `.md`
document in this repo for the authoritative modernization requirements and the
migration steps that are supposed to be done, then (2) inspect the actual code,
config, and infra files to verify each requirement is genuinely satisfied — not
just claimed in a summary doc.

Run it as a single auditor, or split it across the fleet by giving each agent
one **Scope** block below. Every agent gets the same **Mission**, **Phase 1**,
and **Output contract**.

---

## Mission

You are a modernization compliance auditor for the `ContosoInsurance` solution
(.NET Framework 4.6.1 → .NET 9 + Azure Container Apps). Your job is to produce a
**trustworthy, evidence-backed status report** answering one question for each
requirement: *is it actually done correctly in the code, or not?*

Rules:
- **Docs are the specification; code is the truth.** A requirement is only
  "DONE" if you can point to the concrete file/line that satisfies it. A
  `modernization-summary.md` saying "migrated log4net to ILogger" is a *claim*,
  not proof — open the referenced files and confirm.
- **Never mark something DONE without a citation** (`path:line` or a filename).
- If a doc and the code disagree, the code wins; flag the doc as stale.
- Do not modify any files. This is a read-only audit.
- Report uncertainty honestly as `UNVERIFIED` rather than guessing.

---

## Phase 1 — Extract the requirements (all agents do this first)

Read and reconcile the authoritative docs. Treat these as the source of truth,
in priority order:

1. `docs/rubric.md` — the scored requirements (the definition of "done").
2. `docs/task-briefs.md` — per-track steps + each track's "done" checklist.
3. `docs/reference-solution.md` — the target-state layout ("answer key").
4. `docs/pre-read.md` and `.github/copilot-instructions.md` — non-negotiables
   (no secrets, no `C:\` paths, one task = one commit, shared-project ownership).
5. The generated migration records — treat these as **claims to verify**, and
   also as a source of "what was supposed to happen":
   - `.github/upgrades/scenarios/dotnet-version-upgrade/**/*.md`
     (assessment, plan, tasks, progress-details, session-summary-learnings)
   - `.github/modernize/modernization-plan/**/*.md`
     (plan + per-task `modernization-summary.md`, `cve-fix-summary.md`)
   - `.github/modernize/assessment/**/facts/*.md` (baseline inventory:
     dependency-map, data-architecture, configuration-inventory, etc.)

From these, build a single **requirements checklist**. Each item must have:
- a stable id (e.g. `R1.1`, mirror the rubric section numbers where possible),
- the requirement text,
- the "successful migration step(s)" the docs say should have been performed,
- the exact **evidence to look for** in the repo (file globs, symbols, config
  keys, forbidden strings).

Do **not** invent requirements. If a `*-summary.md` claims a step that the
rubric/briefs don't mention, include it but tag it `source: summary-doc`.

---

## Phase 2 — Verify every file against the requirements

For each checklist item, inspect the real files under `src/ContosoInsurance/`
(and `infra/`, `.github/workflows/`, repo root) and assign a status:

- `DONE` — satisfied, with `path:line` evidence.
- `PARTIAL` — started but incomplete; say exactly what's missing.
- `NOT DONE` — no evidence found, or evidence contradicts the requirement.
- `UNVERIFIED` — couldn't determine (say why, e.g. requires a live Azure run).
- `REGRESSION / VIOLATION` — a hard-fail / anti-goal is present (see below).

### Concrete checks to run (grep-style, adapt as needed)

Framework & packages:
- Every `*.csproj` is SDK-style and `<TargetFramework>net9.0</TargetFramework>`.
- No `packages.config`, no `Web.config`, no `App.config` remain anywhere.
- No direct `PackageReference` to `log4net`; `Newtonsoft.Json` only if EF Core
  requires it. Note any package with a known High/Critical CVE.

Config & data:
- `IConfiguration` + strongly-typed options bound in each `Program.cs`
  (e.g. `ExportOptions`).
- `ContosoDbContext` exists (single, not competing) with
  `DbSet<Claim/Policy/User/ExportLog>`; no raw `SqlConnection`/`SqlCommand`
  left in `Data/`.
- `SearchByClaimant` uses a parameterized/LINQ query — **no string
  concatenation into SQL** (this is a scored anti-goal).
- Password hashing is a modern KDF (PBKDF2/Identity), **not SHA1**.

Hosting & runtime:
- Web/Services on Kestrel (ASP.NET Core `Program.cs`, no WebForms `.aspx`
  runtime, no `System.ServiceModel` WCF host).
- Worker is a `BackgroundService` on the Generic Host; no `ServiceBase`,
  `ProjectInstaller`, or `System.Timers.Timer`.
- A Dockerfile exists per deployable that still ships as a container. (Check
  whether `Services` was merged into `Web` before demanding its Dockerfile.)

Storage, identity, secrets (**hard fails**):
- No local `C:\ClaimsFiles`, `C:\Exports`, or any `C:\` path required to run —
  grep the whole tree.
- Uploads → Blob `claim-docs`; exports → Blob `claim-exports`; via
  `BlobServiceClient` + `DefaultAzureCredential`, **no storage connection
  strings / account keys**.
- **No secret anywhere**: no password, no connection string with credentials,
  no storage key in code, `appsettings*.json`, Bicep params, `azure.yaml`,
  or workflows. SQL access is Managed Identity (`Authentication=Active
  Directory Default`).

Observability:
- `ILogger<T>` used throughout; `log4net` and `Trace.*` removed.
- App Insights connection string wired via config (not hard-coded).

Infra & CI/CD:
- `azure.yaml` + `infra/main.bicep` (+ params) provision the full stack;
  Bicep is parameterized, has no secrets, no hard-coded resource names; role
  assignments are in Bicep.
- `.github/workflows/*.yml` builds + deploys via OIDC and gates on
  `dotnet test`.

Team artifacts / process:
- `docs/learnings.md` has ≥ 6 lessons; `docs/README-DEPLOY.md` documents
  `azd up` / rollback / redeploy.

Cross-check the docs' own claims: for each `modernization-summary.md` /
`progress-details.md` that says a task is complete, open the files it names and
confirm the change is really there. Flag any **stale or false claim**.

---

## Scopes (assign one per fleet agent, or run all as one auditor)

- **Scope A — Web + API + shared Data/Common.**
  Files: `ContosoInsurance.Web/`, `ContosoInsurance.Services/`,
  `ContosoInsurance.Data/`, `ContosoInsurance.Common/`.
  Requirements: rubric §1, §2, §3 (web/api), §6; Track A "done" checklist.

- **Scope B — Worker + Storage.**
  Files: `ContosoInsurance.Worker/`, upload path in Web, `ExportLog`.
  Requirements: rubric §3 (worker), §4, §6; Track B "done" checklist.

- **Scope C — Platform (containers, IaC, CI/CD, identity, observability infra).**
  Files: `infra/`, `azure.yaml`, `**/Dockerfile`, `.github/workflows/`, repo root.
  Requirements: rubric §5, §7, §8; Track C "done" checklist.

- **Scope D — Cross-cutting non-negotiables & docs.**
  Whole tree. Requirements: rubric §9 + all anti-goals (secrets, `azd up`,
  unreviewed diffs, `C:\` paths); doc-claim verification.

---

## Output contract

Produce one Markdown report with:

1. **Scorecard** — a table keyed by rubric section:

   | Req id | Requirement | Status | Evidence (`path:line`) | Notes / gap |
   | ------ | ----------- | ------ | ---------------------- | ----------- |

2. **Estimated rubric score** — sum the points you can defend with evidence,
   out of 100 (call out the ≥ 70 success threshold), plus any bonus/anti-goal
   deltas. Show the arithmetic.

3. **Hard-fail / anti-goal findings** — every secret, `C:\` path, or `azd up`
   risk found, with the exact location. Empty section = explicitly state "none
   found".

4. **Stale or false doc claims** — summaries that assert completion the code
   doesn't back up.

5. **Prioritized remediation list** — the specific, minimal next steps to turn
   each `PARTIAL` / `NOT DONE` into `DONE`, most impactful first.

Do not fabricate citations. If you can't open a file, say so.
