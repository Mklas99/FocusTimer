#pragma warning disable
namespace FocusTimer.Persistence.Tests;

using System.Text.Json;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class ExclusionRuleSettingsTests
{
    [Fact]
    public async Task RulesRoundTripInOrder()
    {
        var path = Temp();
        try
        {
            var provider = new JsonSettingsProvider(path);
            var settings = await provider.LoadAsync();
            settings.ExclusionRules = new List<WindowMatchRule>
            {
                new("keepass", null), new(null, "*bank*"), new("chrome", "*private*"),
            };
            await provider.SaveAsync(settings);
            var loaded = await new JsonSettingsProvider(path).LoadAsync();
            Assert.Equal(settings.ExclusionRules, loaded.ExclusionRules);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task MissingListMeansNothingExcluded()
    {
        var path = Temp();
        try
        {
            await File.WriteAllTextAsync(path, "{\"breakIntervalMinutes\":37}");
            var loaded = await new JsonSettingsProvider(path).LoadAsync();
            Assert.Empty(loaded.ExclusionRules);
            Assert.Equal(37, loaded.BreakIntervalMinutes);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"x\"")]
    [InlineData("{}")]
    [InlineData("5")]
    public async Task NonArrayValueIsIgnoredWithoutDiscardingOtherSettings(string value)
    {
        var path = Temp();
        try
        {
            await File.WriteAllTextAsync(path, "{\"exclusionRules\":" + value + ",\"breakIntervalMinutes\":37}");
            var loaded = await new JsonSettingsProvider(path).LoadAsync();
            Assert.Empty(loaded.ExclusionRules);
            Assert.Equal(37, loaded.BreakIntervalMinutes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task MalformedRulesAreDroppedWithWarningAndOthersKept()
    {
        var path = Temp();
        try
        {
            await File.WriteAllTextAsync(path,
                "{\"exclusionRules\":[{\"appPattern\":\"a\"},5,{},{\"appPattern\":\" \",\"titlePattern\":\"\"}," +
                "{\"appPattern\":3,\"titlePattern\":null},{\"titlePattern\":\"t\"}],\"breakIntervalMinutes\":37}");
            var logger = new CapturingLogger();
            var loaded = await new JsonSettingsProvider(path, logger).LoadAsync();
            Assert.Equal(new[] { new WindowMatchRule("a", null), new WindowMatchRule(null, "t") }, loaded.ExclusionRules);
            Assert.Equal(37, loaded.BreakIntervalMinutes);
            Assert.Contains(logger.Warnings, w => w.Contains("4 malformed exclusion rule"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CloneCopiesTheList()
    {
        var settings = new Settings { ExclusionRules = new List<WindowMatchRule> { new("a", null) } };
        var clone = settings.Clone();
        clone.ExclusionRules.Add(new WindowMatchRule("b", null));
        Assert.Single(settings.ExclusionRules);
        Assert.Equal(2, clone.ExclusionRules.Count);
        Assert.Empty(new Settings().ExclusionRules);
    }

    private static string Temp() => Path.Combine(Path.GetTempPath(), $"exclusions-{Guid.NewGuid():N}.json");

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Warnings = new();
        public void LogDebug(string message) { }
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) => Warnings.Add(message);
        public void LogError(string message, Exception? ex = null) { }
    }
}
