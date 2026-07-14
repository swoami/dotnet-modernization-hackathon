<#
.SYNOPSIS
  Scans the repository (remote track branches preferred) and auto-detects
  hackathon task completion. Writes docs/tracker-status.js consumed by
  docs/tracker.html.

.EXAMPLE
  pwsh docs/scan-progress.ps1                 # single scan of current refs
  pwsh docs/scan-progress.ps1 -Watch -Fetch   # facilitator mode: fetch + rescan every 60s
#>
param(
    [switch]$Watch,
    [int]$IntervalSeconds = 60,
    [switch]$Fetch,
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"
Push-Location $RepoRoot
try {

$S   = "src/ContosoInsurance"
$WEB = "$S/ContosoInsurance.Web"
$SVC = "$S/ContosoInsurance.Services"
$DAT = "$S/ContosoInsurance.Data"
$COM = "$S/ContosoInsurance.Common"
$WRK = "$S/ContosoInsurance.Worker"

function Test-GitRef([string]$Ref) {
    git rev-parse --verify --quiet "$Ref^{commit}" *> $null
    return ($LASTEXITCODE -eq 0)
}

function Resolve-TrackRef([string]$Branch) {
    foreach ($c in @("origin/$Branch", $Branch, "origin/integration", "integration", "origin/main", "main", "HEAD")) {
        if (Test-GitRef $c) { return $c }
    }
    return "HEAD"
}

$script:TreeCache = @{}
function Get-Tree([string]$Ref) {
    if (-not $script:TreeCache.ContainsKey($Ref)) {
        $script:TreeCache[$Ref] = @(git ls-tree -r --name-only $Ref 2>$null)
    }
    return $script:TreeCache[$Ref]
}

function Test-FileAt([string]$Ref, [string]$Path) {
    return ((Get-Tree $Ref) -contains $Path)
}

function Test-AnyAt([string]$Ref, [string]$Regex) {
    return [bool]((Get-Tree $Ref) -match $Regex)
}

# Case-insensitive regex grep at a ref, restricted to pathspecs.
function Test-GrepAt([string]$Ref, [string]$Pattern, [string[]]$Paths) {
    git grep -q -i -E $Pattern $Ref -- @Paths *> $null
    return ($LASTEXITCODE -eq 0)
}

function Test-SdkNet9([string]$Ref, [string]$Csproj) {
    if (-not (Test-FileAt $Ref $Csproj)) { return $false }
    $c = (git show "${Ref}:${Csproj}" 2>$null) -join "`n"
    return ($c -match '<Project\s+Sdk=') -and ($c -match '<TargetFramework>net9')
}

function Invoke-Scan {
    $script:TreeCache = @{}

    $RA = Resolve-TrackRef "track/a-web-api"
    $RB = Resolve-TrackRef "track/b-worker-storage"
    $RC = Resolve-TrackRef "track/c-platform"
    # Team-level ref for checkpoints: integration first, then main.
    $RM = $null
    foreach ($c in @("origin/integration", "integration", "origin/main", "main", "HEAD")) {
        if (Test-GitRef $c) { $RM = $c; break }
    }

    $t = [ordered]@{}

    # ---------- Track A ----------
    $t["A2"]  = (Test-SdkNet9 $RA "$WEB/ContosoInsurance.Web.csproj") -and
                (Test-SdkNet9 $RA "$SVC/ContosoInsurance.Services.csproj")
    $t["A3"]  = -not (Test-AnyAt $RA 'ContosoInsurance\.(Web|Services)/packages\.config$')
    $t["A4"]  = (-not (Test-GrepAt $RA 'log4net' @("$S/*.csproj"))) -and
                ((-not (Test-GrepAt $RA 'Newtonsoft\.Json' @("$S/*.csproj"))) -or
                 (Test-GrepAt $RA 'Newtonsoft\.Json"\s+Version="(1[3-9]|[2-9][0-9])\.' @("$S/*.csproj")))
    $t["A5"]  = (Test-FileAt $RA "$WEB/appsettings.json") -and (-not (Test-FileAt $RA "$WEB/Web.config"))
    $t["A6"]  = ((Test-AnyAt $RA 'ContosoInsurance\.Web/Pages/.*\.cshtml$') -or
                 (Test-AnyAt $RA 'ContosoInsurance\.Web/Components/Pages/.*\.razor$')) -and
                (-not (Test-AnyAt $RA 'ContosoInsurance\.Web/.*\.aspx$'))
    $t["A7"]  = (-not (Test-AnyAt $RA 'ContosoInsurance\.Services/.*\.svc$')) -and
                (Test-GrepAt $RA 'MapPost' @("$SVC/*.cs"))
    $t["A8"]  = Test-GrepAt $RA 'class\s+ContosoDbContext' @("$S/*.cs")
    $t["A9"]  = (Test-GrepAt $RA 'AddAuthentication' @("$WEB/*.cs")) -and
                (Test-GrepAt $RA 'AddCookie' @("$WEB/*.cs"))
    $t["A10"] = (Test-GrepAt $RA 'Active Directory Default' @("$WEB/appsettings*.json")) -and
                (-not (Test-GrepAt $RA 'Password=' @("$WEB/appsettings*.json")))
    $t["A11"] = Test-GrepAt $RA 'AddApplicationInsightsTelemetry' @("$S/*.cs")
    $t["A12"] = -not (Test-GrepAt $RA 'Trace\.(WriteLine|TraceWarning|TraceError)' @("$S/*.cs"))
    $t["A13"] = Test-GrepAt $RA 'MapHealthChecks|AddHealthChecks' @("$WEB/*.cs")

    $t["A-D1"] = (Test-SdkNet9 $RA "$WEB/ContosoInsurance.Web.csproj") -and
                 (Test-SdkNet9 $RA "$SVC/ContosoInsurance.Services.csproj") -and
                 (Test-SdkNet9 $RA "$DAT/ContosoInsurance.Data.csproj") -and
                 (Test-SdkNet9 $RA "$COM/ContosoInsurance.Common.csproj")
    $t["A-D2"] = (-not (Test-AnyAt $RA 'packages\.config$')) -and (-not (Test-AnyAt $RA '/Web\.config$'))
    $t["A-D3"] = Test-FileAt $RA "$WEB/Program.cs"
    $t["A-D4"] = $t["A8"] -and (-not (Test-GrepAt $RA 'SqlCommand' @("$DAT/*.cs")))
    $t["A-D5"] = $t["A8"] -and (-not (Test-GrepAt $RA '\+\s*namePart\s*\+' @("$DAT/*.cs")))
    $t["A-D6"] = -not (Test-GrepAt $RA 'SHA1' @("$S/*.cs"))
    $t["A-D7"] = -not (Test-GrepAt $RA 'log4net' @("$S"))
    $t["A-D8"] = $t["A7"] -and (-not (Test-GrepAt $RA 'ChannelFactory' @("$WEB/*.cs")))

    # ---------- Track B ----------
    $t["B2"]  = Test-SdkNet9 $RB "$WRK/ContosoInsurance.Worker.csproj"
    $t["B3"]  = (-not (Test-GrepAt $RB 'ServiceBase|ProjectInstaller' @("$WRK/*.cs"))) -and
                (-not (Test-AnyAt $RB 'ContosoInsurance\.Worker/ProjectInstaller\.cs$'))
    $t["B4"]  = Test-GrepAt $RB 'BackgroundService' @("$WRK/*.cs")
    $t["B5"]  = Test-GrepAt $RB 'PeriodicTimer' @("$WRK/*.cs")
    $t["B6"]  = (Test-FileAt $RB "$WRK/appsettings.json") -and (-not (Test-FileAt $RB "$WRK/App.config"))
    $t["B7"]  = Test-GrepAt $RB 'ILogger' @("$WRK/*.cs")
    $t["B8"]  = (Test-GrepAt $RB 'Azure\.Storage\.Blobs' @("$WRK/*.csproj")) -and
                (Test-GrepAt $RB 'Azure\.Identity' @("$WRK/*.csproj"))
    $t["B9"]  = (Test-GrepAt $RB 'claim-exports' @("$S")) -and
                (Test-GrepAt $RB 'Upload(Blob)?Async' @("$WRK/*.cs"))
    $t["B10"] = (Test-GrepAt $RB 'IClaimDocumentStore' @("$S/*.cs")) -and
                (Test-GrepAt $RB 'claim-docs' @("$S"))
    $t["B11"] = Test-GrepAt $RB 'AddApplicationInsightsTelemetryWorkerService' @("$WRK/*.cs")
    $t["B12"] = (Test-GrepAt $RB 'DbSet<ExportLog>' @("$S/*.cs")) -and
                (Test-GrepAt $RB 'ExportLog' @("$WRK/*.cs"))

    $t["B-D1"] = $t["B2"] -and $t["B4"]
    $t["B-D2"] = $t["B3"] -and (-not (Test-GrepAt $RB 'System\.Timers' @("$WRK/*.cs")))
    $t["B-D3"] = $t["B6"]
    $t["B-D4"] = $t["B9"]
    $t["B-D5"] = Test-GrepAt $RB 'claim-docs' @("$S")
    $t["B-D6"] = $t["B12"]
    $t["B-D7"] = $t["B7"] -and (-not (Test-GrepAt $RB 'log4net' @("$WRK")))

    # ---------- Track C ----------
    $t["C1"]  = (Test-FileAt $RC "azure.yaml") -or (Test-FileAt $RC "$S/azure.yaml")
    $t["C2"]  = (Test-AnyAt $RC 'ContosoInsurance\.Web/Dockerfile$') -and
                (Test-AnyAt $RC 'ContosoInsurance\.Worker/Dockerfile$')
    $t["C3"]  = (Test-AnyAt $RC 'infra/main\.bicep$') -and
                (Test-GrepAt $RC 'Microsoft\.App|containerapp' @("*.bicep")) -and
                (Test-GrepAt $RC 'Microsoft\.Sql|sqlServer' @("*.bicep")) -and
                (Test-GrepAt $RC 'storageAccount|Microsoft\.Storage' @("*.bicep"))
    $t["C4"]  = Test-GrepAt $RC 'APPLICATIONINSIGHTS_CONNECTION_STRING' @("*.bicep")
    $t["C5"]  = Test-GrepAt $RC 'database\.windows\.net|SQL_CONNECTION|Active Directory Default' @("*.bicep")
    $t["C6"]  = Test-GrepAt $RC 'STORAGE_ACCOUNT|storageAccountName' @("*.bicep")
    $t["C7"]  = Test-GrepAt $RC 'keyVaultUrl' @("*.bicep")
    $t["C8"]  = (Test-AnyAt $RC '\.github/workflows/.*\.ya?ml$') -and
                (Test-GrepAt $RC 'azd (deploy|up)' @(".github/workflows")) -and
                (Test-GrepAt $RC 'id-token' @(".github/workflows"))
    $t["C9"]  = Test-AnyAt $RC 'docs/README-DEPLOY\.md$'

    $t["C-D1"] = $t["C1"]
    $t["C-D2"] = (Test-AnyAt $RC 'infra/main\.bicep$') -and (Test-AnyAt $RC 'infra/main\.bicepparam$')
    $t["C-D3"] = $t["C2"]
    $t["C-D4"] = Test-GrepAt $RC 'userAssignedIdentities|UserAssigned' @("*.bicep")
    $t["C-D5"] = Test-GrepAt $RC 'Microsoft\.Authorization/roleAssignments|roleDefinitionId' @("*.bicep")
    $t["C-D6"] = (Test-GrepAt $RC 'Microsoft\.OperationalInsights|logAnalytics' @("*.bicep")) -and
                 (Test-GrepAt $RC 'Microsoft\.Insights/components|applicationInsights' @("*.bicep"))
    $t["C-D7"] = (Test-AnyAt $RC 'infra/.*\.bicep$') -and
                 (-not (Test-GrepAt $RC 'Password=' @("*.bicep", "azure.yaml", ".github")))
    $t["C-D8"] = $t["C8"]
    $t["C-D9"] = $t["C9"]

    # ---------- Checkpoints (team-level ref: integration -> main) ----------
    $t["CP1-1"] = (Test-SdkNet9 $RM "$WEB/ContosoInsurance.Web.csproj") -and
                  (Test-SdkNet9 $RM "$SVC/ContosoInsurance.Services.csproj") -and
                  (Test-SdkNet9 $RM "$DAT/ContosoInsurance.Data.csproj") -and
                  (Test-SdkNet9 $RM "$COM/ContosoInsurance.Common.csproj") -and
                  (Test-SdkNet9 $RM "$WRK/ContosoInsurance.Worker.csproj")
    $t["CP1-2"] = -not (Test-AnyAt $RM 'packages\.config$')
    $t["CP1-4"] = (Test-FileAt $RM "$WEB/appsettings.json") -and
                  (Test-FileAt $RM "$SVC/appsettings.json") -and
                  (Test-FileAt $RM "$WRK/appsettings.json")
    $t["CP1-5"] = Test-GrepAt $RM 'class\s+ContosoDbContext' @("$S/*.cs")
    $t["CP1-6"] = ((Test-FileAt $RM "azure.yaml") -or (Test-FileAt $RM "$S/azure.yaml")) -and
                  (Test-AnyAt $RM '(^|/)infra/')
    $t["CP2-2"] = (Test-GrepAt $RM 'Active Directory Default' @("$S")) -and
                  (-not (Test-GrepAt $RM 'Password=' @("$S/*/appsettings*.json")))
    $t["CP2-3"] = Test-GrepAt $RM 'BlobServiceClient' @("$S/*.cs")
    $t["CP2-4"] = Test-GrepAt $RM 'AddApplicationInsightsTelemetry' @("$S/*.cs")
    $t["CP2-5"] = (Test-AnyAt $RM 'infra/main\.bicep$') -and
                  (Test-GrepAt $RM 'Microsoft\.App|containerapp' @("*.bicep"))
    $t["CP2-6"] = (Test-AnyAt $RM 'ContosoInsurance\.Web/Dockerfile$') -and
                  (Test-AnyAt $RM 'ContosoInsurance\.Worker/Dockerfile$')
    $t["CP2-7"] = Test-AnyAt $RM '\.github/workflows/.*\.ya?ml$'
    # Not auto-detectable (left manual in the tracker):
    # A1, B1 (assess runs), CP1-3 (CVE scan), CP2-1 (solution builds), CP3-* (runtime smoke tests)

    $payload = [ordered]@{
        generatedAt = (Get-Date).ToUniversalTime().ToString("o")
        refs        = [ordered]@{ A = $RA; B = $RB; C = $RC; team = $RM }
        tasks       = $t
    }
    $js = "window.REPO_STATUS = " + ($payload | ConvertTo-Json -Depth 4) + ";"
    Set-Content -Path (Join-Path $RepoRoot "docs/tracker-status.js") -Value $js -Encoding UTF8

    $done = @($t.Values | Where-Object { $_ }).Count
    Write-Host ("[{0}] scanned A={1} B={2} C={3} team={4} -> {5}/{6} auto-tasks detected done" -f `
        (Get-Date -Format "HH:mm:ss"), $RA, $RB, $RC, $RM, $done, $t.Count)
}

do {
    if ($Fetch) { git fetch origin --quiet 2>$null }
    Invoke-Scan
    if ($Watch) { Start-Sleep -Seconds $IntervalSeconds }
} while ($Watch)

} finally {
    Pop-Location
}

# Explicitly force a clean exit code. Several helper functions above (Test-GitRef,
# Test-GrepAt, Test-AnyAt, etc.) intentionally call git in ways that can leave
# $LASTEXITCODE non-zero even on a fully successful scan (e.g. "ref not found" or
# "no grep match" are valid negative results, not errors). When GitHub Actions runs
# this script via `shell: pwsh` it invokes `pwsh -Command ". 'script.ps1'"`, and pwsh
# can surface that stale $LASTEXITCODE as the process exit code, intermittently
# failing the job even though the scan completed correctly. Real failures still throw
# (ErrorActionPreference = Stop) and never reach this line.
exit 0
