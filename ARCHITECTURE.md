# FocusTimer – Architecture Guide

## Overview

FocusTimer is a .NET 8 / Avalonia cross-platform desktop timer widget with clean dependency injection, event-driven decoupling, and platform-specific implementations. The application is organized into five projects, each with distinct responsibilities.

## Project Structure

### 1. **FocusTimer.Host** (`net8.0-windows`, WinExe executable)

**Purpose**: Windows executable entry point that orchestrates application startup.

**Key Responsibilities**:
- Main entry point (`Program.cs` with `Main()`)
- Dependency Injection (DI) container setup and wiring
- Service registration for all layers (Core, App, Persistence, Platform.Windows)
- Avalonia application builder configuration
- Platform detection and conditional service registration

**Key Components**:
```csharp
Program.Main(string[] args)
  ├─ BuildServices() - registers all DI services
  ├─ BuildAvaloniaApp() - configures Avalonia framework
  └─ Runs the app with .StartWithClassicDesktopLifetime()
```

**Dependencies**: FocusTimer.App, Core, Persistence, Platform.Windows

**Note**: FocusTimer.Host is Windows-only for now. Multi-platform support would require:
- Multi-targeting: `<TargetFrameworks>net8.0;net8.0-windows</TargetFrameworks>`
- Conditional Platform.Windows reference: `<Condition>`
- A separate Linux Platform project (future work)

---

### 2. **FocusTimer.App** (`net8.0`, Class Library)

**Purpose**: User interface implementation with Avalonia XAML, ViewModels, and window logic.

**Key Responsibilities**:
- Avalonia XAML views (CompactModeView, SettingsWindow, TimerWidgetWindow)
- ReactiveUI ViewModels (MainWindowViewModel, SettingsWindowViewModel, TimerWidgetViewModel)
- Converters (Color, Boolean, PlayPause state converters)
- Services specific to UI (AppController, ThemeManager, TrayStateController)
- Implements IAppInitializer for coordinated startup via DI

**Architecture Pattern: MVVM + ReactiveUI**
```
View (XAML)
  ├─ DataContext = ViewModel
  └─ Bindings to ViewModel properties & commands
       ├─ ICommand for user interactions
       └─ Properties with INotifyPropertyChanged
```

**Key Components**:

- **App.cs** (IAppInitializer)
  - Implements InitializeAsync() to receive injected services
  - Configures logging, tray icon, and applies theme on startup
  - Hooks into OnFrameworkInitializationCompleted() to trigger async initialization

- **AppController.cs** (Singleton)
  - Orchestrates window lifecycle (show/hide TimerWidget, ShowSettings, etc.)
  - Subscribes to EntriesLoggedEvent via EventBus
  - Manages user interactions with tray and windows

- **TimerWidgetViewModel.cs** (Reactive)
  - Holds timer state (IsRunning, TimeElapsed, CurrentEntry)
  - Publishes EntriesLoggedEvent when entries are logged
  - Exposes Logger and HotkeyService for views (property injection pattern)

- **ViewModels & Views**
  - SettingsWindowViewModel: Settings state and validation
  - TimerWidgetWindow: Compact timer display
  - Converters: Color opacity, angle rotation, play/pause icons

**Dependencies**: Core, Persistence

**Cross-Platform**: Built as `net8.0` (no Windows-specific code; platform-specific features accessed via Core interfaces)

---

### 3. **FocusTimer.Core** (`net8.0`, Class Library)

**Purpose**: Domain models, service interfaces, and business logic (framework-agnostic).

**Key Responsibilities**:
- Domain models (TimeEntry, Session, Settings)
- Service interfaces (contracts for all external integrations)
- Event infrastructure (IEventBus<T>, EventBus, domain events)
- Logging interfaces (IAppLogger)
- AppHost static accessor (for legacy global service access—being phased out)
- IAppInitializer interface (DI-based app initialization contract)

**Core Service Interfaces**:
```csharp
IAppLogger           // Structured logging
ISettingsProvider    // Load/save settings
ISessionRepository   // Log time entries to persistent storage
IGlobalHotkeyService // Register OS hotkeys
IActiveWindowService // Detect window/app in focus
INotificationService // Show Toast notifications
ITrayIconController  // Manage system tray
IIdleDetectionService // Poll OS idle state
IAutoStartService    // Register app in startup mechanisms
```

**Event Bus Pattern**:
```csharp
IEventBus<T>
  ├─ Task Publish<T>(message) - notify all subscribers
  └─ IDisposable Subscribe<T>(Action<T> handler) - register handler

// Usage in publisher (ViewModel):
await eventBus.Publish(new EntriesLoggedEvent { Entries = ... });

// Usage in subscriber (AppController):
eventBus.Subscribe<EntriesLoggedEvent>(e => HandleEntriesLogged(e.Entries));
```

**EntriesLoggedEvent**: Domain event published when timer entries are saved, enabling decoupled communication between UI and business logic.

**AppHost Static Accessor** (Legacy):
```csharp
public static class AppHost {
    public static IServiceProvider? Services { get; set; }
}
```
This allows late-binding access to services from non-DI-aware contexts. It is currently used only for App initialization but should be phased out as more code becomes DI-aware.

**Dependencies**: None (Core is at the leaf of the dependency graph)

---

### 4. **FocusTimer.Persistence** (`net8.0`, Class Library)

**Purpose**: Data persistence implementations (settings and session logs).

**Key Responsibilities**:
- JSON settings provider (load/save application settings)
- CSV session repository (append time entries to dated logs)
- ServiceCollectionExtensions for DI registration

**Key Components**:

- **JsonSettingsProvider.cs**
  - Implements ISettingsProvider
  - Stores settings in `%APPDATA%\Roaming\FocusTimer\settings.json`
  - Automatic JSON serialization/deserialization with sane defaults

- **CsvSessionRepository.cs**
  - Implements ISessionRepository
  - Appends TimeEntry records to CSV files (one per date: `2026-03-29.csv`)
  - Retention policy: keeps last N days of logs (configurable)
  - Thread-safe concurrent writes

- **ServiceCollectionExtensions.cs**
  - AddPersistenceServices() extension method
  - Registers JsonSettingsProvider and CsvSessionRepository as singletons
  - Called from Host.Program during DI setup

**Data Format**:
```csv
Date,Start,End,Duration,App,WindowTitle,Project
2026-03-29,09:15:00,09:45:30,00:30:30,Visual Studio Code,README.md - FocusTimer,Work
2026-03-29,09:46:00,10:30:15,00:44:15,Slack,Slack (1) - Notifications,Communication
```

**Extensibility**:
- Swap CSV implementation for SQLite, PostgreSQL, or cloud storage by providing an alternate ISessionRepository
- Settings can be extended in Core.Models.Settings

**Dependencies**: Core

---

### 5. **FocusTimer.Platform.Windows** (`net8.0-windows`, Class Library)

**Purpose**: Windows-specific platform integrations.

**Key Responsibilities**:
- Global hotkey registration (P/Invoke to Windows APIs)
- Active window detection (GetForegroundWindow, GetWindowText)
- Toast notifications (WinRT)
- Idle state detection (GetLastInputInfo)
- System tray integration
- Auto-start registry management

**Key Components**:

- **WindowsHotkeyService.cs**: Implements IGlobalHotkeyService
  - Registers hotkeys via P/Invoke to RegisterHotKey
  - Handles WM_HOTKEY messages in message loop
  - Raises HotkeyPressed event

- **WindowsActiveWindowService.cs**: Implements IActiveWindowService
  - Polls current foreground window via GetForegroundWindow
  - Returns app name and window title for logging

- **WindowsNotificationService.cs**: Implements INotificationService
  - Shows system toast notifications
  - WinRT API integration

- **WindowsIdleDetectionService.cs**: Implements IIdleDetectionService
  - GetLastInputInfo API to detect idle periods
  - Configurable idle timeout threshold

- **WindowsAutoStartService.cs**: Implements IAutoStartService
  - Manages HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry entry
  - Enables/disables app auto-launch on login

**Dependencies**: Core (interfaces only)

**Linux Equivalent**: Would be `FocusTimer.Platform.Linux` with stubs (X11/DBus for hotkeys, idle detection, notifications).

---

## Dependency Graph

```
FocusTimer.Host (exe, Windows-only)
  ├─ FocusTimer.App (UI library)
  │  ├─ FocusTimer.Core (models, interfaces)
  │  └─ FocusTimer.Persistence (data)
  │
  ├─ FocusTimer.Core
  │
  ├─ FocusTimer.Persistence
  │  └─ FocusTimer.Core
  │
  └─ FocusTimer.Platform.Windows
     └─ FocusTimer.Core
```

**Direction**: Host references everything. No circular dependencies. Core has no dependencies.

---

## Dependency Injection Pattern

### Registration (FocusTimer.Host/Program.cs)

```csharp
var services = new ServiceCollection();

// Core services
services.AddSingleton<IAppLogger>(sp => new Serilog.SerilogAdapter(...));
services.AddSingleton<IAppInitializer, App>();

// Persistence
services.AddPersistenceServices(); // Extension method

// Platform-specific (Windows only)
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddWindowsPlatform();
}

// App services
services.AddSingleton<AppController>();
services.AddSingleton<ThemeManager>();
services.AddSingleton<ITrayIconController, AppController>();

// Event bus for decoupled messaging
services.AddSingleton<IEventBus<EntriesLoggedEvent>, EventBus<EntriesLoggedEvent>>();

var provider = services.BuildServiceProvider();
AppHost.Services = provider; // For legacy fallback access
```

### Resolution (Startup)

```csharp
// Avalonia initializes App (from DI container or framework instantiation)
app.OnFrameworkInitializationCompleted()
  ├─ Resolves AppController, IAppLogger, ITrayIconController from AppHost.Services
  └─ Calls ((IAppInitializer)app).InitializeAsync(...) with injected services
       └─ App initializes tray, settings, hotkeys, shows UI
```

### Injection Points

- **ViewModels**: Constructor injection of IAppLogger, IGlobalHotkeyService, IEventBus<T>
  - ViewModel exposes Logger and HotkeyService properties for view code-behind
- **AppController**: Constructor injection of 11+ services + IEventBus<EntriesLoggedEvent>
  - Subscribes to EntriesLoggedEvent in constructor
- **App (IAppInitializer)**: Receives injected services in InitializeAsync() method

---

## Event-Driven Architecture

### Problem Solved
Previously, ViewModels called AppController methods directly (tight coupling):
```csharp
// Old (tightly coupled)
viewModel.OnEntriesLogged += (entries) => appController.LogSession(entries);
```

### Solution: EventBus

Publishers (ViewModels) emit domain events; subscribers (AppController) react:

```csharp
// In TimerWidgetViewModel (publisher)
await _eventBus.Publish(new EntriesLoggedEvent { Entries = entries });

// In AppController (subscriber)
_eventBus.Subscribe<EntriesLoggedEvent>(e => 
{
    foreach (var entry in e.Entries)
        _sessionRepository.AddSessionEntry(entry);
});
```

**Benefits**:
- UI doesn't know about AppController
- AppController can be replaced/mocked in tests
- Multiple subscribers can react to the same event
- Event history can be logged for debugging

---

## Startup Sequence

```
Main() [Host]
  ├─ BuildServices() - DI container setup
  ├─ BuildAvaloniaApp() - Avalonia configuration
  └─ .StartWithClassicDesktopLifetime()
       │
       └─ Avalonia Framework
            ├─ App.Initialize() - XAML loading, tray icon discovery
            └─ App.OnFrameworkInitializationCompleted() - DI-injected initialization
                 └─ ((IAppInitializer)app).InitializeAsync(appController, logger, tray, services)
                      ├─ Create and configure system tray icon
                      ├─ Register tray controllers
                      ├─ AppController.InitializeAsync()
                      │  ├─ Load settings
                      │  └─ Register global hotkeys
                      ├─ Show TimerWidget (if not start minimized)
                      └─ Subscribe to EntriesLoggedEvent in AppController
```

---

## Threading & Async Patterns

- **Main Thread**: Avalonia UI thread (dispatcher)
- **Hotkey Thread**: P/Invoke window message loop (separate thread on Windows)
- **Timer**: Uses Dispatcher.UIThread.InvokeAsync() for UI updates
- **Event Bus**: Thread-safe subscription/publication (ConcurrentDictionary + lock)

**Sync-over-Async**: `.Wait()` used only in OnFrameworkInitializationCompleted() (framework callback is synchronous)

---

## Extending the Architecture

### Add a New Platform (e.g., Linux)

1. Create `FocusTimer.Platform.Linux` project (net8.0-linux)
2. Implement IGlobalHotkeyService, IActiveWindowService, etc. using X11/DBus
3. In Host.Program, register conditionally:
   ```csharp
   else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
       services.AddLinuxPlatform();
   ```
4. Multi-target Host: `<TargetFrameworks>net8.0-windows;net8.0-linux</TargetFrameworks>`

### Add a New ViewModel

1. Create in `FocusTimer.App/ViewModels/`
2. Inject IAppLogger, IEventBus<T>, other dependencies into constructor
3. Create corresponding View (.axaml) in `FocusTimer.App/Views/`
4. Register in DI if needed (singleton or factory)

### Add a New Domain Event

1. Create in `FocusTimer.Core/Models/`
2. Inherit no base class (plain DTO with properties)
3. Publish: `await _eventBus.Publish(new MyEvent { ... })`
4. Subscribe: `_eventBus.Subscribe<MyEvent>(e => ...)`

---

## Design Patterns

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Dependency Injection** | Host.Program | Manage service lifetimes and wiring |
| **Service Locator** (legacy) | AppHost.Services | Fallback for framework initialization |
| **Repository** | Persistence, Core.Interfaces | Persist TimeEntry and Settings |
| **Event Bus** | Core.Services | Decoupled async pub/sub messaging |
| **MVVM** | App.ViewModels, Views | Reactive UI state management |
| **Converter** | App.Converters | XAML value transformation |
| **Adapter** | Platform.Windows | Bridge OS APIs to Core interfaces |

---

## Next Steps

1. **Remove AppHost.Services**: Once more code is DI-aware, eliminate the static service locator
2. **Add View Factory**: Use DI to create windows instead of `new TimerWidgetWindow()`
3. **Unit Tests**: Add Core and Persistence test projects
4. **Multi-Platform Host**: Target net8.0;net8.0-windows and create FocusTimer.Platform.Linux
5. **Installer**: Wrap Host.exe in WiX, Inno Setup, or MSIX for distribution
