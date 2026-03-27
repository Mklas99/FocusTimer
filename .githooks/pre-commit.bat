@echo off
REM Pre-commit hook for FocusTimer (Windows)
REM Runs code analysis before allowing commits
REM Install: copy .githooks\pre-commit.bat .git\hooks\pre-commit.bat

setlocal enabledelayedexpansion

REM Get project root
for /F "delims=" %%I in ('git rev-parse --show-toplevel') do set PROJECT_ROOT=%%I
cd /d "%PROJECT_ROOT%"

echo.
echo Checking code quality before commit...
echo.

REM Get staged C# files
for /F "delims=" %%F in ('git diff --cached --name-only --diff-filter=ACM ^| findstr /E "\.cs$"') do (
    set "HAS_CS_FILES=true"
)

if not defined HAS_CS_FILES (
    echo No C# files to check
    exit /b 0
)

echo Building project...
dotnet build FocusTimer.sln -c Release -q
if !errorlevel! neq 0 (
    echo Build failed! Fix errors before committing.
    exit /b 1
)
echo Build passed
echo.

echo Running code analysis...
dotnet build FocusTimer.sln -c Release -p:TreatWarningsAsErrors=true -p:EnforceCodeStyleInBuild=true -q
if !errorlevel! neq 0 (
    echo Code analysis issues found!
    echo Run: dotnet build FocusTimer.sln -c Release -p:EnforceCodeStyleInBuild=true
    echo to see detailed issues.
    exit /b 1
)
echo Code analysis passed
echo.

echo All pre-commit checks passed!
exit /b 0
