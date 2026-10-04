#pragma warning disable
namespace FocusTimer.Core.Tests;

using System.Text.Json;
using FocusTimer.Core.Models;

public class RuleListConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void WindowRulesRoundTripInOrderAndWriteExplicitNulls()
    {
        var settings = new Settings();
        settings.ExclusionRules = new() { new("keepass", null), new(null, "*bank*") };
        settings.SegmentationRules = new() { new("chrome", "*") };
        var json = JsonSerializer.Serialize(settings, Options);
        Assert.Contains("\"titlePattern\":null", json);
        var back = JsonSerializer.Deserialize<Settings>(json, Options)!;
        Assert.Equal(settings.ExclusionRules, back.ExclusionRules);
        Assert.Equal(settings.SegmentationRules, back.SegmentationRules);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"text\"")]
    [InlineData("5")]
    [InlineData("{}")]
    [InlineData("true")]
    public void NonArrayWindowRuleValuesBecomeAnEmptyList(string value)
    {
        var s = JsonSerializer.Deserialize<Settings>("{\"exclusionRules\":" + value + ",\"segmentationRules\":" + value + "}", Options)!;
        Assert.Empty(s.ExclusionRules);
        Assert.Empty(s.SegmentationRules);
    }

    [Fact]
    public void MalformedWindowRulesAreDroppedAndPropertyNamesAreCaseInsensitive()
    {
        var json = "{\"exclusionRules\":[{\"APPPATTERN\":\"a\"},5,null,[],{},{\"appPattern\":7},{\"appPattern\":\"  \",\"titlePattern\":\"\"},{\"titlePattern\":\"t\"}]}";
        var s = JsonSerializer.Deserialize<Settings>(json, Options)!;
        Assert.Equal(new[] { new WindowMatchRule("a", null), new WindowMatchRule(null, "t") }, s.ExclusionRules);
    }

    [Fact]
    public void ProjectRulesRoundTripAndTrimNames()
    {
        var settings = new Settings { ProjectRules = new() { new("code", null, "Alpha"), new(null, "*repo*", "Beta") } };
        var json = JsonSerializer.Serialize(settings, Options);
        Assert.Equal(settings.ProjectRules, JsonSerializer.Deserialize<Settings>(json, Options)!.ProjectRules);
        var trimmed = JsonSerializer.Deserialize<Settings>("{\"projectRules\":[{\"appPattern\":\"x\",\"projectName\":\"  Name \"}]}", Options)!;
        Assert.Equal("Name", Assert.Single(trimmed.ProjectRules).ProjectName);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"text\"")]
    [InlineData("{}")]
    [InlineData("1")]
    public void NonArrayProjectRuleValuesBecomeAnEmptyList(string value) =>
        Assert.Empty(JsonSerializer.Deserialize<Settings>("{\"projectRules\":" + value + "}", Options)!.ProjectRules);

    [Fact]
    public void MalformedProjectRulesAreDropped()
    {
        var json = "{\"projectRules\":[{\"appPattern\":\"a\"},{\"projectName\":\"P\"},{\"appPattern\":\"a\",\"projectName\":\"\"}," +
            "7,null,[],{\"appPattern\":1,\"projectName\":\"P\"},{\"APPPATTERN\":\"ok\",\"PROJECTNAME\":\"Good\"}]}";
        var rules = JsonSerializer.Deserialize<Settings>(json, Options)!.ProjectRules;
        Assert.Equal(new[] { new ProjectRule("ok", null, "Good") }, rules);
    }

    [Fact]
    public void RuleModelsReportValidityAndMatching()
    {
        Assert.False(new ProjectRule("a", null, null).IsValid);
        Assert.False(new ProjectRule(null, null, "P").IsValid);
        Assert.True(new ProjectRule(null, "t*", "P").IsValid);
        Assert.False(new ProjectRule(null, null, "P").Matches(new ActiveWindowInfo { ProcessName = "x" }));
        Assert.True(new ProjectRule("x", null, "P").Matches(new ActiveWindowInfo { ProcessName = "x" }));
        Assert.False(new ProjectRule("x", null, "P").Matches(null));
        Assert.Equal(new WindowMatchRule("a", "b"), new ProjectRule("a", "b", "P").WindowRule);
    }

    [Fact]
    public void SettingsRuleListsNormalizeNullAssignments()
    {
        var s = new Settings { ExclusionRules = null!, SegmentationRules = null!, ProjectRules = null! };
        Assert.Empty(s.ExclusionRules);
        Assert.Empty(s.SegmentationRules);
        Assert.Empty(s.ProjectRules);
    }
}
