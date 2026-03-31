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
3. **Event Bus**: Decouples publishers (ViewModels) from subscribers (AppController) via IEventBus<T>
4. **MVVM Pattern**: Avalonia ViewModels expose properties and commands; Views bind to them
5. **Immutable Models**: Domain entities (TimeEntry, Session, Settings) are immutable POCO classes

---

## Dependency Injection (DI)

### Registration (Host/Program.cs)

All services are registered in a single location for visibility and maintainability:

```csharp
public static IServiceCollection BuildServices()
{
    var services = new ServiceCollection();

    // Logging
    services.AddLogging(configure =>
        configure.AddSerilog(new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(...)
            .CreateLogger()));

    // Core services
    services.AddSingleton<IAppLogger>(sp => 
        new SerilogAdapter(sp.GetRequiredService<ILogger<IAppLogger>>()));
    services.AddSingleton<IAppInitializer, App>();

    // Persistence
    services.AddPersistenceServices();

    // Platform-specific
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        services.AddWindowsPlatform();
    }

    // App services
    services.AddSingleton<AppController>();
    services.AddSingleton<ThemeManager>();
    services.AddSingleton<ITrayIconController>(sp => 
        sp.GetRequiredService<AppController>());

    // Event bus
    services.AddSingleton(typeof(IEventBus<>), typeof(EventBus<>));

    return services;
}
```

### Resolving Services

#### Constructor Injection (Preferred)

```csharp
public class TimerWidgetViewModel : ViewModelBase
{
    private readonly IAppLogger _logger;
    private readonly IEventBus<EntriesLoggedEvent> _eventBus;
    private readonly IGlobalHotkeyService _hotkeyService;

    public TimerWidgetViewModel(
        IAppLogger logger,
        IEventBus<EntriesLoggedEvent> eventBus,
        IGlobalHotkeyService hotkeyService)
    {
        _logger = logger;
        _eventBus = eventBus;
        _hotkeyService = hotkeyService;
    }
}
```

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
await _eventBus.Publish(new EntriesLoggedEvent { Entries = entries });

// AppController (subscriber)
_eventBus.Subscribe<EntriesLoggedEvent>(e =>
{
    foreach (var entry in e.Entries)
        _sessionRepository.AddSessionEntry(entry);
});
```

### Implementing the Event Bus

```csharp
public interface IEventBus<T>
{
    Task Publish(T message);
    IDisposable Subscribe(Action<T> handler);
}

public class EventBus<T> : IEventBus<T>
{
    private readonly ConcurrentDictionary<Guid, Action<T>> _subscribers = new();

    public Task Publish(T message)
    {
        foreach (var handler in _subscribers.Values)
        {
            try
            {
                handler(message);
            }
            catch (Exception ex)
            {
                // Log but don't throw
                Debug.WriteLine($"EventBus error: {ex}");
            }
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(Action<T> handler)
    {
        var id = Guid.NewGuid();
        _subscribers.TryAdd(id, handler);
        return new Unsubscriber(id, _subscribers);
    }

    private class Unsubscriber : IDisposable
    {
        public void Dispose() => _subscribers.TryRemove(_id, out _);
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

```csharp
// In Host/Program.cs
services.AddSingleton<IEventBus<MyDomainEvent>, EventBus<MyDomainEvent>>();

// In AppController constructor
public AppController(IEventBus<MyDomainEvent> eventBus, ...)
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

public class StatsViewModel : ViewModelBase
{
    private readonly ISessionRepository _repository;
    private readonly IAppLogger _logger;
    
    private int _todayTotal;
    public int TodayTotal
    {
        get => _todayTotal;
        set => this.RaiseAndSetIfChanged(ref _todayTotal, value);
    }

    public StatsViewModel(ISessionRepository repository, IAppLogger logger)
    {
        _repository = repository;
        _logger = logger;
        LoadStats();
    }

    private void LoadStats()
    {
        try
        {
            var entries = _repository.GetEntriesForDate(DateTime.Today);
            TodayTotal = entries.Sum(e => (int)e.Duration.TotalMinutes);
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
    private readonly IEventBus<SettingsChangedEvent> _eventBus;

    public async Task SaveAsync(Settings settings)
    {
        var old = _currentSettings;
        // ... save to JSON ...
        _currentSettings = settings;
        
        await _eventBus.Publish(new SettingsChangedEvent 
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
[TestFixture]
public class TimerWidgetViewModelTests
{
    private TimerWidgetViewModel _viewModel;
    private Mock<IAppLogger> _mockLogger;
    private Mock<IEventBus<EntriesLoggedEvent>> _mockEventBus;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<IAppLogger>();
        _mockEventBus = new Mock<IEventBus<EntriesLoggedEvent>>();
        
        _viewModel = new TimerWidgetViewModel(
            _mockLogger.Object,
            _mockEventBus.Object,
            new Mock<IGlobalHotkeyService>().Object);
    }

    [Test]
    public async Task StartTimer_PublishesStartedEvent()
    {
        // Arrange
        var expectedEvent = new TimerStartedEvent();

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
[TestFixture]
public class EventBusTests
{
    private EventBus<TestEvent> _eventBus;

    [SetUp]
    public void Setup() => _eventBus = new EventBus<TestEvent>();

    [Test]
    public async Task Subscribe_HandlerReceivesPublishedMessage()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        _eventBus.Subscribe(e => receivedEvent = e);
        var msg = new TestEvent { Data = "test" };

        // Act
        await _eventBus.Publish(msg);

        // Assert
        Assert.That(receivedEvent?.Data, Is.EqualTo("test"));
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

- **Classes/Interfaces**: PascalCase (e.g., `AppController`, `ISessionRepository`)
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

Set environment variable before running:
```powershell
$env:SERILOG_MINIMUM_LEVEL = "Verbose"
dotnet run --project src/FocusTimer.Host
```

### Inspect Event Bus Traffic

Add debug logging to EventBus.cs:
```csharp
public async Task Publish(T message)
{
    System.Diagnostics.Debug.WriteLine($"[EventBus] Publishing {typeof(T).Name}");
    // ... rest of implementation
}
```

### Break on Exceptions

In Visual Studio: Debug → Windows → Exception Settings → Tick "CLR Exceptions"

### Watch Service Registrations

Add in Host.Program.Main():
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
**Solution**: Verify service is registered in Host.Program.BuildServices()

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

**Last Updated**: March 2026
