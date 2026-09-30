param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [switch]$Sign,
    [string]$SignToolPath = $env:SIGNTOOL_PATH,
    [string]$CertificatePath = $env:SIGNING_CERT_PATH,
    [string]$CertificatePassword = $env:SIGNING_CERT_PASSWORD,
    [string]$CertificateThumbprint = $env:SIGNING_CERT_THUMBPRINT,
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsDir = Join-Path $repoRoot "artifacts"
$installerProject = Join-Path $repoRoot "installer\FocusTimer.Installer\FocusTimer.Installer.wixproj"
$bootstrapperProject = Join-Path $repoRoot "installer\FocusTimer.Bootstrapper\FocusTimer.Bootstrapper.wixproj"
$hostProject = Join-Path $repoRoot "src\FocusTimer.Host\FocusTimer.Host.csproj"
$runtimeVersion = '8.0.31'
$runtimeUrl = "https://builds.dotnet.microsoft.com/dotnet/Runtime/$runtimeVersion/dotnet-runtime-$runtimeVersion-win-x64.exe"
$runtimeHash = '2dc2f346a4bcb53ab15ab6ab84348c02eb31b0a3d4e8e06aac73b4c89f1b001af01943ed4c5735fa5585f1dd731a942d2034577dfe937f4454367ff2d6edf929'

if ($Runtime -ne 'win-x64') {
    throw "Only win-x64 is supported by the current WiX installer and runtime bootstrapper."
}

function Reset-ArtifactDirectory {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedArtifacts = [System.IO.Path]::GetFullPath($artifactsDir)
    if (-not $resolvedPath.StartsWith($resolvedArtifacts + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a directory outside artifacts: $resolvedPath"
    }

    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolvedPath -Force | Out-Null
}

function Resolve-SemVerFromText {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $match = [regex]::Match($Text, "v?(\d+)\.(\d+)\.(\d+)")
    if ($match.Success) {
        return "{0}.{1}.{2}" -f $match.Groups[1].Value, $match.Groups[2].Value, $match.Groups[3].Value
    }

    return $null
}

function Invoke-Checked {
    param(
        [scriptblock]$Command,
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Resolve-ProductVersion {
    param([string]$RequestedVersion)

    if (-not [string]::IsNullOrWhiteSpace($RequestedVersion)) {
        $parsed = Resolve-SemVerFromText -Text $RequestedVersion
        if ($parsed) {
            return $parsed
        }

        throw "Provided -Version '$RequestedVersion' is not a valid semantic version (x.y.z)."
    }

    $candidates = @(
        $env:FOCUSTIMER_VERSION,
        $env:GITHUB_REF_NAME,
        $env:GITHUB_REF,
        $env:BUILD_SOURCEBRANCHNAME,
        $env:BUILD_SOURCEBRANCH,
        $env:CI_COMMIT_TAG
    )

    foreach ($candidate in $candidates) {
        $parsed = Resolve-SemVerFromText -Text $candidate
        if ($parsed) {
            return $parsed
        }
    }

    $gitTag = (git -C $repoRoot describe --tags --abbrev=0 2>$null)
    $parsedGitTag = Resolve-SemVerFromText -Text $gitTag
    if ($parsedGitTag) {
        return $parsedGitTag
    }

    return "0.1.1"
}

function Invoke-Signing {
    param(
        [string]$FilePath,
        [string]$ToolPath,
        [string]$CertPath,
        [string]$CertPassword,
        [string]$Thumbprint,
        [string]$TimeUrl
    )

    if (-not (Test-Path $FilePath)) {
        throw "Cannot sign missing file: $FilePath"
    }

    if (-not (Test-Path $ToolPath)) {
        throw "signtool not found at '$ToolPath'. Set -SignToolPath or SIGNTOOL_PATH."
    }

    if (-not [string]::IsNullOrWhiteSpace($CertPath)) {
        if (-not (Test-Path $CertPath)) {
            throw "Signing certificate file not found: $CertPath"
        }

        & $ToolPath sign /fd SHA256 /td SHA256 /tr $TimeUrl /f $CertPath /p $CertPassword $FilePath
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $FilePath" }
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($Thumbprint)) {
        & $ToolPath sign /fd SHA256 /td SHA256 /tr $TimeUrl /sha1 $Thumbprint $FilePath
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $FilePath" }
        return
    }

    throw "No signing identity provided. Set -CertificatePath/-CertificatePassword or -CertificateThumbprint (or env vars)."
}

function New-WixComponentFragment {
    param([string]$SourceDir, [string]$OutputPath)

    $files = @(Get-ChildItem -Path $SourceDir -File | Where-Object Extension -ne '.pdb' | Sort-Object Name)
    if ($files.Count -eq 0) {
        throw "No application files found in $SourceDir"
    }
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">')

    foreach ($file in $files) {
        $rawId = $file.Name -replace '[^a-zA-Z0-9]', '_'
        $compId = 'cmp_' + $rawId
        $fileId = 'f_' + $rawId
        [void]$sb.AppendLine("      <Component Id=`"$compId`" Guid=`"*`">")
        [void]$sb.AppendLine("        <File Id=`"$fileId`" Source=`"$($file.FullName)`" KeyPath=`"yes`" />")
        [void]$sb.AppendLine('      </Component>')
    }

    [void]$sb.AppendLine('    </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')

    Set-Content -Path $OutputPath -Value $sb.ToString() -Encoding UTF8
    Write-Host "Generated WiX component fragment: $OutputPath ($($files.Count) files)" -ForegroundColor Gray
}

$resolvedVersion = Resolve-ProductVersion -RequestedVersion $Version
$installerOutDir = Join-Path $artifactsDir 'installer'
Reset-ArtifactDirectory -Path $installerOutDir
Write-Host "Using product version: $resolvedVersion" -ForegroundColor Yellow

foreach ($variant in @('selfcontained', 'framework-dependent')) {
    $selfContainedValue = if ($variant -eq 'selfcontained') { 'true' } else { 'false' }
    $frameworkDependentValue = if ($variant -eq 'framework-dependent') { 'true' } else { 'false' }
    $publishDir = Join-Path $artifactsDir "publish\$Runtime-$variant"
    $wixWorkDir = Join-Path $artifactsDir "wix\$variant"
    Reset-ArtifactDirectory -Path $publishDir
    Reset-ArtifactDirectory -Path $wixWorkDir

    Write-Host "Publishing $variant application..." -ForegroundColor Cyan
    Invoke-Checked -FailureMessage "dotnet publish failed for $variant." -Command {
        dotnet publish $hostProject `
            -c Release `
            -f net8.0-windows `
            -r $Runtime `
            -p:SelfContained=$selfContainedValue `
            -p:PublishSelfContained=$selfContainedValue `
            -p:PublishSingleFile=true `
            -p:EnableCompressionInSingleFile=false `
            -p:PublishTrimmed=false `
            -p:Version=$resolvedVersion `
            -o $publishDir
    }

    $exePath = Join-Path $publishDir 'FocusTimer.Host.exe'
    if (-not (Test-Path $exePath)) {
        throw "Executable was not produced at $publishDir"
    }

    $wixFragmentPath = Join-Path $wixWorkDir 'ProductComponents.wxs'
    New-WixComponentFragment -SourceDir $publishDir -OutputPath $wixFragmentPath

    if ($Sign) {
        Write-Host "Signing $variant EXE..." -ForegroundColor Cyan
        Invoke-Signing -FilePath $exePath -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
    }

    $wixOutputDir = Join-Path $wixWorkDir 'output'
    Write-Host "Building $variant MSI..." -ForegroundColor Cyan
    Invoke-Checked -FailureMessage "WiX installer build failed for $variant." -Command {
        dotnet build $installerProject `
            -c Release `
            -t:Rebuild `
            -p:ProductVersion=$resolvedVersion `
            -p:PublishDir=$publishDir `
            -p:ComponentFragment=$wixFragmentPath `
            -p:IsFrameworkDependent=$frameworkDependentValue `
            -o $wixOutputDir
    }

    $msiPath = Join-Path $installerOutDir "FocusTimer.Installer.$variant.msi"
    Copy-Item -LiteralPath (Join-Path $wixOutputDir 'FocusTimer.Installer.msi') -Destination $msiPath
    if ($Sign) {
        Write-Host "Signing $variant MSI..." -ForegroundColor Cyan
        Invoke-Signing -FilePath $msiPath -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
    }

    $installedFiles = @(Get-ChildItem -Path $publishDir -File | Where-Object Extension -ne '.pdb')
    $installedBytes = ($installedFiles | Measure-Object -Property Length -Sum).Sum
    Write-Host "$variant`: $($installedFiles.Count) installed file(s), $([math]::Round($installedBytes / 1MB, 2)) MiB payload, $([math]::Round((Get-Item $msiPath).Length / 1MB, 2)) MiB MSI, $([math]::Round((Get-Item $exePath).Length / 1MB, 2)) MiB EXE"
}

$runtimeDir = Join-Path $artifactsDir 'runtime'
$runtimeInstaller = Join-Path $runtimeDir "dotnet-runtime-$runtimeVersion-win-x64.exe"
New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
$runtimeIsVerified = (Test-Path $runtimeInstaller) -and ((Get-FileHash $runtimeInstaller -Algorithm SHA512).Hash -ieq $runtimeHash)
if (-not $runtimeIsVerified) {
    $runtimeDownload = Join-Path $runtimeDir "dotnet-runtime-$runtimeVersion-win-x64.download"
    Write-Host "Downloading .NET $runtimeVersion runtime installer for bundle metadata..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeDownload
    if ((Get-FileHash $runtimeDownload -Algorithm SHA512).Hash -ine $runtimeHash) {
        throw "Downloaded .NET runtime hash did not match the published SHA-512 value."
    }
    Move-Item -LiteralPath $runtimeDownload -Destination $runtimeInstaller -Force
}

$frameworkMsi = Join-Path $installerOutDir 'FocusTimer.Installer.framework-dependent.msi'
$bundleWorkDir = Join-Path $artifactsDir 'wix\bootstrapper'
Reset-ArtifactDirectory -Path $bundleWorkDir
Write-Host 'Building framework-dependent setup with automatic .NET download...' -ForegroundColor Cyan
Invoke-Checked -FailureMessage 'Bootstrapper build failed.' -Command {
    dotnet build $bootstrapperProject `
        -c Release `
        -t:Rebuild `
        -p:ProductVersion=$resolvedVersion `
        -p:FrameworkMsi=$frameworkMsi `
        -p:RuntimeInstaller=$runtimeInstaller `
        -p:RuntimeUrl=$runtimeUrl `
        -o $bundleWorkDir
}
$bundlePath = Join-Path $installerOutDir 'FocusTimer.Setup.framework-dependent.exe'
Copy-Item -LiteralPath (Join-Path $bundleWorkDir 'FocusTimer.Bootstrapper.exe') -Destination $bundlePath
if ($Sign) {
    Invoke-Signing -FilePath $bundlePath -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
}
Write-Host "Framework-dependent setup: $([math]::Round((Get-Item $bundlePath).Length / 1MB, 2)) MiB (downloads .NET only if missing)"

$portableDir = Join-Path $artifactsDir "publish\$Runtime-portable"
Reset-ArtifactDirectory -Path $portableDir
Write-Host 'Publishing compressed portable EXE...' -ForegroundColor Cyan
Invoke-Checked -FailureMessage 'Portable EXE publish failed.' -Command {
    dotnet publish $hostProject `
        -c Release `
        -f net8.0-windows `
        -r $Runtime `
        -p:SelfContained=true `
        -p:PublishSelfContained=true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:PublishTrimmed=false `
        -p:Version=$resolvedVersion `
        -o $portableDir
}
$portableExe = Join-Path $portableDir 'FocusTimer.Host.exe'
if (-not (Test-Path $portableExe)) {
    throw "Portable executable was not produced at $portableDir"
}
if ($Sign) {
    Invoke-Signing -FilePath $portableExe -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
}
Write-Host "Portable EXE: $([math]::Round((Get-Item $portableExe).Length / 1MB, 2)) MiB"
Write-Host "Installers: $installerOutDir" -ForegroundColor Green

if (-not $Sign) {
    Write-Host "Tip: enable signing with -Sign and signing env vars (SIGNTOOL_PATH, SIGNING_CERT_PATH, SIGNING_CERT_PASSWORD, SIGNING_CERT_THUMBPRINT)." -ForegroundColor DarkYellow
}
