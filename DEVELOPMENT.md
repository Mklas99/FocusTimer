# Development Setup Guide for FocusTimer

## Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio Code** or **Visual Studio 2022**
- **Git** - [Download](https://git-scm.com/)

## Required VS Code Extensions

The following extensions are recommended for optimal development experience:

1. **C# Extension Pack** - `ms-dotnettools.csharp`
   - Provides IntelliSense, debugging, and code navigation for C#
   - Includes support for Roslyn analyzers

2. **EditorConfig for VS Code** - `editorconfig.editorconfig`
   - Enforces consistent coding styles across the project

3. **SonarLint** - `sonarsource.sonarlint-vscode` (optional)
   - Real-time code quality feedback

4. **Prettier** - `esbenp.prettier-vscode` (optional)
   - For any YAML/JSON formatting in the project

### Quick Install Command
```bash
code --install-extension ms-dotnettools.csharp
code --install-extension editorconfig.editorconfig
code --install-extension sonarsource.sonarlint-vscode
code --install-extension esbenp.prettier-vscode
```

## Development Process

### 1. Code Style & Formatting
The project uses `.editorconfig` to enforce consistent code style:
- **Indentation**: 4 spaces
- **Line endings**: LF (Unix-style)
- **Charset**: UTF-8
- **Naming conventions**: PascalCase for types, camelCase for private fields, I-prefix for interfaces

All formatting is **automated on save** in VS Code. No manual formatting needed!

### 2. Code Analysis
Three levels of code analysis are enforced:

#### a) Built-in .NET Analyzers
- **StyleCop** - Code style and documentation rules
- **Microsoft.CodeAnalysis.NetAnalyzers** - Code quality rules
- Violations appear in VS Code's Problems panel
- Treat warnings as errors in Release builds

#### b) EditorConfig Rules
- Defined in `.editorconfig`
- Enforced by the C# extension
- Applied automatically on save

#### c) Unused Imports Removal
- Configured via `organizeImportsOnFormat`
- Automatically removed on file save
- Unused using statements are flagged

### 3. Building & Validation

#### Full Build
```bash
dotnet build FocusTimer.sln -c Release
```

#### Build with Code Analysis
```bash
dotnet build FocusTimer.sln -c Release -p:TreatWarningsAsErrors=true -p:EnforceCodeStyleInBuild=true
```

#### Run Analysis Script
**Windows:**
```bash
.\analyze-code.bat
```

**macOS/Linux:**
```bash
bash analyze-code.sh
```

### 4. Documentation
- All **public types and members** must have XML documentation comments
- Required by StyleCop and enforced during build
- Format:
  ```csharp
  /// <summary>
  /// Brief description of the method.
  /// </summary>
  /// <param name="paramName">Description of parameter</param>
  /// <returns>Description of return value</returns>
  public string DoSomething(string paramName)
  {
  }
  ```

### 5. Code Quality Workflow

#### In VS Code:
1. **Write code** - No special formatting needed
2. **Save file** (Ctrl+S) - Automatic formatting, unused imports removed, style issues fixed
3. **Check Problems panel** (Ctrl+Shift+M) - Review any remaining issues
4. **Commit changes** - Git will run pre-commit checks

#### Before Committing:
```bash
# Run full analysis
dotnet build FocusTimer.sln -c Release

# Check for any compilation warnings
```

### 6. SonarQube Integration

The project includes SonarQube configuration for cloud-based code quality analysis.

#### To Use SonarQube:

1. **Sign up on SonarCloud:**
   - Go to https://sonarcloud.io
   - Sign up with GitHub account

2. **Add token to configuration:**
   - Update `sonarqube-properties.txt` with your token

3. **Run SonarScanner (if installed):**
   ```bash
   sonar-scanner ^
     -Dsonar.projectKey=focustimer ^
     -Dsonar.sources=src ^
     -Dsonar.host.url=https://sonarcloud.io ^
     -Dsonar.login=YOUR_TOKEN
   ```

#### Run SonarQube locally in Docker

You can run a local SonarQube server and the SonarScanner CLI in Docker. Files added:

- [docker-compose.yml](docker-compose.yml#L1): Compose for SonarQube server + scanner container.
- [sonar-project.properties.template](sonar-project.properties.template#L1): Project settings template (converted from sonarqube-properties.txt). Rename if you use the standalone scanner.
- [scripts/run-sonar.sh](scripts/run-sonar.sh#L1): Bash helper to run the server and scanner.
- [scripts/run-sonar.ps1](scripts/run-sonar.ps1#L1): PowerShell helper for Windows.

Usage (macOS / Linux):
```bash
.\scripts\run-sonar-dotnet.sh YOUR_SONAR_TOKEN
```

Usage (Windows PowerShell):
```powershell
.\scripts\run-sonar-dotnet.ps1 -Token YOUR_SONAR_TOKEN
```

Notes:
- The scripts start a SonarQube container on `http://localhost:9000` and then run the scanner container connected to it.
- Provide your Sonar token via the `SONAR_TOKEN` environment variable or as the first argument.
- For CI, you can run `docker-compose up -d sonarqube` and then `docker-compose run --rm -e SONAR_TOKEN=$TOKEN scanner`.

### 7. Common Tasks

#### Organize Imports in a File
- Automatically done on save
- Manual trigger: Use Command Palette → "Organize Imports"

#### Fix Code Style Issues
- Automatic on save
- Manual: Use Command Palette → "Format Document"

#### Remove Unused Usings
- Automatic on save
- Manual: Use Code Actions (Ctrl+.)

#### View All Code Issues
- Open Problems panel: `Ctrl+Shift+M`
- Filter by type (Error/Warning)

## Configuration Files

### `.editorconfig`
- Core formatting and style rules
- Applied to all file types (C#, YAML, Markdown, etc.)
- Understood natively by VS Code with EditorConfig extension

### `Directory.Build.props`
- Solution-wide properties
- Analyzer packages and configuration
- Applied to all projects automatically

### `stylecop.json`
- StyleCop-specific rules and exceptions
- Documentation requirements
- Naming conventions

### `.vscode/settings.json`
- VS Code editor preferences
- C# extension configuration
- Auto-formatting on save settings

### `sonarqube-properties.txt`
- SonarQube analysis configuration
- Project metadata
- Token and server settings

## Troubleshooting

### Formatting Not Working
1. Ensure C# extension is installed and enabled
2. Check that `.editorconfig` is in the repository root
3. Restart VS Code: `Ctrl+Shift+P` → "Developer: Reload Window"

### Analyzer Warnings Appear After Build
1. These are expected - they help maintain code quality
2. Review the warning in the Problems panel
3. Red squiggles indicate build-breaking errors
4. Yellow squiggles indicate warnings (configure as needed)

### Still Getting "Unused Import" Messages
1. Ensure `organizeImportsOnFormat` is enabled in settings.json
2. Import removal happens on save, not in real-time
3. Save the file again to trigger the cleanup

### ReSharper/Other Tools Conflicts
- Disable other analyzers that might conflict
- The built-in analyzers are our source of truth
- Adjust in `.editorconfig` if needed

## Best Practices

✅ **DO:**
- Write clean, well-documented code
- Address warnings before committing
- Use the auto-formatting (don't manually format)
- Run analyzers before pushing updates
- Keep `.editorconfig` up to date when adding rules

❌ **DON'T:**
- Ignore warnings and errors
- Commit with unresolved code analysis issues
- Manually format code (let the tools do it)
- Disable analyzers without discussing in the team
- Check in generated code as project code

---

For more information:
- [EditorConfig Documentation](https://editorconfig.org/)
- [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Microsoft.CodeAnalysis.NetAnalyzers](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [SonarQube for C#](https://docs.sonarqube.org/latest/analysis/languages/csharp/)
