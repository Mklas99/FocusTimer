param(
    [int]$Threshold = 60,
    [string]$Format = "opencover,cobertura",
    [string]$Configuration = "Debug",
    [switch]$NoBuild
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

$failed = @()

foreach ($project in $projects)
{
    $testProject = $project.TestProject
    $includeFilter = $project.Include

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
    $outputDir = Join-Path $coverageRoot $projectName
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null

    Write-Host "Running coverage for $projectName (threshold: $Threshold%, format: $Format)..."

    # Build args as an array so PowerShell passes each value verbatim (no manual quote escaping).
    $dotnetArgs = @(
        "test", $testProject,
        "--configuration", $Configuration,
        "/p:CollectCoverage=true",
        "/p:CoverletOutputFormat=$Format",
        "/p:CoverletOutput=$outputDir/",
        "/p:Include=$includeFilter",
        "/p:Threshold=$Threshold",
        "/p:ThresholdType=line",
        "/p:ThresholdStat=total"
    )

    if ($NoBuild) { $dotnetArgs += "--no-build" }

    & dotnet @dotnetArgs

    # $ErrorActionPreference does not cover native exit codes; check explicitly.
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Coverage run failed for $projectName (exit code $LASTEXITCODE)."
        $failed += $projectName
    }
}

if ($failed.Count -gt 0)
{
    Write-Error "Coverage run failed for: $($failed -join ', ')"
    exit 1
}

Write-Host "Coverage run finished for all projects. Reports are in $coverageRoot"
