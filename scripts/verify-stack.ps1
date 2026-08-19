param(
    [string]$HealthUrl = "http://localhost:5000/health",
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repoRoot
try {
    docker compose config --quiet
    docker compose up --build -d

    $expectedServices = @(docker compose config --services)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
            $runningServices = @(docker compose ps --status running --services)
            $missingServices = @($expectedServices | Where-Object { $_ -notin $runningServices })
            $apiIsHealthy = $response.StatusCode -eq 200 -and $response.Content.Trim() -eq "Healthy"
            $allServicesAreRunning = $missingServices.Count -eq 0
            if ($apiIsHealthy -and $allServicesAreRunning) {
                Write-Host "FixedIT API je zdrav: $($response.Content.Trim())"
                Write-Host "Svi servisi su pokrenuti: $($runningServices -join ', ')"
                exit 0
            }
        }
        catch {
            Start-Sleep -Seconds 3
        }
    } while ((Get-Date) -lt $deadline)

    docker compose ps
    docker compose logs --tail 100 fixedit-api
    docker compose logs --tail 100 fixedit-notifications
    throw "FixedIT stack nije postao zdrav u roku od $TimeoutSeconds sekundi."
}
finally {
    Pop-Location
}
