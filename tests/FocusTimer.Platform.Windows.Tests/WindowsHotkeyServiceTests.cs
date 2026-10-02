namespace FocusTimer.Platform.Windows.Tests;

using System.Runtime.InteropServices;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public partial class WindowsHotkeyServiceTests
{
    [Fact]
    public void RegisterHotkey_GivenNoWindowHandle_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);
        var raised = false;
        service.HotkeyPressed += (_, _) => raised = true;

        var ex = Record.Exception(() => service.RegisterHotkey("Ctrl+Alt+T", () => { }));

        Assert.Null(ex);
        Assert.False(raised);
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void Register_GivenNoWindowHandle_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);

        var ex = Record.Exception(() =>
            service.Register(new HotkeyDefinition(HotkeyModifiers.Control, 'K')));

        Assert.Null(ex);
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void SetWindowHandle_GivenZero_IsIgnoredAndLeavesServiceUsable()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);

        var ex = Record.Exception(() =>
        {
            service.SetWindowHandle(IntPtr.Zero);
            service.RegisterHotkey("Ctrl+Alt+T", () => { });
        });

        Assert.Null(ex);

        // Falling back to "no handle" behavior confirms SetWindowHandle(Zero) never took effect.
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void UnregisterAll_GivenNoWindowHandle_IsNoOp()
    {
        using var service = new WindowsHotkeyService();

        var ex = Record.Exception(service.UnregisterAll);

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_GivenNoWindowHandleEverSet_DoesNotThrowEvenWhenCalledTwice()
    {
        var service = new WindowsHotkeyService();

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void ProcessHotkeyMessage_GivenUnknownId_DoesNotRaiseEvent()
    {
        using var service = new WindowsHotkeyService();
        var raised = false;
        service.HotkeyPressed += (_, _) => raised = true;

        service.ProcessHotkeyMessage(int.MaxValue);

        Assert.False(raised);
    }

    // Regression coverage for the GetWindowLongPtr/SetWindowLongPtr EntryPointNotFoundException
    // bug: [LibraryImport] requires the exact export name ("...PtrW"), unlike classic [DllImport]
    // which auto-probes the A/W suffix. These tests exercise the real Win32 subclassing path
    // against an actual (message-only, invisible) window rather than a fake handle, so a
    // regression would surface as a thrown exception here instead of a silently-swallowed one
    // in production (see TimerWidgetWindow.axaml.cs's try/catch around SetWindowHandle).
    [Fact]
    public void SetWindowHandle_GivenRealWindow_HooksWndProcWithoutThrowing()
    {
        using var window = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();

        var ex = Record.Exception(() => service.SetWindowHandle(window.Handle));

        Assert.Null(ex);
        Assert.NotEqual(window.OriginalWndProc, window.GetCurrentWndProc());
    }

    [Fact]
    public void SetWindowHandle_GivenOrdinaryWindowMessage_ForwardsToOriginalWndProc()
    {
        using var window = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();
        service.SetWindowHandle(window.Handle);

        // WM_NULL passes through SubclassWndProc to CallWindowProcW.
        var ex = Record.Exception(() => window.SendNullMessage());

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_GivenRealWindowWasHooked_RestoresOriginalWndProc()
    {
        using var window = new NativeMessageWindow();
        var service = new WindowsHotkeyService();
        service.SetWindowHandle(window.Handle);

        var ex = Record.Exception(service.Dispose);

        Assert.Null(ex);
        Assert.Equal(window.OriginalWndProc, window.GetCurrentWndProc());
    }

    [Fact]
    public void Dispose_GivenRealWindowHooked_DoesNotThrowWhenCalledTwice()
    {
        using var window = new NativeMessageWindow();
        var service = new WindowsHotkeyService();
        service.SetWindowHandle(window.Handle);

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void SetWindowHandle_GivenSameRealHandleTwice_IsNoOp()
    {
        using var window = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();

        var ex = Record.Exception(() =>
        {
            service.SetWindowHandle(window.Handle);
            service.SetWindowHandle(window.Handle);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void SetWindowHandle_GivenDifferentRealHandle_UnhooksFirstWindowAndHooksSecond()
    {
        using var firstWindow = new NativeMessageWindow();
        using var secondWindow = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();

        var ex = Record.Exception(() =>
        {
            service.SetWindowHandle(firstWindow.Handle);
            service.SetWindowHandle(secondWindow.Handle);
        });

        Assert.Null(ex);
        Assert.Equal(firstWindow.OriginalWndProc, firstWindow.GetCurrentWndProc());
        Assert.NotEqual(secondWindow.OriginalWndProc, secondWindow.GetCurrentWndProc());
    }

    [Fact]
    public void RegisterHotkey_GivenMalformedDefinitionWithRealWindowSet_LogsWarningAndDoesNotThrow()
    {
        using var window = new NativeMessageWindow();
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);
        service.SetWindowHandle(window.Handle);

        var ex = Record.Exception(() => service.RegisterHotkey("NotAValidHotkey", () => { }));

        Assert.Null(ex);
        Assert.Contains(logger.Warnings, w => w.Contains("Invalid hotkey definition"));
    }

    [Theory]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift)]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Win)]
    public void RegisteredHotkey_NativeMessageRaisesDefinitionAndUnregisterSuppressesIt(HotkeyModifiers modifiers)
    {
        using var window = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();
        var definition = new HotkeyDefinition(modifiers, 0x87); // F24 avoids ordinary typing shortcuts.
        var received = new List<HotkeyDefinition>();
        service.HotkeyPressed += (_, args) => received.Add(args.Definition);
        service.SetWindowHandle(window.Handle);
        service.Register(definition);

        window.SendHotkeyMessage(999);
        Assert.Empty(received);
        window.SendHotkeyMessage(1);
        var actual = Assert.Single(received);
        Assert.Equal(definition.KeyCode, actual.KeyCode);
        Assert.Equal(definition.Modifiers, actual.Modifiers);

        service.UnregisterAll();
        window.SendHotkeyMessage(1);
        Assert.Single(received);

        // A new registration must work after cleanup and reuse the first ID.
        service.Register(definition);
        window.SendHotkeyMessage(1);
        Assert.Equal(2, received.Count);
    }

    [Fact]
    public void DuplicateNativeRegistration_LogsConflictWithoutDispatchingTheFailedDefinition()
    {
        using var firstWindow = new NativeMessageWindow();
        using var secondWindow = new NativeMessageWindow();
        using var first = new WindowsHotkeyService();
        var logger = new RecordingLogger();
        using var second = new WindowsHotkeyService(logger);
        var definition = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x87);
        int firstEvents = 0;
        int secondEvents = 0;
        first.HotkeyPressed += (_, _) => firstEvents++;
        second.HotkeyPressed += (_, _) => secondEvents++;
        first.SetWindowHandle(firstWindow.Handle);
        second.SetWindowHandle(secondWindow.Handle);
        first.Register(definition);
        second.Register(definition);

        firstWindow.SendHotkeyMessage(1);
        secondWindow.SendHotkeyMessage(1);

        Assert.Equal(1, firstEvents);
        Assert.Equal(0, secondEvents);
        Assert.Contains(logger.Warnings, message => message.Contains("Failed to register hotkey"));
    }

    [Fact]
    public void SwitchingWindow_DropsOldRegistrationsAndDeliversOnlyNewMessages()
    {
        using var firstWindow = new NativeMessageWindow();
        using var secondWindow = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();
        var definition = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x87);
        int events = 0;
        service.HotkeyPressed += (_, _) => events++;
        service.SetWindowHandle(firstWindow.Handle);
        service.Register(definition);
        firstWindow.SendHotkeyMessage(1);

        service.SetWindowHandle(secondWindow.Handle);
        firstWindow.SendHotkeyMessage(1);
        secondWindow.SendHotkeyMessage(1);
        Assert.Equal(1, events);
        Assert.Equal(firstWindow.OriginalWndProc, firstWindow.GetCurrentWndProc());

        service.Register(definition);
        secondWindow.SendHotkeyMessage(1);
        Assert.Equal(2, events);
        service.Dispose();
        Assert.Equal(secondWindow.OriginalWndProc, secondWindow.GetCurrentWndProc());
    }

    [Fact]
    public void RegisteredHotkey_WithoutSubscribers_CanProcessAndReleaseItsRegistration()
    {
        using var window = new NativeMessageWindow();
        using var service = new WindowsHotkeyService();
        service.SetWindowHandle(window.Handle);
        service.RegisterHotkey("Ctrl+Alt+Shift+F24", () => { });

        window.SendHotkeyMessage(1);
        service.UnregisterAll();

        // Verify release using a second real service rather than inspecting private dictionaries.
        var logger = new RecordingLogger();
        using var replacement = new WindowsHotkeyService(logger);
        replacement.SetWindowHandle(window.Handle);
        replacement.Register(new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x87));
        int events = 0;
        replacement.HotkeyPressed += (_, _) => events++;
        replacement.ProcessHotkeyMessage(1);
        Assert.Equal(1, events);
        Assert.Empty(logger.Warnings);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Warnings { get; } = new();

        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) => this.Warnings.Add(message);
    }

    /// <summary>
    /// A real, invisible, message-only native window used to exercise WindowsHotkeyService's
    /// WndProc subclassing against genuine Win32 window-handle APIs.
    /// </summary>
    private sealed partial class NativeMessageWindow : IDisposable
    {
        private const int GwlWndProc = -4;
        private static readonly IntPtr HwndMessage = new(-3);

        public NativeMessageWindow()
        {
            this.Handle = CreateWindowExW(0, "Message", null, 0, 0, 0, 0, 0, HwndMessage, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (this.Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to create a message-only test window. Win32 error {Marshal.GetLastWin32Error()}.");
            }

            this.OriginalWndProc = this.GetCurrentWndProc();
        }

        public IntPtr Handle { get; }

        public IntPtr OriginalWndProc { get; }

        public IntPtr GetCurrentWndProc() => GetWindowLongPtrW(this.Handle, GwlWndProc);

        public void SendNullMessage() => SendMessageW(this.Handle, 0, IntPtr.Zero, IntPtr.Zero);

        public void SendHotkeyMessage(int id) => SendMessageW(this.Handle, 0x0312, new IntPtr(id), IntPtr.Zero);

        public void Dispose() => DestroyWindow(this.Handle);

        [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial IntPtr CreateWindowExW(
            uint dwExStyle,
            string lpClassName,
            string? lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static partial IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DestroyWindow(IntPtr hWnd);
    }
}
