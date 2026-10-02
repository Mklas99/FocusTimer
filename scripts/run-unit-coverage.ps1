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
    @{ TestProjects = @("tests/FocusTimer.Core.Tests/FocusTimer.Core.Tests.csproj"); Include = "[FocusTimer.Core]*"; Name = "FocusTimer.Core" },
    @{ TestProjects = @("tests/FocusTimer.Persistence.Tests/FocusTimer.Persistence.Tests.csproj"); Include = "[FocusTimer.Persistence]*"; Name = "FocusTimer.Persistence" },
    # App coverage is the union of the plain-logic tests and the headless-Avalonia window
    # tests. The two live in separate assemblies (not just separate classes) because
    # Avalonia's headless Application.Current is a process-wide singleton that several
    # FocusTimer.App.Tests cases rely on staying null; running both in one process would
    # make either the headless tests or the plain-logic tests order-dependent and flaky.
    @{ TestProjects = @("tests/FocusTimer.App.Tests/FocusTimer.App.Tests.csproj", "tests/FocusTimer.App.HeadlessTests/FocusTimer.App.HeadlessTests.csproj"); Include = "[FocusTimer.App]*"; Name = "FocusTimer.App" },
    @{ TestProjects = @("tests/FocusTimer.Platform.Windows.Tests/FocusTimer.Platform.Windows.Tests.csproj"); Include = "[FocusTimer.Platform.Windows]*"; Name = "FocusTimer.Platform.Windows" },
    @{ TestProjects = @("tests/FocusTimer.Host.Tests/FocusTimer.Host.Tests.csproj"); Include = "[FocusTimer.Host]*"; Name = "FocusTimer.Host" }
)

$coverageRoot = Join-Path $repoRoot "artifacts/test-coverage"
New-Item -Path $coverageRoot -ItemType Directory -Force | Out-Null

$failed = @()

foreach ($project in $projects)
{
    $includeFilter = $project.Include
    $projectName = $project.Name
    $outputDir = Join-Path $coverageRoot $projectName
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null

    Write-Host "Running coverage for $projectName (threshold: $Threshold%, format: $Format)..."

    $mergeWith = $null
    for ($i = 0; $i -lt $project.TestProjects.Count; $i++)
    {
        $testProject = $project.TestProjects[$i]
        $isLast = $i -eq ($project.TestProjects.Count - 1)

        # Only the final test project in the chain enforces the threshold, since it is the
        # one whose merged report reflects every test project's combined coverage so far.
        $outputFormat = if ($isLast) { $Format } else { "json" }
        $outputTarget = if ($isLast) { "$outputDir/" } else { "$outputDir/$([System.IO.Path]::GetFileNameWithoutExtension($testProject)).json" }

        # Build args as an array so PowerShell passes each value verbatim (no manual quote escaping).
        $dotnetArgs = @(
            "test", $testProject,
            "--configuration", $Configuration,
            "/p:CollectCoverage=true",
            # Quote the value: an unquoted comma makes MSBuild treat "cobertura" as a separate switch (MSB1006).
            "/p:CoverletOutputFormat=`"$outputFormat`"",
            "/p:CoverletOutput=$outputTarget",
            "/p:Include=$includeFilter"
        )

        if ($mergeWith) { $dotnetArgs += "/p:MergeWith=$mergeWith" }

        if ($isLast)
        {
            $dotnetArgs += @(
                "/p:Threshold=$Threshold",
                "/p:ThresholdType=`"line,branch`"",
                "/p:ThresholdStat=total"
            )
        }

        if ($NoBuild) { $dotnetArgs += "--no-build" }

        & dotnet @dotnetArgs

        # $ErrorActionPreference does not cover native exit codes; check explicitly.
        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Coverage run failed for $projectName (exit code $LASTEXITCODE)."
            $failed += $projectName
            break
        }

        $mergeWith = $outputTarget
    }
}

if ($failed.Count -gt 0)
{
    Write-Error "Coverage run failed for: $($failed -join ', ')"
    exit 1
}

Write-Host "Coverage run finished for all projects. Reports are in $coverageRoot"
