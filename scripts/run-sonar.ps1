param([string]$Token)
# Usage: .\scripts\run-sonar.ps1 -Token <your_token>
if (-not $Token) { $Token = $env:SONAR_TOKEN }
if (-not $Token) {
  $Token = Read-Host "Enter Sonar token"
}

docker-compose up -d sonarqube
Write-Host "Waiting for SonarQube to be available at http://localhost:9000..."
$max = 120
for ($i=0; $i -lt $max; $i++) {
  try {
    $r = Invoke-WebRequest -UseBasicParsing -Uri http://localhost:9000 -TimeoutSec 3
    if ($r.StatusCode -eq 200) { break }
  } catch { }
  Start-Sleep -Seconds 2
}

docker-compose run --rm -e SONAR_TOKEN=$Token scanner
