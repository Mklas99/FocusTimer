// Avalonia's headless Dispatcher.UIThread is bound to whichever thread first calls
// SetupWithoutStarting(). xUnit runs separate test classes on separate threads by
// default, so every test in this assembly must share that one thread.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Headless;

/// <summary>
/// Boots Avalonia's headless platform once per test run so window-backed classes
/// (AppController, TrayStateController, App) can be exercised without a real display.
/// </summary>
public static class HeadlessAvaloniaFixture
{
    private static bool _initialized;
    private static readonly object Gate = new();

    public static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            AppBuilder.Configure<FocusTimer.App.App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            _initialized = true;
        }
    }
}
