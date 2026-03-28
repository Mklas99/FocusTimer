param([string]$Token, [string]$HostUrl = "http://localhost:9000")
# Usage: .\scripts\run-sonar-dotnet.ps1 -Token <token> [-HostUrl <host>]
if (-not $Token) { $Token = $env:SONAR_TOKEN }
if (-not $Token) { Write-Error "SONAR token required (pass -Token or set SONAR_TOKEN)"; exit 1 }

# Ensure dotnet-sonarscanner global tool is installed
$installed = & dotnet tool list -g --no-self-update | Select-String "dotnet-sonarscanner"
if (-not $installed) {
  Write-Host "Installing dotnet-sonarscanner global tool..."
  dotnet tool install --global dotnet-sonarscanner
  $env:PATH += ";" + "$env:USERPROFILE\.dotnet\tools"
}

Write-Host "Starting SonarScanner (MSBuild) begin..."
Write-Host "Starting SonarScanner (MSBuild) begin... (verbose)"
dotnet sonarscanner begin /k:"focustimer" /d:sonar.host.url="$HostUrl" /d:sonar.login="$Token" /d:sonar.verbose=true

Write-Host "Building solution"
dotnet build FocusTimer.sln -c Release

Write-Host "Ending SonarScanner (MSBuild)"
dotnet sonarscanner end /d:sonar.login="$Token"
