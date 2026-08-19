param(
    [string]$DesktopApiBaseUrl = "http://localhost:5000",
    [string]$MobileApiBaseUrl = "http://10.0.2.2:5000"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location (Join-Path $repoRoot "desktop/fixedit_desktop")
try {
    flutter build windows --release "--dart-define=API_BASE_URL=$DesktopApiBaseUrl"
}
finally {
    Pop-Location
}

Push-Location (Join-Path $repoRoot "mobilne/fixedit_mobile")
try {
    flutter build apk --release "--dart-define=API_BASE_URL=$MobileApiBaseUrl"
}
finally {
    Pop-Location
}

Write-Host "Desktop build: desktop/fixedit_desktop/build/windows/x64/runner/Release/"
Write-Host "Android build: mobilne/fixedit_mobile/build/app/outputs/flutter-apk/app-release.apk"
