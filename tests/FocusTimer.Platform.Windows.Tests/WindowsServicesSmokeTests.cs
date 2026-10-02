namespace FocusTimer.Platform.Windows.Tests;

using FocusTimer.Core.Models;
using Microsoft.Win32;

public class WindowsServicesSmokeTests
{
    [Fact]
    public void AutoStartService_SetAndQuery_DoNotThrow()
    {
        string keyPath = $@"Software\FocusTimerTests\{Guid.NewGuid():N}";
        Registry.CurrentUser.CreateSubKey(keyPath)?.Dispose();
        try
        {
            var service = new WindowsAutoStartService(keyPath, null);
            service.SetAutoStart(enabled: false);
            Assert.False(service.IsAutoStartEnabled());
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public async Task ActiveWindowService_GetForegroundWindow_ReturnsNullOrStructuredInfo()
    {
        var service = new WindowsActiveWindowService();

        var info = await service.GetForegroundWindowAsync();

        if (info is null)
        {
            Assert.Null(info);
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(info.ProcessName) && string.IsNullOrWhiteSpace(info.WindowTitle));
    }

    [Fact]
    public async Task NotificationService_ShowMethods_DoNotThrowWithoutAvaloniaApp()
    {
        var service = new WindowsNotificationService();

        var ex = await Record.ExceptionAsync(async () =>
        {
            await service.ShowNotificationAsync("Title", "Message");
            await service.ShowBreakReminderAsync("Break", requireAcknowledgement: false);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void HotkeyService_GivenNoWindowHandle_OperationsDoNotThrowAndDoNotRaiseEvent()
    {
        using var service = new WindowsHotkeyService();
        var calls = 0;
        service.HotkeyPressed += (_, _) => calls++;

        var ex = Record.Exception(() =>
        {
            service.SetWindowHandle(IntPtr.Zero);
            service.Register(new HotkeyDefinition { Modifiers = HotkeyModifiers.Control, KeyCode = (int)'K' });
            service.ProcessHotkeyMessage(999);
            service.UnregisterAll();
        });

        Assert.Null(ex);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void IdleDetectionService_DisposeTwice_DoesNotThrow()
    {
        var service = new WindowsIdleDetectionService();

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }
}
