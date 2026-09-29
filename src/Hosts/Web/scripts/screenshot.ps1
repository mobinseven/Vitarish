param(
  [string]$Url = "http://localhost:3000/home",
  [string]$OutputPath = "artifacts/home-dashboard-proof/home-dashboard-latest.png",
  [string]$ViewportSize = "1440,1000",
  [int]$WaitMilliseconds = 500,
  [int]$TimeoutMilliseconds = 30000,
  [string]$WaitForSelector,
  [string]$StorageStatePath,
  [switch]$IgnoreHttpsErrors
)

$webRoot = Split-Path -Parent $PSScriptRoot
$playwright = Join-Path $webRoot "node_modules/.bin/playwright.cmd"
$output = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $webRoot $OutputPath }

if (-not (Test-Path -LiteralPath $playwright)) {
  throw "Playwright was not found at $playwright. Run npm install from the Web host first."
}

New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null

$screenshotArguments = @(
  "--viewport-size=$ViewportSize",
  "--wait-for-timeout=$WaitMilliseconds",
  "--timeout=$TimeoutMilliseconds",
  "--full-page"
)

if ($WaitForSelector) {
  $screenshotArguments += @("--wait-for-selector", $WaitForSelector)
}

if ($StorageStatePath) {
  $screenshotArguments += @("--load-storage", $StorageStatePath)
}

if ($IgnoreHttpsErrors) {
  $screenshotArguments += "--ignore-https-errors"
}

$screenshotArguments += @($Url, $output)
& $playwright screenshot @screenshotArguments

if ($LASTEXITCODE -ne 0) {
  throw "Screenshot capture failed with exit code $LASTEXITCODE. Is the app running at $Url?"
}

Write-Host "Screenshot saved to $output"
