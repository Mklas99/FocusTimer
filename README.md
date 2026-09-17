# FocusTimer

[![SonarQube Cloud](https://sonarcloud.io/images/project_badges/sonarcloud-light.svg)](https://sonarcloud.io/summary/new_code?id=Mklas99_FocusTimer)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Mklas99_FocusTimer&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Mklas99_FocusTimer)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Mklas99_FocusTimer&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Mklas99_FocusTimer)


A clean, cross-platform .NET 8 / Avalonia desktop timer widget with dependency injection, event-driven architecture, and Windows-specific integrations (hotkeys, notifications, idle detection).

## Quick Start

### Prerequisites
- **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10+** (for compiled executable)
- **Visual Studio 2022** (optional, for IDE development)

### Clone & Build

```powershell
git clone <repo-url>
cd FocusTimer
dotnet build
```

### Run the Application

```powershell
dotnet run --project src/FocusTimer.Host
```

Alternatively, run the compiled executable directly:
```powershell
./src/FocusTimer.Host/bin/Debug/net8.0-windows/FocusTimer.Host.exe
```

### Build for Release

```powershell
dotnet build --configuration Release
```

The release executable is at:
```
src/FocusTimer.Host/bin/Release/net8.0-windows/FocusTimer.Host.exe
```

---

## Solution Structure

| Project | Framework | Type | Purpose |
|---------|-----------|------|---------|
| **FocusTimer.Host** | net8.0-windows | WinExe | Entry point; DI & Avalonia setup |
| **FocusTimer.App** | net8.0 | Library | UI (XAML, ViewModels, Windows) |
| **FocusTimer.Core** | net8.0 | Library | Domain models, interfaces, EventBus |
| **FocusTimer.Persistence** | net8.0 | Library | JSON settings, CSV session logs |
| **FocusTimer.Platform.Windows** | net8.0-windows | Library | OS integrations (hotkeys, notifications, idle) |

**Only FocusTimer.Host produces an executable. All other projects are libraries.**

Dependency graph: Host → App, Core, Persistence, Platform.Windows. No circular dependencies.

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture, design patterns, and extension guide.

---

## Features

- **Compact Timer Widget**: Minimal, distraction-free UI for time tracking
- **System Tray Integration**: Hide/show and control timer from tray menu
- **Global Hotkeys**: Configurable keyboard shortcuts (Windows)
- **Automatic Time Entry Logging**: CSV file per date with session details
- **JSON Settings**: Persist user preferences (theme, hotkeys, start minimized, etc.)
- **Idle Detection**: Detect OS idle state and optionally auto-pause
- **Responsive Design**: Avalonia reactive MVVM bindings
- **Clean Dependency Injection**: All services registered and wired in Host
- **Event-Driven Architecture**: Decoupled messaging via EventBus

---

## Publishing & Distribution

### Self-Contained Executable (Single .exe)

Creates a standalone executable that includes the .NET runtime (no runtime pre-install required on target machines).

```powershell
dotnet publish src/FocusTimer.Host/FocusTimer.Host.csproj -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./artifacts/publish/win-x64-selfcontained
```

Output: `artifacts/publish/win-x64-selfcontained/FocusTimer.Host.exe`

**Pros**: One file to distribute
**Cons**: Larger file size due to included runtime

### Framework-Dependent Executable

Smaller executable that requires .NET 8 runtime on target machines.

```powershell
dotnet publish src/FocusTimer.Host/FocusTimer.Host.csproj -c Release -f net8.0-windows -o ./publish
```

Output: `publish/` directory with FocusTimer.Host.exe and supporting .dll files

**Pros**: Smaller size
**Cons**: Requires .NET 8 runtime on target machine

### Windows Installer (MSI) via WiX Toolset

This repository now includes a WiX installer project:
- `installer/FocusTimer.Installer/FocusTimer.Installer.wixproj`
- `installer/FocusTimer.Installer/Product.wxs`

Build both self-contained EXE + MSI in one command:

```powershell
./scripts/build-installer.ps1 -Runtime win-x64
```

Version resolution order for MSI/exe metadata:
- `-Version` parameter (if provided)
- CI tag variables (`FOCUSTIMER_VERSION`, `GITHUB_REF_NAME`, `GITHUB_REF`, `BUILD_SOURCEBRANCHNAME`, `BUILD_SOURCEBRANCH`, `CI_COMMIT_TAG`)
- Latest git tag (`git describe --tags --abbrev=0`)
- Fallback: `0.1.1`

Outputs:
- Self-contained EXE: `artifacts/publish/win-x64-selfcontained/FocusTimer.Host.exe`
- MSI (WiX output): `artifacts/installer/`

Manual WiX build (after publishing):

```powershell
dotnet build installer/FocusTimer.Installer/FocusTimer.Installer.wixproj -c Release -p:ProductVersion=1.0.0 -p:PublishDir=artifacts/publish/win-x64-selfcontained -o artifacts/installer
```

### Downloadable Releases (GitHub)

Pushing a tag matching `v*.*.*` (e.g. `v0.1.0`) triggers `.github/workflows/release.yml`, which runs `build-installer.ps1` and publishes a GitHub Release with the MSI installer and the portable self-contained single-file EXE:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

### WiX UI Feature Options

Installer UI uses `WixUI_FeatureTree` and exposes optional features:
- Desktop shortcut
- Start Menu uninstall shortcut

Users can toggle these during install in the feature selection step.

### Code-Signing Placeholders (EXE + MSI)

The installer script supports optional signing:

```powershell
./scripts/build-installer.ps1 -Runtime win-x64 -Sign
```

Configure signing through parameters or environment variables:
- `SIGNTOOL_PATH`
- `SIGNING_CERT_PATH`
- `SIGNING_CERT_PASSWORD`
- `SIGNING_CERT_THUMBPRINT`

These are placeholders and safe defaults for CI/CD release hardening.

WiX reference: [wixtoolset.org](https://wixtoolset.org/)

---

## Development

### Code Analysis

StyleCop and Microsoft.CodeAnalysis.NetAnalyzers are enabled by default. Format code:

```powershell
dotnet format
```

### Logging

Logs are written to:
- **Console**: During development
- **File**: `Documents\FocusTimer\logs\` (override the root with the `FOCUSTIMER_LOG_DIR` environment variable)

Settings are stored separately, in `%APPDATA%\Roaming\FocusTimer\settings.json`.

### Running Tests

```powershell
dotnet test
```

Run per-project unit-coverage with 60% minimum thresholds:

```powershell
./scripts/run-unit-coverage.ps1
```

Override the threshold value when needed:

```powershell
./scripts/run-unit-coverage.ps1 -Threshold 70
```

### SonarQube Analysis (Optional)

```powershell
./scripts/run-sonar-dotnet.ps1 -token <your-sonarqube-token>
```

---

## Architecture Overview

### Startup Sequence

1. **Host.Program.Main()** - Sets up DI container with all services
2. **Avalonia Framework Initialization** - Creates App instance
3. **App.OnFrameworkInitializationCompleted()** - Calls IAppInitializer.InitializeAsync()
4. **App.InitializeAsync()** - Loads settings, configures tray, registers hotkeys, shows UI

### Dependency Injection

All services are registered in `FocusTimer.Host/Program.cs`'s static constructor: Serilog logger, platform services (Windows implementations or Linux stubs, chosen via `RuntimeInformation.IsOSPlatform`), persistence, `IEventBus`, `AppController`, and the app's ViewModels.

### Event Bus Pattern

`IEventBus` is a single, non-generic bus (not one instance per event type). ViewModels publish events; controllers subscribe and react:

```csharp
// Publisher (TimerWidgetViewModel)
_eventBus.Publish(new EntriesLoggedEvent { Entries = entries });

// Subscriber (AppController constructor)
_eventBus.Subscribe<EntriesLoggedEvent>(e => LogSessions(e.Entries));
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for complete details, project descriptions, and extension guide.

---

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| App crashes on startup | Service not registered | Check FocusTimer.Host/Program.cs static constructor |
| Hotkeys not working | Registered before window shown | Ensure RegisterHotkeys() called after UI visible |
| Tray icon missing | Not in visual tree | Verify TrayIcon element in CompactModeView.axaml |
| Timer freezes | Long task on UI thread | Use Dispatcher.UIThread.InvokeAsync() |

---

## Future Enhancements

- **Pomodoro Mode**: Auto work/break cycles
- **Sound Cues**: Chimes for breaks or pause resume
- **Analytics Dashboard**: In-app "Today" view by app/project
- **Window Position Memory**: Restore widget to last-used monitor
- **Multi-Platform**: Full Linux feature parity (the app already builds and runs on Linux; hotkeys/notifications/idle/auto-start are currently stubs)

See `docs/versions/current/OpenIssues.md` for the full, tracked list of gaps.

---

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Build and test: `dotnet build && dotnet test`
3. Format: `dotnet format`
4. Commit and push: `git push origin feature/my-feature`
5. Open a pull request

---

## License

GNU General Public License v3.0 — see [LICENSE](LICENSE).
