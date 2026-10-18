namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;
using FocusTimer.Core.Models;

public sealed class NotificationPolicyTests
{
    [Fact]
    public void PolicySnapshotsPreferencesAndNeverSuppressesErrors()
    {
        var settings = new Settings
        {
            InformationNotificationsEnabled = false,
            WarningNotificationsEnabled = false,
            InformationDismissalSeconds = 2,
            WarningDismissalSeconds = 8,
            ErrorDismissalSeconds = 120,
        };
        var policy = new NotificationPolicy(settings);
        settings.InformationNotificationsEnabled = true;
        settings.InformationDismissalSeconds = 9;
        Assert.False(policy.IsEnabled(NotificationSeverity.Information));
        Assert.False(policy.IsEnabled(NotificationSeverity.Warning));
        Assert.True(policy.IsEnabled(NotificationSeverity.Error));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDismissalDelay(NotificationSeverity.Information));
        Assert.Equal(TimeSpan.FromSeconds(8), policy.GetDismissalDelay(NotificationSeverity.Warning));
        Assert.Equal(TimeSpan.FromSeconds(120), policy.GetDismissalDelay(NotificationSeverity.Error));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(121)]
    public void InvalidStoredDurationsUseExistingDefaults(int invalid)
    {
        var policy = new NotificationPolicy(new Settings
        {
            InformationDismissalSeconds = invalid,
            WarningDismissalSeconds = invalid,
            ErrorDismissalSeconds = invalid,
        });
        Assert.Equal(TimeSpan.FromSeconds(5), policy.GetDismissalDelay(NotificationSeverity.Information));
        Assert.Equal(TimeSpan.FromSeconds(15), policy.GetDismissalDelay(NotificationSeverity.Warning));
        Assert.Equal(TimeSpan.FromSeconds(15), policy.GetDismissalDelay(NotificationSeverity.Error));
    }
}
