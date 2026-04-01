namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;

public class CoreModelsTests
{
    [Fact]
    public void TimeEntry_GetCurrentDuration_GivenOpenEntry_UsesProvidedNow()
    {
        var start = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc);
        var entry = new TimeEntry { StartTime = start };

        var duration = entry.GetCurrentDuration(start.AddMinutes(15));

        Assert.Equal(TimeSpan.FromMinutes(15), duration);
    }

    [Fact]
    public void TimeEntry_Duration_GivenClosedEntry_ReturnsDifference()
    {
        var start = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc);
        var entry = new TimeEntry
        {
            StartTime = start,
            EndTime = start.AddMinutes(5),
        };

        Assert.Equal(TimeSpan.FromMinutes(5), entry.Duration);
    }

    [Theory]
    [InlineData(10.0, 3.0)]
    [InlineData(0.1, 0.5)]
    [InlineData(2.25, 2.25)]
    public void Settings_WidgetScale_GivenOutOfRange_ClampsValue(double input, double expected)
    {
        var settings = new Settings { WidgetScale = input };

        Assert.Equal(expected, settings.WidgetScale);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    [InlineData(0.6, 0.6)]
    public void Settings_WidgetOpacity_GivenOutOfRange_ClampsValue(double input, double expected)
    {
        var settings = new Settings { WidgetOpacity = input };

        Assert.Equal(expected, settings.WidgetOpacity);
    }

    [Fact]
    public void Settings_PropertyChanged_GivenNewValue_RaisesEvent()
    {
        var settings = new Settings();
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        settings.ActiveThemeName = "Light";

        Assert.Contains(nameof(Settings.ActiveThemeName), changed);
    }

    [Fact]
    public void Settings_PropertyChanged_GivenSameValue_DoesNotRaiseEvent()
    {
        var settings = new Settings();
        var changed = 0;
        settings.PropertyChanged += (_, _) => changed++;

        var existing = settings.ActiveThemeName;
        settings.ActiveThemeName = existing;

        Assert.Equal(0, changed);
    }

    [Fact]
    public void HotkeyDefinition_ParseAndToString_GivenValidValue_RoundTrips()
    {
        var parsed = HotkeyDefinition.Parse("Ctrl+Alt+k");

        Assert.True(parsed.HasValue);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, parsed.Value.Modifiers);
        Assert.Equal('K', parsed.Value.KeyCode);
        Assert.Equal("Ctrl+Alt+K", parsed.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+AB")]
    public void HotkeyDefinition_Parse_GivenInvalidValues_ReturnsNull(string? input)
    {
        Assert.Null(HotkeyDefinition.Parse(input));
    }

    [Theory]
    [InlineData("Shift+z", HotkeyModifiers.Shift)]
    [InlineData("Win+x", HotkeyModifiers.Win)]
    [InlineData("Control+y", HotkeyModifiers.Control)]
    public void HotkeyDefinition_Parse_GivenSupportedModifierAliases_ParsesExpectedModifiers(string input, HotkeyModifiers expected)
    {
        var parsed = HotkeyDefinition.Parse(input);

        Assert.True(parsed.HasValue);
        Assert.Equal(expected, parsed.Value.Modifiers);
    }
}
