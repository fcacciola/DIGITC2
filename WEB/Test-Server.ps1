param(
  [Parameter(Mandatory = $true)]
  [string] $InputFile,

  [string] $SessionName,

  [string] $ServerUrl = "http://127.0.0.1:5188",

  [switch] $StartServer,

  [string] $DownloadFolder = ""
)

$ErrorActionPreference = "Stop"

function Join-UrlPath {
  param(
    [string] $BaseUrl,
    [string] $Path
  )

  return $BaseUrl.TrimEnd("/") + "/" + $Path.TrimStart("/")
}

function Get-VisibleFiles {
  param(
    [object[]] $Nodes
  )

  foreach ($node in $Nodes) {
    if ($node.kind -eq "file") {
      $node
    }

    if ($null -ne $node.children) {
      Get-VisibleFiles -Nodes $node.children
    }
  }
}

$resolvedInput = Resolve-Path -LiteralPath $InputFile
$inputItem = Get-Item -LiteralPath $resolvedInput.Path
$extension = $inputItem.Extension

if ($extension -notin @(".wav", ".txt")) {
  throw "Only .wav and .txt files are supported. Got: $extension"
}

if ([string]::IsNullOrWhiteSpace($SessionName)) {
  $SessionName = $inputItem.BaseName
}

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$serverProcess = $null

try {
  $healthUrl = Join-UrlPath $ServerUrl "health"

  if ($StartServer) {
    Write-Host "Starting server at $ServerUrl ..."
    $serverProject = Join-Path $repoRoot "WEB\Server\DigitC2.Server.csproj"
    $serverProcess = Start-Process `
      -FilePath "dotnet" `
      -ArgumentList @("run", "--project", $serverProject, "--urls", $ServerUrl) `
      -WorkingDirectory $repoRoot `
      -WindowStyle Hidden `
      -PassThru

    $ready = $false

    for ($attempt = 1; $attempt -le 30; $attempt++) {
      if ($serverProcess.HasExited) {
        throw "Server process exited before becoming healthy. Try running dotnet run manually to see startup output."
      }

      try {
        $health = Invoke-RestMethod -Uri $healthUrl -Method Get
        if ($health.status -eq "ok") {
          $ready = $true
          break
        }
      }
      catch {
        Start-Sleep -Seconds 1
      }
    }

    if (-not $ready) {
      throw "Server did not become healthy at $healthUrl."
    }
  }
  else {
    try {
      $health = Invoke-RestMethod -Uri $healthUrl -Method Get
      if ($health.status -ne "ok") {
        throw "Unexpected health response."
      }
    }
    catch {
      throw "Server is not reachable at $healthUrl. Start it first, or run this script with -StartServer."
    }
  }

  Write-Host "Uploading $($resolvedInput.Path) ..."
  $uploadUrl = Join-UrlPath $ServerUrl "api/jobs"
  $responseJson = curl.exe -s -F "file=@$($resolvedInput.Path)" -F "name=$SessionName" $uploadUrl
  if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($responseJson)) {
    throw "Upload failed. Server did not return a response from $uploadUrl."
  }

  $response = $responseJson | ConvertFrom-Json

  if ($null -ne $response.error) {
    throw $response.error
  }

  if ([string]::IsNullOrWhiteSpace($response.resultUrl)) {
    throw "Upload response did not include a resultUrl. Raw response: $responseJson"
  }

  Write-Host ""
  Write-Host "Job created:"
  $response | ConvertTo-Json -Depth 8

  $resultUrl = Join-UrlPath $ServerUrl $response.resultUrl
  $manifest = Invoke-RestMethod -Uri $resultUrl -Method Get

  Write-Host ""
  Write-Host "Result manifest:"
  $manifest | ConvertTo-Json -Depth 20

  if ([string]::IsNullOrWhiteSpace($DownloadFolder)) {
    $DownloadFolder = Join-Path $repoRoot "WEB\Server\App_Data\SmokeDownloads\$($response.jobId)"
  }

  New-Item -ItemType Directory -Force -Path $DownloadFolder | Out-Null

  $files = @(Get-VisibleFiles -Nodes $manifest.files)
  foreach ($file in $files) {
    $destination = Join-Path $DownloadFolder $file.relativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Invoke-WebRequest -Uri $file.url -OutFile $destination
  }

  Write-Host ""
  Write-Host "Downloaded $($files.Count) visible result file(s) to:"
  Write-Host $DownloadFolder
}
finally {
  if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
    Write-Host ""
    Write-Host "Stopping server process $($serverProcess.Id) ..."
    Stop-Process -Id $serverProcess.Id -Force
  }
}
