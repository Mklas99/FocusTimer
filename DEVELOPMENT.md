# FocusTimer Development Guide

This guide covers extending FocusTimer's architecture, adding features, and understanding the design patterns used throughout the codebase.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Dependency Injection (DI)](#dependency-injection-di)
3. [Event-Driven Architecture](#event-driven-architecture)
4. [Adding New Features](#adding-new-features)
5. [Writing Tests](#writing-tests)
6. [Code Quality & Standards](#code-quality--standards)
7. [Common Patterns](#common-patterns)
8. [Debugging Tips](#debugging-tips)

---

## Project Overview

### Five-Project Architecture

```
FocusTimer.Host (entry point, DI setup)
├── FocusTimer.App (UI: ViewModels, XAML, Windows)
├── FocusTimer.Core (models, interfaces, EventBus)
├── FocusTimer.Persistence (data: JSON, CSV)
└── FocusTimer.Platform.Windows (OS integrations)
```

### Dependency Direction

- **No circular dependencies** – Host depends on everything, App/Persistence depend on Core, Platform depends only on Core
- **Core is self-contained** – No external dependencies except Serilog

### Key Design Principles

1. **Dependency Injection**: All services are wired in Host.Program and resolved via IServiceProvider
2. **Interface-Based Contracts**: All external integrations (logging, persistence, OS features) are defined as interfaces in Core
3. **Event Bus**: Decouples publishers (ViewModels) from subscribers (AppController) via a single, non-generic `IEventBus`
4. **MVVM Pattern**: Avalonia ViewModels expose properties and commands; Views bind to them
5. **Immutable Models**: Domain entities such as `TimeEntry` and `Settings` are immutable or init-only POCOs

### Persistence dependency

The current worklog CSV schema uses [CsvHelper](https://joshclose.github.io/CsvHelper/) `33.1.0`, pinned by
`CsvHelperVersion` in `Directory.Build.props`. It is the streaming RFC 4180 codec used only by
`FocusTimer.Persistence`; Core stays storage-library independent. CsvHelper is dual-licensed under MS-PL and
Apache-2.0; this project uses the Apache-2.0 option and retains its required notices in distributed packages.

### Current worklog behavior

`IWorklogStore` is the only Core boundary for worklogs. Its CSV implementation writes schema version `1` daily
files at `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv`, with header-driven RFC 4180 parsing and stable column order.
Entries have durable entry/session identities, offset-aware timestamps, derived duration, provenance, revision,
and UTC last-modified metadata. Use the store's typed outcomes and warnings rather than parsing files directly.
The app retains a failed append in memory and retries it with the same entry IDs while running; this improves
transient-failure recovery but does not provide a crash-surviving outbox.

This is a development-only format change. If a daily file has an unsupported/older header, the store refuses to
append or rewrite it and leaves its bytes unchanged. Move or remove that development file manually before using
the new build. Do not add migration, backup, installer, or rollback behavior here: the release-grade strategy is
owned by `OI-20` in `docs/versions/current/OpenIssues.md`.

---

## Dependency Injection (DI)

### Registration (Host/Program.cs)

All services are registered in `Program`'s static constructor (there is no separate `BuildServices()` method — it runs once, before `Main()`):

```csharp
var services = new ServiceCollection();

// Logging: a Serilog logger is built directly (not via Microsoft.Extensions.Logging)
// and wrapped in SerilogAppLogger, which implements IAppLogger.
services.AddSingleton<IAppLogger>(appLogger);

// Platform-specific: real Windows implementations, or Linux no-op stubs otherwise
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<IGlobalHotkeyService, Platform.Windows.WindowsHotkeyService>();
    services.AddSingleton<ITrayIconController, TrayStateController>();
    // ...and the other Windows service registrations
}
else
{
    services.AddSingleton<IGlobalHotkeyService, LinuxHotkeyServiceStub>();
    // ...and the other Linux*Stub registrations
}

// Persistence
FocusTimer.Persistence.ServiceCollectionExtensions.AddPersistenceServices(services);

// App/Core services
services.AddSingleton<AppController>();
services.AddSingleton<ThemeManager>();

// Event bus: single, non-generic instance for the whole app
services.AddSingleton<IEventBus, EventBus>();

var provider = services.BuildServiceProvider();
```

### Resolving Services

#### Constructor Injection (Preferred)

```csharp
public class TimerWidgetViewModel : ViewModelBase
{
    private readonly IAppLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly IGlobalHotkeyService? _hotkeyService;

    public TimerWidgetViewModel(
        IAppLogger logger,
        IEventBus eventBus,
        IGlobalHotkeyService? hotkeyService)
    {
        _logger = logger;
        _eventBus = eventBus;
        _hotkeyService = hotkeyService;
    }
}
```

(The real `TimerWidgetViewModel` constructor takes several more dependencies — `ISettingsProvider`, `IWorklogStore`, `SessionTracker`, `BreakReminderService`, `ITimerService` — trimmed here for clarity.)

#### Method Injection (Startup Only)

```csharp
public override void OnFrameworkInitializationCompleted()
{
    base.OnFrameworkInitializationCompleted();

    if (AppHost.Services != null)
    {
        var appController = AppHost.Services.GetService<AppController>();
        var logger = AppHost.Services.GetService<IAppLogger>();

        // Use services...
    }
}
```

#### Service Locator Pattern (Avoid)

AppHost.Services is a fallback for the framework initialization hook. Avoid using it elsewhere; use constructor injection instead.

---

## Event-Driven Architecture

### Problem: Tight Coupling

Without EventBus:
```csharp
// ViewModel
_appController.LogSession(entries);  // Direct dependency

// Problem: Hard to test, hard to swap implementations
```

### Solution: EventBus

```csharp
// ViewModel (publisher)
_eventBus.Publish(new EntriesLoggedEvent { Entries = entries });

// AppController (subscriber)
_eventBus.Subscribe<EntriesLoggedEvent>(e =>
{
    _ = PersistEntriesAsync(e.Entries);
});

private async Task PersistEntriesAsync(IReadOnlyCollection<TimeEntry> entries)
{
    var outcome = await _worklogStore.AppendAsync(entries);
    if (!outcome.IsSuccess)
        _logger.LogWarning(outcome.Message ?? $"Worklog persistence failed: {outcome.Kind}");
}
```

### The Event Bus (actual implementation)

`IEventBus` is a single, non-generic interface — one bus instance for the whole app, keyed internally by event type, not one bus per `T`. `Publish` is synchronous (`void`, not `Task`).

```csharp
// FocusTimer.Core/Interfaces/IEventBus.cs
public interface IEventBus
{
    void Publish<T>(T message);
    IDisposable Subscribe<T>(Action<T> handler);
}

// FocusTimer.Core/Services/EventBus.cs
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T message)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            foreach (var d in list.ToArray()) // copy to avoid mutation during iteration
            {
                try { ((Action<T>)d)?.Invoke(message); }
                catch { /* swallow to avoid breaking the publisher */ }
            }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (list) { list.Add(handler); }
        return new Subscription<T>(_handlers, handler); // removes handler on Dispose
    }
}
```

### Creating Domain Events

```csharp
// In Core/Models/
public class MyDomainEvent
{
    public required string Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### Registering Event Handlers

No per-event registration is needed — `IEventBus` is registered once (`services.AddSingleton<IEventBus, EventBus>();`) and handles every event type. Just inject `IEventBus` and subscribe:

```csharp
// In AppController constructor
public AppController(IEventBus eventBus, ...)
{
    _eventBus = eventBus;
    _eventBus.Subscribe<MyDomainEvent>(e => HandleEvent(e));
}
```

---

## Adding New Features

### Example 1: Add a New Platform Service

**Goal**: Add Windows clipboard access

**Steps**:

1. **Define the interface** (Core/Interfaces/IClipboardService.cs):
```csharp
namespace FocusTimer.Core.Interfaces;

public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
}
```

2. **Implement on Windows** (Platform.Windows/WindowsClipboardService.cs):
```csharp
using System.Runtime.InteropServices;
using FocusTimer.Core.Interfaces;

namespace FocusTimer.Platform.Windows;

public class WindowsClipboardService : IClipboardService
{
    public async Task<string> GetTextAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return System.Windows.Forms.Clipboard.GetText();
            }
            catch
            {
                return string.Empty;
            }
        });
    }

    public async Task SetTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
            }
            catch { }
        });
    }
}
```

3. **Register in Host/Program.cs**:
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<IClipboardService, WindowsClipboardService>();
}
```

4. **Inject and use**:
```csharp
public class MyViewModel
{
    public MyViewModel(IClipboardService clipboard) { ... }

    public async Task CopyToClipboard(string text)
    {
        await _clipboard.SetTextAsync(text);
    }
}
```

### Example 2: Add a New ViewModel

**Goal**: Create a stats/analytics view

**Steps**:

1. **Create ViewModel** (App/ViewModels/StatsViewModel.cs):
```csharp
using ReactiveUI;

namespace FocusTimer.App.ViewModels;

public sealed class StatsViewModel : ViewModelBase
{
    private readonly IWorklogStore _worklogStore;
    private readonly IAppLogger _logger;
    private readonly TimeProvider _timeProvider;

    private int _todayTotal;
    public int TodayTotal
    {
        get => _todayTotal;
        set => this.RaiseAndSetIfChanged(ref _todayTotal, value);
    }

    public StatsViewModel(IWorklogStore worklogStore, IAppLogger logger, TimeProvider timeProvider)
    {
        _worklogStore = worklogStore;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task RefreshAsync()
    {
        try
        {
            var date = _timeProvider.GetLocalNow().Date;
            var zone = _timeProvider.LocalTimeZone;
            var start = new DateTimeOffset(date, zone.GetUtcOffset(date));
            var endDate = date.AddDays(1);
            var end = new DateTimeOffset(endDate, zone.GetUtcOffset(endDate));
            var result = await _worklogStore.QueryAsync(new WorklogQuery(start, end));
            if (!result.Outcome.IsSuccess)
                return; // Log/surface the typed outcome in the real ViewModel.

            TodayTotal = (int)result.Entries.Aggregate(TimeSpan.Zero, (sum, entry) => sum + entry.Duration).TotalMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load stats", ex);
        }
    }
}
```

2. **Create View** (App/Views/StatsView.axaml):
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="FocusTimer.App.Views.StatsView">
    <StackPanel>
        <TextBlock Text="{Binding TodayTotal}" />
    </StackPanel>
</UserControl>
```

3. **Register in DI** (if needed as singleton):
```csharp
services.AddSingleton<StatsViewModel>();
```

### Example 3: Add a New Domain Event

**Goal**: Track when settings change

**Steps**:

1. **Create event class** (Core/Models/SettingsChangedEvent.cs):
```csharp
namespace FocusTimer.Core.Models;

public class SettingsChangedEvent
{
    public required Settings OldSettings { get; init; }
    public required Settings NewSettings { get; init; }
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
```

2. **Publish from settings provider** (Persistence/JsonSettingsProvider.cs):
```csharp
public class JsonSettingsProvider : ISettingsProvider
{
    private readonly IEventBus _eventBus;

    public async Task SaveAsync(Settings settings)
    {
        var old = _currentSettings;
        // ... save to JSON ...
        _currentSettings = settings;

        _eventBus.Publish(new SettingsChangedEvent
        {
            OldSettings = old,
            NewSettings = settings
        });
    }
}
```

3. **Subscribe in Host/Program.cs or AppController**:
```csharp
_eventBus.Subscribe<SettingsChangedEvent>(e =>
{
    if (e.NewSettings.Theme != e.OldSettings.Theme)
        _themeManager.ApplyTheme(e.NewSettings.Theme);
});
```

---

## Writing Tests

### Unit Testing ViewModels

```csharp
public class TimerWidgetViewModelTests
{
    private TimerWidgetViewModel _viewModel;
    private Mock<IAppLogger> _mockLogger;
    private Mock<IEventBus> _mockEventBus;

    public TimerWidgetViewModelTests()
    {
        _mockLogger = new Mock<IAppLogger>();
        _mockEventBus = new Mock<IEventBus>();

        _viewModel = new TimerWidgetViewModel(
            _mockLogger.Object,
            _mockEventBus.Object,
            new Mock<IGlobalHotkeyService>().Object);
            // (plus the other constructor dependencies - see above)
    }

    [Fact]
    public async Task StartTimer_PublishesStartedEvent()
    {
        // Act
        await _viewModel.StartTimer();

        // Assert
        _mockEventBus.Verify(x =>
            x.Publish(It.IsAny<TimerStartedEvent>()), Times.Once);
    }
}
```

### Testing Services

```csharp
public class EventBusTests
{
    private EventBus _eventBus;

    public EventBusTests() => _eventBus = new EventBus();

    [Fact]
    public void Subscribe_HandlerReceivesPublishedMessage()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        _eventBus.Subscribe<TestEvent>(e => receivedEvent = e);
        var msg = new TestEvent { Data = "test" };

        // Act
        _eventBus.Publish(msg);

        // Assert
        Assert.Equal("test", receivedEvent?.Data);
    }

    private class TestEvent
    {
        public string? Data { get; set; }
    }
}
```

---

## Code Quality & Standards

### StyleCop Rules

FocusTimer enforces StyleCop formatting via Directory.Build.props:

- **Using directives**: Alphabetically sorted, System first
- **Access modifiers**: Explicit on all members
- **Line length**: Keep under 120 characters when possible
- **Documentation**: Public types and methods require XML doc comments

### Format Code

```powershell
dotnet format
```

### Analyze with SonarQube

```powershell
./scripts/run-sonar-dotnet.ps1 -token <token>
```

### Naming Conventions

- **Classes/Interfaces**: PascalCase (e.g., `AppController`, `IWorklogStore`)
- **Methods**: PascalCase (e.g., `RegisterHotkeys()`)
- **Properties**: PascalCase (e.g., `IsRunning`)
- **Private fields**: camelCase with underscore (e.g., `_logger`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `IDLE_TIMEOUT_MS`)

---

## Common Patterns

### Property with Notification (MVVM)

```csharp
private bool _isRunning;
public bool IsRunning
{
    get => _isRunning;
    set => this.RaiseAndSetIfChanged(ref _isRunning, value);
}
```

### Command with Parameter

```csharp
public ReactiveCommand<TimeSpan, Unit> SetTimerCommand { get; }

public MyViewModel()
{
    SetTimerCommand = ReactiveCommand.Create<TimeSpan>(duration =>
    {
        // Handle command
    });
}
```

### Safe Singleton Registration

```csharp
services.AddSingleton<MySingleton>();
// or with factory
services.AddSingleton<IMyInterface>(sp =>
    new MyImplementation(sp.GetRequiredService<Dependency1>(), ...));
```

### Logging Pattern

```csharp
using FocusTimer.Core.Interfaces;

public class MyService
{
    private readonly IAppLogger _logger;

    public MyService(IAppLogger logger) => _logger = logger;

    public void DoSomething()
    {
        try
        {
            _logger.LogInformation("Doing something...");
            // Work...
            _logger.LogDebug("Something completed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to do something", ex);
        }
    }
}
```

---

## Debugging Tips

### Enable Verbose Logging

There's no environment variable for log level today. Two options:
- Change `.MinimumLevel.Debug()` in `Program.cs`'s Serilog setup directly.
- Unlock the hidden developer mode (click the version label 7x in Settings → About) to access an in-app log-level picker.

Log output location can be redirected via `FOCUSTIMER_LOG_DIR`:
```powershell
$env:FOCUSTIMER_LOG_DIR = "C:\temp\focustimer-logs"
dotnet run --project src/FocusTimer.Host
```

### Inspect Event Bus Traffic

Add debug logging to EventBus.cs:
```csharp
public void Publish<T>(T message)
{
    System.Diagnostics.Debug.WriteLine($"[EventBus] Publishing {typeof(T).Name}");
    // ... rest of implementation
}
```

### Break on Exceptions

In Visual Studio: Debug → Windows → Exception Settings → Tick "CLR Exceptions"

### Watch Service Registrations

Add at the end of `Program`'s static constructor, right after `services.BuildServiceProvider()`:
```csharp
var provider = services.BuildServiceProvider();
// Use reflection to inspect registrations
foreach (var descriptor in services)
{
    System.Diagnostics.Debug.WriteLine(
        $"{descriptor.ServiceType.Name} -> {descriptor.ImplementationType?.Name}");
}
```

### Profile Startup Time

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
// ... initialization code ...
sw.Stop();
_logger.LogInformation($"Startup took {sw.ElapsedMilliseconds}ms");
```

---

## Troubleshooting Common Issues

### "Service not registered" Exception

**Problem**: `InvalidOperationException` when resolving a service
**Solution**: Verify service is registered in `Program`'s static constructor (`FocusTimer.Host/Program.cs`)

### ViewModel Properties Not Updating

**Problem**: UI doesn't reflect ViewModel changes
**Solution**: Use `RaiseAndSetIfChanged()` for all properties in MVVM ViewModels

### EventBus Subscribers Not Firing

**Problem**: Published event not received by subscribers
**Solution**:
- Verify event type matches exactly (including namespace)
- Ensure subscriber is registered before publish
- Check for exceptions in handler (swallowed by EventBus)

### Hotkeys Not Working

**Problem**: Global hotkey doesn't trigger
**Solution**:
- Call RegisterHotkeys() after window is shown (needs window handle)
- Check if another app registered the same hotkey
- Verify hotkey didn't conflict with Windows shortcuts

---

## Resources

- **Avalonia**: [docs.avaloniaui.net](https://docs.avaloniaui.net)
- **ReactiveUI**: [reactiveui.net](https://reactiveui.net)
- **.NET DI**: [microsoft.com](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- **Serilog**: [serilog.net](https://serilog.net)

---

## Contributing

When contributing new features:

1. Create a branch: `git checkout -b feature/my-feature`
2. Follow [naming conventions](#naming-conventions) and [StyleCop rules](#stylecop-rules)
3. Write tests for business logic
4. Test on Windows 10+ target OS
5. Format and lint: `dotnet format && dotnet build`
6. Commit with clear messages
7. Open a PR with description of changes

---

See [ARCHITECTURE.md](ARCHITECTURE.md) for the current project structure and service inventory — this guide's line-by-line examples can drift from it faster than that reference does.
