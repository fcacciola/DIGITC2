param(
  [string]$Configuration = "Release",
  [string]$Output = "$PSScriptRoot\publish"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$client = Join-Path $PSScriptRoot "Client"
$server = Join-Path $PSScriptRoot "Server"
$wwwroot = Join-Path $server "wwwroot"

Push-Location $client
try {
  npm.cmd install
  npm.cmd run build
}
finally {
  Pop-Location
}

if (Test-Path $wwwroot) {
  Remove-Item -LiteralPath $wwwroot -Recurse -Force
}

New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item -Path (Join-Path $client "dist\*") -Destination $wwwroot -Recurse -Force

dotnet publish (Join-Path $server "DigitC2.Server.csproj") `
  --configuration $Configuration `
  --output $Output

Write-Host ""
Write-Host "Published to: $Output"
Write-Host "Set the shared password before running in production, for example:"
Write-Host '$env:DigitC2__SharedPassword="your-shared-password"'
Write-Host "dotnet DigitC2.Server.dll --urls http://0.0.0.0:5188"
