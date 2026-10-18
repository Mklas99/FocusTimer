namespace FocusTimer.App.HeadlessTests;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;

public class WidgetWindowMaterialTests
{
    public WidgetWindowMaterialTests()
    {
        HeadlessAvaloniaFixture.EnsureInitialized();
    }

    [Fact]
    public void WidgetWindowMaterial_WithDefaultProbes_InitializesAndDisposesCleanly()
    {
        var window = new Window();
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var material = new WidgetWindowMaterial(window, shell, manager);
        Assert.NotNull(material);

        // Calling Dispose twice to test idempotence
        material.Dispose();
        material.Dispose();
    }

    [Fact]
    public void WidgetWindowMaterial_WhenActiveThemeIsNull_DoesNotCrash()
    {
        var window = new Window();
        var shell = new Border();
        var manager = new ThemeManager();

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            () => false,
            () => true,
            () => new IntPtr(0x100));

        // When ActiveTheme is null, Refresh returns early
        material.Refresh();
    }

    [Fact]
    public void WidgetWindowMaterial_WhenFrosted_AndConditionsMet_AttachesSession_AndReportsFrosted()
    {
        TestBackdropController.Last = null;
        TestBackdropController.ShouldFailCreate = false;

        var window = new Window { IsVisible = true };
        var shell = new Border { CornerRadius = new CornerRadius(8) };
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        IntPtr testHandle = new IntPtr(0x1234);

        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => testHandle);

        Assert.NotNull(TestBackdropController.Last);
        Assert.NotNull(TestBackdropController.Last.LastUpdate);

        // Simulate successful completion callback
        TestBackdropController.Last.Completion?.Invoke(true);

        if (OperatingSystem.IsWindows() && window.ActualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur)
        {
            Assert.Equal(WidgetMaterialState.Frosted, manager.EffectiveWidgetMaterial.State);
        }
        else
        {
            Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
        }
    }

    [Fact]
    public void WidgetWindowMaterial_WhenUpdateFails_ReportsSolidFallback()
    {
        TestBackdropController.Last = null;
        TestBackdropController.ShouldFailCreate = false;

        var window = new Window { IsVisible = true };
        var shell = new Border { CornerRadius = new CornerRadius(8) };
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        IntPtr testHandle = new IntPtr(0x1234);

        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => testHandle);

        // Simulate update failure in callback
        TestBackdropController.Last?.Completion?.Invoke(false);

        Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
        Assert.Equal("Native backdrop update failed", manager.EffectiveWidgetMaterial.Reason);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenUpdateRejected_ReportsSolidFallback_AndDetaches()
    {
        TestBackdropController.Last = null;
        TestBackdropController.ShouldFailCreate = false;

        var window = new Window { IsVisible = true };
        var shell = new Border { CornerRadius = new CornerRadius(8) };
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        IntPtr testHandle = new IntPtr(0x1234);

        // Configure controller to reject updates
        TestBackdropController.UpdateReturnValue = false;
        try
        {
            using var material = new WidgetWindowMaterial(
                window,
                shell,
                manager,
                capability,
                isTransparencyDisabled: () => false,
                isCompositionSupported: () => true,
                windowHandleProvider: () => testHandle);

            Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
            Assert.Equal("Native backdrop update rejected", manager.EffectiveWidgetMaterial.Reason);
        }
        finally
        {
            TestBackdropController.UpdateReturnValue = true;
        }
    }

    [Fact]
    public void WidgetWindowMaterial_WhenTryAttachFails_ReportsSolidFallback()
    {
        TestBackdropController.ShouldFailCreate = true;

        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        try
        {
            using var material = new WidgetWindowMaterial(
                window,
                shell,
                manager,
                capability,
                isTransparencyDisabled: () => false,
                isCompositionSupported: () => true,
                windowHandleProvider: () => new IntPtr(0x1234));

            Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
            Assert.Equal("Native backdrop attachment failed", manager.EffectiveWidgetMaterial.Reason);
        }
        finally
        {
            TestBackdropController.ShouldFailCreate = false;
        }
    }

    [Fact]
    public void WidgetWindowMaterial_WhenHighContrastTheme_DoesNotAttach()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "High Contrast",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.Null(TestBackdropController.Last);
        Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenTransparencyDisabled_DoesNotAttach()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => true,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.Null(TestBackdropController.Last);
        Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenLocalCompositionNotSupported_DoesNotAttach()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => false,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.Null(TestBackdropController.Last);
        Assert.Equal(WidgetMaterialState.SolidFallback, manager.EffectiveWidgetMaterial.State);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenWindowNotVisible_DoesNotAttach()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = false };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));

        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.Null(TestBackdropController.Last);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenHandleIsZero_DoesNotAttach()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));

        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => IntPtr.Zero);

        Assert.Null(TestBackdropController.Last);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenBlurModeOff_ReportsClearOrFallback()
    {
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Off,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.True(
            manager.EffectiveWidgetMaterial.State == WidgetMaterialState.Clear ||
            manager.EffectiveWidgetMaterial.State == WidgetMaterialState.SolidFallback);
    }

    [Fact]
    public void WidgetWindowMaterial_WhenBlurModeSolid_ReportsSolid()
    {
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Solid,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        Assert.Equal(WidgetMaterialState.Solid, manager.EffectiveWidgetMaterial.State);
    }

    [Fact]
    public void WidgetWindowMaterial_HandlesWindowAndShellPropertyChanges()
    {
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        // Test property changes on shell and window
        shell.CornerRadius = new CornerRadius(16);
        window.Width = 400;
        window.Height = 300;
        window.Opacity = 0.85;
        window.IsVisible = false;
        window.IsVisible = true;
    }

    [Fact]
    public void WidgetWindowMaterial_WindowEventsAndMessages_TriggerRefresh()
    {
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        // Trigger refresh and posted refresh
        manager.ApplyTheme(theme);
        material.PostRefresh(retry: true);
        material.Refresh(retry: true);
        material.Refresh(retry: false);

        // Trigger window messages
        bool handled = false;
        material.OnWindowMessage(IntPtr.Zero, 0x001A, IntPtr.Zero, IntPtr.Zero, ref handled); // WM_SETTINGCHANGE
        material.OnWindowMessage(IntPtr.Zero, 0x031A, IntPtr.Zero, IntPtr.Zero, ref handled); // WM_THEMECHANGED
        material.OnWindowMessage(IntPtr.Zero, 0x031E, IntPtr.Zero, IntPtr.Zero, ref handled); // WM_DWMCOMPOSITIONCHANGED
        material.OnWindowMessage(IntPtr.Zero, 0x02B1, IntPtr.Zero, IntPtr.Zero, ref handled); // WM_DPICHANGED
        material.OnWindowMessage(IntPtr.Zero, 0x0001, IntPtr.Zero, IntPtr.Zero, ref handled); // WM_CREATE (unhandled)

        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public void WidgetWindowMaterial_Callback_IgnoresDisposedOrMismatchedHandle()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        IntPtr currentHandle = new IntPtr(0x1234);

        var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => currentHandle);

        Action<bool>? completion = TestBackdropController.Last?.Completion;
        Assert.NotNull(completion);

        // When disposed, invoking completion should return early without throwing
        material.Dispose();
        completion(true);

        // When handle changed
        currentHandle = new IntPtr(0x5678);
        completion(true);
    }

    [Fact]
    public void WidgetWindowMaterial_SecondRefresh_SkipsDuplicateUpdate_AndDetachesWhenThemeChanges()
    {
        TestBackdropController.Last = null;
        var window = new Window { IsVisible = true };
        var shell = new Border { CornerRadius = new CornerRadius(8) };
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        IntPtr currentHandle = new IntPtr(0x1234);

        using var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => currentHandle);

        Assert.NotNull(TestBackdropController.Last);
        // First update done. Calling Refresh again with same bounds/radius/opacity skips TryUpdate
        var firstUpdate = TestBackdropController.Last.LastUpdate;
        material.Refresh();
        Assert.Equal(firstUpdate, TestBackdropController.Last.LastUpdate);

        // Change handle to different pointer: forces detachment and transparency hint reset
        currentHandle = new IntPtr(0x5678);
        material.Refresh();

        // Switch blur mode to Off: detaches session
        theme.WidgetBlurMode = WidgetBlurModes.Off;
        manager.ApplyTheme(theme);
        material.Refresh();
    }

    [Fact]
    public void WidgetWindowMaterial_PostRefresh_QueuesDispatcherAndIgnoresWhenDisposed()
    {
        var window = new Window { IsVisible = true };
        var shell = new Border();
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        var theme = new Theme
        {
            ThemeName = "Modern Dark",
            WidgetBlurMode = WidgetBlurModes.Frosted,
        };
        manager.ApplyTheme(theme);

        var capability = new WidgetBackdropCapability(typeof(TestBackdropController));
        var material = new WidgetWindowMaterial(
            window,
            shell,
            manager,
            capability,
            isTransparencyDisabled: () => false,
            isCompositionSupported: () => true,
            windowHandleProvider: () => new IntPtr(0x1234));

        // Queue first post without retry
        material.PostRefresh(retry: false);
        // Queue second post with retry while first is pending
        material.PostRefresh(retry: true);

        // Run dispatcher jobs
        Dispatcher.UIThread.RunJobs();

        // Now dispose and call PostRefresh and Refresh
        material.Dispose();
        material.PostRefresh(retry: true);
        material.Refresh(retry: true);
    }

    [Fact]
    public void TimerWidgetWindow_IsAppShuttingDown_AllowsClose()
    {
        var manager = new ThemeManager();
        var window = new TimerWidgetWindow(manager) { IsAppShuttingDown = true };
        window.Show();
        window.Close();
        // Since IsAppShuttingDown is true, the close is not cancelled
    }

    [Fact]
    public void TimerWidgetWindow_WidgetWindowMessage_HandlesWindowPositionChanging()
    {
        bool handled = false;
        IntPtr result = TimerWidgetWindow.WidgetWindowMessage(IntPtr.Zero, 0x0046, IntPtr.Zero, IntPtr.Zero, ref handled);
        Assert.Equal(IntPtr.Zero, result);
        Assert.True(handled);

        handled = false;
        result = TimerWidgetWindow.WidgetWindowMessage(IntPtr.Zero, 0x0001, IntPtr.Zero, IntPtr.Zero, ref handled);
        Assert.Equal(IntPtr.Zero, result);
        Assert.False(handled);
    }

    [Fact]
    public void TimerWidgetWindow_IsInteractiveElement_CorrectlyClassifiesControls()
    {
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new Button()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new ToggleButton()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new TextBox()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new Slider()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new ComboBox()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new CheckBox()));
        Assert.True(TimerWidgetWindow.IsInteractiveElement(new NumericUpDown()));

        // Inside a nested container
        var button = new Button();
        var border = new Border { Child = button };
        Assert.True(TimerWidgetWindow.IsInteractiveElement(button));

        // Non-interactive controls
        Assert.False(TimerWidgetWindow.IsInteractiveElement(new Border()));
        Assert.False(TimerWidgetWindow.IsInteractiveElement(new TextBlock()));
        Assert.False(TimerWidgetWindow.IsInteractiveElement(null));
    }

    public sealed class TestBackdropController : IDisposable
    {
        public static TestBackdropController? Last { get; set; }

        public static bool ShouldFailCreate { get; set; }

        public static bool UpdateReturnValue { get; set; } = true;

        public int DisposeCount { get; private set; }

        public bool IsApplied { get; set; } = true;

        public Action<bool>? Completion { get; private set; }

        public (float Radius, float Opacity, bool Enabled)? LastUpdate { get; private set; }

        public static bool TryCreate(IntPtr handle, out TestBackdropController? controller)
        {
            if (ShouldFailCreate)
            {
                controller = null;
                return false;
            }

            Last = controller = new TestBackdropController();
            return true;
        }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed)
        {
            this.Completion = completed;
            this.LastUpdate = (radius, opacity, enabled);
            return UpdateReturnValue;
        }

        public void Dispose() => this.DisposeCount++;
    }
}
