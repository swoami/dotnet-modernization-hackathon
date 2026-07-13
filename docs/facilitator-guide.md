# Facilitator guide

You are running one full-day session for a team of six senior .NET engineers
who have used Copilot chat before but are new to the app-modernization
extensions and the modernization CLI. Your job: keep timing, unblock people,
enforce checkpoints, and prevent the platform track from bottlenecking the
other two.

---

## Before the day

**One week out**
- Share `docs/pre-read.md`. Nudge people to install the extensions in advance.
- Confirm the shared Azure RG exists and every attendee has `Owner` on it.
  Note the RG name and subscription ID.
- Confirm a GitHub org / repo where the team can push. Configure a federated
  credential (OIDC) between GitHub Actions and a service principal that has
  `Contributor` + `User Access Administrator` on the RG.
- Grab a room with a projector, whiteboard, and reliable Wi-Fi.
- Pre-provision an Azure OpenAI or generic AI-Foundry-region only if the team
  needs Copilot's higher-tier tooling — check the extension's model
  requirements the week of the event; they change.

**Day before**
- Clone this repo, run through the *guided demo* yourself (below).
- Pre-open every tab you'll show.
- Have `dotnet list package --vulnerable --include-transitive` output for the
  current legacy solution ready — a great slide.

---

## Guided demo script (09:30–10:00)

The goal is to make the appmod loop *concrete* for the team before they split.
Do this on the projector.

1. Open `src/ContosoInsurance/ContosoInsurance.sln` in VS Code.
2. Command palette → **"GitHub Copilot app modernization (.NET): Assess"**.
   Point at the whole solution.
3. Walk through the generated plan. Call out:
   - Framework upgrade tasks
   - `packages.config` migration tasks
   - CVE-tagged package upgrades
   - `Web.config` → `appsettings.json` tasks
   - Cloud-readiness recommendations (usually you'll see them on the
     Azure-migration side after upgrading)
4. Pick **one** small task — good candidate: *"Convert ContosoInsurance.Common
   to SDK-style project targeting net9.0"*.
5. Run the task. When the agent proposes the diff, show:
   - The **explanation** panel
   - The **file diff**
   - What you'd inspect before accepting (target framework, package versions,
     removed lines)
6. Accept, then run `dotnet build ContosoInsurance.Common\ContosoInsurance.Common.csproj`
   in the terminal. Show the build succeed.
7. Commit that change with a conventional message, e.g.
   `chore(common): migrate to SDK-style project targeting net9.0 (appmod)`.

That's the whole loop. Everything else scales this pattern.

---

## Branch strategy

- Each pair works on `track/a-web-api`, `track/b-worker-storage`, `track/c-platform`.
- Rebase against `main` at each checkpoint. Never rebase after someone else
  has branched from your branch.
- **`integration` branch** at Checkpoint 2: merge all three tracks. Fix
  conflicts as a group at the whiteboard.
- Merge `integration` → `main` after successful smoke test.

---

## Shared code contract

Two projects are shared across tracks:

- **`ContosoInsurance.Common`** — Track A owns the log4net → `ILogger` swap
  because it lands there first. Tracks B and C consume the resulting
  `AddContosoLogging()` extension method.
- **`ContosoInsurance.Data`** — Tracks A and B jointly own the EF Core
  DbContext. Suggested split:
  - Track A: `ContosoDbContext`, `DbSet<Claim>`, `DbSet<Policy>`, `DbSet<User>`,
    `ClaimsRepository`, `PolicyRepository`, `UserRepository` rewrites.
  - Track B: adds `DbSet<ExportLog>` + write path, migration for it.

Agree on the DbContext class name **at 10:15** (start of Sprint 1). Do not
let both tracks generate different DbContexts in parallel.

---

## Copilot patterns to encourage

**Ask for the plan, then the code.** For any non-trivial change, prompt
Copilot Chat to describe *what* it will do first. Accept the plan, then say
"apply that". Reduces silent rewrites.

**One task = one commit.** If Copilot proposes a diff that spans four concerns,
push back and ask for four smaller diffs.

**Show your context.** When something goes sideways, open the file explicitly
(don't rely on active-editor pickup) and re-issue the prompt with the file
attached.

**Trust but verify the build.** Run `dotnet build` after every accepted diff.
`dotnet test` after every meaningful accept if you have tests.

**Use the extension when it applies, chat otherwise.** The `Assess` /task-list
flow is best for well-known transformations (framework upgrade, `packages.config`,
`Web.config`, WCF, ADO.NET → EF Core, containerization plan). Chat / Agent
mode is better for one-off refactors and glue code.

---

## Common gotchas (and the unblock)

| Symptom | Likely cause | Unblock |
| --- | --- | --- |
| `Assess` produces very few tasks | Extension pointed at a folder that doesn't contain the `.sln` | Re-run at the `src/ContosoInsurance/` level, pass the `.sln` explicitly |
| Framework upgrade task leaves `System.Web` references | WebForms not yet converted; upgrade skipped those files | Do the WebForms → Razor Pages step **first**, then re-run framework upgrade |
| SQL connection fails locally after MI change | `Authentication=Active Directory Default` needs `Az.Account` / `DefaultAzureCredential` context | `az login`; ensure the developer's account has been added to the AAD SQL admin group / SQL DB user |
| `azd up` fails at role assignment | User doesn't have `User Access Administrator` on the RG | Ask the org's Azure admin, or scope the RBAC to a component the user *does* own |
| Container App won't pull image | Missing `AcrPull` on the managed identity, or wrong registry server | Verify with `az acr repository show`, check `identity` block on the Container App resource |
| WebForms → Razor Pages diff is enormous | Copilot tried the whole app in one shot | Break by page: `Default.aspx` first, then `Login.aspx`, then `Upload.aspx` |
| WCF client in `Default.aspx.cs` still there after Services rewrite | Track A left the SOAP client dangling | Replace with `HttpClient` typed client (`IHttpClientFactory.CreateClient("Scoring")`) |
| `log4net` still in `Common.csproj` after all tracks say they removed it | Transitive reference from `Data` or `Services` | `dotnet list package --include-transitive` |
| Local file paths (`C:\ClaimsFiles`) leak into config or code | Not everyone finished the Blob swap | Repo-wide grep for `C:\\ClaimsFiles\|C:\\Exports` at Checkpoint 2 |
| Newtonsoft.Json survives | EF Core dependency chain re-adds a version | OK if it's a fresh, unvulnerable version; make sure app code uses `System.Text.Json` |

---

## Time-box discipline

- **10:00 hard stop on setup drift.** If someone can't install by 10:00, pair
  them with a working machine and don't slow the room.
- **12:30 hard stop on WebForms rewriting.** If Razor conversion isn't
  progressing, drop to Razor for `Default.aspx` only and keep Login/Upload
  as ASP.NET Core minimal-hosted static + form posts. Ship it.
- **15:30 hard stop on new features.** Anything not on the branch by 15:30
  is a stretch goal. Everyone shifts to `azd up` and smoke test.

---

## Scoring / demo criteria

See `docs/rubric.md`. Read it out at the start so the team optimizes for the
right things.

---

## After the day

- Merge `integration` → `main`.
- Push the deployed environment or tear it down (decide upfront — keeping it
  costs money).
- Publish `docs/learnings.md` inside the org's engineering wiki.
- File follow-ups: WebForms → Blazor, WCF → CoreWCF, Entra ID auth, integration
  tests, load test, cost review.
