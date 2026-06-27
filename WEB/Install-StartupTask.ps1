param(
  [Parameter(Mandatory = $true)]
  [string]$SharedPassword,

  [string]$TaskName = "Transgraphier 2.4.1",
  [string]$PublishDir = "$PSScriptRoot\publish",
  [string]$WorkspaceRoot = "$PSScriptRoot\data",
  [int]$Port = 5188,
  [switch]$AtStartup
)

$ErrorActionPreference = "Stop"

$localHostDir = Join-Path $PSScriptRoot "LocalHost"
$runner = Join-Path $localHostDir "Run-Transgraphier.ps1"
New-Item -ItemType Directory -Force -Path $localHostDir | Out-Null

$escapedPublishDir = $PublishDir.Replace("'", "''")
$escapedWorkspaceRoot = $WorkspaceRoot.Replace("'", "''")
$escapedSharedPassword = $SharedPassword.Replace("'", "''")

@"
`$ErrorActionPreference = "Stop"
& "$PSScriptRoot\Start-Published.ps1" -PublishDir '$escapedPublishDir' -WorkspaceRoot '$escapedWorkspaceRoot' -SharedPassword '$escapedSharedPassword' -Port $Port
"@ | Set-Content -LiteralPath $runner -Encoding UTF8

$trigger = if ($AtStartup) {
  New-ScheduledTaskTrigger -AtStartup
} else {
  New-ScheduledTaskTrigger -AtLogOn
}

$action = New-ScheduledTaskAction `
  -Execute "powershell.exe" `
  -Argument "-ExecutionPolicy Bypass -NoProfile -File `"$runner`"" `
  -WorkingDirectory $PSScriptRoot

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel LeastPrivilege
$settings = New-ScheduledTaskSettingsSet -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask `
  -TaskName $TaskName `
  -Action $action `
  -Trigger $trigger `
  -Principal $principal `
  -Settings $settings `
  -Force | Out-Null

Write-Host "Installed scheduled task: $TaskName"
Write-Host "Runner file: $runner"
Write-Host "Start it now with:"
Write-Host "Start-ScheduledTask -TaskName `"$TaskName`""
