param(
    [int]$Threshold = 60
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$projects = @(
    @{ TestProject = "tests/FocusTimer.Core.Tests/FocusTimer.Core.Tests.csproj"; Include = "[FocusTimer.Core]*" },
    @{ TestProject = "tests/FocusTimer.Persistence.Tests/FocusTimer.Persistence.Tests.csproj"; Include = "[FocusTimer.Persistence]*" },
    @{ TestProject = "tests/FocusTimer.App.Tests/FocusTimer.App.Tests.csproj"; Include = "[FocusTimer.App]FocusTimer.App.ViewModels.ColorPickerWindowViewModel*" },
    @{ TestProject = "tests/FocusTimer.Platform.Windows.Tests/FocusTimer.Platform.Windows.Tests.csproj"; Include = "[FocusTimer.Platform.Windows]FocusTimer.Platform.Windows.WindowsActiveWindowService*" },
    @{ TestProject = "tests/FocusTimer.Host.Tests/FocusTimer.Host.Tests.csproj"; Include = "[FocusTimer.Host]*" }
)

$coverageRoot = Join-Path $repoRoot "artifacts/test-coverage"
New-Item -Path $coverageRoot -ItemType Directory -Force | Out-Null

foreach ($project in $projects)
{
    $testProject = $project.TestProject
    $includeFilter = $project.Include

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
    $outputDir = Join-Path $coverageRoot $projectName
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null

    Write-Host "Running coverage for $projectName (threshold: $Threshold%)..."

    dotnet test $testProject `
        --configuration Debug `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=cobertura `
        /p:CoverletOutput="$outputDir/" `
        /p:Include="$includeFilter" `
        /p:Threshold=$Threshold `
        /p:ThresholdType=line `
        /p:ThresholdStat=total
}

Write-Host "Coverage run finished for all projects. Reports are in $coverageRoot"
