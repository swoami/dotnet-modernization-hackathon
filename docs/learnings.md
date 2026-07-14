# Learnings

Team log — captured live during the hackathon. Each entry is a short story:
what you were trying to do, what prompt/tool you used, what happened, what
you'd do differently.

Keep entries short. Bullets are fine. One-liners are fine.

---

## Track A — Data EF Core modernization (`track/a-web-api`)

### Manual follow-up

- **Removing the static `AppLogger` facade** — after the Data repositories and
  Worker had constructor-injected `ILogger<T>`, `AppLogger` and the legacy
  `ConfigHelper` had no consumers. Delete those facades and their direct-only
  packages rather than retaining compatibility layers; keep
  `AddContosoLogging()` as the shared host logging setup.
- **Repository DI needs a scope in hosted services** — EF Core repositories take a scoped
  `ContosoDbContext`; resolve them within an `IServiceScopeFactory` scope in a
  `BackgroundService` rather than retaining a context or connection string for its lifetime.
- **Read-only repository results need `AsNoTracking()`** — projecting claim/policy joins into
  detached `Claim` results preserves the old repository behavior while allowing async EF Core
  queries and a parameterized LINQ substring search.
- **Identity's `PasswordHasher<TUser>` encapsulates a versioned PBKDF2 format** — use its
  `SuccessRehashNeeded` result to refresh a valid hash, rather than retaining a legacy verifier.

---

## Track B — Worker & Storage modernization (`track/b-worker-storage`)

### Tooling used
`modernize plan create "<description>"` → `modernize plan execute` (GitHub Copilot app modernization CLI)

### Prompts that worked

- **`modernize plan create` with a detailed multi-goal description** — providing all four goals in one
  prompt (SDK upgrade, BackgroundService, appsettings.json, ILogger) produced a well-structured 4-task
  plan with correct dependency ordering. The CLI broke it into discrete tasks automatically.

- **`modernize plan execute` without arguments** — auto-discovered the single plan in
  `.github/modernize/` and ran all tasks in dependency order with no manual steering needed.

- **Asking Copilot Chat "is App.config still used?"** while the CLI was running → instantly identified
  that `ContosoInsurance.Data` repositories call `ConfigHelper.GetConnectionString()` via
  `System.Configuration.ConfigurationManager`. Saved time vs. manually tracing the dependency.

### Prompts that needed a nudge

- **Task 002 stale state across sessions** — the CLI completed the BackgroundService rewrite and wrote
  a consistency-check report, but the session ended before it marked the task `success`. On the next
  `modernize plan execute` run the CLI detected the stale task and resumed cleanly, but it was
  not obvious at first why the task was still showing `started`.

- **App.config → appsettings.json gap not covered by the plan** — the 4-task plan migrated
  `<appSettings>` to `appsettings.json` (task 002) but did not address `<connectionStrings>` because
  the Data repositories still used `ConfigHelper`. The plan stopped short of full App.config removal.
  Required a manual follow-up: add a `ClaimsRepository(string connectionString)` constructor overload,
  inject `IConfiguration` into `ClaimsExporterService`, and then delete `App.config`.

### Gotchas

- **Plaintext password flagged by the CLI's own consistency check** — the CLI added the DB connection
  string (including `****** to `appsettings.json` automatically as part of task 002,
  then immediately flagged it as a Major security issue in its own consistency-check report, and
  replaced it with a placeholder in the same task. The plan correctly treats credentials as a hard
  fail — useful reminder not to accept the first draft of `appsettings.json` blindly.

- **`App.config` survives task 002 by design** — the log4net `<configSections>` block kept
  `App.config` alive as an intermediate state (task 003 removed it). This was correct but created
  a dual-config situation (`App.config` + `appsettings.json`) that was confusing mid-migration.
  The consistency-check report calls this out explicitly, which was helpful.

- **`ContosoInsurance.Data` was not in scope for the Worker-focused plan** — the plan targeted
  `ContosoInsurance.Worker` and `ContosoInsurance.Common`, but the Worker pulls in the Data project
  which still uses `System.Configuration.ConfigurationManager` and the old static `ConfigHelper`.
  The build passes, but the runtime still reads from `ConfigurationManager` unless you explicitly
  thread the connection string through. Worth noting when scoping plans: declare ALL projects that
  need changing, not just the entry point.

- **`[SupportedOSPlatform("windows")]` needed after SDK upgrade** — several CA1416 warnings appeared
  after upgrading to net9.0 because Windows-Service APIs are platform-specific. The CLI added the
  attribute automatically; they all disappeared in task 002 when the Windows Service code was replaced
  entirely.

- **Monitoring a running `modernize plan execute` session** — the CLI runs in its own terminal and
  writes to `.github/modernize/<plan>/` as it goes. The most reliable way to track real-time progress
  is watching `tasks.json` (`status` fields) and checking for new task folders appearing.
  `git diff --stat` after each commit also gives a clean summary of what changed.

### Follow-ups

- **`ContosoInsurance.Data` connection string injection** — `ClaimsRepository`, `PolicyRepository`,
  and `UserRepository` still have parameterless constructors that call `ConfigHelper.GetConnectionString()`.
  The Worker project was fixed (manual step, not covered by the plan), but Web and Services still rely
  on `Web.config`. Full clean-up requires injecting the connection string (or an EF Core `DbContext`)
  via DI across all three projects — Track A owns `DbContext`, coordinate before changing Data.

- **`ExportRoot` default is `C:\Exports`** — cross-platform default; will break on Linux containers.
  Override via `ConnectionStrings__ContosoDb` env var or mount path before containerising (Track C).

- **`Export()` uses synchronous file I/O wrapped in `Task.Run`** — functional but not idiomatic for
  `BackgroundService`. Replace with `File.WriteAllTextAsync` and async ADO.NET when refactoring further.

- **`AppLogger` is now `[Obsolete]`** — `ClaimsRepository` still calls it (CS0618 warning). Task 003
  left this intentionally as a migration signal. Clean it up when the Data layer gets its DI overhaul.

---

## Track C — Platform & infra (`track/c-platform`)

### Prompts that worked

- **Track A** — *"…"* → produced *…*. Kept.
- **Track C — overall approach**: three things made the infra work land cleanly rather than as guesswork:
  1. **Assessment as source of truth** — every non-obvious infra decision (Services deployed as its own Container App vs. merged into Web, port numbers, the SQL DB name, the WCF/port-8080 detail) was justified by re-reading `.github/modernize/assessment/reports-*/facts/*.md` (`architecture-diagram.md`, `api-service-contracts.md`) rather than assumed from the task brief alone. Copilot was explicitly pointed at that folder in the prompt (`@dmh\.github\modernize\assessment\`) instead of being asked to "figure it out."
  2. **Cross-track branch sync** — infra/bicep was drafted from Track C alone first, but every later "make it consistent" pass (env var names, DB name, Dockerfile ports) required diffing against `track/a-web-api` and `track/b-worker-storage`'s actual app code/csproj/appsettings, not just Track C's own docs. Track branches only sync with `main` on push/dispatch, and pushes to `track/*` never auto-refresh each other — so a manual `git fetch`/`git show origin/track/x:<path>` cross-check was necessary each time, it wasn't automatic.
  3. **Fleet mode** (`gh copilot cli` background sub-agent dispatch via the `task` tool, invoked in this session by the literal prompt "You are now in fleet mode. Dispatch sub-agents... in parallel") — used for the 5 independent Bicep modules and again for the KV/OIDC pair, each with an explicit param/output contract up front so the branch could take concurrent pushes with zero merge conflicts. Reserved for genuinely parallel, independent-file work; sequential/dependent work (e.g. README-DEPLOY.md needing the other two done first) was still gated via SQL `todo_deps`, not force-parallelized.
  4. **Specialized upgrade agent for modernization prep** — the GH Copilot CLI environment ships a dedicated `upgrade-agent:upgrade` custom agent (distinct from the generic `general-purpose` agent) purpose-built for the "upgrade and modernize an app" workflow. For modernization-prep tasks that fit its focus — e.g. drafting per-project Dockerfiles for a legacy-to-.NET-9 migration — reach for that specialized agent instead of `general-purpose` first; it carries workflow-specific structure (assess → plan → apply loop) the generic agent has to be told about by hand each time. Worth trying on the next round of Dockerfile/porting prep rather than defaulting straight to `general-purpose`.
- **Track C** — *"Fleet deployed: prepare Draft infra/main.bicep provisioning: [full resource list] … you can use info @dmh\.github\modernize\assessment\ and work from other track branches"* → gave a fleet of 5 background agents an explicit, locked-down param/output contract per module up front (exact file names, param names, output names) so each could draft its own `.bicep` file independently without seeing the others' work, then did the `main.bicep` integration by hand. Zero merge conflicts even though all 5 pushed to the same branch — each agent did `git pull --rebase` before its final push. Kept.
- **Track C** — *"make sure infra is consistent with what is being developed there"* (after track a/b branches appeared) → diffing the actual app code (`ConfigHelper.GetConnectionString("ContosoDb")` in `ContosoInsurance.Data`) against the infra's env var names caught a real bug: `ConnectionStrings__ContosoInsurance` vs the app's expected `ConnectionStrings__ContosoDb`. Cross-branch consistency checks need to read the *app code*, not just assume names — asking Copilot to "make it consistent" only works if you point it at the source of truth.
- **Track C** — *"finish infra tasks: Key Vault secret references · GitHub Actions deploy (OIDC) · README-DEPLOY.md"* → ran 2 independent background agents in parallel (KV secret wiring, OIDC workflow — genuinely independent files) and gated a 3rd (README-DEPLOY.md) as a SQL `todo_deps` dependency on both, since an accurate deploy doc needs to read the *actual* generated workflow/secret names rather than guess them ahead of time. The dependent agent's prompt explicitly said "read `.github/workflows/deploy.yml` once it exists... if not yet present when you start, note TODO and re-check before finishing" as a safety net. Worked cleanly.
- **Track C** — mid-flight scope change: *"azd deployment should be configured for [specific RG/subscription]... make no deployment yet"* arrived while the GitHub Actions agent was still running → used `write_agent` to inject the new requirement into its live conversation instead of waiting for it to finish and re-prompting from scratch. It incorporated the change in its next turn and re-pushed. Much faster than a second full dispatch.
- **Track C — three-CLI split for going from "no deployment yet" to a green `azd deploy`**: getting OIDC deploy actually working end-to-end used `az`, `gh`, and `azd` for three deliberately different jobs, and mixing up which tool owns which job is exactly where it broke the first few times:
  1. **`az` CLI = ground truth about the Azure side.** Used to discover *who am I actually allowed to be* before creating anything: `az account show` to confirm the signed-in session was on the right tenant/subscription (it wasn't, initially — logged into an unrelated tenant), `az role assignment list --assignee <user>` to find out the signed-in user only has **Owner scoped to the resource group**, not the subscription (which later explained why `azd provision`'s subscription-scope validation call was doomed no matter what). Then used to *create* the identity itself: `az ad app create` / `az ad sp create-for-rbac`-equivalent steps, `az role assignment create` (RG-scoped Owner for the new SP), `az ad app federated-credential create` (the GitHub OIDC trust, subject `repo:<org>/<repo>:ref:refs/heads/main`). Finally used for the actual provisioning too: `az deployment group create` (RG-scoped, unlike `azd provision`) and `az deployment group show --query properties.outputs` to read back what got created.
  2. **`gh` CLI = the bridge from "az just created a credential" to "GitHub Actions can use it."** Once `az` produced the `appId`/`tenantId`/`subscriptionId`, `gh secret set AZURE_CLIENT_ID/AZURE_TENANT_ID/AZURE_SUBSCRIPTION_ID` and `gh variable set AZURE_ENV_NAME/AZURE_LOCATION/AZURE_RESOURCE_GROUP` pushed them straight into the repo's Actions config — no manual copy-paste through the GitHub UI, and no risk of a secret value ending up in shell history or a committed file. `gh pr create`/`gh pr merge --squash --admin` also drove every fix through the same repo, one flag away from the wrong fork (see Gotchas below).
  3. **`azd` = deploy/runtime orchestration, and it does *not* inherit `az login`.** This was the single biggest gotcha of the whole exercise: `azure/login@v2` in the workflow only authenticates the `az` CLI session; `azd` keeps a completely separate credential cache and needs its own `azd auth login --federated-credential-provider github` (CI) / `azd auth login --tenant-id <id>` (local, interactive) using the *same* federated credential subject. Skipping this step is what produced a confusing "not logged in" failure right after `az login` had visibly succeeded in the same job.
  - **Net takeaway**: treat `az` as the source of truth for identity/RBAC/resource state and for anything that must run at exact RG scope, `gh` purely as the one-way pipe for turning `az`-created values into Actions secrets/vars, and `azd` as a separate runtime that must be logged in and fed its config independently — don't assume any of the three CLIs shares session state or config with the others.

### Prompts that needed a nudge

- **Track B** — first prompt was too broad; broke into three; then it worked.
- **Track C** — asked the azure.yaml sub-agent to set `docker.path`/`docker.context` from a plausible-sounding guess; double-checked against the actual azd JSON schema (`https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json`) afterward and found `path` (not `dockerfile`) is the correct field, and both `path` and `context` resolve relative to the service's `project` folder — not the repo root or the azure.yaml folder. Fixed by hand after the agent's first pass. Moral: for less-common tool config schemas, verify against the authoritative schema/docs rather than trusting a plausible first draft, even your own.

### Gotchas

- Something surprising you hit and how you got past it.
- `shell: pwsh` steps in GitHub Actions run as `pwsh -Command ". 'script.ps1'"` (dot-sourced). A stale `$LASTEXITCODE` from an intentionally-nonzero `git` call inside the script can leak out as the *step's* exit code, failing the job even though the script's own logic succeeded. Fix: end the script's success path with an explicit `exit 0`. Could not reproduce locally on Windows pwsh — only showed up on the Linux runner; fixed and verified via `workflow_dispatch` rather than local repro.
- GitHub Pages 404s at the site root if `docs/` has no `index.html` — `docs/tracker.html` alone wasn't enough even though the workflow uploads the whole `docs/` folder as the Pages artifact.
- `az bicep build` intermittently failed locally with `WinError 32` (file lock) on this machine — transient, not a real Bicep error; retrying (or just trusting a clean run from another agent) resolved it.
- Bicep's `listKeys()` requires an argument calculable at the *start* of deployment — passing a module's `output ... resourceId` doesn't qualify (BCP181), even though the resource obviously exists by the time it's called. Fix: rebuild the same `resourceId()` locally from the deterministic resource *name* instead of reusing the module output.
- Container Apps env vars can only have `value` OR `secretRef`, never both — switching the SQL connection string from a plaintext `value` to a Key Vault-backed `secretRef` meant removing the old `value:` line entirely, not just adding `secretRef:` alongside it.
- `gh pr create` silently targeted the wrong repo (`heisenberg-alt/...` fork set as `upstream` remote) instead of `origin` (`swoami/...`), failing with a confusing "No commits between main and <branch>" error since it compared branches that didn't exist on that fork. Fix: pass `--repo swoami/dotnet-modernization-hackathon` explicitly rather than relying on `gh`'s remote auto-detection whenever a repo has more than one remote configured.
- A previously-merged, well-intentioned workflow tweak (`push: branches: [main, 'track/*']` on `tracker-pages.yml`) silently made every track-branch push run a doomed `deploy` job, since the `github-pages` environment's branch-protection rule only allows deploys from `main` — the *build* job succeeded (making it look "half-working"), which made the real problem (a wasteful, always-red `deploy` job on every track push) easy to miss at a glance. Worth periodically diffing a shared workflow file against what it looked like a few PRs ago, not just checking whether the latest run passed.
  - **Follow-up fix**: re-added `track/**`/`integration` to the `push` trigger (so the tracker stays current between merges, not just on manual `workflow_dispatch`) but this time guarded the `deploy` job itself with `if: github.ref == 'refs/heads/main'` instead of relying on the trigger filter alone. That way a non-`main` run still rebuilds the artifact (harmless) and the `deploy` job is *skipped*, not failed — fixes both the original regression and the "automatic pushes break the status" symptom in one change, without depending on remembering to keep the trigger narrow forever.
- `azd provision` validates at **subscription scope** even when the target Bicep template declares `targetScope = 'resourceGroup'` — confirmed empirically (not a misconfiguration on our side) by checking that *neither* the deploy service principal *nor* the signed-in user's own account has any subscription-level role, only RG-scoped Owner, and `azd provision` still failed with `AuthorizationFailed` on a subscription-level validate call. Since this hackathon environment only ever grants RG-scoped access, `azd provision` is fundamentally blocked here — switched to `az deployment group create` (RG-scope only) directly in the workflow, then bridged its outputs into the azd environment by hand so the later `azd deploy` step could still find the registry/app names.
- **`az deployment group show --query properties.outputs` mangles the casing of SCREAMING_SNAKE_CASE output keys** — e.g. Bicep declares `output AZURE_RESOURCE_GROUP` but the API returns the key as `azurE_RESOURCE_GROUP`, and `APPLICATIONINSIGHTS_CONNECTION_STRING` comes back missing a letter (`applicationinsighT_CONNECTION_STRING`). Confirmed via `az bicep build --outfile` that the *compiled ARM template* has the correct keys, so this is purely an ARM/CLI display-layer bug, not a Bicep or workflow mistake. The original bridging step (`jq | while read`) trusted the returned key verbatim, so most `azd env set` calls silently used the wrong name — this is what caused `azd deploy` to fail with "could not determine container registry endpoint" even after provisioning had succeeded. Fix: never trust the literal casing for ALL-CAPS output names; case-insensitively match against a hardcoded list of the expected output names instead (`.upper()`/`ascii_upcase`), and use the *known-correct* name — not the mangled one — as the key passed to `azd env set`.
- Debugging the bridging fix above **locally first** (per explicit instruction, instead of round-tripping through CI) surfaced its own gotcha: a Python heredoc (`python3 - <<'PYEOF' ... PYEOF`) embedded inside a `run: |` YAML block scalar only works if every line — including the closing `PYEOF` delimiter — has consistent, LF-only line endings; a file that had picked up CRLF line endings (common on Windows with `core.autocrlf=true`) breaks the heredoc match with a silent "here-document delimited by end-of-file" warning instead of a clear error. Caught it by extracting the exact parsed `run:` string via `yaml.safe_load` and syntax-checking it with `bash -n` before ever pushing — cheaper than finding out from a red Actions run.

### Follow-ups

- Things we did not finish; who wants to pick them up.
- `A1`/`B1` ("Assess") tasks in `docs/tracker.html` are still hardcoded `manual: true` checkboxes — `scan-progress.ps1` could auto-detect them from `.github/modernize/assessment/reports-*` presence on each branch, but this hasn't been implemented. Whoever owns the tracker next could pick this up.
- The local-only `dotnet restore` SSL/`NU1301`/`PartialChain` error hit while testing `azd deploy` on this machine looks like a corporate-proxy TLS-interception issue specific to this sandbox (CI's own hosted runner built the exact same Dockerfiles successfully in an earlier run) — flagged as environment-specific rather than fixed, but worth a second pair of eyes if it ever shows up in CI too.

**Resolved this session** (kept for context — remove once the team has read this):
- ~~`azure.yaml` only has a `contosoinsurance-worker` entry~~ — fixed: `contosoinsurance-web`/`contosoinsurance-services` added with correct `docker.path`/`docker.context` (see Gotchas).
- ~~`docs/README-DEPLOY.md` hasn't been written yet~~ — fixed: covers `azd up`, CI/CD secrets/vars, redeploy/rollback, and post-deploy verification.
- ~~SQL connection string passed as a plaintext Container Apps env var~~ — fixed: now a Key Vault secret (`sql-connection-string`) referenced via `secretRef` + managed identity.
- ~~No live `azd provision`/`az deployment group create` run has been done against `infra/main.bicep` yet~~ — fixed: real infra is now provisioned in `rg-swo-gh-hackathon-team3` (Log Analytics, App Insights, ACR, Key Vault, Storage, SQL, Container Apps environment + all three apps) via `az deployment group create` (see the `az`/`gh`/`azd` split above for why not `azd provision` directly).
- ~~The Azure AD federated credential (OIDC trust) for `.github/workflows/deploy.yml` has not been configured on the Azure side yet~~ — fixed: app registration + service principal + RG-scoped Owner + federated credential created via `az`, secrets/vars pushed via `gh secret set`/`gh variable set`.
