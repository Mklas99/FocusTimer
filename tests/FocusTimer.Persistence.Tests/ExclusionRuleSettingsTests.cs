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

    [Fact]
    public async Task SegmentationRulesRoundTripMissingAndMalformed()
    {
        var path = Temp();
        try
        {
            await File.WriteAllTextAsync(path, "{\"breakIntervalMinutes\":37}");
            Assert.Empty((await new JsonSettingsProvider(path).LoadAsync()).SegmentationRules);
            await File.WriteAllTextAsync(path,
                "{\"segmentationRules\":[{\"appPattern\":\"chrome\"},7,{}],\"exclusionRules\":[{\"appPattern\":\"x\"}],\"breakIntervalMinutes\":37}");
            var logger = new CapturingLogger();
            var provider = new JsonSettingsProvider(path, logger);
            var loaded = await provider.LoadAsync();
            Assert.Equal(new[] { new WindowMatchRule("chrome", null) }, loaded.SegmentationRules);
            Assert.Equal(new[] { new WindowMatchRule("x", null) }, loaded.ExclusionRules);
            Assert.Equal(37, loaded.BreakIntervalMinutes);
            Assert.Contains(logger.Warnings, w => w.Contains("segmentation"));
            await provider.SaveAsync(loaded);
            Assert.Equal(loaded.SegmentationRules, (await new JsonSettingsProvider(path).LoadAsync()).SegmentationRules);
            var clone = loaded.Clone();
            clone.SegmentationRules.Add(new WindowMatchRule("b", null));
            Assert.Single(loaded.SegmentationRules);
        }
        finally { File.Delete(path); }
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

public class ProjectRuleSettingsTests
{
    [Fact]
    public async Task ProjectRulesRoundTripMissingAndMalformed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"projectrules-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"breakIntervalMinutes\":37}");
            Assert.Empty((await new JsonSettingsProvider(path).LoadAsync()).ProjectRules);
            await File.WriteAllTextAsync(path,
                "{\"projectRules\":[{\"appPattern\":\"code\",\"projectName\":\" Alpha \"},{\"appPattern\":\"x\"}," +
                "{\"projectName\":\"NoPattern\"},{\"appPattern\":\"y\",\"projectName\":\"  \"},5,{\"titlePattern\":\"*t*\",\"projectName\":\"B\"}]," +
                "\"breakIntervalMinutes\":37}");
            var logger = new Logger();
            var provider = new JsonSettingsProvider(path, logger);
            var loaded = await provider.LoadAsync();
            Assert.Equal(new[] { new ProjectRule("code", null, "Alpha"), new ProjectRule(null, "*t*", "B") }, loaded.ProjectRules);
            Assert.Equal(37, loaded.BreakIntervalMinutes);
            Assert.Contains(logger.Warnings, w => w.Contains("4 malformed project rule"));
            await provider.SaveAsync(loaded);
            Assert.Equal(loaded.ProjectRules, (await new JsonSettingsProvider(path).LoadAsync()).ProjectRules);
            var clone = loaded.Clone();
            clone.ProjectRules.Add(new ProjectRule("z", null, "Z"));
            Assert.Equal(2, loaded.ProjectRules.Count);
        }
        finally { File.Delete(path); }
    }

    private sealed class Logger : FocusTimer.Core.Interfaces.IAppLogger
    {
        public List<string> Warnings = new();
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) => Warnings.Add(message);
    }
}
