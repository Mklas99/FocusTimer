namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Models;

public sealed class CustomizationSettingsTests
{
    [Fact]
    public async Task OldSettingsKeepExistingBehaviorAndNewPreferencesRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), $"customization-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"breakIntervalMinutes\":37}");
            var provider = new JsonSettingsProvider(path);
            Settings settings = await provider.LoadAsync();
            Assert.False(settings.WidgetButtonsBelowClock);
            Assert.True(settings.InformationNotificationsEnabled);
            Assert.True(settings.WarningNotificationsEnabled);
            Assert.Equal(5, settings.InformationDismissalSeconds);
            Assert.Equal(15, settings.WarningDismissalSeconds);
            Assert.Equal(15, settings.ErrorDismissalSeconds);
            settings.WidgetButtonsBelowClock = true;
            settings.InformationNotificationsEnabled = false;
            settings.WarningNotificationsEnabled = false;
            settings.InformationDismissalSeconds = 1;
            settings.WarningDismissalSeconds = 120;
            settings.ErrorDismissalSeconds = 37;
            settings.DeveloperModeEnabled = true;
            await provider.SaveAsync(settings.Clone());
            Settings restored = await new JsonSettingsProvider(path).LoadAsync();
            Assert.True(restored.WidgetButtonsBelowClock);
            Assert.False(restored.InformationNotificationsEnabled);
            Assert.False(restored.WarningNotificationsEnabled);
            Assert.Equal(1, restored.InformationDismissalSeconds);
            Assert.Equal(120, restored.WarningDismissalSeconds);
            Assert.Equal(37, restored.ErrorDismissalSeconds);
            Assert.True(restored.DeveloperModeEnabled);
            Assert.Equal(37, restored.BreakIntervalMinutes);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
