# Pre-read — Legacy Breakers Hackathon

**Read this before the event.** It won't take long.

---

## 1. The mission

Modernize `ContosoInsurance` — a real legacy .NET Framework 4.6.1 solution
(WebForms + WCF + Windows Service + ADO.NET + SQL Server) — to a
**.NET 9 + Azure Container Apps** stack in one day, using:

- **GitHub Copilot app modernization for .NET** (VS Code extension)
- **The Modernization CLI** (`appcat` / `dotnet-appmod` tooling, via the extension)
- **Azure Developer CLI (`azd`)** for infra + deploy
- Regular Copilot Chat / Edits / Agent mode as your co-pilot

---

## 2. Install checklist

Do this **before** the event. Bring a working laptop.

| Item | How |
| --- | --- |
| Windows 10/11 or macOS/Linux (Windows preferred — legacy solution files) | — |
| Visual Studio Code (latest) | https://code.visualstudio.com |
| GitHub Copilot + Copilot Chat extensions | VS Code Marketplace, sign in with GitHub |
| **GitHub Copilot app modernization — upgrade for .NET** extension | VS Code Marketplace, search "GitHub Copilot app modernization .NET" |
| **GitHub Copilot app modernization — Azure migration for .NET** extension | VS Code Marketplace |
| .NET SDK 9 | `winget install Microsoft.DotNet.SDK.9` |
| Azure Developer CLI (`azd`) | `winget install Microsoft.Azd` |
| Azure CLI (`az`) | `winget install Microsoft.AzureCLI` |
| Docker Desktop | https://www.docker.com/products/docker-desktop |
| Git | `winget install Git.Git` |
| Azure subscription access | Sign in with `az login` and `azd auth login` |

**Verify:**

```powershell
dotnet --version         # 9.x
azd version
az account show
docker version
code --list-extensions | Select-String -Pattern "appmod|copilot"
```

You do **not** need Visual Studio, SQL Server, or IIS. The legacy app does not
need to build locally — the modernization tools work from source.

---

## 3. The mental model

`GitHub Copilot app modernization for .NET` gives you two complementary things:

1. **Assess** — scan the solution, produce a **findings/plan** with grouped
   tasks: framework upgrade, package/CVE issues, config, data, hosting, cloud
   readiness. Think "static analyzer + migration planner + Copilot".
2. **Apply** — execute individual tasks. Each task is an agent run that:
   - reads the plan,
   - proposes code edits,
   - opens a diff you review,
   - moves to the next task.

Two flavors of the extension are shipped as separate installs today:

- **Upgrade** — .NET Fx → .NET (n). Handles project files, NuGet migration,
  API surface changes, `Web.config` → `appsettings.json`, WebForms/WCF
  transformations.
- **Azure migration** — code-level and infra-level cloud readiness:
  Managed Identity, Key Vault, Blob Storage, App Insights, containerization,
  Bicep generation, GitHub Actions.

Chain them: **Upgrade first, then Azure migration.**

---

## 4. What "modernization" means in this hackathon

Every one of these must be visibly done in the final PR:

| Concern | Before | After |
| --- | --- | --- |
| Framework | .NET Fx 4.6.1 | .NET 9 |
| Project files | `packages.config` + old `.csproj` | SDK-style `.csproj` + `PackageReference` |
| Packages / CVEs | `log4net 2.0.8`, `Newtonsoft.Json 11.0.2` | up-to-date, no known CVEs |
| Config | `Web.config` / `App.config` | `appsettings.json` + `IConfiguration` |
| Data access | Raw ADO.NET | **EF Core 9** |
| Logging | `log4net` + `Trace` | `ILogger<T>` + Application Insights |
| Local files | `C:\ClaimsFiles`, `C:\Exports` | Azure Blob Storage |
| Secrets | Plain SQL user/password in config | **Managed Identity** + Key Vault |
| Web hosting | IIS + WebForms | Kestrel + ASP.NET Core (Razor Pages / MVC) *(stretch: Blazor)* |
| Service hosting | Windows Service | .NET Generic Host Worker Service |
| WCF SOAP | `System.ServiceModel` | ASP.NET Core minimal API *(stretch: CoreWCF)* |
| Container | none | Dockerfile |
| Infra | manual | Bicep via `azd` |
| CI/CD | none | GitHub Actions |

---

## 5. Tracks (who owns what)

You are one team of 6 sharing one repo. To parallelize without stepping on
each other, we split into **three pairs**, each owning a vertical slice on
its own feature branch. Details are in `docs/task-briefs.md`.

| Track | Pair | Owns (folders) |
| --- | --- | --- |
| **A — Web + API tier** | 2 people | `ContosoInsurance.Web/`, `ContosoInsurance.Services/`, `ContosoInsurance.Data/` (jointly with B for DbContext) |
| **B — Worker + Storage** | 2 people | `ContosoInsurance.Worker/`, Blob wiring, `ContosoInsurance.Data/` shared |
| **C — Platform** | 2 people | Containerization, Bicep, `azd`, Managed Identity, Key Vault, App Insights, GitHub Actions |

`ContosoInsurance.Common` and `ContosoInsurance.Data` are shared — coordinate at
each checkpoint.

---

## 6. Ground rules

- **Copilot-first.** Use the modernization extension for anything it supports.
  Fall back to Copilot Chat / Agent mode for freeform code changes. Hand-editing
  is a last resort — and note it in your team log.
- **Diffs are reviewed.** Every agent-generated diff gets a real look before
  accept. If Copilot produces something wrong, capture the prompt + output in
  `docs/learnings.md` (create it as you go).
- **Small commits.** One task = one commit ideally. Push often to your branch.
- **Sync at every checkpoint.** Rebase / merge `main` at the times listed on
  the agenda.
- **Ask before rewriting.** Don't rewrite another track's project without a Slack
  message to the pair that owns it.
- **Time-box.** Stretch goals (WebForms → Blazor, WCF → CoreWCF, Entra ID auth)
  only after the core checklist is green.

---

## 7. Suggested pre-event reading (10–15 min each)

- GitHub Copilot app modernization for .NET — overview and workflow (Microsoft Learn)
- `azd` — how `azd init`, `azd up`, `azd deploy` fit together
- Managed identity connections to Azure SQL (SqlClient + `DefaultAzureCredential`)
- EF Core 9 upgrade patterns from ADO.NET
- Application Insights with `ILogger` and OpenTelemetry

You do not need to memorize any of this. The point is to have seen it once so
the vocabulary lands during the hackathon.

---

## 8. What you'll leave with

- A PR (or merged `main`) taking `ContosoInsurance` from .NET Fx 4.6.1 to
  .NET 9, deployed to your shared Azure resource group.
- A `learnings.md` capturing prompts that worked, prompts that didn't,
  gotchas hit, and where Copilot needed a nudge.
- A short team demo.
