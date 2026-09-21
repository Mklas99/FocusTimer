param(
    [string]$Token,
    [string]$HostUrl = "http://localhost:9000",
    [string]$ProjectKey = "focustimer",
    [string]$Organization
)
# Usage: .\scripts\run-sonar-dotnet.ps1 -Token <token> [-HostUrl <host>] [-ProjectKey <key>] [-Organization <org>]
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
$beginArgs = @(
    "begin",
    "/k:$ProjectKey",
    "/d:sonar.host.url=$HostUrl",
    "/d:sonar.token=$Token",
    "/d:sonar.cs.opencover.reportsPaths=**/TestResults/*.coverage.opencover.xml,**/TestResults/*.opencover.xml",
    "/d:sonar.verbose=true"
)
if ($Organization) {
    $beginArgs += "/o:$Organization"
}

& dotnet sonarscanner @beginArgs

Write-Host "Building solution..."
dotnet build FocusTimer.sln -c Release

Write-Host "Running tests with OpenCover coverage..."
$projects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse
foreach ($proj in $projects) {
    $name = $proj.BaseName
    Write-Host "Running tests for $name..."
    dotnet test $proj.FullName -c Release --no-build `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=opencover `
        /p:CoverletOutput="./TestResults/$name.coverage.opencover.xml"
}

Write-Host "Ending SonarScanner (MSBuild)..."
dotnet sonarscanner end /d:sonar.token="$Token"
