# FocusTimer - Development Process Improvements

## Overview
This document outlines all the development process improvements that have been implemented for the FocusTimer project.

## Files Created

### 1. **`.editorconfig`** - Code Style & Formatting Rules
**Location:** Project root
**Purpose:** Defines consistent code formatting across all file types
- C# formatting rules (indentation, spacing, line breaks)
- Naming conventions (PascalCase, camelCase, I-prefix for interfaces)
- .NET coding conventions
- Applied to: `.cs`, `.md`, `.xaml`, `.yml` files
- **Benefit:** No manual formatting needed - all done automatically on save

**Key Settings:**
- Tab size: 4 spaces
- Line endings: LF (Unix-style)
- Charset: UTF-8
- Naming patterns for types, fields, properties

---

### 2. **`Directory.Build.props`** - Solution-Wide Analyzer Configuration
**Location:** Project root
**Purpose:** Centralized configuration for all analyzer packages
- Enables nullable reference types
- Enables implicit usings
- Enables StyleCop and NetAnalyzers
- Treats warnings as errors in builds
- Enforces code style in build process

**Included Packages:**
- `Microsoft.CodeAnalysis.NetAnalyzers` - Code quality rules
- `StyleCop.Analyzers` - Code style and structure rules

**Benefit:**
- Single source of truth for analyzer configuration
- Applied to ALL projects in the solution automatically
- No need to configure each `.csproj` individually

---

### 3. **`.vscode/settings.json`** - VS Code Editor Configuration
**Location:** `.vscode/settings.json`
**Purpose:** Configures VS Code for optimal C# development
- Enables EditorConfig support
- Auto-formatting on save
- Code action on save (organize imports, fix errors)
- Remove unused imports automatically
- Configures Roslyn analyzer integration

**Benefit:**
- Developers get automatic formatting & cleanup on save
- No manual code organization needed
- Consistent experience across the team

---

### 4. **`stylecop.json`** - StyleCop Analyzer Configuration
**Location:** Project root
**Purpose:** Configure StyleCop-specific rules and behavior
- Documentation rules for public types
- Naming conventions exceptions
- Import ordering (System imports first)
- Company copyright information

**Benefit:**
- Ensures consistent documentation
- Enforces proper code organization
- Customizable per team preferences

---

### 5. **`sonarqube-properties.txt`** - Legacy SonarQube Configuration (unused)
**Location:** Project root
**Purpose:** Predates the current CI setup; kept for historical reference only. The actual SonarCloud
analysis (project key `Mklas99_FocusTimer`, org `mklas99`) is driven entirely by
`.github/workflows/sonar-scan.yml`, which passes its own project key, org, and coverage report paths
directly to `dotnet sonarscanner begin` — this file is not read by anything. See the
[SonarQube Integration](#sonarqube-integration) section below for the real setup.

---

### 6. **`DEVELOPMENT.md`** - Developer Guide
**Location:** Project root
**Purpose:** Comprehensive guide for developers on code quality setup
- Prerequisites and extension installation
- Workflow documentation
- Configuration explanations
- Troubleshooting guide
- Best practices

**Benefit:**
- Single source of truth for setup instructions
- Onboarding reference for new developers
- Troubleshooting help

---

### 7. **`.githooks/pre-commit`** - Git Pre-Commit Hook (Linux/macOS)
**Location:** `.githooks/pre-commit`
**Purpose:** Automatic code quality checks before commits
- Builds the entire solution
- Runs code analysis
- Blocks commits with errors/warnings

**Benefit:**
- Prevents broken code from being committed
- Ensures all code meets quality standards
- Reduces CI/CD rejections

---

### 8. **`.githooks/pre-commit.bat`** - Git Pre-Commit Hook (Windows)
**Location:** `.githooks/pre-commit.bat`
**Purpose:** Same as above, optimized for Windows/PowerShell

---

### 9. **`.githooks/SETUP.md`** - Git Hooks Setup Guide
**Location:** `.githooks/SETUP.md`
**Purpose:** Instructions for installing git hooks
- Platform-specific setup steps
- Usage examples
- Troubleshooting

---

### 10. **`analyze-code.sh`** - Code Analysis Script (Linux/macOS)
**Location:** Project root
**Purpose:** Manual code analysis runner
- Builds solution
- Runs analyzers
- Reports results

**Usage:**
```bash
bash analyze-code.sh
```

---

### 11. **`analyze-code.bat`** - Code Analysis Script (Windows)
**Location:** Project root
**Purpose:** Same as above, for Windows/PowerShell

**Usage:**
```bash
.\analyze-code.bat
```

---

## Setup Instructions for Developers

### Step 1: Initial Setup (One-time)
```bash
# 1. Install required VS Code extensions
code --install-extension ms-dotnettools.csharp
code --install-extension editorconfig.editorconfig
code --install-extension sonarsource.sonarlint-vscode

# 2. Reload VS Code
# Ctrl+Shift+P -> Developer: Reload Window
```

### Step 2: Configure Git Hooks

**Windows (PowerShell):**
```powershell
Copy-Item -Path ".githooks\pre-commit.bat" -Destination ".git\hooks\pre-commit"
```

**macOS/Linux:**
```bash
cp .githooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### Step 3: Verify Setup
```bash
# Test that everything works
dotnet build FocusTimer.sln -c Release

# You should see code analyzers running
```

---

## Automatic Features (No Configuration Needed)

✅ **On File Save:**
- Automatic code formatting (indentation, spacing)
- Removal of unused imports
- Organization of import statements
- Code style issue fixes

✅ **On Build:**
- StyleCop rules enforcement
- NetAnalyzers rules enforcement
- Warnings treated as errors in Release builds

✅ **On Git Commit (with hooks installed):**
- Full solution build
- Code analysis checks
- Prevents committing broken code

---

## How to Disable Specific Rules

If a specific analyzer rule doesn't fit your project:

### Method 1: In `.editorconfig`
```editorconfig
# Disable a specific rule for all C# files
dotnet_diagnostic.CA1234.severity = silent
```

### Method 2: In `.csproj`
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CA1234</NoWarn>
</PropertyGroup>
```

### Method 3: In Code (Last Resort)
```csharp
#pragma warning disable SA1600 // Elements should be documented
public void SomeMethod() { }
#pragma warning restore SA1600
```

---

## SonarQube Integration

The project is registered on SonarCloud as `Mklas99_FocusTimer` (org `mklas99`) and analyzed automatically in CI
via `.github/workflows/sonar-scan.yml` — there is nothing to set up for this to happen on every push/PR to
`main`/`master`. `sonarqube-properties.txt` is a legacy file from before that workflow existed; it is **not**
read by CI (the workflow passes project key, org, and coverage paths directly to `dotnet sonarscanner begin`)
and its `sonar.projectKey=focustimer` does not match the real key, so treat it as historical only.

### Running the same analysis locally

```powershell
./scripts/run-sonar-dotnet.ps1 -Token <your-sonarcloud-token> [-HostUrl <url>] [-ProjectKey <key>]
```
This wraps `dotnet sonarscanner begin/end` the same way the CI workflow does: build the solution, run the unit
tests with OpenCover coverage collection (see `scripts/run-unit-coverage.ps1`), and submit both analysis and
coverage to SonarCloud. Generate a token under SonarCloud → Account → Security.

### Running coverage only (no SonarCloud submission)

```powershell
./scripts/run-unit-coverage.ps1 -Threshold 60
```
Generates OpenCover and Cobertura reports per test project under `artifacts/test-coverage/`.

---

## CI/CD Integration

Already wired up — no setup needed:

- **`.github/workflows/sonar-scan.yml`**: on every push to `main`/`master` and every pull request, restores,
  builds, runs `scripts/run-unit-coverage.ps1` (coverage-threshold enforced), publishes a coverage summary to the
  job, and submits the run to SonarCloud when `SONAR_TOKEN` is configured as a repo secret.
- **`.github/workflows/release.yml`**: on pushing a `v*.*.*` tag, builds the self-contained EXE + MSI installer
  and publishes a GitHub release.

---

## Troubleshooting

### Issue: Formatting Not Working
**Solution:**
1. Install EditorConfig extension
2. Restart VS Code
3. Check `.editorconfig` is in root directory

### Issue: "Unused imports" still showing
**Solution:**
1. Wait for file save to complete
2. Check `organizeImportsOnFormat` is enabled
3. Manually save again with Ctrl+S

### Issue: Pre-commit hook not running
**Solution:**
1. Verify hook file location: `.git/hooks/pre-commit`
2. On Linux/macOS: `chmod +x .git/hooks/pre-commit`
3. Check Git version: `git --version`

### Issue: Build takes too long in pre-commit
**Solution:**
1. This is normal - it checks everything
2. Subsequent builds are cached (faster)
3. Can use `--no-verify` to skip if necessary (DON'T ABUSE)

---

## Performance Impact

### VS Code Editor
- EditorConfig: Negligible (~0ms per save)
- Formatting: ~100-500ms (depending on file size)
- Import organization: ~50-200ms

### Build Time
- Debug build: No change
- Release with analyzers: +15-30% (worth it for quality)
- Can be optimized with `dotnet build --no-incremental-clean`

### Git Commit
- Pre-commit hook: ~5-15 seconds (full build + analysis)
- Cached: ~1-3 seconds on subsequent commits

---

## Team Recommendations

### ✅ Best Practices
- Run `analyze-code.bat/sh` before pushing
- Address all warnings, not just errors
- Keep `.editorconfig` updated when adding rules
- Document exceptions with comments

### ❌ Avoid
- Disabling analyzers without discussion
- Using `--no-verify` for commits
- Ignoring pre-commit hook failures
- Committing with unresolved warnings

---

## Additional Resources

- [EditorConfig Documentation](https://editorconfig.org/)
- [StyleCop Analyzers GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Microsoft docs: Code analysis](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis)
- [SonarQube C# Documentation](https://docs.sonarqube.org/latest/analysis/languages/csharp/)
- [Git Hooks Reference](https://git-scm.com/docs/githooks)

---

## Maintenance

### Quarterly Review
- Check for new analyzer versions
- Review rule violations trends
- Update `.editorconfig` if needed
- Communicate any changes to team

### When Adding New Projects
- Ensure new `.csproj` files inherit from `Directory.Build.props`
- Run `analyze-code` script to verify
- Add to any CI/CD pipelines

---

**Questions?** Refer to `DEVELOPMENT.md` for detailed explanations.
