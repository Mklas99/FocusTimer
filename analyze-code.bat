@echo off
REM Code quality analysis script for FocusTimer project (Windows PowerShell)
REM This script performs various code quality checks

setlocal enabledelayedexpansion

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR:~0,-1%

echo.
echo ======================================
echo FocusTimer - Code Quality Analysis
echo ======================================
echo.

REM Build the solution
echo [1/3] Building solution...
dotnet build "%PROJECT_ROOT%\FocusTimer.sln" -c Release
if !errorlevel! neq 0 (
    echo Build failed!
    exit /b 1
)
echo Build succeeded
echo.

REM Run analyzers and code style checks
echo [2/3] Running code analyzers...
dotnet build "%PROJECT_ROOT%\FocusTimer.sln" -c Release -p:TreatWarningsAsErrors=true -p:EnforceCodeStyleInBuild=true
if !errorlevel! neq 0 (
    echo Code analysis found issues!
    exit /b 1
)
echo Code analysis passed
echo.

echo ======================================
echo All checks passed!
echo ======================================
echo.

endlocal
