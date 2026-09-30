#pragma warning disable
namespace FocusTimer.Persistence.Tests;

using System.Text.Json;
using FocusTimer.Core.Models;

public class ActivityPollingSettingsTests
{
    [Theory]
    [InlineData("null", 10)]
    [InlineData("\"5\"", 10)]
    [InlineData("1.5", 10)]
    [InlineData("0", 10)]
    [InlineData("-1", 10)]
    [InlineData("61", 10)]
    [InlineData("{}", 10)]
    [InlineData("[]", 10)]
    [InlineData("true", 10)]
    [InlineData("1", 1)]
    [InlineData("10", 10)]
    [InlineData("60", 60)]
    public async Task InvalidIntervalDoesNotDiscardOtherSettings(string jsonValue, int expected)
    {
        var path = Path.Combine(Path.GetTempPath(), $"polling-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"activityPollingIntervalSeconds\":" + jsonValue + ",\"breakIntervalMinutes\":37}");
            var provider = new JsonSettingsProvider(path);
            var settings = await provider.LoadAsync();
            Assert.Equal(expected, settings.ActivityPollingIntervalSeconds);
            Assert.Equal(37, settings.BreakIntervalMinutes);
            await provider.SaveAsync(settings);
            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            Assert.Equal(expected, saved.RootElement.GetProperty("activityPollingIntervalSeconds").GetInt32());
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(61, 10)]
    [InlineData(1, 1)]
    [InlineData(60, 60)]
    public void ModelNormalizesInterval(int input, int expected)
    {
        Assert.Equal(10, new Settings().ActivityPollingIntervalSeconds);
        Assert.Equal(10, JsonSerializer.Deserialize<Settings>("{}")!.ActivityPollingIntervalSeconds);
        Assert.Equal(expected, new Settings { ActivityPollingIntervalSeconds = input }.ActivityPollingIntervalSeconds);
    }
}
