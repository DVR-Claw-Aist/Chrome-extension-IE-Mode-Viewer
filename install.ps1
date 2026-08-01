param(
    [string]$ExtensionId = "",
    [string]$DotNetPath = "",
    [switch]$Standalone,
    [switch]$Autostart
)

$ErrorActionPreference = "Stop"
$hostName = "com.chrom_ext.ie_host"
$rid = "win-x86"
$tfm = "net11.0-windows"

if ([string]::IsNullOrEmpty($ExtensionId)) {
    if ($Standalone) {
        $ExtensionId = "standalone"
    } else {
        $ExtensionId = Read-Host "Enter extension ID (copy from popup or chrome://extensions)"
        if ([string]::IsNullOrEmpty($ExtensionId)) { Write-Error "Extension ID required"; exit 1 }
    }
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj   = Join-Path $repoRoot "native\IEHost\IEHost.csproj"
$pubDir   = Join-Path $repoRoot "native\IEHost\bin\Release\$tfm\$rid\publish"
$exePub   = Join-Path $pubDir "IEHost.exe"
$appDir   = Join-Path $env:LOCALAPPDATA "IEHost"

# Find dotnet
if ([string]::IsNullOrEmpty($DotNetPath)) {
    $candidates = @(
        (Join-Path $repoRoot "sdk\dotnet.exe"),
        (Join-Path $repoRoot "dotnet-sdk-11.0.1\dotnet.exe")
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($found) { $DotNetPath = $found } else { $DotNetPath = "dotnet" }
}

# Publish self-contained if needed
if (-not (Test-Path $exePub)) {
    Write-Host "Publishing self-contained IEHost.exe ..."
    & $DotNetPath publish -c Release -r $rid --self-contained $csproj --nologo
    if (-not $?) { Write-Error "Build failed -- install .NET 11 SDK"; exit 1 }
}

# Prepare app directory — kill stale processes, then copy
Get-Process "IEHost" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
if (Test-Path $appDir) { Remove-Item "$appDir\*" -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Path $appDir -Force | Out-Null
Get-ChildItem $pubDir | Copy-Item -Destination $appDir -Recurse -Force

# Generate host manifest
$manifest = @{
    name            = $hostName
    description     = "IE Mode Viewer - Trident rendering for Chrome/Edge"
    path            = Join-Path $appDir "IEHost.exe"
    type            = "stdio"
    allowed_origins = @("chrome-extension://$ExtensionId/")
}
$manifestPath = Join-Path $appDir "com.chrom_ext.ie_host.json"
$content = $manifest | ConvertTo-Json
[System.IO.File]::WriteAllText($manifestPath, $content, (New-Object System.Text.UTF8Encoding($false)))

# Registry keys (HKCU = no admin)
$targets = @(
    "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$hostName"
    "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts\$hostName"
)

foreach ($reg in $targets) {
    New-Item -Path $reg -Force | Out-Null
    Set-ItemProperty -Path $reg -Name "(default)" -Value $manifestPath
}

Write-Host "`n[OK] Installed for Chrome and Edge" -ForegroundColor Green
Write-Host "     Extension ID : $ExtensionId"
Write-Host "     Host binary  : $(Join-Path $appDir 'IEHost.exe')"
Write-Host "     Manifest     : $manifestPath"

if ($Standalone) {
    $lnkPath = Join-Path ([Environment]::GetFolderPath('Desktop')) "IE Mode Viewer.lnk"
    $ws = New-Object -ComObject WScript.Shell
    $sc = $ws.CreateShortcut($lnkPath)
    $sc.TargetPath = Join-Path $appDir "IEHost.exe"
    $sc.Arguments = "--standalone"
    $sc.WorkingDirectory = $appDir
    $sc.IconLocation = "$appDir\IEHost.exe,0"
    $sc.Save()
    Write-Host "     Shortcut      : $lnkPath"

    if ($Autostart) {
        $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        New-Item -Path $runKey -Force | Out-Null
        Set-ItemProperty -Path $runKey -Name "IEHost" -Value "`"$appDir\IEHost.exe`" --standalone"
        Write-Host "     Autostart     : enabled (HKCU Run)"
    }
}

Write-Host "`nNext step: reload the extension at chrome://extensions" -ForegroundColor Yellow
Write-Host "         or run the standalone app:  $(Join-Path $appDir 'IEHost.exe') --standalone" -ForegroundColor Yellow
