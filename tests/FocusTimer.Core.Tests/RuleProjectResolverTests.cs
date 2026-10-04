#pragma warning disable
namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class RuleProjectResolverTests
{
    private static TimeEntry E(string app, string title = "t", string? project = null,
        CaptureSource source = CaptureSource.ActiveWindow)
    {
        var t = new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
        return new TimeEntry("id", "s", t, t.AddMinutes(1), app, title, project,
            string.IsNullOrWhiteSpace(project) ? ProjectAssignmentSource.Unassigned : ProjectAssignmentSource.Session,
            null, ActivityKind.Active, EndReason.ApplicationChange, source, SourcePlatform.Windows, "d", 1, t);
    }

    private static RuleProjectResolver Resolver(params ProjectRule[] rules)
    {
        var store = new ProjectRuleStore();
        store.Update(rules);
        return new RuleProjectResolver(store);
    }

    [Fact]
    public void ExplicitProjectWinsOverMatchingRule() =>
        Assert.Equal("Mine", Resolver(new ProjectRule("code", null, "Other")).Resolve(E("code", project: "Mine")));

    [Fact]
    public void RuleLabelsUntaggedAutomaticEntry() =>
        Assert.Equal("Alpha", Resolver(new ProjectRule("code", null, " Alpha ")).Resolve(E("code.exe")));

    [Fact]
    public void FirstMatchingRuleWins() =>
        Assert.Equal("First", Resolver(new ProjectRule("zzz", null, "No"), new ProjectRule("co*", null, "First"), new ProjectRule("code", null, "Second"))
            .Resolve(E("code")));

    [Fact]
    public void NullAppOrTitleDoesNotThrowAndSimplyDoesNotMatchLiteralRules()
    {
        var resolver = Resolver(new ProjectRule("code", null, "A"), new ProjectRule(null, "*x*", "B"), new ProjectRule("*", null, "Any"));
        var t = new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
        var broken = new TimeEntry("id", "s", t, t.AddMinutes(1), null!, null!, null, ProjectAssignmentSource.Unassigned,
            null, ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow, SourcePlatform.Windows, "d", 1, t);
        Assert.Equal("Any", resolver.Resolve(broken));
        Assert.False(new WindowMatchRule("code", "*x*").Matches(new ActiveWindowInfo { ProcessName = null!, WindowTitle = null! }));
    }

    [Fact]
    public void NoMatchStaysUnassigned() => Assert.Null(Resolver(new ProjectRule("zzz", null, "No")).Resolve(E("code")));

    [Fact]
    public void ManualEntriesNeverMatch() =>
        Assert.Null(Resolver(new ProjectRule("*", null, "Any")).Resolve(E("code", source: CaptureSource.Manual)));

    [Fact]
    public void TitlePatternMatchesWindowTitle()
    {
        var r = Resolver(new ProjectRule(null, "*repo-x*", "RepoX"));
        Assert.Equal("RepoX", r.Resolve(E("code", "main.cs - repo-x")));
        Assert.Null(r.Resolve(E("code", "other")));
    }

    [Fact]
    public void RuleEditsApplyRetroactivelyAndRevertOnRemoval()
    {
        var store = new ProjectRuleStore();
        var resolver = new RuleProjectResolver(store);
        var entry = E("code");
        Assert.Null(resolver.Resolve(entry));
        store.Update(new[] { new ProjectRule("code", null, "Alpha") });
        Assert.Equal("Alpha", resolver.Resolve(entry));
        store.Update(Array.Empty<ProjectRule>());
        Assert.Null(resolver.Resolve(entry));
    }

    [Fact]
    public void InvalidRulesAreDroppedAndIdenticalUpdateRaisesNoEvent()
    {
        var store = new ProjectRuleStore(); var raised = 0;
        store.RulesChanged += (_, _) => raised++;
        store.Update(new[] { new ProjectRule("a", null, ""), new ProjectRule(null, null, "P"), new ProjectRule("a", null, "P") });
        Assert.Single(store.Rules);
        Assert.Equal(1, raised);
        store.Update(new[] { new ProjectRule("a", null, "P") });
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ProjectsGroupCaseInsensitivelyThroughTheGrouping()
    {
        var grouping = new ProjectGrouping();
        var r = Resolver(new ProjectRule("code", null, "ALPHA"));
        var a = grouping.Select(E("code"), new GroupingContext(r.Resolve(E("code"))));
        var b = grouping.Select(E("x", project: "alpha"), new GroupingContext(r.Resolve(E("x", project: "alpha"))));
        Assert.Equal(a.Key, b.Key);
    }
}
