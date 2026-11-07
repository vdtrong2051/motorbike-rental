param(
  [string]$RepositoryPath = 'E:\Projects\motorbike-rental',
  [string]$BaseUrl = 'http://127.0.0.1:5119',
  [Parameter(Mandatory=$true)][string]$FixturePath,
  [switch]$InstallDependencies
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $RepositoryPath)) { throw "Repository not found: $RepositoryPath" }
if (!(Test-Path -LiteralPath $FixturePath)) { throw "QA fixture JSON not found: $FixturePath" }
Push-Location $RepositoryPath
try {
  $env:GD4_BASE_URL=$BaseUrl
  $env:GD4_FIXTURE_PATH=(Resolve-Path -LiteralPath $FixturePath).Path
  $env:GD4_CAPTURE='1'
  if ($InstallDependencies) {
    npm install --no-save --no-package-lock playwright@1.56.1
    if ($LASTEXITCODE -ne 0) { throw 'npm install playwright failed' }
    npx playwright install chromium
    if ($LASTEXITCODE -ne 0) { throw 'Chromium install failed' }
  }
  node scripts/qa/gd4-live-audit.mjs
  if ($LASTEXITCODE -ne 0) { throw 'GD4.6 live browser audit FAILED. See artifacts/gd4-live/report.json' }
  Write-Host 'GD4.6 LIVE AUDIT PASS. Read artifacts/gd4-live/report.json and screenshots.' -ForegroundColor Green
} finally { Pop-Location }
