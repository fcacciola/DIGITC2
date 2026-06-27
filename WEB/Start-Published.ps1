param(
  [string]$PublishDir = "$PSScriptRoot\publish",
  [string]$WorkspaceRoot = "$PSScriptRoot\data",
  [string]$SharedPassword = $env:DigitC2__SharedPassword,
  [int]$Port = 5188
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $PublishDir "DigitC2.Server.dll"))) {
  throw "Published server was not found in [$PublishDir]. Run WEB\Publish-Web.ps1 first."
}

if ([string]::IsNullOrWhiteSpace($SharedPassword)) {
  throw "Shared password is required. Pass -SharedPassword or set `$env:DigitC2__SharedPassword."
}

New-Item -ItemType Directory -Force -Path $WorkspaceRoot | Out-Null

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DigitC2__SharedPassword = $SharedPassword
$env:DigitC2__WorkspaceRoot = $WorkspaceRoot
$env:DigitC2__MaxUploadBytes = "104857600"
$env:DigitC2__JobRetentionDays = "14"
$env:DigitC2__CleanupIntervalMinutes = "60"

Push-Location $PublishDir
try {
  Write-Host "Transgraphier 2.4.1"
  Write-Host "Listening on: http://0.0.0.0:$Port"
  Write-Host "Local URL:    http://localhost:$Port"
  Write-Host "Data folder:  $WorkspaceRoot"
  dotnet DigitC2.Server.dll --urls "http://0.0.0.0:$Port"
}
finally {
  Pop-Location
}
