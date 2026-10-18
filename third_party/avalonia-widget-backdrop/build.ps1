param([string] $CheckoutName = 'avalonia-widget-backdrop-clean')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($CheckoutName -notmatch '^avalonia-widget-backdrop-[a-zA-Z0-9-]+$') {
    throw 'CheckoutName must start with avalonia-widget-backdrop- and contain only letters, digits and hyphens.'
}
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$buildRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot '.build'))
$checkout = [IO.Path]::GetFullPath((Join-Path $buildRoot $CheckoutName))
if (-not $checkout.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Checkout escaped the repository build directory.'
}
if (Test-Path -LiteralPath $checkout) { throw "Use a fresh CheckoutName; $checkout already exists." }
$revision = '68ab391c0a3653e0722638e29fcbd9633c7fd386'
$patchHash = '0046A4C388DC4F27E1C6EC1161E66042E4B1F3479FDF5F2EE9F9E6B5A1F66EA4'
if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'backend.patch') -Algorithm SHA256).Hash -ne $patchHash) {
    throw 'Patch checksum differs from the reviewed build input.'
}
$provenance = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'provenance.json') -Raw | ConvertFrom-Json
foreach ($inputFile in $provenance.sourceInputs.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $inputFile.Name) -Algorithm SHA256).Hash -ne $inputFile.Value) {
        throw "Source input checksum differs: $($inputFile.Name)"
    }
}
function Invoke-Checked {
    param([string] $Command, [string[]] $Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
Invoke-Checked git @('clone', '--depth', '1', '--branch', '11.2.0', 'https://github.com/AvaloniaUI/Avalonia.git', $checkout)
Push-Location $checkout
try {
    if ((& git rev-parse HEAD) -ne $revision) { throw 'Upstream tag no longer matches the pinned commit.' }
    Invoke-Checked git @('apply', '--check', (Join-Path $PSScriptRoot 'backend.patch'))
    Invoke-Checked git @('apply', (Join-Path $PSScriptRoot 'backend.patch'))
    $backend = Join-Path $checkout 'src/Windows/Avalonia.Win32'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Win32BackdropControllerV1.cs') -Destination $backend
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FocusTimerBackend.csproj') -Destination $backend
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'backend.packages.lock.json') -Destination (Join-Path $backend 'packages.lock.json')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'generators.packages.lock.json') -Destination 'src/tools/DevGenerators/packages.lock.json'
    $project = 'src/Windows/Avalonia.Win32/FocusTimerBackend.csproj'
    $nugetConfig = Join-Path $checkout 'NuGet.Config'
    Invoke-Checked dotnet @('restore', $project, '--locked-mode', '-p:DisableSourceLink=true', '--configfile', $nugetConfig)
    Invoke-Checked dotnet @('pack', $project, '--no-restore', '-c', 'Release', '-p:DisableSourceLink=true', '-o', (Join-Path $checkout 'packages'), '-v:q')
    $assemblyHash = (Get-FileHash -LiteralPath (Join-Path $backend 'bin/Release/net8.0/Avalonia.Win32.dll') -Algorithm SHA256).Hash
    if ($assemblyHash -ne $provenance.verifiedAssemblySha256) {
        throw 'Built assembly differs from qualification provenance. Check SDK and source inputs before using it.'
    }
    Get-FileHash -LiteralPath (Join-Path $checkout 'packages/Avalonia.Win32.11.2.0.5-focustimer.5.nupkg') -Algorithm SHA256
} finally { Pop-Location }
