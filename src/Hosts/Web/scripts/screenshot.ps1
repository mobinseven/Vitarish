param(
  [string]$Url = "http://localhost:3000/home",
  [string]$OutputPath = "artifacts/home-dashboard-proof/home-dashboard-latest.png",
  [string]$ViewportSize = "1440,1000",
  [int]$WaitMilliseconds = 500
)

$webRoot = Split-Path -Parent $PSScriptRoot
$playwright = Join-Path $webRoot "node_modules/.bin/playwright.cmd"
$output = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $webRoot $OutputPath }

if (-not (Test-Path -LiteralPath $playwright)) {
  throw "Playwright was not found at $playwright. Run npm install from the Web host first."
}

New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null

& $playwright screenshot `
  --viewport-size $ViewportSize `
  --wait-for-timeout $WaitMilliseconds `
  --full-page `
  $Url `
  $output

if ($LASTEXITCODE -ne 0) {
  throw "Screenshot capture failed with exit code $LASTEXITCODE. Is the app running at $Url?"
}

Write-Host "Screenshot saved to $output"
