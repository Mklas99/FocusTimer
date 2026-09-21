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
Program (static ctor) - registers all DI services, runs once before Main()
Program.Main(string[] args)
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
- Domain models (immutable TimeEntry and Settings)
- Service interfaces (contracts for all external integrations)
- Concrete, platform-agnostic business logic services (see below) that App/Host wire up via DI
- Event infrastructure (IEventBus, EventBus, domain events)
- Logging interfaces (IAppLogger)
- AppHost static accessor (for legacy global service access—being phased out)
- IAppInitializer interface (DI-based app initialization contract)

**Core Service Interfaces**:
```csharp
IAppLogger           // Structured logging (implemented in Core: SerilogAppLogger)
ISettingsProvider    // Load/save settings (implemented in Persistence)
IWorklogStore        // Append, query, patch, and delete worklog entries (implemented in Persistence)
IGlobalHotkeyService // Register OS hotkeys (Platform.Windows / Linux stub)
IActiveWindowService // Detect window/app in focus (Platform.Windows / Linux stub)
INotificationService // Show Toast notifications (Platform.Windows / Linux stub)
ITrayIconController  // Manage system tray (implemented in App: TrayStateController)
IIdleDetectionService // Poll OS idle state (Platform.Windows / Linux stub)
IAutoStartService    // Register app in startup mechanisms (Platform.Windows / Linux stub)
IThemeService        // Load/apply/import/export themes (implemented in Core: ThemeService)
ITimerService        // Timer state and elapsed-time tracking (implemented in Core: TimerService)
```

**Core.Services (concrete, no interface — used directly by App/Host)**:
- `SessionTracker` — tracks active-window changes, builds TimeEntry segments for the current session
- `BreakReminderService` — fires break reminders on an interval, tracks acknowledgement
- `TodayStatsService` — aggregates today's tracked time for the tray tooltip/UI
- `EventBus` — implements `IEventBus`

**Event Bus Pattern**:

`IEventBus` is a single, non-generic bus (there is one instance for the whole app, not one per event type); `Publish` is synchronous.
```csharp
IEventBus
  ├─ void Publish<T>(message) - notify all subscribers of type T
  └─ IDisposable Subscribe<T>(Action<T> handler) - register handler

// Usage in publisher (ViewModel):
eventBus.Publish(new EntriesLoggedEvent { Entries = ... });

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

**Purpose**: Data persistence implementations (settings and versioned worklogs).

**Key Responsibilities**:
- JSON settings provider (load/save application settings)
- Current-schema CSV worklog store (idempotent append, query, same-day patch, and delete)
- ServiceCollectionExtensions for DI registration

**Key Components**:

- **JsonSettingsProvider.cs**
  - Implements ISettingsProvider
  - Stores settings in `%APPDATA%\Roaming\FocusTimer\settings.json` (supports custom path injection for isolated test execution)
  - Automatic JSON serialization/deserialization with sane defaults

- **CsvSessionRepository.cs**
  - Implements `IWorklogStore`
  - Stores current-schema entries at `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv`
  - Uses header-driven RFC 4180 CSV, durable entry/session IDs, and typed outcomes
  - Serializes per-file mutations and uses same-directory atomic replacement for patch/delete
  - Retains eligible finalized current-schema daily files only

- **ServiceCollectionExtensions.cs**
  - AddPersistenceServices() extension method
  - Registers JsonSettingsProvider and CsvSessionRepository as singletons
  - Called from Host.Program during DI setup

**Data Format**: Schema version `1` has a stable header containing `EntryId`, `SessionId`, offset-aware
`StartedAt`/`EndedAt`, derived duration, app/window/project data, provenance, revision, and UTC modification
time. Text fields use RFC 4180 escaping and may contain commas, quotes, Unicode, and line breaks. The store
rejects an existing unsupported header without changing the file; it does not read or migrate prior development
formats.

**Extensibility**:
- Swap CSV implementation for SQLite, PostgreSQL, or cloud storage by providing an alternate `IWorklogStore`
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
- Auto-start registry management

System tray integration (`ITrayIconController`) is implemented in FocusTimer.App (`TrayStateController`), not here — it drives Avalonia's own tray APIs rather than a Win32 call, so it isn't platform-specific.

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

**Linux today**: there is no separate `FocusTimer.Platform.Linux` project yet. `Program.cs` registers no-op stub implementations from `FocusTimer.Core/Stubs` (`LinuxHotkeyServiceStub`, `LinuxActiveWindowServiceStub`, `LinuxNotificationServiceStub`, `LinuxIdleDetectionServiceStub`, `LinuxAutoStartServiceStub`) when not running on Windows, so the app builds and runs on Linux with these features inert. Real X11/DBus-backed implementations (likely in their own `FocusTimer.Platform.Linux` project) are tracked as `OI-06`.

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

All wiring happens in `Program`'s static constructor (there is no separate `BuildServices()` method):

```csharp
var services = new ServiceCollection();

// Logger (Serilog), built first so early startup can log
services.AddSingleton<IAppLogger>(appLogger); // pre-built SerilogAppLogger instance

// Platform services: real Windows implementations, or Linux no-op stubs
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<IActiveWindowService, Platform.Windows.WindowsActiveWindowService>();
    services.AddSingleton<INotificationService, Platform.Windows.WindowsNotificationService>();
    services.AddSingleton<IAutoStartService, Platform.Windows.WindowsAutoStartService>();
    services.AddSingleton<IGlobalHotkeyService, Platform.Windows.WindowsHotkeyService>();
    services.AddSingleton<ITrayIconController, TrayStateController>();
    services.AddSingleton<IIdleDetectionService, Platform.Windows.WindowsIdleDetectionService>();
}
else
{
    services.AddSingleton<IActiveWindowService, LinuxActiveWindowServiceStub>();
    // ...and the other Linux*Stub registrations (see FocusTimer.Core/Stubs)
}

// Persistence (falls back to manual registration if the extension method isn't found via reflection)
FocusTimer.Persistence.ServiceCollectionExtensions.AddPersistenceServices(services);

// Core business services
services.AddSingleton<IThemeService, Core.Services.ThemeService>();
services.AddSingleton<ThemeManager>();
services.AddSingleton<Core.Interfaces.IEventBus, Core.Services.EventBus>();
services.AddSingleton<SessionTracker>();
services.AddSingleton<ITimerService, TimerService>();
services.AddSingleton<BreakReminderService>();
services.AddSingleton<TodayStatsService>();
services.AddSingleton<AppController>();

// ViewModels (transient, plus factory delegates for windows created after startup)
services.AddTransient<MainWindowViewModel>();
services.AddTransient<TimerWidgetViewModel>();
services.AddTransient<SettingsWindowViewModel>();
services.AddTransient<Func<TimerWidgetViewModel>>(sp => () => sp.GetRequiredService<TimerWidgetViewModel>());
services.AddTransient<Func<SettingsWindowViewModel>>(sp => () => sp.GetRequiredService<SettingsWindowViewModel>());

var provider = services.BuildServiceProvider();
Program.Services = provider;
AppHost.Services = provider; // For legacy fallback access
```

### Resolution (Startup)

```csharp
// Avalonia initializes App (instantiated by the framework, not resolved from DI)
app.OnFrameworkInitializationCompleted()
  ├─ Resolves AppController, IAppLogger, ITrayIconController from AppHost.Services
  └─ Calls ((IAppInitializer)app).InitializeAsync(...) with injected services
       └─ App initializes tray, settings, hotkeys, shows UI
```

### Injection Points

- **ViewModels**: Constructor injection of IAppLogger, IGlobalHotkeyService, IEventBus
  - ViewModel exposes Logger and HotkeyService properties for view code-behind
- **AppController**: Constructor injection of ~11 services + IEventBus
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
_eventBus?.Publish(new EntriesLoggedEvent { Entries = entries });

// In AppController (subscriber)
_eventBus.Subscribe<EntriesLoggedEvent>(e => OnEntriesLogged(e.Entries));
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
  ├─ (static ctor already ran) - DI container setup
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

### Add a New Platform (e.g., real Linux support)

The `else` branch in `Program.cs`'s static constructor already registers Linux no-op stubs (`FocusTimer.Core/Stubs`) for every platform interface, so the app runs (inertly) on Linux today. To give it real functionality (`OI-06`):

1. Create a `FocusTimer.Platform.Linux` project (or extend the stubs in place, if staying in Core) implementing IGlobalHotkeyService, IActiveWindowService, etc. using X11/DBus
2. In `Program.cs`, replace the `Linux*Stub` registrations in the `else` branch with the real implementations
3. If distributing a Linux build, multi-target Host: `<TargetFrameworks>net8.0-windows;net8.0-linux</TargetFrameworks>` (Host is currently `net8.0-windows` only)

### Add a New ViewModel

1. Create in `FocusTimer.App/ViewModels/`
2. Inject IAppLogger, IEventBus, other dependencies into constructor
3. Create corresponding View (.axaml) in `FocusTimer.App/Views/`
4. Register in DI if needed (singleton or factory)

### Add a New Domain Event

1. Create in `FocusTimer.Core/Models/`
2. Inherit no base class (plain DTO with properties)
3. Publish: `_eventBus.Publish(new MyEvent { ... })`
4. Subscribe: `_eventBus.Subscribe<MyEvent>(e => ...)`

---

## Design Patterns

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Dependency Injection** | Host.Program | Manage service lifetimes and wiring |
| **Service Locator** (legacy) | AppHost.Services | Fallback for framework initialization |
| **Repository** | Persistence, Core.Interfaces | Persist TimeEntry and Settings |
| **Event Bus** | Core.Services | Decoupled synchronous pub/sub messaging |
| **MVVM** | App.ViewModels, Views | Reactive UI state management |
| **Converter** | App.Converters | XAML value transformation |
| **Adapter** | Platform.Windows | Bridge OS APIs to Core interfaces |

---

## Next Steps

1. **Remove AppHost.Services**: Once more code is DI-aware, eliminate the static service locator (still used in `App.axaml.cs`, `TimerWidgetWindow.axaml.cs`, `Program.cs`)
2. **Add View Factory**: `AppController` still does `new TimerWidgetWindow(...)` directly; ViewModels are already DI-created via `Func<T>` factories, windows aren't yet
3. **Real Linux platform implementations**: stubs exist and are wired up (see `FocusTimer.Core/Stubs`), but hotkeys/notifications/idle/auto-start are inert on Linux — tracked as `OI-06`
4. **Multi-Platform Host**: Host is still `net8.0-windows`-only (`WinExe`); multi-targeting is required to ship a Linux build once (3) lands

See `docs/versions/current/OpenIssues.md` for the full, current backlog of known gaps.
