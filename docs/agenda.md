# Agenda — Legacy Breakers (Full Day, 8 hours)

Team size: 6 (three pairs). One repo, three feature branches. One shared Azure
resource group with team-level Owner.

Times below assume a **09:00 start**; shift as needed.

| Time | Duration | Block | What happens |
| --- | --- | --- | --- |
| **09:00–09:30** | 0:30 | **Kickoff** | Facilitator: mission recap, tour of `src/ContosoInsurance/`, tool-check screen-share, branch strategy, define "done" |
| **09:30–10:00** | 0:30 | **Guided demo** | Facilitator runs `Assess` from the appmod extension on the solution live, walks through the generated plan, executes **one** task end-to-end so everyone sees the loop |
| **10:00–10:15** | 0:15 | **Split + brief** | Pairs go to their track briefs (`docs/task-briefs.md`), cut branches: `track/a-web-api`, `track/b-worker-storage`, `track/c-platform` |
| **10:15–12:30** | 2:15 | **Sprint 1 — Upgrade** | All three tracks run the **Upgrade** extension against their scope: framework, `.csproj` modernization, NuGet/CVE fixes, `Web.config` → `appsettings.json`, ADO.NET → EF Core (Track A), Windows Service → Worker Service (Track B), Track C scaffolds `azd init` and starts the containerization plan |
| **12:30–13:15** | 0:45 | **Lunch + checkpoint 1** | 15-min check-in over lunch: each pair reports in 2 min; teams rebase against `main`; facilitator resolves cross-track conflicts (shared `Data` project especially) |
| **13:15–15:15** | 2:00 | **Sprint 2 — Azure migration** | Run the **Azure migration** extension. Track A: Managed Identity for Azure SQL, App Insights, replace remaining `log4net` with `ILogger`. Track B: local files → Blob Storage, health checks. Track C: Bicep (Container Apps, Azure SQL, Key Vault, ACR, App Insights), Dockerfiles, GitHub Actions workflow, wire secrets via Key Vault references |
| **15:15–15:30** | 0:15 | **Checkpoint 2** | Facilitator-led. Everyone commits, rebases, and merges to a `integration` branch. Fix any conflicts as a team. |
| **15:30–16:15** | 0:45 | **Deploy** | `azd up` into the shared RG. First deploy will fail — that's expected. Diagnose with Copilot Chat + `azd` logs. Do smoke test: sign in, list claims, upload a doc, watch the worker export appear in Blob, check App Insights traces |
| **16:15–16:45** | 0:30 | **Hardening pass** | Fix whatever the smoke test surfaced. Add missing checklist items. Merge `integration` → `main`. |
| **16:45–17:15** | 0:30 | **Team demo** | Each track drives a 6–7 minute demo of what they modernized, showing Copilot prompts that worked and diffs that mattered. |
| **17:15–17:30** | 0:15 | **Retro + wrap** | 3 keeps, 3 changes, 3 learnings. Commit `docs/learnings.md`. Optional: log stretch goals as issues for the next sprint. |

---

## Checkpoints (what "on track" looks like)

**Checkpoint 1 — end of Sprint 1 (12:30):**
- [ ] All projects target `net9.0` and are SDK-style
- [ ] `packages.config` is gone everywhere
- [ ] No High/Critical CVEs remaining in `dotnet list package --vulnerable`
- [ ] `appsettings.json` scaffolded for Web, Services, Worker
- [ ] EF Core `ContosoDbContext` compiles; at least `GetRecent` migrated
- [ ] Track C has `azure.yaml`, an empty `infra/` folder, and Docker plan drafted

**Checkpoint 2 — pre-deploy (15:15):**
- [ ] All three branches merged into `integration`; solution builds
- [ ] Managed Identity code path in place (no SQL password in config)
- [ ] Blob Storage client wired for uploads and exports (behind config flag)
- [ ] App Insights connection string flows via config; `ILogger` writes to it
- [ ] Bicep provisions: Container Apps env, ACR, Azure SQL, Key Vault, App Insights, Log Analytics, managed identity, role assignments
- [ ] Dockerfiles present for Web, Services (or its replacement), Worker
- [ ] GitHub Actions workflow drafted (build + `azd deploy` on push to main)

**Post-deploy (16:15):**
- [ ] Web app reachable, login works, claims list renders
- [ ] Upload → file lands in Blob
- [ ] Worker → export file lands in Blob within one interval
- [ ] Traces / requests visible in App Insights
- [ ] No secrets in appsettings; all sensitive values flow via Managed Identity or Key Vault
