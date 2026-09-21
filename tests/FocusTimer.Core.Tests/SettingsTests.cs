namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;

public class SettingsTests
{
    [Theory]
    [InlineData(-5.0, 0.5)]
    [InlineData(10.0, 3.0)]
    [InlineData(1.5, 1.5)]
    public void WidgetScale_GivenValue_ClampsToSupportedRange(double input, double expected)
    {
        var settings = new Settings { WidgetScale = input };

        Assert.Equal(expected, settings.WidgetScale);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    [InlineData(0.4, 0.4)]
    public void WidgetOpacity_GivenValue_ClampsToUnitRange(double input, double expected)
    {
        var settings = new Settings { WidgetOpacity = input };

        Assert.Equal(expected, settings.WidgetOpacity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void DeviceId_GivenBlankValue_GeneratesNewGuid(string? blank)
    {
        var settings = new Settings { DeviceId = blank! };

        Assert.True(Guid.TryParse(settings.DeviceId, out _));
    }

    [Fact]
    public void DeviceId_GivenValue_KeepsValue()
    {
        var settings = new Settings { DeviceId = "device-42" };

        Assert.Equal("device-42", settings.DeviceId);
    }

    [Fact]
    public void NewInstances_HaveDistinctDeviceIds()
    {
        Assert.NotEqual(new Settings().DeviceId, new Settings().DeviceId);
    }

    [Fact]
    public void PropertyChanged_GivenNewValue_RaisesEventWithPropertyName()
    {
        var settings = new Settings();
        var raised = new List<string?>();
        settings.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        settings.BreakIntervalMinutes = 25;
        settings.HotkeyShowHide = "Ctrl+Alt+H";
        settings.ActiveThemeName = "Light";

        Assert.Equal(
            new[] { nameof(Settings.BreakIntervalMinutes), nameof(Settings.HotkeyShowHide), nameof(Settings.ActiveThemeName) },
            raised);
    }

    [Fact]
    public void PropertyChanged_GivenSameValue_DoesNotRaiseEvent()
    {
        var settings = new Settings();
        var count = 0;
        settings.PropertyChanged += (_, _) => count++;

        settings.BreakIntervalMinutes = settings.BreakIntervalMinutes;
        settings.WidgetScale = 5.0; // clamps to 3.0 (changes once)
        settings.WidgetScale = 9.0; // clamps to 3.0 again (no change)

        Assert.Equal(1, count);
    }

    [Fact]
    public void Defaults_MatchDocumentedValues()
    {
        var settings = new Settings();

        Assert.True(settings.AlwaysOnTop);
        Assert.True(settings.BreakRemindersEnabled);
        Assert.True(settings.WorkLoggingEnabled);
        Assert.Equal(50, settings.BreakIntervalMinutes);
        Assert.Equal(90, settings.DataRetentionDays);
        Assert.Equal("Dark", settings.ActiveThemeName);
        Assert.Equal(Settings.DefaultApplicationLogDirectory, settings.LogDirectory);
        Assert.Equal(Settings.DefaultWorklogDirectory, settings.WorklogDirectory);
    }

    [Fact]
    public void DefaultDirectories_LiveUnderRootDirectory()
    {
        Assert.StartsWith(Settings.DefaultRootDirectory, Settings.DefaultApplicationLogDirectory);
        Assert.StartsWith(Settings.DefaultRootDirectory, Settings.DefaultWorklogDirectory);
    }
}
