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
$publishDir = Join-Path $repoRoot "artifacts\publish\$($Runtime)-selfcontained"
$installerProject = Join-Path $repoRoot "installer\FocusTimer.Installer\FocusTimer.Installer.wixproj"
$installerOutDir = Join-Path $repoRoot "artifacts\installer"

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
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($Thumbprint)) {
        & $ToolPath sign /fd SHA256 /td SHA256 /tr $TimeUrl /sha1 $Thumbprint $FilePath
        return
    }

    throw "No signing identity provided. Set -CertificatePath/-CertificatePassword or -CertificateThumbprint (or env vars)."
}

function New-WixComponentFragment {
    param([string]$SourceDir, [string]$OutputPath)

    $files = Get-ChildItem -Path $SourceDir -File | Sort-Object Name
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
Write-Host "Using product version: $resolvedVersion" -ForegroundColor Yellow

Write-Host "Publishing FocusTimer.Host as self-contained (multi-file)..." -ForegroundColor Cyan
Invoke-Checked -FailureMessage "dotnet publish failed." -Command {
    dotnet publish (Join-Path $repoRoot "src\FocusTimer.Host\FocusTimer.Host.csproj") `
        -c Release `
        -f net8.0-windows `
        -r $Runtime `
        --self-contained true `
        -p:PublishTrimmed=false `
        -p:Version=$resolvedVersion `
        -o $publishDir
}

if (-not (Test-Path (Join-Path $publishDir "FocusTimer.Host.exe"))) {
    throw "Self-contained executable was not produced at $publishDir"
}

$wixFragmentPath = Join-Path $repoRoot "installer\FocusTimer.Installer\ProductComponents.wxs"
New-WixComponentFragment -SourceDir $publishDir -OutputPath $wixFragmentPath

if ($Sign) {
    Write-Host "Signing self-contained EXE..." -ForegroundColor Cyan
    Invoke-Signing -FilePath (Join-Path $publishDir "FocusTimer.Host.exe") -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
}

Write-Host "Building MSI installer with WiX Toolset..." -ForegroundColor Cyan
Invoke-Checked -FailureMessage "WiX installer build failed." -Command {
    dotnet build $installerProject `
        -c Release `
        -p:ProductVersion=$resolvedVersion `
        -p:PublishDir=$publishDir `
        -o $installerOutDir
}

$msiPath = Join-Path $installerOutDir "FocusTimer.Installer.msi"
if ($Sign) {
    Write-Host "Signing MSI installer..." -ForegroundColor Cyan
    Invoke-Signing -FilePath $msiPath -ToolPath $SignToolPath -CertPath $CertificatePath -CertPassword $CertificatePassword -Thumbprint $CertificateThumbprint -TimeUrl $TimestampUrl
}

Write-Host "Done." -ForegroundColor Green
Write-Host "Application install dir: $publishDir"
Write-Host "Installer output: $installerOutDir"

if (-not $Sign) {
    Write-Host "Tip: enable signing with -Sign and signing env vars (SIGNTOOL_PATH, SIGNING_CERT_PATH, SIGNING_CERT_PASSWORD, SIGNING_CERT_THUMBPRINT)." -ForegroundColor DarkYellow
}
